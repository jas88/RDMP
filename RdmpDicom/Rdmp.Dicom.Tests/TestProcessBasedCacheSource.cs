// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using NUnit.Framework;
using Rdmp.Core.Caching.Requests.FetchRequestProvider;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Cache;
using Rdmp.Dicom.Cache.Pipeline;
using Rdmp.Core.ReusableLibraryCode.Progress;
using System;
using System.IO;
using System.Linq;
using Tests.Common;

namespace Rdmp.Dicom.Tests;

class TestProcessBasedCacheSource : UnitTests
{
    [Test]
    public void TestWithEcho()
    {
        var source = new ProcessBasedCacheSource();

        if (IsLinux)
        {
            source.Command = "/bin/echo";
            source.Args = "Hey Thomas go get %s and store in %d";
        }
        else
        {
            source.Command = "cmd.exe";
            source.Args = "/c echo Hey Thomas go get %s and store in %d";
        }

        source.TimeFormat = "dd/MM/yy";
        source.ThrowOnNonZeroExitCode = true;

        // What dates to load
        var cp = WhenIHaveA<CacheProgress>();
        cp.CacheFillProgress = new DateTime(2001, 12, 24);
        cp.SaveToDatabase();

        // Where to put files
        var lmd = cp.LoadProgress.LoadMetadata;

        var dir = new DirectoryInfo(TestContext.CurrentContext.WorkDirectory);
        var loadDir = LoadDirectory.CreateDirectoryStructure(dir, "blah", true);

        lmd.LocationOfForLoadingDirectory = loadDir.ForLoading.FullName;
        lmd.LocationOfForArchivingDirectory = loadDir.ForArchiving.FullName;
        lmd.LocationOfExecutablesDirectory = loadDir.ExecutablesPath.FullName;
        lmd.LocationOfCacheDirectory = loadDir.Cache.FullName;
        lmd.SaveToDatabase();

        source.PreInitialize(new CacheFetchRequestProvider(cp), ThrowImmediatelyDataLoadEventListener.Quiet);
        source.PreInitialize(cp.CatalogueRepository, ThrowImmediatelyDataLoadEventListener.Quiet);
        source.PreInitialize(new PermissionWindow(cp.CatalogueRepository), ThrowImmediatelyDataLoadEventListener.Quiet);

        var toMem = new ToMemoryDataLoadEventListener(true);
        var fork = new ForkDataLoadEventListener(toMem, ThrowImmediatelyDataLoadEventListener.Quiet);
        source.GetChunk(fork, new());

        Assert.That(
            toMem.GetAllMessagesByProgressEventType()[ProgressEventType.Information].Select(static v => v.Message)
                .ToArray(),
            Does.Contain($"Hey Thomas go get 24/12/01 and store in {Path.Combine(loadDir.Cache.FullName, "ALL")}"));
    }

    private static bool IsLinux => Environment.OSVersion.Platform != PlatformID.Win32NT;
}