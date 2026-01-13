// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using SharpCompress.Archives;
using System;
using System.IO;
using System.Linq;

namespace DrsPlugin.Attachers;

public static class RarHelper
{
    public static void ExtractMultiVolumeArchive(DirectoryInfo sourceDir, string destinationDir = null)
    {
        ExtractMultiVolumeArchive(sourceDir.FullName, destinationDir);
    }

    public static void ExtractMultiVolumeArchive(string sourceDir, string destinationDir = null)
    {
        destinationDir ??= sourceDir;
        var firstVolume = Directory.EnumerateFiles(sourceDir, "*.rar").FirstOrDefault() ?? throw new InvalidOperationException($"No RAR files found in {sourceDir}");
        ArchiveFactory.WriteToDirectory(firstVolume, destinationDir);
    }
}