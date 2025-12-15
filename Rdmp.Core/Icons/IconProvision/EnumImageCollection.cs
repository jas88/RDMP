// Copyright (c) The University of Dundee 2018-2024
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Frozen;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Rdmp.Core.Icons.IconProvision;

public sealed class EnumImageCollection<T> where T : struct, Enum, IConvertible
{
    private readonly FrozenDictionary<T, Image<Rgba32>> _images;

    /// <summary>
    /// Creates an image collection by loading embedded PNG resources based on enum values.
    /// Images are loaded once and cached in a FrozenDictionary for efficient runtime access.
    /// Uses case-insensitive lookup via EmbeddedIconHelper.
    /// </summary>
    /// <param name="resourcePrefix">The resource name prefix (e.g., "Rdmp.Core.Icons.").</param>
    public EnumImageCollection(string resourcePrefix)
    {
        // Use EmbeddedIconHelper for case-insensitive lookup (PNG files may have different case than enum values)
        var dict = Enum.GetValues<T>()
            .ToDictionary(s => s, s => EmbeddedIconHelper.Get($"{resourcePrefix}{s}"));
        var missingImages = dict.Where(i => i.Value is null).Select(p => p.Key).ToList();
        if (missingImages.Count != 0)
            throw new IconProvisionException(
                $"The following expected images were missing from embedded resources with prefix '{resourcePrefix}':{Environment.NewLine}{string.Join($",{Environment.NewLine}", missingImages)}");
        _images = dict.ToFrozenDictionary();
    }

    public Image<Rgba32> this[T index] => _images[index];

    public FrozenDictionary<string, Image<Rgba32>> ToStringDictionary(int newSizeInPixels = -1)
    {
        return _images.ToFrozenDictionary(
            k => k.Key.ToString(),
            v => newSizeInPixels == -1 ? v.Value : v.Value.Clone(x => x.Resize(newSizeInPixels, newSizeInPixels)));
    }
}