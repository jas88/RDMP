// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

/*
 * From http://www.techmikael.com/2009/07/removing-exif-data-continued.html
 * */

using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace DrsPlugin.Attachers;

public class JpegPatcher : IImagePatcher
{
    public Stream PatchAwayExif(Stream inStream, Stream outStream)
    {
        // Load image using ImageSharp which automatically strips metadata when saving
        using var image = Image.Load(inStream);

        // Save as JPEG without metadata
        var encoder = new JpegEncoder();
        image.Save(outStream, encoder);

        return outStream;
    }

    public byte[] ReadPixelData(Stream stream)
    {
        // Load the image and convert to byte array
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public string GetFileExtension()
    {
        return ".jpeg";
    }
}