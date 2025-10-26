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

namespace DrsPlugin.Attachers;

/// <summary>
/// Factory for creating cached patcher instances
/// </summary>
internal static class CachedPatcherFactory
{
    private static readonly JpegPatcher JpegPatcher = new();
    private static readonly PngPatcher PngPatcher = new();

    internal static IImagePatcher Create(string extension)
    {
        return extension switch
        {
            ".jpg" => JpegPatcher,
            ".jpeg" => JpegPatcher,
            ".png" => PngPatcher,
            _ => throw new InvalidOperationException(
                $"Can't create a patcher for images with extension type: {extension}")
        };
    }
}

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
}

public class JpegPatcher : IImagePatcher
{
    public byte[] ReadPixelData(Stream stream)
    {
        if (!CheckIsJpegFile(stream))
            throw new InvalidOperationException("This is not a jpeg file");

        SkipAppHeaderSection(stream);

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static bool CheckIsJpegFile(Stream stream)
    {
        var jpegHeader = new byte[2];
        jpegHeader[0] = (byte)stream.ReadByte();
        jpegHeader[1] = (byte)stream.ReadByte();
        return jpegHeader[0] == 0xff && jpegHeader[1] == 0xd8;
    }

    public Stream PatchAwayExif(Stream inStream, Stream outStream)
    {
        if (CheckIsJpegFile(inStream))
        {
            SkipAppHeaderSection(inStream);
        }
        outStream.WriteByte(0xff);
        outStream.WriteByte(0xd8);

        inStream.CopyTo(outStream);
        return outStream;
    }

    private static void SkipAppHeaderSection(Stream inStream)
    {
        var header = new byte[2];
        header[0] = (byte)inStream.ReadByte();
        header[1] = (byte)inStream.ReadByte();

        while (header[0] == 0xff && header[1] >= 0xe0 && header[1] <= 0xef)
        {
            var exifLength = inStream.ReadByte();
            exifLength <<= 8;
            exifLength |= inStream.ReadByte();

            for (var i = 0; i < exifLength - 2; i++)
            {
                inStream.ReadByte();
            }
            header[0] = (byte)inStream.ReadByte();
            header[1] = (byte)inStream.ReadByte();
        }
        inStream.Position -= 2; //skip back two bytes
    }
}