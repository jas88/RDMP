// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using Rdmp.Core.Caching.Pipeline.Sources;
using Rdmp.Core.Caching.Requests;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad.Modules.DataProvider;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStore.SciStoreServices81;
using SCIStorePlugin.Data;
using SCIStorePlugin.DataProvider.RetryStrategies;
using SCIStorePlugin.Repositories;

namespace SCIStorePlugin.Cache.Pipeline;

/// <summary>
/// Pipeline source that fetches laboratory investigation reports from the SCIStore SOAP web service for a specified healthboard and discipline over a date range, with retry logic and download request tracking.
/// </summary>
public class SCIStoreWebServiceSource : CacheSource<SCIStoreCacheChunk>
{
    private const bool VERBOSE = false;

    [DemandsInitialization("The target of the web service")]
    public WebServiceConfiguration Configuration { get; set; }

    [DemandsInitialization("The healthboard you are trying to load - do not change this mid lifespan of your corporation - set once EVER")]
    public HealthBoard HealthBoard { get; set; }

    [DemandsInitialization("The type of lab to be retrieved")]
    public Discipline Discipline { get; set; }

    [DemandsInitialization("The type of lab to be retrieved",DemandType.Unspecified,10)]
    public int NumberOfTimesToRetry { get; set; }

    [DemandsInitialization("Comma seperated list of integers that are the number of seconds to wait if the endpoint does not respond.  We can wait this number of seconds before trying again. Note that if NumberOfTimesToRetry is longer than this string then the last number will be used for all final wait times e.g. 5 tries 3,10,60 means waiting 3s then 10s then 60s then 60s then 60s then bombing",
        DemandType.Unspecified,"3,10,30,60,120,300")]
    public string NumberOfSecondsToWaitBetweenRetries { get; set; }

    [DemandsInitialization("Determines the behaviour of the source if it is unable to read after NumberOfTimesToRetry attempts.  True will audit it as a CacheFetchFailure leaving a hole in your data (which you can attempt to manually rerun later if you like) while False will bomb the process instead.")]
    public bool AuditFailureAndMoveOn { get; set; }

    private IRetryStrategy _retryStrategy;

    // made public so the type of Downloader can be mocked/stubbed for testing purpose
    public IRepositorySupportsDateRangeQueries<CombinedReportData> Downloader { get; set; }

    public override SCIStoreCacheChunk DoGetChunk(ICacheFetchRequest request, IDataLoadEventListener listener,GracefulCancellationToken cancellationToken)
    {
        if (Request == null)
            throw new Exception("A CacheFetchRequest object needs to be provided for GetChunk");

        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"About to request chunk for {HealthBoard} {Discipline} on {Request.Start}"));

        CheckObjectIsValid(listener);

        if (Request.IsRetry)
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "(this is a retry attempt)"));

        Chunk = null;
        if (!ShouldBeginFetch(listener, cancellationToken))
        {
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Fetch not required (see previous messages)"));
            return null;
        }

        try
        {
            InitialiseDownloader(listener);
            InitializeRetryStrategy();
        }
        catch (Exception e)
        {
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Initialisation failed", e));
        }

        var chunkStart = Request.Start;

        //pretty pointless message since we just said what the Request.Start was above
        if(VERBOSE)
