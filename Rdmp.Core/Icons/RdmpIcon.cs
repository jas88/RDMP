// Copyright (c) The University of Dundee 2018-2024
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using Rdmp.Core.Icons.IconOverlays;
using Rdmp.Core.ReusableLibraryCode.Icons.IconProvision;

namespace Rdmp.Core.Icons;

/// <summary>
/// Immutable, platform independent description of an icon: an embedded PNG resource (see <see cref="EmbeddedIconHelper"/>)
/// plus an ordered list of <see cref="Operations"/> (overlays, greyscale) to apply to it.  No pixels are held; user interfaces
/// render the description with whatever graphics library they use.
/// </summary>
/// <remarks>
/// Instances have structural equality (same base resource and same operations in the same order) so they can be used as cache
/// keys.  Instances of the base icons are obtained from <see cref="EmbeddedIconHelper"/> (or the static icon classes e.g.
/// <see cref="IconProvision.CatalogueIcons"/>) and derived using <see cref="WithOverlay(OverlayKind)"/>,
/// <see cref="WithOverlay(RdmpIcon)"/> and <see cref="AsGreyscale"/>.
/// </remarks>
public sealed class RdmpIcon : IEquatable<RdmpIcon>
{
    private readonly RdmpIconOperation[] _operations;
    private readonly int _hashCode;

    /// <summary>
    /// The full manifest resource name (including the .png extension) of the base image within the Rdmp.Core assembly
    /// </summary>
    public string ResourceName { get; }

    /// <summary>
    /// Operations to apply (in order) to the base image
    /// </summary>
    public IReadOnlyList<RdmpIconOperation> Operations => _operations;

    internal RdmpIcon(string resourceName) : this(resourceName, [])
    {
    }

    private RdmpIcon(string resourceName, RdmpIconOperation[] operations)
    {
        ResourceName = resourceName ?? throw new ArgumentNullException(nameof(resourceName));
        _operations = operations;

        var hash = new HashCode();
        hash.Add(resourceName, StringComparer.Ordinal);
        foreach (var op in operations)
            hash.Add(op);
        _hashCode = hash.ToHashCode();
    }

    /// <summary>
    /// Returns a new icon which is this icon with the standard overlay <paramref name="kind"/> drawn on top.  Passing
    /// <see cref="OverlayKind.None"/> returns this icon unchanged.
    /// </summary>
    public RdmpIcon WithOverlay(OverlayKind kind) =>
        kind == OverlayKind.None ? this : WithOverlay(IconOverlayProvider.GetOverlayIcon(kind));

    /// <summary>
    /// Returns a new icon which is this icon with <paramref name="overlay"/> drawn on top (at the top left corner, alpha blended)
    /// </summary>
    public RdmpIcon WithOverlay(RdmpIcon overlay)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        return Append(RdmpIconOperation.Overlay(overlay));
    }

    /// <summary>
    /// Returns a greyscale version of this icon (as it currently looks i.e. including any overlays applied so far).
    /// Calling this on an icon that is already greyscale returns the same icon.
    /// </summary>
    public RdmpIcon AsGreyscale() =>
        _operations.Length > 0 && _operations[^1].Kind == RdmpIconOperationKind.Greyscale
            ? this
            : Append(RdmpIconOperation.Greyscale);

    private RdmpIcon Append(RdmpIconOperation op) => new(ResourceName, [.. _operations, op]);

    /// <inheritdoc/>
    public bool Equals(RdmpIcon other) =>
        other is not null &&
        (ReferenceEquals(this, other) ||
         (_hashCode == other._hashCode &&
          string.Equals(ResourceName, other.ResourceName, StringComparison.Ordinal) &&
          _operations.AsSpan().SequenceEqual(other._operations)));

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is RdmpIcon other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _hashCode;

    public static bool operator ==(RdmpIcon left, RdmpIcon right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(RdmpIcon left, RdmpIcon right) => !(left == right);

    /// <inheritdoc/>
    public override string ToString() =>
        _operations.Length == 0
            ? ResourceName
            : $"{ResourceName} [{string.Join(", ", _operations.Select(static o => o.ToString()))}]";
}

/// <summary>
/// The kinds of operation that can be applied to an <see cref="RdmpIcon"/>
/// </summary>
public enum RdmpIconOperationKind
{
    /// <summary>
    /// Draw another icon (<see cref="RdmpIconOperation.OverlayIcon"/>) on top of the image at the top left corner using
    /// normal (source over) alpha blending.  The image keeps its original size.
    /// </summary>
    Overlay,

    /// <summary>
    /// Convert the image to greyscale using ITU-R BT.709 luminance weights (0.2126 R + 0.7152 G + 0.0722 B), preserving alpha
    /// </summary>
    Greyscale
}

/// <summary>
/// A single step in the description of an <see cref="RdmpIcon"/>
/// </summary>
public sealed record RdmpIconOperation
{
    /// <summary>
    /// What the operation does
    /// </summary>
    public RdmpIconOperationKind Kind { get; }

    /// <summary>
    /// The icon to draw on top when <see cref="Kind"/> is <see cref="RdmpIconOperationKind.Overlay"/>, otherwise null
    /// </summary>
    public RdmpIcon OverlayIcon { get; }

    private RdmpIconOperation(RdmpIconOperationKind kind, RdmpIcon overlayIcon)
    {
        Kind = kind;
        OverlayIcon = overlayIcon;
    }

    /// <summary>
    /// The greyscale operation
    /// </summary>
    public static RdmpIconOperation Greyscale { get; } = new(RdmpIconOperationKind.Greyscale, null);

    /// <summary>
    /// Creates an operation which draws <paramref name="overlayIcon"/> on top of the image
    /// </summary>
    public static RdmpIconOperation Overlay(RdmpIcon overlayIcon) =>
        new(RdmpIconOperationKind.Overlay, overlayIcon ?? throw new ArgumentNullException(nameof(overlayIcon)));

    /// <inheritdoc/>
    public override string ToString() =>
        Kind == RdmpIconOperationKind.Overlay ? $"Overlay({OverlayIcon})" : Kind.ToString();
}
