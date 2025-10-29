// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using FAnsi.Discovery;
using Microsoft.Win32;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.DataProvider;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.Python.DataProvider;

#nullable enable

public enum PythonVersion
{
    NotSet,
    Version2,
    Version3
}

public sealed class PythonDataProvider:IPluginDataProvider
{
    [DemandsInitialization("The Python script to run")]
    public string? FullPathToPythonScriptToRun { get; set; }

    [DemandsInitialization("The maximum number of seconds to allow the python script to run for before declaring it a failure, 0 for indefinitely")]
    public int MaximumNumberOfSecondsToLetScriptRunFor { get; set; }

    [DemandsInitialization("Python version required to run your script")]
    public PythonVersion Version { get; set; }

    [DemandsInitialization("Override Python Executable Path")]
    public FileInfo? OverridePythonExecutablePath { get; set; }


    public void LoadCompletedSoDispose(ExitCodeType exitCode, IDataLoadEventListener postLoadEventsListener)
    {

    }

    public void Check(ICheckNotifier notifier)
    {

        if (Version == PythonVersion.NotSet)
        {
            notifier.OnCheckPerformed(
                new CheckEventArgs("Version of Python required for script has not been selected", CheckResult.Fail));
            return;
        }

        if (FullPathToPythonScriptToRun?.Contains(' ') == true && FullPathToPythonScriptToRun?.Contains('"') == false)
            notifier.OnCheckPerformed(
                new CheckEventArgs(
                    "FullPathToPythonScriptToRun contains spaces but is not wrapped by quotes which will likely fail when we assemble the python execute command",
                    CheckResult.Fail));

        if (!File.Exists(FullPathToPythonScriptToRun?.Trim('\"', '\'')))
            notifier.OnCheckPerformed(
                new CheckEventArgs(
                    $"File {FullPathToPythonScriptToRun} does not exist (FullPathToPythonScriptToRun)",
                    CheckResult.Warning));

        //make sure Python is installed
        try
        {
            var version = GetPythonVersion();

            if (version?.StartsWith(GetExpectedPythonVersion(), StringComparison.Ordinal)==true)
                notifier.OnCheckPerformed(
                    new CheckEventArgs(
                        $"Found Expected Python version {version} on the host machine at {GetPython(Version == PythonVersion.Version2 ? '2' : '3').path}", CheckResult.Success));
            else if (version is not null && ((version[0] == '3' && Version == PythonVersion.Version3) ||
                                             (version[0] == '2' && Version == PythonVersion.Version2)))
                notifier.OnCheckPerformed(
                    new CheckEventArgs(
                        $"Found Compatible Python version {version} on the host machine at {GetPython(Version == PythonVersion.Version2 ? '2' : '3').path}", CheckResult.Success));
            else
            {
                notifier.OnCheckPerformed(
                    new CheckEventArgs(
                        $"Python version on the host machine is {version} which is incompatible with the desired version {GetExpectedPythonVersion()}",
                        CheckResult.Fail));
            }
        }
        catch (FileNotFoundException e)
        {
            notifier.OnCheckPerformed(new CheckEventArgs(e.Message, CheckResult.Fail, e));
        }
        catch (Exception e)
        {
            //python is not installed
            notifier.OnCheckPerformed(e.Message.Equals("The system cannot find the file specified")
                ? new CheckEventArgs("Python is not installed on the host", CheckResult.Fail, e)
                : new CheckEventArgs(e.Message, CheckResult.Fail, e));
        }
    }

    public string? GetPythonVersion()
    {
        const string getVersion = """
                                  -c "import sys; print(sys.version)"
                                  """;
        var toMemory = new ToMemoryDataLoadEventListener(true);
        var result = ExecuteProcess(toMemory, getVersion, 600);

        if (result != 0)
            return null;

        var msg = toMemory.EventsReceivedBySender[this].SingleOrDefault();

        if (msg != null)
            return msg.Message;

        throw new Exception($"Call to {getVersion} did not return any value but exited with code {result}");
    }

    private string GetPythonCommand()
    {
        if (OverridePythonExecutablePath == null)
            return GetPython(Version==PythonVersion.Version2?'2':'3').path;

        if (!OverridePythonExecutablePath.Exists)
            throw new FileNotFoundException(
                $"The specified OverridePythonExecutablePath:{OverridePythonExecutablePath} does not exist");
        if(OverridePythonExecutablePath.Name != "python.exe")
            throw new FileNotFoundException(
                $"The specified OverridePythonExecutablePath:{OverridePythonExecutablePath} file is not called python.exe... what is going on here?");

        return OverridePythonExecutablePath.FullName;
    }

    public void Initialize(ILoadDirectory hicProjectDirectory, DiscoveredDatabase dbInfo)
    {

    }

    public ExitCodeType Fetch(IDataLoadJob job, GracefulCancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(FullPathToPythonScriptToRun))
        {
            job.OnNotify(this,
                new NotifyEventArgs(ProgressEventType.Error,
                    "No Python script provided"));
            return ExitCodeType.Error;
        }

        int exitCode;
        try
        {
            exitCode = ExecuteProcess(job, FullPathToPythonScriptToRun, MaximumNumberOfSecondsToLetScriptRunFor);
        }
        catch (TimeoutException e)
        {
            job.OnNotify(this,new NotifyEventArgs(ProgressEventType.Error, "Python command timed out (See inner exception for details)",e));
            return ExitCodeType.Error;
        }

