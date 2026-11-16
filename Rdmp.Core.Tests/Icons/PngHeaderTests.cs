// Copyright (c) The University of Dundee 2018-2024
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using NUnit.Framework;
using Rdmp.Core.Icons;

namespace Rdmp.Core.Tests.Icons;

internal class PngHeaderTests
{
    /// <summary>
    /// Builds the first 24 bytes of a PNG: signature, IHDR length (13), "IHDR", width, height (big-endian)
    /// </summary>
    private static byte[] Header(int width, int height) =>
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0, 0, 0, 13,
        (byte)'I', (byte)'H', (byte)'D', (byte)'R',
        (byte)(width >> 24), (byte)(width >> 16), (byte)(width >> 8), (byte)width,
        (byte)(height >> 24), (byte)(height >> 16), (byte)(height >> 8), (byte)height
    ];

    [TestCase(19, 19)]
    [TestCase(20, 19)]
    [TestCase(800, 600)]
    [TestCase(70000, 3)]
    public void GetDimensions_ReadsBigEndianWidthAndHeight(int width, int height)
    {
        Assert.That(PngHeader.GetDimensions(Header(width, height)), Is.EqualTo((width, height)));
    }

    [Test]
    public void IsPng_TrueForValidHeader()
    {
        Assert.That(PngHeader.IsPng(Header(1, 1)));
    }

    [Test]
    public void IsPng_FalseForTooShort()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PngHeader.IsPng(Header(1, 1).AsSpan(0, PngHeader.MinimumLength - 1)), Is.False);
            Assert.That(PngHeader.IsPng([]), Is.False);
        });
    }

    [Test]
    public void IsPng_FalseForBmp()
    {
        // a Windows BMP starts "BM"
        var bmp = new byte[64];
        bmp[0] = (byte)'B';
        bmp[1] = (byte)'M';
        Assert.That(PngHeader.IsPng(bmp), Is.False);
    }

    [Test]
    public void IsPng_FalseWhenFirstChunkIsNotIhdr()
    {
        var data = Header(1, 1);
        data[12] = (byte)'X';
        Assert.That(PngHeader.IsPng(data), Is.False);
    }

    [Test]
    public void GetDimensions_ThrowsForNonPng()
    {
        Assert.Throws<ArgumentException>(() => PngHeader.GetDimensions(new byte[64]));
    }

    [Test]
    public void GetDimensions_ThrowsForZeroOrNegativeSize()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => PngHeader.GetDimensions(Header(0, 5)));
            Assert.Throws<ArgumentException>(() => PngHeader.GetDimensions(Header(-1, 5)));
        });
    }
}
