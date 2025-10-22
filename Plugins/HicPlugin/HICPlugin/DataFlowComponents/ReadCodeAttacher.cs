﻿using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Rdmp.Core.ReusableLibraryCode.Checks;
using FAnsi.Discovery;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Rdmp.Core.DataLoad;
using Rdmp.Core.Curation;
using Rdmp.Core.DataLoad.Engine.Attachers;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad.Engine.Job;
using Microsoft.Data.SqlClient;

namespace HICPlugin.DataFlowComponents;

public partial class ReadCodeAttacher:IPluginAttacher
{
    private DiscoveredDatabase _dbInfo;

    public bool DisposeImmediately { get; set; }

    public void LoadCompletedSoDispose(ExitCodeType exitCode,IDataLoadEventListener postLoadEventListener)
    {

    }


    private const int MaxAdditionalCrudColumns = 10;


    public ExitCodeType Attach(IDataLoadJob job, GracefulCancellationToken token)
    {

        var listTables = _dbInfo.DiscoverTables(false);
            
        if(listTables.Length != 1)
            throw new Exception(
                $"Expected there only to be 1 table on the destination RAW server, called something like z_TRUD_ReadCodes but found {listTables.Length}:{string.Join(",", listTables.Select(t => t.GetFullyQualifiedName()))}");

        var destinationTable = listTables[0];
            
        var timerForPerformance = new Stopwatch();
        timerForPerformance.Start();

        var con = (SqlConnection) _dbInfo.Server.GetConnection();
        con.Open();

        foreach (var file in LoadDirectory.ForLoading.EnumerateFiles("*key*").ToArray())
        {
            job.OnNotify(this,new NotifyEventArgs(ProgressEventType.Warning,
                $"Found file that probably doesnt containing anything useful so deleting it, file is called {file.FullName}"));
            file.Delete();
        }

        DeleteCrudFile("chgrep.txt");
        DeleteCrudFile("contents.txt");

        //prepare destination table (for bulk insert)
        var destination = new DataTable();
        destination.Columns.Add("ReadCode");
        destination.Columns.Add("Version");
        destination.Columns.Add("OriginFilename");
            
        for (var i = 1; i <= MaxAdditionalCrudColumns; i++)
            destination.Columns.Add($"Column{i}");
            
            
        //make sure all these columns actually exist on the target server
        var listColumns = destinationTable.DiscoverColumns().Select(c=>c.GetRuntimeName()).ToArray();

        foreach (DataColumn expectedColumn in destination.Columns)
            if (!listColumns.Contains(expectedColumn.ColumnName))
                throw new Exception(
                    $"When interrogating the destination database we found a table called {destinationTable} but it was missing expected column {expectedColumn}");

        var readCodeColumn = destinationTable.DiscoverColumn("ReadCode");
        var maxReadCodeLength = readCodeColumn.DataType.GetLengthIfString();

        if(maxReadCodeLength == -1)
            throw new Exception("ReadCode reported its length as -1!");

        PopulateDataTableForBulkInsert(destination, maxReadCodeLength,"*.v3",3,"|",false,job);
        PopulateDataTableForBulkInsert(destination, maxReadCodeLength,"*.txt",2,"  ",true,job); //split on double spaces

        using var bulkCopy = new SqlBulkCopy(con)
        {
            DestinationTableName = destinationTable.GetRuntimeName(),
            BulkCopyTimeout = 5000
        };

        foreach (DataColumn dataColumn in destination.Columns)
            bulkCopy.ColumnMappings.Add(dataColumn.ColumnName, dataColumn.ColumnName);

        //send to server
        bulkCopy.WriteToServer(destination);
            
        //tell user about how long the whole process took
        timerForPerformance.Stop();
        job.OnProgress(this,new ProgressEventArgs(_dbInfo.ToString(), new ProgressMeasurement(destination.Rows.Count,ProgressType.Records), timerForPerformance.Elapsed));

        con.Close();

        return ExitCodeType.Success;
    }



    public void Check(ICheckNotifier notifier)
    {

        //https://isd.digital.nhs.uk/trud3/user/authenticated/group/0/pack/9/subpack/19/releases

        //Find any .v3 files?
    }

    private void DeleteCrudFile(string fileName)
    {
        var todelete = Path.Combine(LoadDirectory.ForLoading.FullName, fileName);
            
        if(File.Exists(todelete))
            File.Delete(todelete);
    }

    private void PopulateDataTableForBulkInsert(DataTable destination, int maxReadCodeLength, string filePattern, int verison, string separator, bool ignoreFirstLineInFile, IDataLoadJob job)
    {
         
        //read the input file and prepare records for bulk insert
        foreach (var file in LoadDirectory.ForLoading.EnumerateFiles(filePattern))
        {
            job.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information,
                $"Preparing to read data from file:{file.FullName}"));

            var readAllLines = File.ReadAllLines(file.FullName);
            var isFirstLineInFile = true;

            foreach (var line in readAllLines)
            {
                if (isFirstLineInFile && ignoreFirstLineInFile)
                {
                    isFirstLineInFile = false;
                    continue;
                }

                //skip blank lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                //split by pipes
                var strings = line.Split(new []{separator},StringSplitOptions.RemoveEmptyEntries);

                //make sure there are enough columns in the data table
                if (strings.Length > MaxAdditionalCrudColumns +1)
                    throw new Exception(
                        $"Found {strings.Length} columns in file {file.Name} but had only allocated space for {MaxAdditionalCrudColumns} crud columns (Column1,2,3 etc) + ReadCode");

                //if it is nothing but dots, ignore it.
                if (AllDots().IsMatch(strings[0]))
                {
                    job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                        $"Discarded read code \"{strings[0]}\" because it consisted of nothing but dots"));
                    continue;
                }

                var dr = destination.Rows.Add();

                //first column in file has something long in it (probably not a read code)
                if (strings[0].Length > maxReadCodeLength)
                    throw new Exception(
                        $"Found readcode {strings[0]} in file {file.Name} which is too big to fit in database which has a column called ReadCode with length {maxReadCodeLength}");

                if (file.Name.Equals("Gpiset.v3"))
                {
                    //this extra special file has GP|somereadcode|someother randomstuff
                    //instead of the expected somereadcode|somerandomstuff
                    //so for this file only, take element 1 instead of element 0 (LIKE ALLL THE OTHER FILES!)
                    dr["ReadCode"] = strings[1];
                    strings[1] = "";
                }
                else
                    dr["ReadCode"] = strings[0];

                dr["OriginFilename"] = file.Name;
                dr["Version"] = verison;

                //populate whatever random crud they decided to put into this file
                var superComboValue = "";

                for (var i = 1; i < strings.Length; i++)
                    if (!string.IsNullOrWhiteSpace(strings[i]))
                        superComboValue += $"{strings[i]}|";

                if(!string.IsNullOrWhiteSpace(superComboValue))
                    dr["Column1"] = superComboValue;
                else
                    dr["Column1"] = Guid.NewGuid().ToString();//part of primary key so must have value

                isFirstLineInFile = false;
            }
        }
    }

    public void Detach()
    {

    }

    public ILoadDirectory LoadDirectory { get; set; }

    public bool RequestsExternalDatabaseCreation => true;

    public void Initialize(ILoadDirectory hicProjectDirectory, DiscoveredDatabase dbInfo)
    {
        _dbInfo = dbInfo;
        LoadDirectory = hicProjectDirectory;
    }

    [GeneratedRegex("^\\.+$")]
    private static partial Regex AllDots();
}