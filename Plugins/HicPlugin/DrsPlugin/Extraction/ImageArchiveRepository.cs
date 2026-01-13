// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using Rdmp.Core.ReusableLibraryCode.Annotations;

namespace DrsPlugin.Extraction;

/// <summary>
/// This class wraps functionality for interacting with the image archive used across loading and extraction
/// </summary>
internal static class ImageArchiveRepository
{
    /// <summary>
    /// Extracts a set of images from one archive. Looks for entries named after the keys in extractionMap and saves them to the path given in the map's corresponding value.
    /// </summary>
    /// <param name="archiveFilePath">Path to the file from which to extract the images</param>
    /// <param name="extractionMap">Map of entry names in the archive to full output path</param>
    internal static void ExtractImageSetFromArchive([NotNull] string archiveFilePath, [NotNull] Dictionary<string, string> extractionMap)
    {
        using var archive = new LibArchive.Net.LibArchiveReader(archiveFilePath);
        foreach(var entry in archive.Entries())
        {
            if (extractionMap.TryGetValue(entry.Name,out var destination))
            {
                using var outputStream = new FileStream(destination, FileMode.CreateNew);
                using var source=entry.Stream;
                source.CopyTo(outputStream);
                extractionMap.Remove(entry.Name);
            }
            if (extractionMap.Count == 0)
                return;
        }
        // Check that we found all the entries
        if (extractionMap.Count != 0)
            throw new InvalidOperationException(
                $"The following entries were not found in {archiveFilePath}: {string.Join(", ", extractionMap.Keys)}");
    }
}