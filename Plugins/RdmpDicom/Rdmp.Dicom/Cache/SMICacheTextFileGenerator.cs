// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System.IO;
using System.Linq;
using System.Text;
using FAnsi.Discovery;
using Rdmp.Core.Curation;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.DataProvider.FromCache;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.DataLoad.Engine.Job.Scheduling;

namespace Rdmp.Dicom.Cache;

/// <summary>
/// Looks in the cache folder and generates a file "LoadMe.txt" which lists all the zip files matching the
/// fetch date
/// </summary>
class SMICacheTextFileGenerator:CachedFileRetriever
{
    private DirectoryInfo _forLoading;

    public override void Initialize(ILoadDirectory hicProjectDirectory, DiscoveredDatabase dbInfo)
    {
        _forLoading = hicProjectDirectory.ForLoading;
    }

    public override ExitCodeType Fetch(IDataLoadJob dataLoadJob, GracefulCancellationToken cancellationToken)
    {

        var scheduledJob = ConvertToScheduledJob(dataLoadJob);

        var jobs = GetDataLoadWorkload(scheduledJob);

        if (!jobs.Any())
            return ExitCodeType.OperationNotRequired;


        StringBuilder sb = new();

        foreach (var file in jobs.Values)
            sb.AppendLine(file.FullName);

        File.WriteAllText(Path.Combine(_forLoading.FullName, "LoadMe.txt"), sb.ToString());

        scheduledJob.PushForDisposal(new UpdateProgressIfLoadsuccessful(scheduledJob));
        return ExitCodeType.Success;
    }

}