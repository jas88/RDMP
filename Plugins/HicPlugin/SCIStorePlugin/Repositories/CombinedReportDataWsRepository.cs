// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad.Modules.DataProvider;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStore.SciStoreServices81;
using SCIStorePlugin.Data;

namespace SCIStorePlugin.Repositories;


/// <summary>
/// SCI Store web service repository for a specific health board and discipline (passed in on construction)
/// </summary>
public class CombinedReportDataWsRepository : WsRepository<CombinedReportData>, IRepositorySupportsDateRangeQueries<CombinedReportData>
{
    private const bool VERBOSE = false;

    public IPermissionWindow PermissionWindow { get; set; }

    public int ResultsFetchedSoFar { get; private set; }
    public int NumReportsForInterval { get; private set; }

    private readonly SCIStoreServicesClient _client;
    private readonly Discipline _discipline;
    private readonly HealthBoard _healthBoard;
    private Credentials _cred;

    #region Events
    public override event WsNotifyHandler Notify;
    protected virtual void OnNotify(string message)
    {
        Notify?.Invoke(this, message);
    }

    #pragma warning disable CS0067 // Events are never used - this is legacy plugin code
    public override event AfterReadAllHandler AfterReadAll;

    public override event AfterReadSingleHandler AfterReadSingle;
    #pragma warning restore CS0067
    protected virtual void OnAfterReadSingle(CombinedReportData report)
    {
        AfterReadSingle?.Invoke(this, report);
    }

    #endregion

