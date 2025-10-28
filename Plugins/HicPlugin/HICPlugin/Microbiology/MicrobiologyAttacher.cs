// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.Attachers;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.DataAccess;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace HICPlugin.Microbiology;

/// <summary>
/// Attacher for loading microbiology laboratory data from flat files into a normalized relational structure with header, test, isolation, and result tables
/// </summary>
public class MicrobiologyAttacher : Attacher, IPluginAttacher
{

    [DemandsInitialization("The 'header' table which contains all the lab details e.g. CHI, SampleDate, Clinician etc")]
    public TableInfo LabTable { get; set; }

    [DemandsInitialization("The 'results' table which contains all the different results for each header lab details (TestCode and ResultCode)")]
    public TableInfo TestsTable { get; set; }

    [DemandsInitialization("The table which contains all the specimens which are isolations???")]
    public TableInfo IsolationsTable { get; set; }

    [DemandsInitialization("The table which contains all the isolation results")]
    public TableInfo IsolationResultsTable { get; set; }

    [DemandsInitialization("The table which contains all the specimens which are NOT isolations???")]
    public TableInfo NoIsolationsTable { get; set; }

    readonly List<MB_Tests> Tests = new();
    readonly List<MB_Lab> Labs = new();
    readonly List<MB_NoIsolations> NoIsolations = new();
    readonly List<MB_IsolationResult> IsolationResults = new();
    readonly List<MB_Isolation> Isolations = new();

    [DemandsInitialization("The file(s) to attach e.g. *.txt, this is NOT a REGEX")]
    public string FilePattern { get; set; }

    public MicrobiologyAttacher():base(true)
    {
            
    }

    public override void LoadCompletedSoDispose(ExitCodeType exitCode, IDataLoadEventListener postLoadEventsListener)
    {
            
    }

    private readonly Dictionary<Type,PropertyInfo[]> _propertyCache = new();
    private readonly Dictionary<TableInfo, DataTable> _dataTables = new();
    private readonly Dictionary<PropertyInfo,int>  _lengthsDictionary = new();
    private IDataLoadJob _currentJob;


