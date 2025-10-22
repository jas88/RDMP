﻿using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;
using HICPluginTests;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using NUnit.Framework;
using Rdmp.Core.Caching.Requests;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Cache;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Curation.Data.Spontaneous;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad.Modules.DataProvider;
using Rdmp.Core.ReusableLibraryCode;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStorePlugin.Cache.Pipeline;
using SCIStorePlugin.Data;
using SCIStorePlugin.DataProvider.RetryStrategies;
using SCIStorePlugin.Repositories;
using Tests.Common;

namespace SCIStorePluginTests.Integration;

public class SCIStoreWebServiceSourceTests : DatabaseTests
{
    [Test]
    public void CheckerTest_InvalidConfiguration()
    {
        var component = new SCIStoreWebServiceSource
        {
            Configuration = new WebServiceConfiguration(CatalogueRepository)
            {
                Endpoint = "foo"
            },
            HealthBoard = HealthBoard.T,
            Discipline = Discipline.Immunology,
        };

        component.Check(IgnoreAllErrorsCheckNotifier.Instance);

    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void CacheFetchFailureIsRecordedSuccessfully(bool auditAsFailure)
    {
        LoadMetadata loadMetadata = null;
        LoadProgress loadSchedule = null;
        ICacheProgress cacheProgress = null;

        try
        {
            using (var con = ((ITableRepository)CatalogueRepository).GetConnection())
            {
                var cmd = DatabaseCommandHelper.GetCommand("DELETE FROM CacheFetchFailure", con.Connection, con.Transaction);
                cmd.ExecuteNonQuery();
            }

            // create entities in database
            loadMetadata = new LoadMetadata(CatalogueRepository);
            loadSchedule = new LoadProgress(CatalogueRepository, loadMetadata);
            cacheProgress = new CacheProgress(CatalogueRepository, loadSchedule);

            // set up the request provider to return a specific CacheFetchRequest instance
            var cacheFetchRequest = new CacheFetchRequest(CatalogueRepository, new DateTime(2015, 1, 1))
            {
                ChunkPeriod = new TimeSpan(1, 0, 0),
                CacheProgress = cacheProgress,
                PermissionWindow = new SpontaneouslyInventedPermissionWindow(cacheProgress)
            };

            var requestProvider = new MockCacheFetchRequestProvider(cacheFetchRequest);

            // Create a stubbed retry strategy which will fail and throw the 'DownloadRequestFailedException'
            var faultException = new FaultException(new FaultReason("Error on the server"), new FaultCode("Fault Code"), "Action");
            var downloadException = new DownloadRequestFailedException(cacheFetchRequest.Start, cacheFetchRequest.ChunkPeriod, faultException);
            var failStrategy = new MockRetryStrategy(downloadException);

            // Create the source
            var source = new SCIStoreWebServiceSource
            {
                PermissionWindow = new SpontaneouslyInventedPermissionWindow(cacheProgress),
                RequestProvider = requestProvider,
                NumberOfTimesToRetry = 1,
                NumberOfSecondsToWaitBetweenRetries = "1",
                AuditFailureAndMoveOn = auditAsFailure,
                // todo: why does the source need this if it is in the CacheFetchRequest object?
                Downloader = failStrategy.WebService
            };

            source.SetPrivateVariableRetryStrategy_NunitOnly(failStrategy);


            // Create the cancellation token and ask the source for a chunk
            var stopTokenSource = new CancellationTokenSource();
            var abortTokenSource = new CancellationTokenSource();
            var token = new GracefulCancellationToken(stopTokenSource.Token, abortTokenSource.Token);


            SCIStoreCacheChunk chunk;

            if (auditAsFailure)
                chunk = source.GetChunk(ThrowImmediatelyDataLoadEventListener.Quiet, token);
            else
            {
                Assert.Throws<DownloadRequestFailedException>(
                    () => source.GetChunk(ThrowImmediatelyDataLoadEventListener.Quiet, token));
                return;
            }

            Assert.That(chunk,Is.Not.Null);
           Assert.That(downloadException, Is.EqualTo(chunk.DownloadRequestFailedException));

            var failures = CatalogueRepository.GetAllObjects<CacheFetchFailure>();
            var numFailures = failures.Length;
           Assert.That(1, Is.EqualTo(numFailures));//, "The cache fetch failure was not recorded correctly.");

            var failure = failures[0];
           Assert.That(cacheFetchRequest.Start, Is.EqualTo(failure.FetchRequestStart));
        }
        catch (Exception e)
        {
            Assert.Fail(ExceptionHelper.ExceptionToListOfInnerMessages(e, true));
        }
        finally
        {
            cacheProgress?.DeleteInDatabase();
            loadSchedule?.DeleteInDatabase();
            loadMetadata?.DeleteInDatabase();
        }
    }
}

internal class AsyncRepositoryTest : IRepositorySupportsDateRangeQueries<CombinedReportData>
{
    private bool _abort;
    private bool _stop;

    public IEnumerable<CombinedReportData> ReadAll()
    {
        throw new NotImplementedException();
    }

    public void Create(IEnumerable<CombinedReportData> reports, IDataLoadEventListener listener)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<CombinedReportData> ReadSince(DateTime day)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IEnumerable<CombinedReportData>> ChunkedReadFromDateRange(DateTime start, DateTime end, IDataLoadEventListener job)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<CombinedReportData> ReadForInterval(DateTime day, TimeSpan timeSpan)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<CombinedReportData> ReadForIntervalAsync(DateTime day, TimeSpan timeSpan)
    {
        while (_abort || _stop)
            Thread.Sleep(100);

        if (_abort || _stop) return null;

        return new List<CombinedReportData>();
    }

    public void Abort()
    {
        _abort = true;
    }

    public void Stop()
    {
        _stop = true;
    }

    public IEnumerable<CombinedReportData> ReadForInterval(DateTime day, TimeSpan timeSpan, IDataLoadEventListener listener, GracefulCancellationToken token)
    {
        throw new NotImplementedException();
    }
}