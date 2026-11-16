// Copyright (c) The University of Dundee 2018-2021
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using Rdmp.Core.Icons;

namespace Rdmp.UI;

/// <summary>
/// Renders platform independent <see cref="RdmpIcon"/> descriptions into System.Drawing <see cref="Bitmap"/>s (and converts
/// between other image representations).
/// </summary>
public static class ImageTools
{
    /// <summary>
    /// ITU-R BT.709 luminance weights, matching the greyscale conversion historically used by RDMP
    /// </summary>
    private const float RedWeight = 0.2126f;
    private const float GreenWeight = 0.7152f;
    private const float BlueWeight = 0.0722f;

    private static readonly ConcurrentDictionary<RdmpIcon, Lazy<Bitmap>> IconCache = new();

    private static readonly ConcurrentDictionary<byte[], Lazy<Bitmap>> ByteArrayCache = new();

    /// <summary>
    /// Returns a <see cref="Bitmap"/> depicting <paramref name="icon"/> (base image with all operations applied).  Results are
    /// cached and shared so callers must not dispose or modify the returned <see cref="Bitmap"/>.  Returns null if
    /// <paramref name="icon"/> is null.
    /// </summary>
    public static Bitmap ImageToBitmap(this RdmpIcon icon) =>
        icon is null
            ? null
            : IconCache.GetOrAdd(icon,
                static k => new Lazy<Bitmap>(() => Render(k), LazyThreadSafetyMode.ExecutionAndPublication)).Value;

    /// <summary>
    /// Decodes the image file data <paramref name="img"/> (e.g. PNG) into a <see cref="Bitmap"/>.  Results are cached (by array
    /// instance) and shared so callers must not dispose or modify the returned <see cref="Bitmap"/>.
    /// </summary>
    public static Bitmap ImageToBitmap(this byte[] img) =>
        ByteArrayCache.GetOrAdd(img,
            static k => new Lazy<Bitmap>(() =>
            {
                using var ms = new MemoryStream(k);
                return CopyToArgb(ms);
            }, LazyThreadSafetyMode.ExecutionAndPublication)).Value;

    /// <summary>
    /// Encodes <paramref name="img"/> as PNG file data
    /// </summary>
    public static byte[] ToPngBytes(this Image img)
    {
        ArgumentNullException.ThrowIfNull(img);
        using var stream = new MemoryStream();
        img.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    /// <summary>
    /// Renders <paramref name="icon"/> as PNG file data (base image with all operations applied)
    /// </summary>
    public static byte[] ToPngBytes(this RdmpIcon icon)
    {
        ArgumentNullException.ThrowIfNull(icon);
        using var bmp = Render(icon);
        return bmp.ToPngBytes();
    }

    /// <summary>
    /// Creates a new <see cref="Bitmap"/> (owned by the caller) depicting <paramref name="icon"/>.  Every call decodes the
    /// embedded resources afresh and never touches shared (cached) <see cref="Bitmap"/>s, so it is safe to call from any thread.
    /// </summary>
    private static Bitmap Render(RdmpIcon icon)
    {
        Bitmap bmp;
        using (var stream = EmbeddedIconHelper.OpenPngStream(icon))
            bmp = CopyToArgb(stream);

        try
        {
            foreach (var op in icon.Operations)
                switch (op.Kind)
                {
                    case RdmpIconOperationKind.Overlay:
                        DrawOverlay(bmp, op.OverlayIcon);
                        break;
                    case RdmpIconOperationKind.Greyscale:
                        var grey = ToGreyscale(bmp);
                        bmp.Dispose();
                        bmp = grey;
                        break;
                    default:
                        throw new NotSupportedException($"Unknown icon operation {op.Kind}");
                }

            return bmp;
        }
        catch
        {
            bmp.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Decodes <paramref name="stream"/> and copies the pixels into a new 32bpp ARGB <see cref="Bitmap"/> (which, unlike a
    /// <see cref="Bitmap"/> constructed directly from a stream, does not depend on the stream staying open and can be drawn on
    /// even if the source was palette based).
    /// </summary>
    private static Bitmap CopyToArgb(Stream stream)
    {
        using var source = new Bitmap(stream);
        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        try
        {
            using var g = Graphics.FromImage(result);
            g.CompositingMode = CompositingMode.SourceCopy;
            DrawUnscaled(g, source);
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Draws <paramref name="overlay"/> on top of <paramref name="target"/> at the top left corner (source over alpha
    /// blending, clipped to the bounds of <paramref name="target"/>)
    /// </summary>
    private static void DrawOverlay(Bitmap target, RdmpIcon overlay)
    {
        using var overlayBmp = Render(overlay);
        using var g = Graphics.FromImage(target);
        g.CompositingMode = CompositingMode.SourceOver;
        DrawUnscaled(g, overlayBmp);
    }

    /// <summary>
    /// Returns a new greyscale copy of <paramref name="source"/> using BT.709 luminance weights (alpha is preserved)
    /// </summary>
    private static Bitmap ToGreyscale(Bitmap source)
    {
        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        try
        {
            // System.Drawing multiplies row vectors [R G B A 1] by the matrix so each output channel is the dot product of the
            // input with a column: every colour column gets the same luminance weights.
            var matrix = new ColorMatrix([
                [RedWeight, RedWeight, RedWeight, 0, 0],
                [GreenWeight, GreenWeight, GreenWeight, 0, 0],
                [BlueWeight, BlueWeight, BlueWeight, 0, 0],
                [0, 0, 0, 1, 0],
                [0, 0, 0, 0, 1]
            ]);

            using var attributes = new ImageAttributes();
            attributes.SetColorMatrix(matrix);

            using var g = Graphics.FromImage(result);
            g.CompositingMode = CompositingMode.SourceCopy;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height), 0, 0, source.Width, source.Height,
                GraphicsUnit.Pixel, attributes);
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Draws <paramref name="image"/> at (0,0) pixel for pixel, ignoring the image resolution (DPI) so no scaling happens
    /// </summary>
    private static void DrawUnscaled(Graphics g, Image image)
    {
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height), 0, 0, image.Width, image.Height,
            GraphicsUnit.Pixel);
    }
}
