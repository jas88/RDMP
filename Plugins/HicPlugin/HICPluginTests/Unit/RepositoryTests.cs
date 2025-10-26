// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using HICPluginTests;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;
using SCIStorePlugin.Repositories;

namespace SCIStorePluginTests.Unit;

class RepositoryTests
{
    [Test]
    public void DeserialisationOfXMLInterferingWithFloats()
    {
        var data = CombinedReportXmlDeserializer.DeserializeFromXmlString(TestReports.report_with_float_values);

        var readCodeConstraint = new MockReferentialIntegrityConstraint(static x=>x.Equals("TTTT.")?null:"Not a read code");
        var reportFactory = new SciStoreReportFactory(readCodeConstraint);
        var report = reportFactory.Create(data, ThrowImmediatelyDataLoadEventListener.Quiet);

        var bloodSample = report.Samples.First();
        var result = bloodSample.Results.First(static r => r.ReadCodeValue.Equals("TTTT."));

       Assert.That(result.QuantityValue ?? 0.0m, Is.EqualTo(8.9));
    }

    [Test]
    public void TestDataTableRepository()
    {
        var report = new SciStoreReport
        {
            Header = new SciStoreHeader
            {
                CHI = "1010101010",
                Discipline = "Test"
            },

            Samples = new HashSet<SciStoreSample>
            {
                new()
                {
                    SampleName = "Blood",
                    LabNumber = "123",
                    TestIdentifier = "TESTID",
                    TestReportID = "234",
                    Results = new []
                    {
                        new SciStoreResult
                        {
                            LabNumber = "123",
                            TestIdentifier = "TESTID",
                            TestReportID = "234",
                            ClinicalCircumstanceDescription = "TESTCCD",
                            ReadCodeValue = "T1",
                            LocalClinicalCodeValue = "TEST_LOCAL",
                            QuantityValue = new decimal(15.2)
                        },
                        new SciStoreResult
                        {
                            LabNumber = "123",
                            TestIdentifier = "ANOTHERTESTID",
                            TestReportID = "234",
                            ClinicalCircumstanceDescription = "ANOTHERTESTIDCCD",
                            ReadCodeValue = "AT1",
                            LocalClinicalCodeValue = "ANOTHERTEST_LOCAL"
                        }
                    }
                }
            }
        };

        var dataTableSchemaSource = new TestDataTableSchemaProvider();
        var repo = new SciStoreDataTableRepository(dataTableSchemaSource);

        var reports = new List<SciStoreReport> {report};
        var listener = new TestDataLoadEventListener();
            
        repo.Create(reports, listener);

       Assert.That(repo.HeadersTable.Rows.Count, Is.EqualTo(1));
       Assert.That(repo.SampleDetailsTable.Rows.Count, Is.EqualTo(1));
       Assert.That(repo.ResultsTable.Rows.Count, Is.EqualTo(2));

       Assert.That(repo.ResultsTable.Rows[0]["TestIdentifier"], Is.EqualTo("TESTID"));
       Assert.That(repo.ResultsTable.Rows[1]["LocalClinicalCodeValue"], Is.EqualTo("ANOTHERTEST_LOCAL"));

        Assert.That(repo.ResultsTable.Rows[0]["QuantityValue"].ToString().Length == 4, Is.True);
    }

    [Test]
    public void TestInvalidCharacterReplacement()
    {
        const string badXmlString = @"<TestInterpretation>
                  <Interpretation>
                    for 4 days.&#x1B;(s3B XX to&#x1B;(s&#x1B;(s3Bexclude another pathology.&#x1B;(s0B";

        const string expectedXmlString = @"<TestInterpretation>
                  <Interpretation>
                    for 4 days.[b] XX to[unknown|x1B;(s][b]exclude another pathology.[/b]";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(badXmlString));
        var actualString = CombinedReportXmlDeserializer.RemoveInvalidCharactersFromStream(stream);
       Assert.That(actualString, Is.EqualTo(expectedXmlString));
    }
}

internal class TestDataLoadEventListener : IDataLoadEventListener
{
    public void OnNotify(object sender, NotifyEventArgs e)
    {
        Console.WriteLine($"{e.ProgressEventType}: {e.Message}");
    }

    public void OnProgress(object sender, ProgressEventArgs e)
    {
        Console.WriteLine($"{e}: {e.Progress.Value}");
    }
}

internal class TestDataTableSchemaProvider : IDataTableSchemaSource
{
    public void SetSchema(DataTable dataTable)
    {
        switch (dataTable.TableName)
        {
            case "Header":
                dataTable.Columns.Add(new DataColumn("CHI", typeof(string)));
                break;
            case "SampleDetails":
                dataTable.Columns.Add(new DataColumn("SampleName", typeof(string)));
                break;
            case "Results":
                dataTable.Columns.Add(new DataColumn("TestIdentifier", typeof(string)));
                dataTable.Columns.Add(new DataColumn("LocalClinicalCodeValue", typeof(string)));
                dataTable.Columns.Add(new DataColumn("QuantityValue", typeof (float)));
                break;
            default:
                throw new Exception("Unknown table");
        }

    }
}