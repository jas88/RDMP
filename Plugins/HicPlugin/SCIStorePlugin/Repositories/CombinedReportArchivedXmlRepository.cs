// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;
using LibArchive.Net;

namespace SCIStorePlugin.Repositories;

/// <summary>
/// Repository for reading combined laboratory reports from ZIP archives containing XML files
/// </summary>
public class CombinedReportArchivedXmlRepository : ISciStoreRepository<CombinedReportData>
{
    private readonly DirectoryInfo _sourceDirectory;

    public CombinedReportArchivedXmlRepository(DirectoryInfo sourceDirectory)
    {
        _sourceDirectory = sourceDirectory;
    }

    public IEnumerable<CombinedReportData> ReadAll()
    {
        var reports = new List<CombinedReportData>();

        if (_sourceDirectory.EnumerateFiles().Any(info => !info.Extension.Equals(".zip")))
            throw new Exception(
                $"There are some non-zip files in '{_sourceDirectory.FullName}'. This directory should *only* contain zip archives.");

        var archives = _sourceDirectory.EnumerateFiles("*.zip", SearchOption.AllDirectories);

        var deserializer = new CombinedReportXmlDeserializer();
        foreach (var archive in archives)
        {
            try
            {
                // Migrated to libarchive.net for better performance
                using var libArchive = new LibArchiveReader(archive.FullName);
                foreach (var entry in libArchive.Entries())
                {
                    var report = DeserializeFromLibArchiveEntry(entry, deserializer);
                    if (report != null)
                        reports.Add(report);
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error while reading archive: {archive.FullName}", e);
            }
        }

        return reports;
    }

    /// <summary>
    /// Deserialize data directly from libarchive.net entry stream
    /// </summary>
    private static CombinedReportData DeserializeFromLibArchiveEntry(object libArchiveEntry, CombinedReportXmlDeserializer deserializer)
    {
        try
        {
            // Get the stream directly from libarchive entry
            var streamProperty = libArchiveEntry.GetType().GetProperty("Stream");
            if (streamProperty?.GetValue(libArchiveEntry) is Stream stream)
            {
                return deserializer.DeserializeFromStream(stream);
            }
        }
        catch (Exception)
        {
            // Fallback: return null or handle error appropriately
        }

        return null;
    }

    public string FindArchiveContainingReport(CombinedReportData report)
    {
        var archives = _sourceDirectory.EnumerateFiles("*.zip", SearchOption.AllDirectories);

        foreach (var archive in archives)
        {
            // Migrated to libarchive.net for better performance
            using var libArchive = new LibArchiveReader(archive.FullName);
            if (libArchive.Entries().Select(entry => entry.Name.Split(new [] {'-', '.'})).Any(nameParts =>
                nameParts.Length > 2 && nameParts[1].Equals(report.SciStoreRecord.LabNumber) &&
                nameParts[2].Equals(report.SciStoreRecord.TestReportID)))
            {
                return archive.FullName;
            }
        }
        return null;
    }

    public IEnumerable<CombinedReportData> ReadSince(DateTime day)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IEnumerable<CombinedReportData>> ChunkedReadFromDateRange(DateTime start, DateTime end, IDataLoadEventListener job)
    {
        throw new NotImplementedException();
    }

    public void Create(IEnumerable<CombinedReportData> reports, IDataLoadEventListener listener)
    {
        throw new NotImplementedException();
    }
}

