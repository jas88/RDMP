// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Data;
using System.Linq;
using SCIStore.SciStoreServices81;

namespace SCIStorePlugin.Data;

/// <summary>
/// Lookup table entry mapping SCIStore test codes to Read codes and common codes, including scheme versions, healthboard identifiers, and clinical code descriptions.
/// </summary>
public class TestCodeLookup
{
    public string TestCode { get; set; }
    public string TestCodeSchemeVersion { get; set; }
    public string ReadCode { get; set; }
    public string ReadCodeSchemeVersion { get; set; }
    public string HealthBoard { get; set; }
    public string ClinicalCodeDescription { get; set; }
    public string CommonCode { get; set; }

    public TestCodeLookup(TEST_RESULT_TYPE test)
    {
        var testNum = 1;

        var numCodes = FindNumberOfClinicalInformationTypes(test.TestPerformed);
        foreach (var testName in test.TestPerformed.TestName)
        {
            if (testName.Item is not CLINICAL_INFORMATION_TYPE clinicalInformation) continue;

            ClinicalCodeDescription = clinicalInformation.ClinicalCodeDescription;

            var clinicalCode = clinicalInformation.ClinicalCode;
            switch (clinicalCode.ClinicalCodeScheme.ClinicalCodeSchemeId)
            {
                case "Read":
                    ReadCodeSchemeVersion = clinicalCode.ClinicalCodeScheme.ClinicalCodeSchemeVersion;
                    ReadCode = string.Join(",", clinicalCode.ClinicalCodeValue);
                    break;
                case "Local":
                    TestCodeSchemeVersion = clinicalCode.ClinicalCodeScheme.ClinicalCodeSchemeVersion;
                    TestCode = string.Join(",", clinicalCode.ClinicalCodeValue);
                    break;
                case "Undefined":
                    // in this case, we have to rely on the ordering of the nodes in the XML file
                    // TODO: Could do something more sophisticated, such as a regex to determine if it is a read code or not?
                    if (numCodes == 1) // If there is only one undefined code, it will be a local code
                    {
                        TestCodeSchemeVersion = clinicalCode.ClinicalCodeScheme.ClinicalCodeSchemeVersion;
                        TestCode = string.Join(",", clinicalCode.ClinicalCodeValue);
                    }
                    else
                    {
                        if (testNum == 1)
                        {
                            ReadCodeSchemeVersion = clinicalCode.ClinicalCodeScheme.ClinicalCodeSchemeVersion;
                            ReadCode = string.Join(",", clinicalCode.ClinicalCodeValue);
                        }
                        else
                        {
                            TestCodeSchemeVersion = clinicalCode.ClinicalCodeScheme.ClinicalCodeSchemeVersion;
                            TestCode = string.Join(",", clinicalCode.ClinicalCodeValue);
                        }
                    }
                    break;
            }
            ++testNum;
        }
    }

    private int FindNumberOfClinicalInformationTypes(TEST_TYPE testPerformed)
    {
        return testPerformed.TestName.Count(type => type.Item is CLINICAL_INFORMATION_TYPE);
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