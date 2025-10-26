// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Core.Validation.Constraints.Secondary;
using Rdmp.Core.ReusableLibraryCode;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStore.SciStoreServices81;

namespace SCIStorePlugin.Data;

public sealed class SciStoreSample
{
    private string _labNumber;
    private string _testReportId;

    public string LabNumber
    {
        get => _labNumber;
        set => _labNumber = UsefulStuff.RemoveIllegalFilenameCharacters(value);
    }

    public string TestReportID
    {
        get => _testReportId;
        set => _testReportId = UsefulStuff.RemoveIllegalFilenameCharacters(value);
    }

    public string SampleName { get; set; }
    public DateTime? DateTimeSampled { get; set; }
    public DateTime? DateTimeReceived { get; set; }
    public string SampleRequesterComment { get; set; }
    public string ServiceProviderComment { get; set; }
    public string TestIdentifier { get; set; }

    // Denormalised from TestSetDetails
    public string TestSet_ClinicalCircumstanceDescription { get; set; }
    public string TestSet_ReadCodeValue { get; set; }
    public string TestSet_ReadCodeScheme { get; set; }
    public string TestSet_ReadCodeSchemeId { get; set; }
    public string TestSet_ReadCodeDescription { get; set; }
    public string TestSet_LocalClinicalCodeValue { get; set; }
    public string TestSet_LocalClinicalCodeScheme { get; set; }
    public string TestSet_LocalClinicalCodeSchemeId { get; set; }
    public string TestSet_LocalClinicalCodeDescription { get; set; }

    [NoMappingToDatabase]
    public TestSet TestSetDetails { get; set; }
    [NoMappingToDatabase]
    public ICollection<SciStoreResult> Results { get; set; }

    public void PopulateDenormalisedTestSetDetailsFields()
    {
        TestSet_ClinicalCircumstanceDescription = TestSetDetails.ClinicalCircumstanceDescription;

        if (TestSetDetails.ReadCode != null)
        {
            var code = TestSetDetails.ReadCode;
            TestSet_ReadCodeValue = code.Value;
            TestSet_ReadCodeScheme = code.Scheme;
            TestSet_ReadCodeSchemeId = code.SchemeId.ToString();
            TestSet_ReadCodeDescription = code.Description;
        }

        if (TestSetDetails.LocalCode != null)
        {
            var code = TestSetDetails.LocalCode;
            TestSet_LocalClinicalCodeValue = code.Value;
            TestSet_LocalClinicalCodeScheme = code.Scheme;
            TestSet_LocalClinicalCodeSchemeId = code.SchemeId.ToString();
            TestSet_LocalClinicalCodeDescription = code.Description;
        }
    }

    public int ResolveTestResultOrderDuplication()
    {
        var resolutions = 0;

        //todo potentially change this to .AddResult method and make Results private
        if(Results is List<SciStoreResult> toWorkOn)
        {
            toWorkOn.Sort();


            for (var index = Results.Count-2; index >= 0; index--)
            {
                var previous = toWorkOn[index];
                var result = toWorkOn[index + 1];

                //only remove values that are EXACT duplicates (not just on primary key)
                if (previous.IsIdenticalTo(result))
                {
                    resolutions++;
                    toWorkOn.RemoveAt(index);
                }
            }

            Results = new HashSet<SciStoreResult>(toWorkOn);
        }
        else
        {
            throw new Exception(
                $"Results is an {Results.GetType()} expected it to be a List, possibly you have called this method multiple times or something");
        }

        return resolutions;
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((SciStoreSample) obj);
    }

    private bool Equals(SciStoreSample other)
    {
        return string.Equals(LabNumber, other.LabNumber) && string.Equals(TestIdentifier, other.TestIdentifier) && string.Equals(TestReportID, other.TestReportID);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LabNumber, TestIdentifier, TestReportID);
    }
}

