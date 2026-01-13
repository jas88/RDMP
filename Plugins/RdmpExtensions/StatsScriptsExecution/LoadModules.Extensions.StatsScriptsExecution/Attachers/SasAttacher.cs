// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Diagnostics;
using System.IO;
using FAnsi.Discovery;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.Attachers;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.StatsScriptsExecution.Attachers;

/// <summary>
/// Data load attacher that executes SAS scripts via sas.exe during the attach phase, providing database connection strings and output paths as macro variables with timeout and logging support.
/// </summary>
public class SasAttacher : Attacher, IPluginAttacher
{
    public SasAttacher() : base(true)
    {
    }

    [DemandsInitialization("SAS root directory (contains sas.exe)", mandatory: true)]
    public DirectoryInfo SASRootDirectory { get; set; }

    [DemandsInitialization("SAS script to run", mandatory: true)]
    public FileInfo FullPathToSASScript { get; set; }

    [DemandsInitialization("The maximum number of seconds to allow the SAS script to run for before declaring it a failure, 0 for indefinetly")]
    public int MaximumNumberOfSecondsToLetScriptRunFor { get; set; }

    [DemandsInitialization("Database connection string", mandatory: true)]
    public ExternalDatabaseServer InputDatabase { get; set; }

    [DemandsInitialization("Output directory", mandatory: true)]
    public DirectoryInfo OutputDirectory { get; set; }

    public override void Check(ICheckNotifier notifier)
    {
        try
        {
            if (!SASRootDirectory.Exists)
                throw new DirectoryNotFoundException(
                    $"The specified SAS root directory: {SASRootDirectory.FullName} does not exist");

            var fullPathToSasExe = Path.Combine(SASRootDirectory.FullName, "sas.exe");
            if (!File.Exists(fullPathToSasExe))
                throw new FileNotFoundException(
                    $"The specified SAS root directory: {SASRootDirectory.FullName} does not contain sas.exe");

            if (!FullPathToSASScript.Exists)
                throw new FileNotFoundException(
                    $"The specified SAS script to run: {FullPathToSASScript.FullName} does not exist");

            if (!OutputDirectory.Exists)
                throw new DirectoryNotFoundException(
                    $"The specified output directory: {OutputDirectory.FullName} does not exist");
        }
        catch (Exception e)
        {
            notifier.OnCheckPerformed(new CheckEventArgs(e.Message, CheckResult.Fail, e));
        }
    }

    public override void LoadCompletedSoDispose(ExitCodeType exitCode, IDataLoadEventListener postLoadEventListener)
    {
    }

    public override ExitCodeType Attach(IDataLoadJob job, GracefulCancellationToken cancellationToken)
    {
        var processStartInfo = CreateCommand();

        int exitCode;
        try
        {
            exitCode = ExecuteProcess(processStartInfo, MaximumNumberOfSecondsToLetScriptRunFor, job);
        }
        catch (TimeoutException e)
        {
            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "SAS script timed out (See inner exception for details", e));
            return ExitCodeType.Error;
        }

        job.OnNotify(this, new NotifyEventArgs(exitCode == 0 ? ProgressEventType.Information : ProgressEventType.Error,
            $"SAS script terminated with exit code {exitCode}"));

        return exitCode == 0 ? ExitCodeType.Success : ExitCodeType.Error;
    }

    private int ExecuteProcess(ProcessStartInfo processStartInfo, int scriptTimeout, IDataLoadJob job)
    {
        processStartInfo.UseShellExecute = false;
        processStartInfo.CreateNoWindow = true;

        job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Starting SAS."));

        Process p;
        try
        {
            p = new Process
            {
                StartInfo = processStartInfo
            };

            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"commandline: {processStartInfo.Arguments}"));

            p.Start();
        }
        catch (Exception e)
        {
            throw new Exception(
                $"Failed to launch:{Environment.NewLine}{processStartInfo.FileName}{Environment.NewLine} with Arguments:{processStartInfo.Arguments}", e);
        }

        var startTime = DateTime.Now;
        while (!p.WaitForExit(100))
        {
            if (TimeoutExpired(startTime))//if timeout expired
            {
                bool killed;
                try
                {
                    p.Kill();
                    killed = true;
                }
                catch (Exception)
                {
                    killed = false;
                }

                throw new TimeoutException(
                    $"Process command {processStartInfo.FileName} with arguments {processStartInfo.Arguments} did not complete after  {scriptTimeout} seconds {(killed ? "(After timeout we killed the process successfully)" : "(We also failed to kill the process after the timeout expired)")}");
            }
        }

        return p.ExitCode;
    }

    private bool TimeoutExpired(DateTime startTime)
    {
        if (MaximumNumberOfSecondsToLetScriptRunFor == 0)
            return false;

        return DateTime.Now - startTime > new TimeSpan(0, 0, 0, MaximumNumberOfSecondsToLetScriptRunFor);
    }

    private ProcessStartInfo CreateCommand()
    {
        var scriptFileName = FullPathToSASScript.Name.Replace(FullPathToSASScript.Extension, "");
        var actualOutputDir = CreateActualOutputDir(scriptFileName);
        var sasFullPath = Path.Combine(SASRootDirectory.FullName, "sas.exe");

        var fullPrintPath = Path.Combine(actualOutputDir, $"{scriptFileName}.out");
        var fullLogPath = Path.Combine(actualOutputDir, $"{scriptFileName}.log");

        var dataInConnection = GetSASConnectionString(InputDatabase);
        var dataOutConnection = GetSASConnectionString(_dbInfo);

        var command =
            $"-set output \"{actualOutputDir}\" -set connect \"{dataInConnection}\" -set connectout \"{dataOutConnection}\" -sysin \"{FullPathToSASScript.FullName}\" -nosplash -noterminal -nostatuswin -noicon -print \"{fullPrintPath}\" -log \"{fullLogPath}\"";

        var info = new ProcessStartInfo(sasFullPath)
        {
            Arguments = command
        };

        return info;
    }

    private string GetSASConnectionString(DiscoveredDatabase db)
    {
        return $"Server={db.Server.Name};Database={db.GetRuntimeName()};IntegratedSecurity=true;DRIVER=SQL Server";
    }

    private string GetSASConnectionString(ExternalDatabaseServer db)
    {
        return $"Server={db.Server};Database={db.Database};IntegratedSecurity=true;DRIVER=SQL Server";
    }

    private string CreateActualOutputDir(string scriptFileName)
    {
        var timeStampString = DateTime.Now.ToString("yyyyMMddTHHmmss");
        var dir = Path.Combine(OutputDirectory.FullName, $"{timeStampString}_{scriptFileName}");

        try
        {
            Directory.CreateDirectory(dir);
        }
        catch (Exception)
        {
            return OutputDirectory.FullName;
        }

        return dir;
    }
}