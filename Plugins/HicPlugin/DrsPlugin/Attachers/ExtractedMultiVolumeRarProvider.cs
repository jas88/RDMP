// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using Rdmp.Core.ReusableLibraryCode.Progress;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DrsPlugin.Attachers;

/// <summary>
/// This version extracts the files from the archive first - should provide performance benefits for large archives
/// </summary>
public class ExtractedMultiVolumeRarProvider : IArchiveProvider, IDisposable
{
    private readonly string _archiveDirectory;
    private string _fileDirectory;
    private readonly IDataLoadEventListener _listener;

    private bool _isExtracted;
    private bool _doNotDeleteFileDirectory;

    public ExtractedMultiVolumeRarProvider(string archiveDirectory, IDataLoadEventListener listener)
    {
        _archiveDirectory = archiveDirectory;
        _listener = listener;
    }

    public void UseExistingExtractionDirectory(string fileDirectory)
    {
        _fileDirectory = fileDirectory;
        if (!Directory.Exists(_fileDirectory))
            throw new InvalidOperationException($"The directory {_fileDirectory} does not exist.");

        _isExtracted = true;
        _doNotDeleteFileDirectory = true; // would be a bit anti-social to remove a directory that we don't control the lifecycle of
    }

    private void ExtractFiles()
    {
        if (_isExtracted) return;

        _fileDirectory = Path.Combine(_archiveDirectory, Path.GetRandomFileName());
        _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Creating a temporary directory for archive extraction: {_fileDirectory}"));
        Directory.CreateDirectory(_fileDirectory);

        RarHelper.ExtractMultiVolumeArchive(_archiveDirectory, _fileDirectory);
        _isExtracted = true;
    }

    public MemoryStream GetEntry(string entryName)
    {
        if (!_isExtracted)
            ExtractFiles();

        var filepath = Path.Combine(_fileDirectory, entryName);
        if (!File.Exists(filepath))
            throw new InvalidOperationException($"Could not find the entry: {entryName}");

        using var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);
        return ReadMemoryStreamFromStream(fs);
    }

    private static MemoryStream ReadMemoryStreamFromStream(Stream stream)
    {
        var outputStream = new MemoryStream();
        stream.CopyTo(outputStream);
        outputStream.Seek(0, SeekOrigin.Begin);
        return outputStream;
    }

    public int GetNumEntries()
    {
        if (!_isExtracted)
            ExtractFiles();

        return Directory.EnumerateFiles(_fileDirectory).Count();
    }

    public IEnumerable<KeyValuePair<string, MemoryStream>> EntryStreams
    {
        get
        {
            if (!_isExtracted)
                ExtractFiles();

            foreach (var entryName in Directory.EnumerateFiles(_fileDirectory).Select(Path.GetFileName))
            {
                yield return new KeyValuePair<string, MemoryStream>(entryName, GetEntry(entryName));
            }
        }
    }

    public IEnumerable<string> EntryNames
    {
        get
        {
            if (!_isExtracted)
                ExtractFiles();

            return Directory.EnumerateFiles(_fileDirectory);
        }
    }

    public string Name => $"Multi-volume archive at {_archiveDirectory}";

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (!_doNotDeleteFileDirectory)
            Directory.Delete(_fileDirectory, true);

        _isExtracted = false;
    }
}