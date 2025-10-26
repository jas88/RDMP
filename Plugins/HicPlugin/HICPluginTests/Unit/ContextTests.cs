// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using HICPluginTests;
using NUnit.Framework;
using Rdmp.Core.Caching.Pipeline;
using Rdmp.Core.Caching.Requests;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.DataFlowPipeline.Requirements;
using SCIStorePlugin.Cache.Pipeline;
using System.IO;
using Tests.Common;

namespace SCIStorePluginTests.Unit;

public class ContextTests : DatabaseTests
{
    [Test]
    public void Context_LegalSource()
    {
        var testDirHelper = new TestDirectoryHelper(GetType());
        testDirHelper.SetUp();

        var projDir = LoadDirectory.CreateDirectoryStructure(testDirHelper.Directory, "Test", true);
        var lmd = new LoadMetadata(CatalogueRepository)
        {
            LocationOfForLoadingDirectory = Path.Join(projDir.RootPath.FullName, "Data", "ForLoading"),
            LocationOfForArchivingDirectory = Path.Join(projDir.RootPath.FullName, "Data", "ForArchiving"),
            LocationOfCacheDirectory = Path.Join(projDir.RootPath.FullName, "Cahce"),
            LocationOfExecutablesDirectory = Path.Join(projDir.RootPath.FullName, "Executables"),
        };
        lmd.SaveToDatabase();

        var cp = new MockCacheProgress(lmd);

        var provider = new MockCacheFetchRequestProvider();

        var useCase = new CachingPipelineUseCase(cp, true, provider);
        var cacheContext = (DataFlowPipelineContext<ICacheChunk>)useCase.GetContext();

        //we shouldn't be able to have data export sources in this context
        Assert.That(cacheContext.IsAllowable(typeof(SCIStoreWebServiceSource)), Is.True);
    }
}