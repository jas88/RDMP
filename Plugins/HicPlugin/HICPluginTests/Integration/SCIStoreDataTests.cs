// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System.IO;
using System.Linq;
using System.Xml.Serialization;
using HICPluginTests;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;

namespace SCIStorePluginTests.Integration;


public class SCIStoreDataTests
{
    [Test]
    [Category("Integration")]
    [Ignore("unit-test.xml needs added to project as a resource")]
    public void TestResultsDuplicationInTestSetDetails()
    {
        var testFilePath = @"";
        var xmlSerialiser = new XmlSerializer(typeof (CombinedReportData));
        CombinedReportData data = null;
        using (var fs = new FileStream(testFilePath, FileMode.Open))
        {
            data = (CombinedReportData) xmlSerialiser.Deserialize(fs);
        }

        var readCodeConstraint = new MockReferentialIntegrityConstraint();

        var reportFactory = new SciStoreReportFactory(readCodeConstraint);
        var report = reportFactory.Create(data, ThrowImmediatelyDataLoadEventListener.Quiet);

       Assert.That(report.Samples.Count, Is.EqualTo(7));

        var totalNumResults = report.Samples.Aggregate(0, static (s, n) => s + n.Results.Count);
       Assert.That(totalNumResults, Is.EqualTo(21));

    }


    [Test]
    [Category("Integration")]
    [Ignore("unit-test.xml needs added to project as a resource")]
    public void Test_MultipleTestResultOrdersThatAreTheSame()
    {

        var testFilePath = @"";
        var xmlSerialiser = new XmlSerializer(typeof(CombinedReportData));
        CombinedReportData data = null;
        using (var fs = new FileStream(testFilePath, FileMode.Open))
        {
            data = (CombinedReportData)xmlSerialiser.Deserialize(fs);
        }

        var readCodeConstraint = new MockReferentialIntegrityConstraint();
        var reportFactory = new SciStoreReportFactory(readCodeConstraint);
        var report = reportFactory.Create(data, ThrowImmediatelyDataLoadEventListener.Quiet);

       Assert.That(report.Samples.Count, Is.EqualTo(7));

        var totalNumResults = report.Samples.Aggregate(0, static (s, n) => s + n.Results.Count);
       Assert.That(totalNumResults, Is.EqualTo(21));

        //artificially introduce duplication
        foreach (var sciStoreSample in report.Samples)
        {

            foreach (var sciStoreResult in sciStoreSample.Results)
            {
                sciStoreResult.ClinicalCircumstanceDescription = "Test for fish presence";
                sciStoreResult.TestResultOrder = 0;

            }
            sciStoreSample.ResolveTestResultOrderDuplication();
        }

        var totalNumResultsAfterResolvingArtificiallyCreatedDuplication = report.Samples.Aggregate(0, static (s, n) => s + n.Results.Count);
       Assert.That(totalNumResultsAfterResolvingArtificiallyCreatedDuplication, Is.EqualTo(7));


    }
}