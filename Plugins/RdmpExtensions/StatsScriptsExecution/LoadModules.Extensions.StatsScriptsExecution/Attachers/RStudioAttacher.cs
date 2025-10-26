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
/// Data load attacher that executes R scripts via Rscript.exe during the attach phase, passing database connection details and output directories as command-line arguments with timeout support.
/// </summary>
public class RStudioAttacher : Attacher, IPluginAttacher
{
    public RStudioAttacher() : base(true)
    {
    }

    [DemandsInitialization("Rscript root directory (contains Rscript.exe)", mandatory: true)]
    public DirectoryInfo RscriptRootDirectory { get; set; }

    [DemandsInitialization("R script to run", mandatory: true)]
    public FileInfo FullPathToRScript { get; set; }

    [DemandsInitialization("The maximum number of seconds to allow the R script to run for before declaring it a failure, 0 for indefinetly")]
    public int MaximumNumberOfSecondsToLetScriptRunFor { get; set; }

    [DemandsInitialization("Database connection string", mandatory: true)]
    public ExternalDatabaseServer InputDatabase { get; set; }

    [DemandsInitialization("Output directory", mandatory: true)]
    public DirectoryInfo OutputDirectory { get; set; }

    [DemandsInitialization("Default Packages to load. R will be run in 'vanilla' mode, so please specify ALL the pakcges you need in a comma separated list. Remember to leave the defaults (RODBC,graphics,grDevices,stats)",
        defaultValue: "RODBC,graphics,grDevices,stats")]
    public string DefaultPackages { get; set; }

    public override void Check(ICheckNotifier notifier)
    {
        try
        {
            if (!RscriptRootDirectory.Exists)
                throw new DirectoryNotFoundException(
                    $"The specified Rscript root directory: {RscriptRootDirectory.FullName} does not exist");

            var fullPathToRscriptExe = Path.Combine(RscriptRootDirectory.FullName, "Rscript.exe");
            if (!File.Exists(fullPathToRscriptExe))
                throw new FileNotFoundException(
                    $"The specified Rscript root directory: {RscriptRootDirectory.FullName} does not contain Rscript.exe");

            if (!FullPathToRScript.Exists)
                throw new FileNotFoundException(
                    $"The specified R script to run: {FullPathToRScript.FullName} does not exist");

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
            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "R script timed out (See inner exception for details", e));
            return ExitCodeType.Error;
        }

        job.OnNotify(this, new NotifyEventArgs(exitCode == 0 ? ProgressEventType.Information : ProgressEventType.Error,
            $"R script terminated with exit code {exitCode}"));

        return exitCode == 0 ? ExitCodeType.Success : ExitCodeType.Error;
    }

    private int ExecuteProcess(ProcessStartInfo processStartInfo, int scriptTimeout, IDataLoadJob job)
    {
        processStartInfo.UseShellExecute = false;
        processStartInfo.CreateNoWindow = true;

        processStartInfo.RedirectStandardError = true;

        job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Starting Rscript."));

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

        var errors = p.StandardError.ReadToEnd();
        if (!string.IsNullOrEmpty(errors))
            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, $"Error from R: {errors}"));

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
        var scriptFileName = FullPathToRScript.Name.Replace(FullPathToRScript.Extension, "");
        var actualOutputDir = CreateActualOutputDir(scriptFileName);
        var rscriptFullPath = Path.Combine(RscriptRootDirectory.FullName, "Rscript.exe");

        var fullPrintPath = Path.Combine(actualOutputDir, $"{scriptFileName}.Rout");

        var command =
            $"--vanilla --default-packages={DefaultPackages} \"{FullPathToRScript.FullName.Replace('\\', '/')}\" {InputDatabase.Server} {InputDatabase.Database} {_dbInfo.Server} {_dbInfo.GetRuntimeName()} \"{actualOutputDir.TrimEnd('\\').Replace('\\', '/')}/\" >\"{fullPrintPath.Replace('\\', '/')}\"";

        var info = new ProcessStartInfo(rscriptFullPath)
        {
            Arguments = command
        };

        return info;
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