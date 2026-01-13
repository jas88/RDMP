// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.IO;
using NUnit.Framework;
using Rdmp.Core.Caching.Requests;
using Rdmp.Core.Curation;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStore.SciStoreServices81;
using SCIStorePlugin;
using SCIStorePlugin.Cache.Pipeline;
using SCIStorePlugin.Data;
using Tests.Common;
using NSubstitute;

namespace HICPluginTests.Integration;

public class SCIStoreCacheDestinationTests : DatabaseTests
{
    private TestDirectoryHelper _directoryHelper;

    [OneTimeSetUp]
    protected override void SetUp()
    {
        base.SetUp();

        _directoryHelper = new TestDirectoryHelper(GetType());
        _directoryHelper.SetUp();
    }

    [OneTimeTearDown]
    public new void TearDown()
    {
        _directoryHelper.TearDown();
        base.TearDown();
    }

    /// <summary>
    /// Simple test to determine whether the cache destination component correctly creates an archived zip file given dummy data in a cache chunk
    /// </summary>
    [Test]
    public void ProcessPipelineDataTest()
    {
        var rootDirectory = _directoryHelper.Directory.CreateSubdirectory("ProcessPipelineDataTest");
        var component = new SCIStoreCacheDestination
        {
            HealthBoard = HealthBoard.T,
            Discipline =  Discipline.Biochemistry,
            CacheDirectory = rootDirectory,
            SilentRunning = true,
        };
            
        // this would be provided by a previous component in the caching data flow pipeline
        var report = new CombinedReportData
        {
            HbExtract = HealthBoard.T.ToString(),
            SciStoreRecord = new SciStoreRecord
            {
                LabNumber = "123456",
                TestReportID = "999"
            },
            InvestigationReport = new InvestigationReport
            {
                ReportData = new InvestigationReportMessageType
                {

                }
            }
        };
        var fetchDate = new DateTime(2015, 1, 1);
     
            
        var deleteMe = LoadDirectory.CreateDirectoryStructure(new DirectoryInfo(TestContext.CurrentContext.WorkDirectory),"DeleteMe", true);
        try
        {
            var fetchRequest = Substitute.For<ICacheFetchRequest>();

            var cacheChunk = new SCIStoreCacheChunk(new[] { report }, fetchDate, fetchRequest)
            {
                HealthBoard = HealthBoard.T,
                Discipline = Discipline.Biochemistry
            };


            component.PreInitialize(deleteMe,ThrowImmediatelyDataLoadEventListener.Quiet);
            component.ProcessPipelineData((ICacheChunk)cacheChunk, ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken());

            var downloadDir = Path.Combine(rootDirectory.FullName, "T", "Biochemistry");
            var expectedArchiveFilepath = Path.Combine(downloadDir, "2015-01-01.zip");
            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(expectedArchiveFilepath), Is.True);

                // make sure that the archiver has cleaned up after itself
                Assert.That(Directory.EnumerateFiles(downloadDir, "*.xml"), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(deleteMe.RootPath.FullName, true);
        }
    }

}