        job.OnNotify(this, new NotifyEventArgs(exitCode == 0 ? ProgressEventType.Information : ProgressEventType.Error,
            $"Python script terminated with exit code {exitCode}"));

        return exitCode == 0 ? ExitCodeType.Success : ExitCodeType.Error;
    }

    private int ExecuteProcess(IDataLoadEventListener listener, string script, int maximumNumberOfSecondsToLetScriptRunFor)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = GetPythonCommand(),
            Arguments = script,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process p;

        var allErrorDataConsumed = false;
        var allOutputDataConsumed = false;

        try
        {
            p = new Process
            {
                StartInfo = processStartInfo
            };
            p.OutputDataReceived += (s, e) => allOutputDataConsumed = OutputDataReceived(e, listener,false);
            p.ErrorDataReceived += (s, e) => allErrorDataConsumed = OutputDataReceived(e, listener,true);

            p.Start();
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();

        }
        catch (Exception e)
        {
            throw new Exception(
                $"Failed to launch:{Environment.NewLine}{processStartInfo.FileName}{Environment.NewLine} with Arguments:{processStartInfo.Arguments}",e);
        }

        // To avoid deadlocks, always read the output stream first and then wait.
        var startTime = DateTime.Now;


        while (!p.WaitForExit(100))//while process has not exited
        {
            if (!TimeoutExpired(startTime)) continue; //if timeout expired

            var killed = false;
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
                $"Process command {processStartInfo.FileName} with arguments {processStartInfo.Arguments} did not complete after  {maximumNumberOfSecondsToLetScriptRunFor} seconds {(killed ? "(After timeout we killed the process successfully)" : "(We also failed to kill the process after the timeout expired)")}");
        }

        while (!allErrorDataConsumed || !allOutputDataConsumed)
        {
            Task.Delay(100);

            if(TimeoutExpired(startTime))
                throw new TimeoutException("Timeout expired while waiting for all output streams from the Python process to finish being read");
        }

        lock(this)
            if (_outputDataReceivedExceptions.Any())
                throw _outputDataReceivedExceptions.Count == 1
                    ? _outputDataReceivedExceptions[0]
                    : new AggregateException(_outputDataReceivedExceptions);

        return p.ExitCode;
    }

    private readonly List<Exception> _outputDataReceivedExceptions = new();

    private bool OutputDataReceived(DataReceivedEventArgs e, IDataLoadEventListener listener,bool isErrorStream)
    {
        if(e.Data == null)
            return true;

        lock (this)
        {
            try
            {
                //it has expired the standard out
                listener.OnNotify(this, new NotifyEventArgs(isErrorStream?ProgressEventType.Warning : ProgressEventType.Information, e.Data));
            }
            catch (Exception ex)
            {
                //the notify handler is crashing... let's stop trying to read data from this async handler.  Also add the exception to the list because we don't want it throwing out of this lambda
                _outputDataReceivedExceptions.Add(ex);
                return true;
            }
        }

        return false;
    }
    private bool TimeoutExpired(DateTime startTime)
    {
        if (MaximumNumberOfSecondsToLetScriptRunFor == 0)
            return false;

        return DateTime.Now - startTime > new TimeSpan(0, 0, 0, MaximumNumberOfSecondsToLetScriptRunFor);
    }


    public string GetFullPythonPath()
    {
        return GetPython(Version == PythonVersion.Version2 ? '2' : '3').path;
    }

    [SupportedOSPlatform("windows")]
    private static IEnumerable<(decimal minor, string fullVersion, string path)> GetPythonVersions(RegistryKey? k,char major)
    {
        if (k is null) yield break;

        foreach (var v in k.GetSubKeyNames())
        {
            if (v.Length < 3 || v[0] != major || v[1] != '.' || !decimal.TryParse(v[2..], out var minor))
                continue;

            using var details = k.OpenSubKey(v);
            if (details is null) continue;

            var fullVersion = details.GetValue("Version") ?? v;

            using var pathKey = details.OpenSubKey("InstallPath");
            if (pathKey is null) continue;

            var path = pathKey.GetValue("ExecutablePath")?.ToString() ?? Path.Combine(pathKey.GetValue(null)?.ToString() ?? "DUMMY","python.exe");

            if (File.Exists(path))
                yield return (minor,fullVersion.ToString()??"0.0.0", path);
        }
    }

    private static (decimal minor, string fullVersion, string path) GetPython(char major)
    {
        if (!OperatingSystem.IsWindows()) throw new InvalidOperationException("This Python plugin is Windows only for now");

        using var machine = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Python\\PythonCore");
        using var user = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Python\\PythonCore");
        using var machine32 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Python\\PythonCore");
        using var user32 = Registry.CurrentUser.OpenSubKey("SOFTWARE\\WOW6432Node\\Python\\PythonCore");
        var candidate = GetPythonVersions(machine, major).Union(GetPythonVersions(user, major)).DefaultIfEmpty()
            .MaxBy(static v => v.minor);
        return candidate;
    }

    private string GetExpectedPythonVersion()
    {
        if (Version != PythonVersion.Version2 && Version!=PythonVersion.Version3)
            throw new Exception("Python version not set yet or invalid");

        if (!OperatingSystem.IsWindows()) throw new InvalidOperationException("This Python plugin is Windows only for now");

        var major = Version == PythonVersion.Version2 ? '2' : '3';
        return GetPython(major).fullVersion;
    }
}