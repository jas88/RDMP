// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using FAnsi.Discovery;
using Rdmp.Core.CohortCommitting.Pipeline;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad.Engine.Pipeline.Destinations;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace HICPlugin.DataFlowComponents;

/// <summary>
/// Cohort destination that commits cohorts to the HIC (Health Informatics Centre) cohort management system via stored procedures, supporting both new cohorts and versioned updates to existing cohorts
/// </summary>
public class HICCohortManagerDestination : IPluginCohortDestination
{
    [Obsolete("This was misspelled in old versions of this plugin")]
    // ReSharper disable once IdentifierTypo
    public string NewCohortsStoredProceedure
    {
        get => NewCohortsStoredProcedure;
        set => NewCohortsStoredProcedure = value;
    }

    [DemandsInitialization("The name of the stored procedure which will commit entirely new cohorts")]
    public string NewCohortsStoredProcedure { get; set; }

    [Obsolete("This was misspelled in old versions of this plugin")]
    // ReSharper disable once IdentifierTypo
    public string ExistingCohortStoredProceedure
    {
        get => ExistingCohortsStoredProcedure;
        set => ExistingCohortsStoredProcedure = value;
    }

    [DemandsInitialization("The name of the stored procedure which will augment existing cohorts with new versions")]
    public string ExistingCohortsStoredProcedure { get; set; }

    public ICohortCreationRequest Request { get; set; }
    public bool CreateExternalCohort { get; set; }

    public DataTable AllAtOnceDataTable;
    private string _privateIdentifier;

    public HICCohortManagerDestination()
    {
        CreateExternalCohort = true;
    }

    public DataTable ProcessPipelineData(DataTable toProcess, IDataLoadEventListener listener,GracefulCancellationToken cancellationToken)
    {
        var sw = new Stopwatch();
        sw.Start();
        if (AllAtOnceDataTable == null)
        {
            if(!toProcess.Columns.Contains(_privateIdentifier))
                throw new Exception($"Pipeline did not have a column called {_privateIdentifier}");

            AllAtOnceDataTable = new DataTable("CohortUpload_HICCohortManagerDestination");
            AllAtOnceDataTable.Columns.Add(_privateIdentifier);
        }

        foreach (DataRow dr in toProcess.Rows)
            AllAtOnceDataTable.Rows.Add(dr[_privateIdentifier]);

        listener.OnProgress(this,new ProgressEventArgs("Buffering all identifiers in memory",new ProgressMeasurement(AllAtOnceDataTable.Rows.Count,ProgressType.Records),sw.Elapsed));
        sw.Stop();

        return null;
    }

