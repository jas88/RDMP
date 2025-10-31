// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using Rdmp.Core.Caching.Pipeline.Sources;
using Rdmp.Core.Caching.Requests;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;
using System;
using System.Diagnostics;

namespace Rdmp.Dicom.Cache.Pipeline;

/// <summary>
/// Cache source that fetches DICOM images by executing an external process/command with templated start/end dates and output directory
/// </summary>
public class ProcessBasedCacheSource : CacheSource<SMIDataChunk>
{
    [DemandsInitialization(@"Process to start (path only)",Mandatory = true)]
    public string Command {get;set;}

    [DemandsInitialization(@"Arguments to provide to the Process.  Template with 
%s start time
%e end time time to fetch
%d directory to put files fetched
Example:. './GetImages.exe ""%s"" ""%e%""'")]
    public string Args {get;set;}

    [DemandsInitialization("The datetime format for %s and %e.",Mandatory = true,DefaultValue = "yyyy-MM-dd HH:mm:ss")]
    public string TimeFormat {get;set;}

    [DemandsInitialization("True to throw an Exception if the process run returns a nonzero exit code", DefaultValue = true)]
    public bool ThrowOnNonZeroExitCode {get;set;}

    public override void Abort(IDataLoadEventListener listener)
    {

    }

    public override void Check(ICheckNotifier notifier)
    {

    }

    public override void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
    {

    }

    public override SMIDataChunk DoGetChunk(ICacheFetchRequest request, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
    {
        listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information,$"ProcessBasedCacheSource version is {typeof(ProcessBasedCacheSource).Assembly.GetName().Version}.  Assembly is {typeof(ProcessBasedCacheSource).Assembly} " ));

        // Where we are putting the files
        var cacheDir = new System.IO.DirectoryInfo(Request.CacheProgress.LoadProgress.LoadMetadata.LocationOfCacheDirectory);
        var cacheLayout = new SMICacheLayout(cacheDir, new SMICachePathResolver("ALL"));

        Chunk = new SMIDataChunk(Request)
        {
            FetchDate = Request.Start,
            Modality = "ALL",
            Layout = cacheLayout
        };

        var workingDirectory = cacheLayout.GetLoadCacheDirectory(listener);

        listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information,
            $"Working directory is:{workingDirectory}"));
        listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information, $"Fetch Start is:{request.Start}"));
        listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information, $"Fetch End is:{request.End}"));

        listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information, $"Command is:{Command}"));
        listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information, $"Args template is:{Args}"));
        listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information,
            $"Datetime format is:{TimeFormat}"));


        var args = Args
            .Replace("%s",request.Start.ToString(TimeFormat))
            .Replace("%e",request.End.ToString(TimeFormat))
            .Replace("%d",workingDirectory.FullName);

        listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information, $"Args resolved is:{args}"));

        using(var p = new Process())
        {
            p.StartInfo.FileName = Command;
            p.StartInfo.Arguments = args;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.OutputDataReceived += (sender, a) => listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information,a.Data));

            p.Start();
            p.BeginOutputReadLine();

            p.WaitForExit();

            listener.OnNotify(this,new NotifyEventArgs( p.ExitCode == 0 ? ProgressEventType.Information : ProgressEventType.Warning ,
                $"Process exited with code {p.ExitCode}"));

            if(p.ExitCode != 0 && ThrowOnNonZeroExitCode)
                throw new Exception($"Process exited with code {p.ExitCode}");
        }

        return Chunk;
    }

    public override SMIDataChunk TryGetPreview()
    {
        return null;
    }
}