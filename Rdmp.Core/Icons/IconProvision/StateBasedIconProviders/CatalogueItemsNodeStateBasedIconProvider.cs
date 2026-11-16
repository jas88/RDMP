// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using Rdmp.Core.Icons.IconOverlays;
using Rdmp.Core.Providers.Nodes;
using Rdmp.Core.ReusableLibraryCode.Icons.IconProvision;

namespace Rdmp.Core.Icons.IconProvision.StateBasedIconProviders;

internal sealed class CatalogueItemsNodeStateBasedIconProvider : IObjectStateBasedIconProvider
{
    private static readonly RdmpIcon Basic = CatalogueIcons.CatalogueItemsNode;
    private static readonly RdmpIcon Core = IconOverlayProvider.GetOverlay(Basic, OverlayKind.Extractable);

    private static readonly RdmpIcon Internal =
        IconOverlayProvider.GetOverlay(Basic, OverlayKind.Extractable_Internal);

    private static readonly RdmpIcon Supplemental =
        IconOverlayProvider.GetOverlay(Basic, OverlayKind.Extractable_Supplemental);

    private static readonly RdmpIcon Special =
        IconOverlayProvider.GetOverlay(Basic, OverlayKind.Extractable_SpecialApproval);

    private static readonly RdmpIcon Deprecated = IconOverlayProvider.GetOverlay(Basic, OverlayKind.Deprecated);

    public RdmpIcon GetImageIfSupportedObject(object o)
    {
        if (o is not CatalogueItemsNode cin)
            return null;

        return cin.Category == null
            ? Basic
            : cin.Category.Value switch
            {
                Curation.Data.ExtractionCategory.Core => Core,
                Curation.Data.ExtractionCategory.Supplemental => Supplemental,
                Curation.Data.ExtractionCategory.SpecialApprovalRequired => Special,
                Curation.Data.ExtractionCategory.Internal => Internal,
                Curation.Data.ExtractionCategory.Deprecated => Deprecated,
                _ => Basic
            };
    }
}