    public void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
    {
        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "About to send all the buffered data up to the server"));
        var target = Request.NewCohortDefinition.LocationOfCohort;
        var cohortDatabase = target.Discover();

        var tempTableName = QuerySyntaxHelper.MakeHeaderNameSensible(Guid.NewGuid().ToString());
        AllAtOnceDataTable.TableName = tempTableName;

        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, $"Uploading to {tempTableName}"));

        var dest = new DataTableUploadDestination();

        if(_privateIdentifier.Equals("chi",StringComparison.CurrentCultureIgnoreCase))
            dest.AddExplicitWriteType(_privateIdentifier, "varchar(10)");

        dest.AllowResizingColumnsAtUploadTime = true;
        dest.PreInitialize(cohortDatabase,listener);
        dest.ProcessPipelineData(AllAtOnceDataTable, listener, new GracefulCancellationToken());
        dest.Dispose(listener,null);

        var tbl = cohortDatabase.ExpectTable(tempTableName);
        if(!tbl.Exists())
            throw new Exception($"Temp table '{tempTableName}' did not exist in cohort database '{cohortDatabase}'");
        try
        {
            //commit from temp table (most likely place to crash)
            using var con = (SqlConnection) cohortDatabase.Server.GetConnection();
            con.Open();
            SqlCommand cmd;
            using var transaction = con.BeginTransaction("Committing cohort");

            if (Request.NewCohortDefinition.Version == 1)
            {
                cmd = new SqlCommand(NewCohortsStoredProcedure, con, transaction);
                cmd.Parameters.AddWithValue("sourceTableName", tbl.GetFullyQualifiedName());
                cmd.Parameters.AddWithValue("projectNumber", Request.Project.ProjectNumber);
                cmd.Parameters.AddWithValue("description", Request.NewCohortDefinition.Description);
            }
            else
            {
                //get the existing cohort number
                int cohortNumber;
                using (var cmdGetCohortNumber =
                       new SqlCommand(
                           $"(SELECT MAX(cohortNumber) FROM {target.DefinitionTableName} where description = '{Request.NewCohortDefinition.Description}')",
                           con, transaction))
                    cohortNumber = Convert.ToInt32(cmdGetCohortNumber.ExecuteScalar());

                //call the commit
                cmd = new SqlCommand(ExistingCohortsStoredProcedure, con, transaction);
                cmd.Parameters.AddWithValue("sourceTableName", tbl.GetFullyQualifiedName());
                cmd.Parameters.AddWithValue("projectNumber", Request.Project.ProjectNumber);
                cmd.Parameters.AddWithValue("cohortNumber", cohortNumber);
                cmd.Parameters.AddWithValue("description", Request.NewCohortDefinition.Description);
            }

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 100000;
            var cohortId = Convert.ToInt32(cmd.ExecuteScalar());

            listener.OnNotify(this,
                new NotifyEventArgs(ProgressEventType.Information, $"Called stored procedure {cmd.CommandText}"));

            if (cohortId == 0)
                throw new Exception("Stored procedure returned null or 0");

            transaction.Commit();
            listener.OnNotify(this,
                new NotifyEventArgs(ProgressEventType.Information,
                    $"Finished data load and committed transaction{cmd.CommandText}"));

            if (CreateExternalCohort)
            {
                Request.NewCohortDefinition.ID = cohortId;
                listener.OnNotify(this,
                    new NotifyEventArgs(ProgressEventType.Information,
                        "About to attempt to create a pointer to this cohort that has been created"));
                Request.ImportAsExtractableCohort(true,true);
                listener.OnNotify(this,
                    new NotifyEventArgs(ProgressEventType.Information,
                        "Successfully created pointer, you should now have access to your cohort in RDMP"));
            }
        }
        finally
        {
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"Dropping {tbl.GetFullyQualifiedName()}"));
            tbl.Drop();
        }
    }

    public void Abort(IDataLoadEventListener listener)
    {
            
    }

    public void PreInitialize(ICohortCreationRequest value, IDataLoadEventListener listener)
    {
        Request = value;
        var syntaxHelper = value.NewCohortDefinition.LocationOfCohort.GetQuerySyntaxHelper();
        _privateIdentifier = syntaxHelper.GetRuntimeName(Request.NewCohortDefinition.LocationOfCohort.PrivateIdentifierField);
    }

    public void Check(ICheckNotifier notifier)
    {

        var location = Request.NewCohortDefinition.LocationOfCohort;

        //check the cohort database
        location.Check(notifier);

        //now check the stored procedures it has in it
        var spsFound = location.Discover().DiscoverStoredprocedures().Select(sp => sp.Name).ToArray();

        //have they forgotten to tell us what the proc is?
        if (string.IsNullOrEmpty(NewCohortsStoredProcedure))
            notifier.OnCheckPerformed(new CheckEventArgs("DemandsInitialization property NewCohortsStoredProcedure is blank",CheckResult.Fail));
        else
        if (spsFound.Contains(NewCohortsStoredProcedure))//it exists!
            notifier.OnCheckPerformed(new CheckEventArgs(
                $"Found stored procedure {NewCohortsStoredProcedure} in cohort database {location}", CheckResult.Success));
        else //it doesn't exist!
            notifier.OnCheckPerformed(new CheckEventArgs(
                $"Could not find stored procedure {NewCohortsStoredProcedure} in cohort database {location}", CheckResult.Fail));

        //now do the same again for ExistingCohortsStoredProcedure
        if (string.IsNullOrEmpty(ExistingCohortsStoredProcedure))
            notifier.OnCheckPerformed(
                new CheckEventArgs("DemandsInitialization property ExistingCohortsStoredProcedure is blank",
                    CheckResult.Fail));
        else
        if (spsFound.Contains(ExistingCohortsStoredProcedure))
            notifier.OnCheckPerformed(new CheckEventArgs(
                $"Found stored procedure {ExistingCohortsStoredProcedure} in cohort database {location}", CheckResult.Success));
        else
            notifier.OnCheckPerformed(new CheckEventArgs(
                $"Could not find stored procedure {ExistingCohortsStoredProcedure} in cohort database {location}", CheckResult.Fail));


    }
}