#pragma warning disable CS0162
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"About to download, starting from {chunkStart}"));
#pragma warning restore CS0162

        IEnumerable<CombinedReportData> reports;
        try
        {
            _downloadTimer.Start();
            reports = _retryStrategy.Fetch(chunkStart, Request.ChunkPeriod, listener, cancellationToken);
            _downloadTimer.Reset();
        }
        catch (DownloadRequestFailedException e)
        {
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                $"Chunk download failed (will be recorded in CacheFetchFailure): {e.Message}", e));

            if (AuditFailureAndMoveOn)
            {
                Request.RequestFailed(e);

                // Create a chunk with an empty list and the exception property set, then rely on the destination component to pick up the exception and record it as appropriate.
                return CreateCacheChunk(chunkStart, new List<CombinedReportData>(), e);
            }
            throw;
        }

        // null signals that the repository has discovered we are now outside the permission window and has stopped, discarding everything up to now. Cache progress won't be updated in the database so we'll start from the correct point next time.
        return reports == null ? Chunk : CreateCacheChunk(chunkStart, reports);
    }

    private void InitializeRetryStrategy()
    {
        if (_retryStrategy != null) return;

        if(string.IsNullOrWhiteSpace(NumberOfSecondsToWaitBetweenRetries))
            throw new Exception("NumberOfSecondsToWaitBetweenRetries is blank");


        var waitTimes = NumberOfSecondsToWaitBetweenRetries.Split(new []{","}, StringSplitOptions.RemoveEmptyEntries);
            
        var waitTimesAsInts = new List<int>();

        foreach (var t in waitTimes)
        {
            if (!int.TryParse(t, out var i))
                throw new Exception(
                    $"NumberOfSecondsToWaitBetweenRetries contained the value '{t}' which could not be converted into an integer - all values should be a number of seconds");

            waitTimesAsInts.Add(i);
        }

        if(NumberOfTimesToRetry < 0)
            throw new Exception("NumberOfTimesToRetry should be a positive number");
            
        _retryStrategy = new LimitedRetryThenContinueStrategy(NumberOfTimesToRetry,waitTimesAsInts,Downloader);
    }


    /// <summary>
    /// Really don't call this, I added it because of horrible integration tests that force failing strategies on this poor component
    /// </summary>
    public void SetPrivateVariableRetryStrategy_NunitOnly(IRetryStrategy s)
    {
        _retryStrategy = s;
    }

    private SCIStoreCacheChunk CreateCacheChunk(DateTime chunkStart, IEnumerable<CombinedReportData> reports, DownloadRequestFailedException e = null)
    {
        return new SCIStoreCacheChunk(reports.ToArray(), chunkStart, Request)
        {
            HealthBoard = HealthBoard,
            Discipline = Discipline,
            DownloadRequestFailedException = e
        };
    }

    private readonly Stopwatch _downloadTimer = new();


    private void CheckObjectIsValid(IDataLoadEventListener listener)
    {
        if (PermissionWindow == null)
            throw new Exception(
                "A PermissionWindow object is required to determine whether we are allowed to use the downloader");

        InitialiseDownloader(listener);
    }

    private bool ShouldBeginFetch(IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
    {
        if (DownloadIsFinished(listener))
            return false;

        // todo: should have some specific notification/event for this rather than just returning null
        if (!PermissionWindow.WithinPermissionWindow())
        {
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"No longer within the permission window ({PermissionWindow.Name}) for this cache, cannot download anything further for now."));
            return false; // we have strayed outside the permission window
        }

        return !(cancellationToken.IsCancellationRequested);
    }

    private void InitialiseDownloader(IDataLoadEventListener listener)
    {
        if (Downloader != null) return;

        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Initialising downloader."));
            
        if (Configuration == null)
            throw new Exception("The Configuration object is null, so cannot retrieve the EndpointName for the CombinedReportDataWsRepository");

        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Creating web service client."));
        var client = CreateServiceClient();

        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Setting up repository."));

            
        var repo = new CombinedReportDataWsRepository(Configuration, client, Discipline, HealthBoard);
            
        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Repository set up for {HealthBoard} {Discipline}"));

        repo.Notify += (sender, message) => listener.OnNotify(sender, new NotifyEventArgs(ProgressEventType.Information, message));

        Downloader = repo;

        repo.PermissionWindow = PermissionWindow;

        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Finished downloader initialisation."));
    }

    private SCIStoreServicesClient CreateServiceClient()
    {
        return new SCIStoreServicesClient(GeneratingBinding(), new EndpointAddress(Configuration.Endpoint));
    }

    private static BasicHttpsBinding GeneratingBinding()
    {
        var b = new BasicHttpsBinding
        {
            Name = "SCIStoreServices",
            CloseTimeout = new TimeSpan(0, 1, 0),
            OpenTimeout = new TimeSpan(0, 1, 0),
            ReceiveTimeout = new TimeSpan(0, 10, 0),
            SendTimeout = new TimeSpan(0, 10, 0),
            AllowCookies = false,
            BypassProxyOnLocal = false,
            //b.HostNameComparisonMode = HostNameComparisonMode.StrongWildcard;
            MaxBufferPoolSize = 524288,
            MaxBufferSize = 20000000,
            MaxReceivedMessageSize = 20000000,
            TransferMode = TransferMode.Buffered,
            UseDefaultWebProxy = true,
            ReaderQuotas =
            {
                //b.MessageEncoding = WSMessageEncoding.Text;
                MaxDepth = 32,
                MaxStringContentLength = 8192,
                MaxArrayLength = 16384,
                MaxBytesPerRead = 4096,
                MaxNameTableCharCount = 16384
            },
            Security =
            {
                Mode = BasicHttpsSecurityMode.Transport,
                Transport =
                {
                    ClientCredentialType = HttpClientCredentialType.None,
                    ProxyCredentialType = HttpProxyCredentialType.None
                }
            }
        };

        //b.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;

        return b;
    }

    private bool DownloadIsFinished(IDataLoadEventListener listener)
    {
        // if we're outside the permission window then exit, it's up to our host to start us again at the correct time
        if (Request.PermissionWindow != null)
            if (!Request.PermissionWindow.WithinPermissionWindow())
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Now outside the permission window so stopping retrieval of data from the web service"));
                return true;
            }

        // todo: not sure how clever we should be about attempting to address the case where we may start a request which will not complete until outside the PermissionWindow
        // maybe should receive a notification from the downloader so we can then halt the download and return what we have received so far

        // end if our next request will take us past the end of the requested period
        // todo: again, could be a bit cleverer here and modify our request to the underlying downloader to request just enough to take us to the end of the request period
        if (Request.Start.Add(Request.ChunkPeriod) > Request.End)
        {
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"Successfully reached the end of our fetch request. Have reached {Request.Start} and our chunk period ({Request.ChunkPeriod}) would take us past the request end date of {Request.End}"));
            return true;
        }

        return false;
    }

    public void Dispose(IDataLoadEventListener listener)
    {
    }

    public override void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
    {
    }

    public override void Abort(IDataLoadEventListener listener)
    {
    }

    public override SCIStoreCacheChunk TryGetPreview()
    {
        throw new NotImplementedException();
    }

    public bool SilentRunning { get; set; }
    public override void Check(ICheckNotifier notifier)
    {
        // Configuration must have an Endpoint
        if (string.IsNullOrWhiteSpace(Configuration.Endpoint))
            notifier.OnCheckPerformed(new CheckEventArgs("The Configuration object for the web service source does not contain an Endpoint.", CheckResult.Fail));

        // check that we can connect to the web service
        WsRepository<CombinedReportData> wsRepo;
        try
        {
            wsRepo = new CombinedReportDataWsRepository(Configuration, CreateServiceClient(), Discipline, HealthBoard) as WsRepository<CombinedReportData>;
        }
        catch (Exception e)
        {
            notifier.OnCheckPerformed(new CheckEventArgs("Could not create the web service repository object", CheckResult.Fail, e));
            return;
        }

        try
        {
            wsRepo.CheckWebServiceConnection();
            notifier.OnCheckPerformed(new CheckEventArgs(
                $"Web Service Connection to {Configuration.Endpoint} is available", CheckResult.Success, null));
        }
        catch (Exception e)
        {
            notifier.OnCheckPerformed(new CheckEventArgs(
                $"Could not connect to the web service at {Configuration.Endpoint}", CheckResult.Fail, e));
        }


        try
        {
            InitializeRetryStrategy();
            notifier.OnCheckPerformed(new CheckEventArgs("Succesfully created retry strategy", CheckResult.Success));
        }
        catch (Exception e)
        {
            notifier.OnCheckPerformed(new CheckEventArgs("Could not create retry strategy",CheckResult.Fail, e));
            throw;
        }
    }
}