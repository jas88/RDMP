// Copyright (c) The University of Dundee 2018-2024
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Frozen;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Rdmp.Core.Icons;

/// <summary>
/// Helper class for locating embedded icon resources.
/// All icon resource names are indexed once at initialization in a FrozenDictionary.  Images are not decoded here;
/// <see cref="RdmpIcon"/> only describes the icon and user interfaces render it.
/// </summary>
public static class EmbeddedIconHelper
{
    private const string P = "Rdmp.Core.Icons.";
    private const string Extension = ".png";

    private static readonly Assembly ResourceAssembly = typeof(EmbeddedIconHelper).Assembly;
    private static readonly FrozenDictionary<string, RdmpIcon> Cache = BuildCache();

    private static FrozenDictionary<string, RdmpIcon> BuildCache()
    {
        var dict = ResourceAssembly.GetManifestResourceNames()
            .Where(static name => name.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                static name => name[..^Extension.Length],
                static name => new RdmpIcon(name),
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
    /// Gets an icon by its full resource name (without .png extension, case insensitive).
    /// Returns null if not found.
    /// </summary>
    public static RdmpIcon Get(string resourceName) =>
        Cache.TryGetValue(resourceName, out var img) ? img : null;

    /// <summary>
    /// Gets an icon by its full resource name (without .png extension, case insensitive).
    /// Throws if not found - use for required icons in static initializers.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the icon resource is not found.</exception>
    public static RdmpIcon GetRequired(string resourceName) =>
        Cache.TryGetValue(resourceName, out var img)
            ? img
            : throw new InvalidOperationException(
                $"Required embedded icon not found: '{resourceName}'. " +
                $"Ensure the PNG file exists and is embedded as a resource.");

    /// <summary>
    /// Opens a stream over the raw PNG data of the base image of <paramref name="icon"/> (i.e. ignoring any
    /// <see cref="RdmpIcon.Operations"/>).  The caller must dispose the stream.
    /// </summary>
    public static Stream OpenPngStream(RdmpIcon icon)
    {
        ArgumentNullException.ThrowIfNull(icon);
        return ResourceAssembly.GetManifestResourceStream(icon.ResourceName)
               ?? throw new InvalidOperationException($"Failed to load embedded resource: {icon.ResourceName}");
    }

    /// <summary>
    /// Returns the raw PNG bytes of the base image of <paramref name="icon"/> (i.e. ignoring any
    /// <see cref="RdmpIcon.Operations"/>).  No decoding is performed.
    /// </summary>
    public static byte[] GetPngBytes(RdmpIcon icon)
    {
        using var stream = OpenPngStream(icon);
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }
}
