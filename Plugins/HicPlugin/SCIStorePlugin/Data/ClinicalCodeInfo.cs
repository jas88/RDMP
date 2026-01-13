// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using SCIStore.SciStoreServices81;

namespace SCIStorePlugin.Data;

public enum ClinicalCodeScheme
{
    Read,
    Local,
    Undefined
};

/// <summary>
/// Represents clinical code information extracted from SCIStore reports, including the code value, scheme (Read, Local), and description.
/// </summary>
public class ClinicalCodeInfo
{
    public string Value { get; set; }
    public string Scheme { get; set; }
    public ClinicalCodeScheme SchemeId { get; set; }
    public string Description { get; set; }

    public ClinicalCodeInfo(CLINICAL_INFORMATION_TYPE clinicalInformation)
    {
        Value = string.Join(", ", clinicalInformation.ClinicalCode.ClinicalCodeValue);
        Scheme = clinicalInformation.ClinicalCode.ClinicalCodeScheme.ClinicalCodeSchemeVersion;

        if (!Enum.TryParse(clinicalInformation.ClinicalCode.ClinicalCodeScheme.ClinicalCodeSchemeId, out ClinicalCodeScheme schemeId))
            throw new Exception(
                $"Unrecognised <ClinicalCodeSchemeId>: {clinicalInformation.ClinicalCode.ClinicalCodeScheme.ClinicalCodeSchemeId}");

        SchemeId = schemeId;
        Description = clinicalInformation.ClinicalCodeDescription;
    }
}