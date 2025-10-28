// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using FAnsi.Discovery;
using Rdmp.Core.Curation;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.DataProvider.FromCache;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.DataLoad.Engine.Job.Scheduling;

namespace SCIStorePlugin.DataProvider;

/// <summary>
/// Cached file retriever for SCI Store data that extends CachedFileRetriever to provide scheduled data load job integration.
/// </summary>
public class SCIStoreCachedFileRetriever : CachedFileRetriever
{
    private ScheduledDataLoadJob _scheduledJob;

    public override void Initialize(ILoadDirectory hicProjectDirectory, DiscoveredDatabase dbInfo)
    {
            
    }

    public override ExitCodeType Fetch(IDataLoadJob dataLoadJob, GracefulCancellationToken cancellationToken)
    {
        _scheduledJob = ConvertToScheduledJob(dataLoadJob);

        var jobs = GetDataLoadWorkload(_scheduledJob);

        ExtractJobs(_scheduledJob);

        _scheduledJob.PushForDisposal(new DeleteCachedFilesOperation(_scheduledJob, jobs));
        _scheduledJob.PushForDisposal(new UpdateProgressIfLoadsuccessful(_scheduledJob));

        return ExitCodeType.Success;
    }
}