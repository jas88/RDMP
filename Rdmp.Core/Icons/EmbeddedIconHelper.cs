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

namespace Rdmp.Core.Icons;

/// <summary>
/// Helper class for loading embedded icon resources.
/// All icons are loaded once at initialization and cached in a FrozenDictionary.
/// </summary>
public static class EmbeddedIconHelper
{
    private const string P = "Rdmp.Core.Icons.";
    private static readonly FrozenDictionary<string, Image<Rgba32>> Cache = BuildCache();

    private static FrozenDictionary<string, Image<Rgba32>> BuildCache()
    {
        var assembly = typeof(EmbeddedIconHelper).Assembly;
        var dict = assembly.GetManifestResourceNames()
            .Where(name => name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                name => name[..^4],
                name => Image.Load<Rgba32>(assembly.GetManifestResourceStream(name)
                    ?? throw new InvalidOperationException($"Failed to load embedded resource: {name}")),
                StringComparer.OrdinalIgnoreCase);

        // Add aliases for icons that map to other resources (from original .resx files)
        dict[$"{P}Setting"] = dict[$"{P}famfamfam.cog"];
        dict[$"{P}LoadMetadataVersionNode"] = dict[$"{P}CatalogueFolder"];
        dict[$"{P}RegexRedaction"] = dict[$"{P}StandardRegex3"];
        dict[$"{P}RegexRedactionConfiguration"] = dict[$"{P}StandardRegex311"];
        dict[$"{P}RegexRedactionKey"] = dict[$"{P}StandardRegex31"];

        return dict.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets an icon by its full resource name (without .png extension).
    /// Returns null if not found.
    /// </summary>
    public static Image<Rgba32> Get(string resourceName) =>
        Cache.TryGetValue(resourceName, out var img) ? img : null;

    /// <summary>
    /// Gets an icon by its full resource name (without .png extension).
    /// Throws if not found - use for required icons in static initializers.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the icon resource is not found.</exception>
    public static Image<Rgba32> GetRequired(string resourceName) =>
        Cache.TryGetValue(resourceName, out var img)
            ? img
            : throw new InvalidOperationException(
                $"Required embedded icon not found: '{resourceName}'. " +
                $"Ensure the PNG file exists and is embedded as a resource.");
}
