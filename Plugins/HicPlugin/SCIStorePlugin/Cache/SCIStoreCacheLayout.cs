// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Linq;
using Rdmp.Core.Caching.Layouts;
using Rdmp.Core.Caching.Pipeline.Destinations;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace SCIStorePlugin.Cache;

/// <summary>
/// Cache layout implementation for SCIStore data organized by healthboard and discipline, managing daily ZIP archives and cleanup of temporary XML files.
/// </summary>
public class SCIStoreCacheLayout : CacheLayout
{

    private readonly RootHistoryDirectory _rootHistoryDirectory;

    public SCIStoreCacheLayout(DirectoryInfo cacheDirectory, SCIStoreLoadCachePathResolver resolver): base(cacheDirectory, "yyyy-MM-dd", CacheArchiveType.Zip, CacheFileGranularity.Day, resolver)
    {
        _rootHistoryDirectory = new RootHistoryDirectory(cacheDirectory);
    }

    public void Cleanup()
    {
        _rootHistoryDirectory.CleanupLingeringXMLFiles();
    }

    public void CreateArchive(DateTime archiveDate)
    {
        var downloadDirectory = GetLoadCacheDirectory(ThrowImmediatelyDataLoadEventListener.Quiet);
        var dataFiles = downloadDirectory.EnumerateFiles("*.xml").ToArray();
        ArchiveFiles(dataFiles, archiveDate,ThrowImmediatelyDataLoadEventListener.Quiet);
        Cleanup();
    }

    public void ValidateLayout()
    {
        // todo: ask rootHistoryDirectory to validate the structure (actually, rootHistoryDirectory functionality is closely-related to CacheLayout)
        _rootHistoryDirectory.Validate();
    }
}