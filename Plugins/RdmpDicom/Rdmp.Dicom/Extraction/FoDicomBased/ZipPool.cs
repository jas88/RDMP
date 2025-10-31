// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using LibArchive.Net;

namespace Rdmp.Dicom.Extraction.FoDicomBased;

/// <summary>
/// Holds multiple ZipArchive open at once to prevent repeated opening and closing
/// </summary>
public class ZipPool:IDisposable
{
    private int _cacheMisses,_opens;
    public int CacheMisses => _cacheMisses;
    public int CacheHits => _opens - _cacheMisses;

    private readonly ConcurrentDictionary<string, LibArchiveReader> _openZipFiles = new();    // No assuming FS is case-insensitive!

    /// <summary>
    /// The maximum number of zip files to allow to be open at once.  Defaults to 5
    /// </summary>
    private int MaxPoolSize => 5;

    public void Dispose()
    {
        ClearCache();
        GC.SuppressFinalize(this);
    }

    private void ClearCache()
    {
        foreach (var entry in _openZipFiles)
        {
            if (_openZipFiles.TryRemove(entry))
                entry.Value.Dispose();
        }
    }

    public LibArchiveReader OpenRead(string zipPath)
    {
        var key = NormalizePath(zipPath);
        if (_openZipFiles.Count>MaxPoolSize)
            ClearCache();
        Interlocked.Increment(ref _opens);
        return _openZipFiles.GetOrAdd(key, key =>
        {
            Interlocked.Increment(ref _cacheMisses);
            return new LibArchiveReader(key);
        });
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(new Uri(path).LocalPath)
            .TrimEnd('\\','/');
    }

}