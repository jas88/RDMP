// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;

namespace DrsPlugin.Attachers;

/// <summary>
/// Image patcher for PNG files that performs a simple stream copy (PNG files typically don't contain sensitive EXIF data)
/// </summary>
public class PngPatcher : IImagePatcher
{
    public Stream PatchAwayExif(Stream inStream, Stream outStream)
    {
        inStream.CopyTo(outStream);
        return outStream;
    }

    public byte[] ReadPixelData(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public string GetFileExtension()
    {
        return ".png";
    }
}