// Copyright (c) The University of Dundee 2018-2024
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Buffers.Binary;

namespace Rdmp.Core.Icons;

/// <summary>
/// Minimal reader for the fixed-position parts of a PNG file header (signature and IHDR chunk) so that PNG data can be
/// validated and measured without an image library.
/// </summary>
public static class PngHeader
{
    private static ReadOnlySpan<byte> Signature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static ReadOnlySpan<byte> IhdrType => "IHDR"u8;

    /// <summary>
    /// The PNG specification fixes the IHDR chunk's data length at 13 bytes
    /// </summary>
    private const int IhdrLength = 13;

    /// <summary>
    /// The minimum number of bytes needed to read the signature and image dimensions
    /// </summary>
    public const int MinimumLength = 24;

    /// <summary>
    /// Returns true if <paramref name="data"/> starts with the PNG signature followed by an IHDR chunk of the correct length
    /// </summary>
    public static bool IsPng(ReadOnlySpan<byte> data) =>
        data.Length >= MinimumLength && data[..8].SequenceEqual(Signature) &&
        BinaryPrimitives.ReadInt32BigEndian(data.Slice(8, 4)) == IhdrLength && data.Slice(12, 4).SequenceEqual(IhdrType);

    /// <summary>
    /// Reads the pixel width and height from the IHDR chunk of PNG <paramref name="data"/> (bytes 16-23, big-endian)
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if <paramref name="data"/> is not PNG data</exception>
    public static (int Width, int Height) GetDimensions(ReadOnlySpan<byte> data)
    {
        if (!IsPng(data))
            throw new ArgumentException("Data is not a PNG image (missing PNG signature or IHDR chunk)", nameof(data));

        var width = BinaryPrimitives.ReadInt32BigEndian(data.Slice(16, 4));
        var height = BinaryPrimitives.ReadInt32BigEndian(data.Slice(20, 4));

        if (width <= 0 || height <= 0)
            throw new ArgumentException($"PNG has invalid dimensions {width}x{height}", nameof(data));

        return (width, height);
    }
}
