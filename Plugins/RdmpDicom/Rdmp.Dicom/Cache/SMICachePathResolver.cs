// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using Rdmp.Core.Caching.Layouts;
using System.IO;

namespace Rdmp.Dicom.Cache;

/// <summary>
/// Resolves cache directory paths for DICOM images based on modality (e.g., CT, MR), creating modality-specific subdirectories
/// </summary>
public class SMICachePathResolver : ILoadCachePathResolver
{
    public string Modality { get; }

    public SMICachePathResolver(string modality)
    {
        Modality = modality;
    }

    public DirectoryInfo GetLoadCacheDirectory(DirectoryInfo rootDirectory)
    {
        var directoryInfo = new DirectoryInfo(Path.Combine(rootDirectory.FullName, Modality));
        return directoryInfo.Exists ? directoryInfo : Directory.CreateDirectory(directoryInfo.FullName);
    }
}