// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using Rdmp.Core.Icons.IconProvision;
using Rdmp.Core.ReusableLibraryCode.Icons.IconProvision;

namespace Rdmp.Core.Icons.IconOverlays;

/// <summary>
/// Describes icons decorated with overlays or rendered in greyscale.  The results are <see cref="RdmpIcon"/> descriptions
/// which are cheap to create and compare; rendering (and caching of rendered images) is done by the user interface.
/// </summary>
public static class IconOverlayProvider
{
    private static readonly EnumImageCollection<OverlayKind> Images = new("Rdmp.Core.Icons.Overlays.");

    /// <summary>
    /// Returns the overlay image itself (e.g. the small 'link' arrow) for <paramref name="overlayKind"/>
    /// </summary>
    public static RdmpIcon GetOverlayIcon(OverlayKind overlayKind) => Images[overlayKind];

    /// <summary>
    /// Returns <paramref name="forImage"/> with the standard <paramref name="overlayKind"/> drawn on top
    /// </summary>
    public static RdmpIcon GetOverlay(RdmpIcon forImage, OverlayKind overlayKind) => forImage.WithOverlay(overlayKind);

    /// <summary>
    /// Returns <paramref name="forImage"/> with <paramref name="customOverlay"/> drawn on top
    /// </summary>
    public static RdmpIcon GetOverlay(RdmpIcon forImage, RdmpIcon customOverlay) => forImage.WithOverlay(customOverlay);

    /// <summary>
    /// Returns a greyscale version of <paramref name="forImage"/>
    /// </summary>
    public static RdmpIcon GetGreyscale(RdmpIcon forImage) => forImage.AsGreyscale();
}
