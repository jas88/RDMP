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
using System.Threading.Tasks;

namespace DrsPlugin.Attachers;

/// <summary>
/// Archive provider that reads image files directly from the filesystem directory structure, with retry logic for network reliability
/// </summary>
public class FilesystemArchiveProvider : IArchiveProvider
{
    private readonly string _rootPath;
    private readonly string[] _fileExtensions;
    private readonly IDataLoadEventListener _listener;
    private string[] _fileList;

    public FilesystemArchiveProvider(string rootPath, string[] fileExtensions, IDataLoadEventListener listener)
    {
        if (fileExtensions.Any(static ext => !ext.StartsWith(".", StringComparison.Ordinal)))
            throw new ArgumentException("Please ensure the extensions start with '.', i.e. '.txt'");

        _rootPath = rootPath;
        _fileExtensions = fileExtensions;
        _listener = listener;

        Name = "What is this for again?";
    }

    private void CreateFileList()
    {
        _fileList = _fileExtensions
            .SelectMany(
                fileExtension => Directory.GetFiles(_rootPath, $"*{fileExtension}", SearchOption.AllDirectories))
            .ToArray();
    }

    public MemoryStream GetEntry(string entryName)
    {
        if (_fileList == null)
            CreateFileList();

        // Find the file
        var entry = _fileList?.SingleOrDefault(p => Path.GetFileName(p) == entryName) ?? throw new InvalidOperationException($"Could not find file {entryName} in {_rootPath} or subdirectories");
        return new MemoryStream(File.ReadAllBytes(entry));
    }

    public int GetNumEntries()
    {
        if (_fileList == null)
            CreateFileList();

        return _fileList?.Length??0;
    }

    public IEnumerable<KeyValuePair<string, MemoryStream>> EntryStreams
    {
        get
        {
            if (_fileList == null)
                CreateFileList();

            foreach (var path in _fileList)
            {
                MemoryStream memoryStream;
                try
                {
                    memoryStream = new MemoryStream(File.ReadAllBytes(path));
                }
                catch (Exception e)
                {
                    _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                        $"First attempt to load file failed: {path}. Will try again in 30 seconds.", e));

                    // The network is potentially temporarily unavailable, back off a bit and then retry
                    // If we fail again then just let the exception bubble up
                    Task.Delay(30000).Wait();
                    memoryStream = new MemoryStream(File.ReadAllBytes(path));
                }

                yield return new KeyValuePair<string, MemoryStream>(Path.GetFileName(path), memoryStream);
            }
        }
    }

    public IEnumerable<string> EntryNames
    {
        get
        {
            if (_fileList == null)
                CreateFileList();

            return _fileList?.Select(Path.GetFileName);
        }
    }

    public string Name { get; }
}