// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Linq;
using FAnsi.Discovery;
using Rdmp.Core.Curation;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.DataProvider.FromCache;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.DataLoad.Engine.Job.Scheduling;

namespace Rdmp.Dicom.DataProviders;

/// <summary>
/// Retrieves cached DICOM files from the SMI cache for data loading, with scheduled job support and cache cleanup
/// </summary>
[Obsolete("Not clear what this does")]
public class SMICachedFileRetriever : CachedFileRetriever
{
    public override void Initialize(ILoadDirectory hicProjectDirectory, DiscoveredDatabase dbInfo)
    {

    }
    public override ExitCodeType Fetch(IDataLoadJob dataLoadJob, GracefulCancellationToken cancellationToken)
    {
        var scheduledJob = ConvertToScheduledJob(dataLoadJob);

        var jobs = GetDataLoadWorkload(scheduledJob);

        if (!jobs.Any())
            return ExitCodeType.OperationNotRequired;

        ExtractJobs(scheduledJob);

        // for the time being we will not delete files from the cache, need to make this configurable
        scheduledJob.PushForDisposal(new DeleteCachedFilesOperation(scheduledJob, jobs));
        scheduledJob.PushForDisposal(new UpdateProgressIfLoadsuccessful(scheduledJob));

        return ExitCodeType.Success;
    }
}