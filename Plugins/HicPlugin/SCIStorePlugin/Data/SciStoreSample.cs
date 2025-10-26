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

public class SciStoreSampleFactory
{
    private readonly ReferentialIntegrityConstraint _readCodeConstraint;

    public SciStoreSampleFactory(ReferentialIntegrityConstraint readCodeConstraint)
    {
        _readCodeConstraint = readCodeConstraint;
    }

    public SciStoreSample Create(SciStoreHeader header, ResultSuite resultSuite, TEST_SET_RESULT_TYPE testResultSet, IDataLoadEventListener listener)
    {
        var testSetDetailsFactory = new TestSetFactory(_readCodeConstraint);
        var resultFactory = new SciStoreResultFactory(_readCodeConstraint);

        var sample = new SciStoreSample
        {
            LabNumber = header.LabNumber,
            TestReportID = header.TestReportID,
            Results = new List<SciStoreResult>()
        };

        if (testResultSet.TestSetDetails == null)
            throw new Exception($"Sample in Lab {sample.LabNumber}/{sample.TestReportID} has no TestSetDetails block");

        try
        {
            var testSetDetails = testResultSet.TestSetDetails;

            if (testSetDetails.TestIdentifier == null)
                throw new Exception(
                    $"TestIdentifier is a primary key, cannot be null in Lab {sample.LabNumber}/{sample.TestReportID}");

            sample.TestIdentifier = testSetDetails.TestIdentifier.IdValue;
            sample.TestSetDetails = testSetDetailsFactory.CreateFromTestType(testSetDetails, listener);
        }
        catch (Exception e)
        {
            throw new Exception(
                $"Error when creating the TestSetDetails object for Lab {sample.LabNumber}/{sample.TestReportID}: {e}");
        }

        sample.PopulateDenormalisedTestSetDetailsFields();
        PopulateSampleDetails(sample, resultSuite.SampleDetails);

        if (resultSuite.TestResultSets == null)
            throw new Exception($"<TestResultSets> is null in Lab {sample.LabNumber}");

        var testSet = testResultSet.TestSetDetails.TestName;
        if (!testSet.Any())
            throw new Exception($"This TestResultSet has no TestSetDetails: Lab {sample.LabNumber}");

        if (testResultSet.TestResults == null)
        {
            // This TestResultSet does not have any results associated with it, e.g. has a TestSetDetails containing 'Serum', but no TestResults
            return sample;
        }

        foreach (var testResult in testResultSet.TestResults)
            sample.Results.Add(resultFactory.Create(sample, testResult, listener));

        if (sample.TestSetDetails.ClinicalCircumstanceDescription == null)
            throw new Exception(
                $"The TestSet's ClinicalCircumstanceDescription is a primary key and should have been set by now in Lab {sample.LabNumber}/{sample.TestReportID}");

        return sample;
    }

    private void PopulateSampleDetails(SciStoreSample sample, SAMPLE_TYPE sampleDetails)
    {
        // todo - add nicer way of specifying which properties we don't pull out but are potentially available through the API

        if (sampleDetails.SampleAmount != null)
            throw new Exception($"SampleAmount present in Lab {sample.LabNumber}. Please investigate.");

        if (sampleDetails.TissueType != null)
            throw new Exception($"TissueType present in Lab {sample.LabNumber}. Please investigate.");

        // todo: check this, SampleName is null in Tayside Immunology report. For now, setting it to 'Undefined' if a null value is encountered.
        if (sampleDetails.SampleName == null)
        {
            sample.SampleName = "Undefined";
            //throw new Exception("No SampleName in Lab " + LabNumber);
        }
        else
        {
            var sampleNames = sampleDetails.SampleName;
            if (sampleNames.Length > 1)
                throw new Exception($"Not expecting multiple sample names (found {sampleNames.Length})");

            if (sampleNames[0].Item is not string sampleNameItem)
                throw new Exception(
                    $"Could not interpret the sample name as a string in Lab {sample.LabNumber}. Will likely be a 'CLINICAL_INFORMATION_TYPE' but this hasn't been encountered during build, please investigate.");

            sample.SampleName = sampleNameItem;
        }

        if (sampleDetails.SampleRequesterComment != null)
            sample.SampleRequesterComment = string.Join(" ", sampleDetails.SampleRequesterComment);

        if (sampleDetails.ServiceProviderComment != null)
            sample.ServiceProviderComment = string.Join(",", sampleDetails.ServiceProviderComment);

        sample.DateTimeSampled = sampleDetails.DateTimeSampled == DateTime.MinValue ? null : sampleDetails.DateTimeSampled;
        sample.DateTimeReceived = sampleDetails.DateTimeReceived == DateTime.MinValue ? null : sampleDetails.DateTimeReceived;
    }
}