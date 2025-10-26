// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Data;
using SCIStore.SciStoreServices81;

namespace SCIStorePlugin.Data;

public class SampleType
{
    public string Code { get; set; }
    public string CommonCode { get; set; }
    public string Description { get; set; }
    public string HealthBoard { get; set; }

    public SampleType(CLINICAL_CIRCUMSTANCE_TYPE type)
    {
        if (type.Item is string name)
        {
            Description = name;
            Code = Description;
        }

        if (type.Item is CLINICAL_INFORMATION_TYPE clinicalInformation)
        {
            Code = clinicalInformation.ClinicalCode.ClinicalCodeValue[0];
            Description = Code;
        }
    }

    public DataRow PopulateDataRow(DataRow newRow)
    {
        foreach (var prop in GetType().GetProperties())
        {
            if (newRow[prop.Name] == null)
                throw new Exception($"Schema of passed row is incorrect, does not contain a column for '{prop.Name}'");

            newRow[prop.Name] = prop.GetValue(this, null);
        }

        return newRow;
    }
}