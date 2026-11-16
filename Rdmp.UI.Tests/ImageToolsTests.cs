// Copyright (c) The University of Dundee 2018-2024
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using NUnit.Framework;
using Rdmp.Core.Icons;
using Rdmp.Core.Icons.IconOverlays;
using Rdmp.Core.Icons.IconProvision;
using Rdmp.Core.ReusableLibraryCode.Icons.IconProvision;

namespace Rdmp.UI.Tests;

/// <summary>
/// Tests for the System.Drawing rendering of <see cref="RdmpIcon"/> descriptions
/// </summary>
[SupportedOSPlatform("windows7.0")]
internal class ImageToolsTests
{
    private static Bitmap Decode(RdmpIcon icon)
    {
        using var ms = new MemoryStream(EmbeddedIconHelper.GetPngBytes(icon));
        using var raw = new Bitmap(ms);
        return new Bitmap(raw);
    }

    [Test]
    public void ImageToBitmap_PlainIcon_MatchesPng()
    {
        var icon = CatalogueIcons.Catalogue;
        var bmp = icon.ImageToBitmap();
        using var expected = Decode(icon);

        Assert.That(bmp.Size, Is.EqualTo(expected.Size));
        for (var y = 0; y < bmp.Height; y++)
        for (var x = 0; x < bmp.Width; x++)
            Assert.That(bmp.GetPixel(x, y), Is.EqualTo(expected.GetPixel(x, y)), $"Pixel ({x},{y})");
    }

    [Test]
    public void ImageToBitmap_IsCachedByValue()
    {
        var a = CatalogueIcons.Catalogue.WithOverlay(OverlayKind.Link);
        var b = CatalogueIcons.Catalogue.WithOverlay(OverlayKind.Link);

        Assert.That(a, Is.Not.SameAs(b));
        Assert.That(a.ImageToBitmap(), Is.SameAs(b.ImageToBitmap()));
        Assert.That(((RdmpIcon)null).ImageToBitmap(), Is.Null);
    }

    [Test]
    public void ImageToBitmap_Overlay_DrawsOverlayOnTopOfBase()
    {
        var baseIcon = CatalogueIcons.Catalogue;
        var overlayIcon = IconOverlayProvider.GetOverlayIcon(OverlayKind.Add);

        var result = baseIcon.WithOverlay(OverlayKind.Add).ImageToBitmap();
        using var basePixels = Decode(baseIcon);
        using var overlayPixels = Decode(overlayIcon);

        Assert.That(result.Size, Is.EqualTo(basePixels.Size), "Overlay must not change the size of the icon");

        var opaqueOverlayPixels = 0;
        for (var y = 0; y < Math.Min(result.Height, overlayPixels.Height); y++)
        for (var x = 0; x < Math.Min(result.Width, overlayPixels.Width); x++)
        {
            var o = overlayPixels.GetPixel(x, y);
            var r = result.GetPixel(x, y);

            if (o.A == 255)
            {
                opaqueOverlayPixels++;
                Assert.That(r.ToArgb(), Is.EqualTo(o.ToArgb()), $"Opaque overlay pixel ({x},{y}) should replace base");
            }
            else if (o.A == 0)
            {
                Assert.That(r.ToArgb(), Is.EqualTo(basePixels.GetPixel(x, y).ToArgb()),
                    $"Transparent overlay pixel ({x},{y}) should leave base unchanged");
            }
        }

        Assert.That(opaqueOverlayPixels, Is.GreaterThan(0), "Expected the overlay to have some opaque pixels");
    }

    [Test]
    public void ImageToBitmap_Greyscale_UsesBt709Luminance()
    {
        // TableInfo has plenty of coloured pixels (Catalogue's visible pixels are all already grey)
        var icon = CatalogueIcons.TableInfo;
        var grey = icon.AsGreyscale().ImageToBitmap();
        using var original = Decode(icon);

        Assert.That(grey.Size, Is.EqualTo(original.Size));

        var colouredPixels = 0;
        for (var y = 0; y < grey.Height; y++)
        for (var x = 0; x < grey.Width; x++)
        {
            var g = grey.GetPixel(x, y);
            var o = original.GetPixel(x, y);

            Assert.That(g.A, Is.EqualTo(o.A), $"Alpha must be preserved at ({x},{y})");
            if (g.A == 0) continue;

            Assert.That(g.R, Is.EqualTo(g.G), $"Pixel ({x},{y}) is not grey");
            Assert.That(g.G, Is.EqualTo(g.B), $"Pixel ({x},{y}) is not grey");

            if (o.R != o.G || o.G != o.B) colouredPixels++;

            var expected = 0.2126 * o.R + 0.7152 * o.G + 0.0722 * o.B;
            Assert.That(g.R, Is.EqualTo(expected).Within(2), $"Luminance at ({x},{y})");
        }

        Assert.That(colouredPixels, Is.GreaterThan(0), "Test icon should have some colour to convert");
    }

    [Test]
    public void ImageToBitmap_OperationOrderMatters()
    {
        var icon = CatalogueIcons.Catalogue;

        // overlay then greyscale: the green 'add' symbol is grey too
        var overlayThenGrey = icon.WithOverlay(OverlayKind.Add).AsGreyscale().ImageToBitmap();
        // greyscale then overlay: the 'add' symbol keeps its colour
        var greyThenOverlay = icon.AsGreyscale().WithOverlay(OverlayKind.Add).ImageToBitmap();

        Assert.That(HasColour(overlayThenGrey), Is.False);
        Assert.That(HasColour(greyThenOverlay), Is.True);
    }

    [Test]
    public void ImageToBitmap_IsThreadSafe()
    {
        var icons = Enum.GetValues<OverlayKind>()
            .SelectMany(static k => new[]
                { CatalogueIcons.Project.WithOverlay(k), CatalogueIcons.TableInfo.WithOverlay(k).AsGreyscale() })
            .ToArray();

        var results = new Bitmap[icons.Length * 8];
        Parallel.For(0, results.Length, i => results[i] = icons[i % icons.Length].ImageToBitmap());

        Assert.That(results, Is.All.Not.Null);
        for (var i = 0; i < results.Length; i++)
            Assert.That(results[i], Is.SameAs(results[i % icons.Length]), "Equal icons should share one cached Bitmap");
    }

    [Test]
    public void ToPngBytes_RendersOperations()
    {
        var icon = CatalogueIcons.Catalogue.WithOverlay(OverlayKind.Add);
        var png = icon.ToPngBytes();

        Assert.That(PngHeader.IsPng(png));
        Assert.That(PngHeader.GetDimensions(png), Is.EqualTo((icon.ImageToBitmap().Width, icon.ImageToBitmap().Height)));
        Assert.That(png, Is.Not.EqualTo(EmbeddedIconHelper.GetPngBytes(icon)));
    }

    private static bool HasColour(Bitmap bmp)
    {
        for (var y = 0; y < bmp.Height; y++)
        for (var x = 0; x < bmp.Width; x++)
        {
            var p = bmp.GetPixel(x, y);
            if (p.A != 0 && (p.R != p.G || p.G != p.B))
                return true;
        }

        return false;
    }
}