    public CombinedReportDataWsRepository(WebServiceConfiguration wsConfig, SCIStoreServicesClient client, Discipline discipline, HealthBoard healthBoard) : base(wsConfig)
    {
        if(string.IsNullOrWhiteSpace(wsConfig.Endpoint))
            throw new ArgumentException("The web service Endpoint is missing from the Configuration and must be specified");

        _client = client ?? throw new ArgumentNullException(nameof(client),"Client was null");
        _discipline = discipline;
        _healthBoard = healthBoard;
        _cred = null;
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override IEnumerable<CombinedReportData> ReadAll()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override void Create(IEnumerable<CombinedReportData> reports, IDataLoadEventListener listener)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="WebServiceLoginFailureException"></exception>
    public override void CheckWebServiceConnection()
    {
        try
        {
            Login(_client, WsConfig);
        }
        catch (WebServiceLoginFailureException e)
        {
            // retry once then fail
            OnNotify($"Initial login attempt failed: {e.Message}");
            OnNotify("Retrying...");

            // Don't bother to catch the exception this time as we will let someone further up the chain decide what to do
            Login(_client, WsConfig);
        }
    }

    /// <summary>
    /// Currently returns null if the retrieval leaks outside the permission window. For now this results in the loss of all data downloaded for the interval up to now, but can hopefully provide a better implementation at a later date.
    /// </summary>
    /// <param name="day"></param>
    /// <param name="timeSpan"></param>
    /// <param name="listener"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="WebServiceRetrievalFailure"></exception>
    public IEnumerable<CombinedReportData> ReadForInterval(DateTime day, TimeSpan timeSpan, IDataLoadEventListener listener, GracefulCancellationToken token)
    {
        if (!PermissionWindow.WithinPermissionWindow())
            return null;

        List<CombinedReportData> reports;

        try
        {
            Login(_client, WsConfig);
        }
        catch (WebServiceLoginFailureException e)
        {
            throw new WebServiceRetrievalFailure(day, timeSpan, e);
        }

        var criteria = CreateFindResultCriteria(day, timeSpan);
        OnNotify($"Attempting to fetch results for {day:yyyy-MM-dd HH:mm} ({timeSpan.Hours})");
        ResultsFetchedSoFar = 0;

        try
        {
            var items = _client.FindResult(_cred, criteria).Results.ToList();
            reports = CreateCombinedReportDataFromResultObjects(items, token);
            NumReportsForInterval = reports.Count;
            OnNotify(
                $"Found {NumReportsForInterval} results between {criteria.EventDateTime.DateFrom} - {criteria.EventDateTime.DateTo}");
        }
        catch (OperationCanceledException)
        {
            // Need to catch and rethrow this thanks to our Pokemon clause at the end, but still not sure what exceptions the web service might throw so playing safe
            throw;
        }
        catch (Exception e)
        {
            // Catching everything else as I'm not sure what exceptions FindResult might throw
            throw new WebServiceRetrievalFailure(day, timeSpan, e);
        }

        try
        {
            return FetchInvestigationReportData(reports, listener, token);
        }
        catch (LabReportRetrievalFailureException e)
        {
            throw new WebServiceRetrievalFailure(day, timeSpan, e);
        }
    }

    private FindResultCriteria CreateFindResultCriteria(DateTime day, TimeSpan timeSpan)
    {
        var criteria = new FindResultCriteria
        {
            EventDateTime = new FindDateTimeRange
            {
                DateFrom = day,
                DateTo = day + timeSpan
            },
            Discipline = _discipline.ToString(),
            IncludePatientInformation = true
        };
        return criteria;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="results"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="OperationCanceledException"></exception>
    private List<CombinedReportData> CreateCombinedReportDataFromResultObjects(List<FindResultItem> results, GracefulCancellationToken token)
    {
        var reports = new List<CombinedReportData>();
        var headers = new HashSet<SciStoreRecord>();
        foreach (var result in results)
        {
            token.ThrowIfAbortRequested();

            var header = new SciStoreRecord
            {
                CHI = result.PatientDetails.CHI,
                LabNumber = result.ReportDetails.ReportIdentifier,
                TestReportID = result.ReportDetails.TestReportID,
                patientid = result.ReportDetails.PatientID,
                ReportType = result.ReportDetails.ReportIdentifier,
                name = result.PatientDetails.FamilyName
            };

            if (headers.Contains(header))
            {
                //OnNotify("Duplicate result found, " + header.LabNumber + ", " + header.TestReportID);
                continue;
            }
            headers.Add(header);

            reports.Add(new CombinedReportData
            {
                HbExtract = _healthBoard.ToString(),
                SciStoreRecord = header
            });
        }

        return reports;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="reports"></param>
    /// <param name="listener"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="LabReportRetrievalFailureException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    private IEnumerable<CombinedReportData> FetchInvestigationReportData(List<CombinedReportData> reports, IDataLoadEventListener listener, GracefulCancellationToken token)
    {
        var sw = new Stopwatch();
        sw.Start();
        foreach (var report in reports)
        {
            if (!PermissionWindow.WithinPermissionWindow())
                return null;

            token.ThrowIfAbortRequested();

            // todo: we may want to catch the LabReportRetrievalFailureException here and initiate a retry
            report.InvestigationReport = RetrieveInvestigationReportForResult(report.SciStoreRecord);

            ResultsFetchedSoFar++;
        }

        return reports;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    /// <exception cref="LabReportRetrievalFailureException"></exception>
    private InvestigationReport RetrieveInvestigationReportForResult(SciStoreRecord result)
    {
        GetResultResponse response;
        try
        {
            response = _client.GetResult(_cred, new GetResult
            {
                ResultID = result.TestReportID
            });
        }
        catch (Exception e)
        {
            throw new LabReportRetrievalFailureException(result, e);
        }

        return response.InvestigationReport;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="client"></param>
    /// <param name="wsConfig"></param>
    /// <exception cref="WebServiceLoginFailureException"></exception>
    private void Login(SCIStoreServicesClient client, WebServiceConfiguration wsConfig)
    {
        if (client.Endpoint.Binding is not BasicHttpsBinding)
            throw new WebServiceLoginFailureException(
                $"Could not get endpoint binding for endpoint '{wsConfig.Endpoint}' (check the expected type of the binding, e.g. http or https)");

        if (VERBOSE)
#pragma warning disable CS0162
            OnNotify("Logging in to web service");
#pragma warning restore CS0162

        //todo add timeout here and anywhere else you do Login
        var response = client.Login(new Login
        {
            Username = wsConfig.Username,
            Password = wsConfig.GetDecryptedPassword()
        });

        if (string.IsNullOrEmpty(response.Token))
            throw new WebServiceLoginFailureException(
                $"Can't login to SCIStore endpoint '{wsConfig.Endpoint}' with user={wsConfig.Username} (check caching pipeline configuration for password)");

        if(VERBOSE)
#pragma warning disable CS0162
            OnNotify("Creating credentials");
#pragma warning restore CS0162

        _cred = new Credentials
        {
            Token = response.Token,
            UserInfo = new CredentialsUserInfo
            {
                FriendlyName = "HIC NW Lab",//Settings.Default.FriendlyName,
                SystemCode = "HIC_NWLab", //Settings.Default.SystemCode,
                SystemLocation = "HIC", //Settings.Default.SystemLocation,
                UserName = wsConfig.Username
            }
        };
    }
}