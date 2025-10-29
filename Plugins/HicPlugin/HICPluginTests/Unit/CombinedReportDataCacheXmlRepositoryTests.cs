// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStorePlugin;
using SCIStorePlugin.Cache;
using SCIStorePlugin.Data;
using SCIStorePlugin.Repositories;
using System.IO;

namespace HICPluginTests.Unit;

internal class CombinedReportDataCacheXmlRepositoryTests
{
    [Test]
    public void TestPath()
    {
        var dir = new DirectoryInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, "bleh"));
        dir.Create();

        var layout = new SCIStoreCacheLayout( dir,
            new SCIStoreLoadCachePathResolver(HealthBoard.T, Discipline.Biochemistry));

        var repo = new CombinedReportDataCacheXmlRepository(layout);
        repo.Create(new[] { new CombinedReportData{
            HbExtract = "T",
            SciStoreRecord = new SciStoreRecord
            {
                LabNumber = "sdfdsf",
                TestReportID = "fff",
            }}},ThrowImmediatelyDataLoadEventListener.Quiet);
    }
}