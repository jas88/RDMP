// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System.Data;
using System.Linq;
using Rdmp.Core.ReusableLibraryCode.Checks;
using FAnsi.Discovery;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Rdmp.Core.DataLoad;
using Rdmp.Core.Curation;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad.Engine.Attachers;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataLoad.Modules.DataFlowSources;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.DataFlowPipeline.Requirements;

namespace HICPlugin.BespokeAttachers;

/// <summary>
/// Bespoke attacher for loading MetIDQ (Metabolomics) data files during the data load process
/// </summary>
public class MetIDQAttacher : IPluginAttacher
{
    [DemandsInitialization("File pattern to load")]
    public string FilePattern { get; set; }



    public void LoadCompletedSoDispose(ExitCodeType exitCode, IDataLoadEventListener postLoadEventsListener)
    {
            
    }

    public bool DisposeImmediately { get; set; }
    public void Check(ICheckNotifier notifier)
    {
            
    }

    public ExitCodeType Attach(IDataLoadJob job, GracefulCancellationToken token)
    {
        foreach (var file in job.LoadDirectory.ForLoading.GetFiles(FilePattern))
        {
            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"Started processing file {file.FullName}"));

            DelimitedFlatFileDataFlowSource fromCSV = new()
            {
                //Read it all in one go
                MaxBatchSize = int.MaxValue
            };
            fromCSV.PreInitialize(new FlatFileToLoad(file),job);

            fromCSV.GetChunk(job, new GracefulCancellationToken());

            var dt = fromCSV.GetChunk(job, new GracefulCancellationToken());

            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, $"{dt.Rows[0][1]}"));

            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"Found the following headers:{string.Join(",", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}"));
        }

        return ExitCodeType.Error;
    }

    public void Initialize(ILoadDirectory hicProjectDirectory, DiscoveredDatabase dbInfo)
    {
            
    }

    public ILoadDirectory LoadDirectory { get; set; }
    public bool RequestsExternalDatabaseCreation { get; private set; }
}