    public override ExitCodeType Attach(IDataLoadJob job, GracefulCancellationToken token)
    {
        _currentJob = job;

        SetupPropertyBasedReflectionIntoDataTables(null);
        SetupDataTables();

        var sw = new Stopwatch();
        sw.Start();
        var recordCount = 0;

        foreach (var fileToLoad in LoadDirectory.ForLoading.EnumerateFiles(FilePattern))
        {
            var r = new MicroBiologyFileReader(fileToLoad.FullName);
            r.Warning += R_Warning;
            try
            {
                foreach (var result in r.ProcessFile())
                {
                    switch (result)
                    {
                        //header records
                        case MB_Lab:
                            AddResultToDataTable(_dataTables[LabTable],result);
                            break;
                        //things that were isolated
                        case MB_Isolation:
                            AddResultToDataTable(_dataTables[IsolationsTable], result);
                            break;
                        //the results of that isolation
                        case MB_IsolationResult:
                            AddResultToDataTable(_dataTables[IsolationResultsTable], result);
                            break;
                        case MB_NoIsolations:
                            AddResultToDataTable(_dataTables[NoIsolationsTable], result);
                            break;
                        case MB_Tests:
                            AddResultToDataTable(_dataTables[TestsTable], result);
                            break;
                    }

                    recordCount++;

                    if(recordCount%100 == 0)
                        job.OnProgress(this,new ProgressEventArgs("Load Microbiology results into memory",new ProgressMeasurement(recordCount,ProgressType.Records),sw.Elapsed ));
                }
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"Exception thrown by {nameof(MicroBiologyFileReader)} on line:{r.LineNumber} of file:'{r.FileName}' see InnerException for specifics", e);
            }

            job.OnProgress(this, new ProgressEventArgs("Load Microbiology results into memory", new ProgressMeasurement(recordCount, ProgressType.Records), sw.Elapsed));
            sw.Stop();


            job.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information,
                $"About to bulk insert the records read from file {fileToLoad.Name}"));
            //bulk insert all data from the file we just processed
            BulkInsertAllDataTables();
            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"Bulk insert succesful{fileToLoad.Name}"));
        }


        return ExitCodeType.Success;
    }


    private int _warningsSurrendered = 0;

    private void R_Warning(object sender, string message)
    {
        if(_warningsSurrendered++ > 100)
            throw new Exception("100 Warnings encountered... maybe there is something wrong with your file? or the programmer.... best to abort anyway till you figure out the problem");

        var reader = (MicroBiologyFileReader) sender;
        _currentJob.OnNotify(sender,new NotifyEventArgs(ProgressEventType.Warning,
            $"Warning encountered on line {reader.LineNumber} of file {reader.FileName} warning is:{message}"));
    }

    private void BulkInsertAllDataTables()
    {
        foreach (var (tableInfo, dataTable) in _dataTables)
        {
            var targetTableName = tableInfo.GetRuntimeName(LoadStage.Mounting);

            var tbl = _dbInfo.ExpectTable(targetTableName);

            try
            {
                using var blk = tbl.BeginBulkInsert();
                dataTable.EndLoadData();
                blk.Upload(dataTable);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to bulk insert into table {targetTableName}",e);
            }

            dataTable.Clear();
        }
    }

    private void AddResultToDataTable(DataTable dataTable,IMicrobiologyResultRecord result)
    {
        var dataRow = dataTable.Rows.Add();
        foreach (var property in _propertyCache[result.GetType()])
        {
            var o = property.GetValue(result);
            if (o == null)
                dataRow[property.Name] = DBNull.Value;
            else
            {
                if(o is string s && _lengthsDictionary.TryGetValue(property, out var value))
                    if(value < s.Length)
                        throw new Exception(
                            $"Value '{o}' is too long for column {property.Name} when processing result of type {result.GetType().Name}");

                dataRow[property.Name] = o;
            }
        }
    }

    private void SetupDataTables()
    {
        _dataTables.Add(LabTable,CreateDataTableFromType(typeof(MB_Lab)));
        _dataTables.Add(TestsTable, CreateDataTableFromType(typeof(MB_Tests)));
            
        _dataTables.Add(IsolationsTable, CreateDataTableFromType(typeof(MB_Isolation)));
        _dataTables.Add(IsolationResultsTable, CreateDataTableFromType(typeof(MB_IsolationResult)));

        _dataTables.Add(NoIsolationsTable, CreateDataTableFromType(typeof(MB_NoIsolations)));
    }

    private DataTable CreateDataTableFromType(Type t)
    {

        var toReturn = new DataTable();
        toReturn.BeginLoadData();

        if(!_propertyCache.ContainsKey(t))
            throw new Exception($"Property Info Cache for type {t.Name} has not been initialized yet");
         
        //now create columns in the data table for each property
        foreach (var prop in _propertyCache[t])
        {

            //if it is nullable type
            if(prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                toReturn.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);//give it underlying type
            else
                toReturn.Columns.Add(prop.Name, prop.PropertyType);//else give it actual type

        }

        return toReturn;

    }



    public override void Check(ICheckNotifier notifier)
    {
        if (LabTable == null)
            notifier.OnCheckPerformed(new CheckEventArgs("Required argument LabTable is missing", CheckResult.Fail, null));
        if(TestsTable == null)
            notifier.OnCheckPerformed(new CheckEventArgs("Required argument TestsTable is missing", CheckResult.Fail, null)); 
        if(IsolationsTable == null)
            notifier.OnCheckPerformed(new CheckEventArgs("Required argument IsolationsTable is missing", CheckResult.Fail, null));
        if (IsolationResultsTable == null)
            notifier.OnCheckPerformed(new CheckEventArgs("Required argument IsolationResultsTable is missing", CheckResult.Fail, null));
        if(NoIsolationsTable == null)
            notifier.OnCheckPerformed(new CheckEventArgs("Required argument NoIsolationsTable is missing", CheckResult.Fail, null));

            


        SetupPropertyBasedReflectionIntoDataTables(notifier);



    }

    private void SetupPropertyBasedReflectionIntoDataTables(ICheckNotifier notifier)
    {
        ConfirmPropertiesExist("LabTable", LabTable, typeof(MB_Lab), notifier);
        ConfirmPropertiesExist("TestsTable", TestsTable, typeof(MB_Tests), notifier);
        ConfirmPropertiesExist("IsolationsTable", IsolationsTable, typeof(MB_Isolation), notifier);
        ConfirmPropertiesExist("IsolationResultsTable", IsolationResultsTable, typeof(MB_IsolationResult), notifier);

        ConfirmPropertiesExist("NoIsolationsTable", NoIsolationsTable, typeof(MB_NoIsolations), notifier);
    }

    private void ConfirmPropertiesExist(string argumentNameForWhenMissing, TableInfo tableInfo, Type type, ICheckNotifier notifier)
    {
        if(tableInfo == null)
        {
            ComplainOrThrow($"Required TableInfo argument {argumentNameForWhenMissing} is missing", notifier);
            return;
        }

        var properties = type.GetProperties();
        _propertyCache.Add(type, properties);//cache it so we can use it later on on a per row basis without tanking performance

        var columnInfos = tableInfo.ColumnInfos.ToArray();

        var errors = false;
        foreach (var prop in properties)
        {
            var correspondingColumn = columnInfos.FirstOrDefault(c => c.GetRuntimeName().Equals(prop.Name));
            if (correspondingColumn == null)
            {
                ComplainOrThrow($"No column exists called {prop.Name} in TableInfo {tableInfo.GetRuntimeName()}",notifier);
                errors = true;
            }
            else
            {
                var maxLength = correspondingColumn.Discover(DataAccessContext.DataLoad).DataType.GetLengthIfString();
                if(maxLength > -1)
                    _lengthsDictionary.Add(prop,(int)maxLength);
            }
        }

        if (!errors && notifier != null)
            notifier.OnCheckPerformed(new CheckEventArgs(
                $"All columns present and correct in TableInfo {tableInfo.GetRuntimeName()} (when tested against underlying type {type.Name})", CheckResult.Success, null));
    }

    private static void ComplainOrThrow(string message,ICheckNotifier notifier)
    {
        if (notifier == null)
            throw new Exception(message);
        notifier.OnCheckPerformed(new CheckEventArgs(message, CheckResult.Fail, null));
    }
}