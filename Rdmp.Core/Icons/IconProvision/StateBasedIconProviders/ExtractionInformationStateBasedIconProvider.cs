// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Icons.IconOverlays;
using Rdmp.Core.ReusableLibraryCode.Icons.IconProvision;

namespace Rdmp.Core.Icons.IconProvision.StateBasedIconProviders;

internal sealed class ExtractionInformationStateBasedIconProvider : IObjectStateBasedIconProvider
{
    private static readonly RdmpIcon ExtractionInformationCore =
        CatalogueIcons.ExtractionInformation;

    private static readonly RdmpIcon ExtractionInformationSupplemental =
        CatalogueIcons.ExtractionInformation_Supplemental;

    private static readonly RdmpIcon ExtractionInformationSpecialApproval =
        CatalogueIcons.ExtractionInformation_SpecialApproval;

    private static readonly RdmpIcon ExtractionInformationInternalOnly =
        IconOverlayProvider.GetOverlay(ExtractionInformationSpecialApproval, OverlayKind.Internal);

    private static readonly RdmpIcon ExtractionInformationDeprecated =
        IconOverlayProvider.GetOverlay(ExtractionInformationCore, OverlayKind.Deprecated);

    private static readonly RdmpIcon ExtractionInformationProjectSpecific =
        CatalogueIcons.ExtractionInformation_ProjectSpecific;

    private static readonly RdmpIcon NoIconAvailable = CatalogueIcons.NoIconAvailable;

    private static readonly RdmpIcon ExtractionInformationNotExtractable = IconOverlayProvider.GetOverlay(ExtractionInformationCore, OverlayKind.Delete);

    public RdmpIcon GetImageIfSupportedObject(object o)
    {
        if (o is ExtractionCategory cat)
            return GetImage(cat);

        if (o is not ExtractionInformation ei) return null;

        var toReturn = GetImage(ei.ExtractionCategory);

        if (ei.IsExtractionIdentifier)
            toReturn = IconOverlayProvider.GetOverlay(toReturn, OverlayKind.IsExtractionIdentifier);

        if (ei.IsPrimaryKey)
            toReturn = IconOverlayProvider.GetOverlay(toReturn, OverlayKind.Key);

        if (ei.HashOnDataRelease)
            toReturn = IconOverlayProvider.GetOverlay(toReturn, OverlayKind.Hashed);

        return toReturn;
    }

    private static RdmpIcon GetImage(ExtractionCategory category)
    {
        return category switch
        {
            ExtractionCategory.Core => ExtractionInformationCore,
            ExtractionCategory.Supplemental => ExtractionInformationSupplemental,
            ExtractionCategory.SpecialApprovalRequired => ExtractionInformationSpecialApproval,
            ExtractionCategory.Internal => ExtractionInformationInternalOnly,
            ExtractionCategory.Deprecated => ExtractionInformationDeprecated,
            ExtractionCategory.ProjectSpecific => ExtractionInformationProjectSpecific,
            ExtractionCategory.Any => NoIconAvailable,
            ExtractionCategory.NotExtractable => ExtractionInformationNotExtractable,
            _ => throw new ArgumentOutOfRangeException(nameof(category))
        };
    }
}