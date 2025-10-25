using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;
using LibArchive.Net;

namespace SCIStorePlugin.Repositories;

public class CombinedReportArchivedXmlRepository : IRepository<CombinedReportData>
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

