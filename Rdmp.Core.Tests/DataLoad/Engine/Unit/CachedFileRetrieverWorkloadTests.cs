// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using FAnsi.Discovery;
using NSubstitute;
using NUnit.Framework;
using Rdmp.Core.Curation;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.DataProvider.FromCache;
using Rdmp.Core.DataLoad.Engine.Job;

namespace Rdmp.Core.Tests.DataLoad.Engine.Unit;

/// <summary>
/// Tests that <see cref="CachedFileRetriever"/> must know its workload before it retrieves files from the cache
/// </summary>
[Category("Unit")]
public class CachedFileRetrieverWorkloadTests
{
    private sealed class RetrieverThatSkipsTheWorkload : CachedFileRetriever
    {
        public override void Initialize(ILoadDirectory directory, DiscoveredDatabase dbInfo)
        {
        }

        public override ExitCodeType Fetch(IDataLoadJob dataLoadJob, GracefulCancellationToken cancellationToken)
        {
            // GetDataLoadWorkload, which says which files to retrieve, has not been called
            ExtractJobs(dataLoadJob);
            return ExitCodeType.Success;
        }
    }

    [Test]
    public void ExtractJobs_BeforeTheWorkloadIsKnown_Throws()
    {
        // ForLoading is empty, so there are no files in it to compare with the workload
        var forLoading = Directory.CreateDirectory(Path.Join(TestContext.CurrentContext.WorkDirectory,
            nameof(CachedFileRetrieverWorkloadTests), Path.GetRandomFileName()));

        try
        {
            var job = Substitute.For<IDataLoadJob>();
            job.LoadDirectory.ForLoading.Returns(forLoading);

            var ex = Assert.Throws<InvalidOperationException>(() =>
                new RetrieverThatSkipsTheWorkload().Fetch(job, new GracefulCancellationToken()));

            Assert.That(ex?.Message, Does.StartWith("The workload has not been initialised"));
        }
        finally
        {
            forLoading.Delete(true);
        }
    }
}
