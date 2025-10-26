// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using FAnsi.Discovery;
using NLog;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;

namespace SCIStorePlugin.Repositories;

public delegate void InsertionErrorHandler(object sender, SqlException exception, string queryString = "");
public class SciStoreDbRepository : IRepository<SciStoreReport>
{
    public string DatabaseName { get; set; }
    private readonly DatabaseHelper _databaseHelper;
    private readonly SciStoreTableRecord _targetTables;
    private readonly IRepository<SciStoreReport> _errorRepo;

    // SIZE OF Database field holding comment text
    private const int ResultCommentFieldSize = 650;
    private const int ClinicalDetailsFieldSize = 650;

    public event InsertionErrorHandler InsertionError;

    private readonly SqlConnection _connectionToDestination;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public SciStoreDbRepository(DatabaseHelper databaseHelper, SciStoreTableRecord targetTables, IRepository<SciStoreReport> errorRepo)
    {
        _databaseHelper = databaseHelper;
        _targetTables = targetTables;
        _errorRepo = errorRepo;

        _connectionToDestination = new SqlConnection(databaseHelper.ConnectionString());
    }

    public IEnumerable<SciStoreReport> ReadAll()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<SciStoreReport> ReadSince(DateTime day)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IEnumerable<SciStoreReport>> ChunkedReadFromDateRange(DateTime start, DateTime end, IDataLoadEventListener job)
    {
        throw new NotImplementedException();
    }


    private SciStoreReport DoTransform(SciStoreReport report)
    {
        var transformed = new SciStoreReport(report); // don't want to modify the original

        if (report.Header.ClinicalDataRequired != null)
            report.Header.ClinicalDataRequired = Truncate(report.Header.ClinicalDataRequired, ClinicalDetailsFieldSize);

        /*
        foreach (var sample in transformed.Samples)
        {
            foreach (var result in sample.Results)
            {
                if (result.Comment != null)
                    result.Comment = Truncate(result.Comment, ResultCommentFieldSize);
            }
        }
         * */
        return transformed;
    }

    private static string Truncate(string src, int maxLength)
    {
        return src[..Math.Min(maxLength, src.Length)];
    }

    /*
     * From existing loader, since new code doesn't yet handle dtUpdated
     * However, VS flags that this function isn't actually used in the existing code
     * 
    // 21/12/12 JRG Comment... I note that this should also delete the previous results already stored in the database, if any.
    void UpdateHeaderRecord(SciStoreTableRecord TableRec, string LabNumber)
    {
        string sql =
            $"UPDATE {TableRec.DatabaseName}..{TableRec.HeaderTable} SET dtUpdated = GETDATE() WHERE LabNumber = '{LabNumber}'";
        // da.ExecuteCommand(sql);
        reTryExecuteCommand(sql);
    }
    */

    public void Update(IEnumerable<SciStoreReport> reports)
    {
    }

    public void Create(IEnumerable<SciStoreReport> reports, IDataLoadEventListener listener)
    {
        var errors = new List<SciStoreReport>();
        var createdSoFar = 0;

        _connectionToDestination.Open();
        try
        {
            var startNew = Stopwatch.StartNew();
            foreach (var report in reports)
            {
                var transformedReport = DoTransform(report);

                var server = new DiscoveredServer(new SqlConnectionStringBuilder(_databaseHelper.ConnectionString()));
                var dbInfo = server.ExpectDatabase(_databaseHelper.Database);
                try
                {
                    createdSoFar += ReflectionBasedSqlDatabaseInserter.MakeInsertSqlAndExecute(transformedReport.Header, _connectionToDestination, dbInfo, _targetTables.HeaderTable);

                    foreach (var sample in transformedReport.Samples)
                    {
                        createdSoFar += ReflectionBasedSqlDatabaseInserter.MakeInsertSqlAndExecute(sample, _connectionToDestination, dbInfo, _targetTables.SamplesTable);

                        createdSoFar += sample.Results.Sum(result => ReflectionBasedSqlDatabaseInserter.MakeInsertSqlAndExecute(result, _connectionToDestination, dbInfo, _targetTables.ResultsTable));
                    }

                    listener.OnProgress(this, new ProgressEventArgs(dbInfo.GetRuntimeName(), new ProgressMeasurement(createdSoFar, ProgressType.Records), startNew.Elapsed));
                }
                catch (SqlException e)
                {
                    InsertionError?.Invoke(this, e, "CONFIDENTIAL");

                    //do not show query here because it can be logged to logging database with sensitive identifiers in it
                    Log.Warn("Could not insert row into database.", e);
                    errors.Add(report);
                }
            }
        }
        finally
        {
            _connectionToDestination.Close();
        }

        if (errors.Any())
            _errorRepo.Create(errors,listener);
    }
}