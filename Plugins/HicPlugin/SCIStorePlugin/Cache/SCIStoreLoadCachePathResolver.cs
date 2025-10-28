// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using Rdmp.Core.Caching.Layouts;
using SCIStorePlugin.Data;

namespace SCIStorePlugin.Cache;

/// <summary>
/// Resolves cache file paths and filenames for SCIStore reports based on healthboard and discipline, generating consistent directory structures and unique filenames from lab numbers and test report IDs.
/// </summary>
// Implements specific logic for creating paths/archives
// Dependent on the specific type of chunk that is being saved. (I think this makes sense as the cache chunk should contain all the information required to determine the correct filepaths in the archive)
public class SCIStoreLoadCachePathResolver : ILoadCachePathResolver
{
    private readonly HealthBoard _healthBoard;
    private readonly Discipline _discipline;

    public SCIStoreLoadCachePathResolver(HealthBoard healthboard, Discipline discipline)
    {
        _healthBoard = healthboard;
        _discipline = discipline;
    }

    public static string GetFilename(CombinedReportData report)
    {
        if (report.SciStoreRecord == null)
            throw new Exception("This CombinedReportData does not have a SciStoreRecord object, which is required for recovering LabNumber and TestReportID");

        var record = report.SciStoreRecord;
        if (string.IsNullOrWhiteSpace(record.LabNumber))
            throw new Exception("This report has no LabNumber (check construction)");

        if (string.IsNullOrWhiteSpace(record.TestReportID))
            throw new Exception("This report has no TestReportID (check construction)");

        return $"report-{report.SciStoreRecord.LabNumber}-{report.SciStoreRecord.TestReportID}.xml";
    }

    public DirectoryInfo GetLoadCacheDirectory(DirectoryInfo rootDirectory)
    {
        var downloadDirectory = new DirectoryInfo(Path.Combine(rootDirectory.FullName, GetRelativePath()));
        return downloadDirectory.Exists ? downloadDirectory : Directory.CreateDirectory(downloadDirectory.FullName);
    }

    private string GetRelativePath()
    {
        return Path.Combine(_healthBoard.ToString(), _discipline.ToString());
    }
}