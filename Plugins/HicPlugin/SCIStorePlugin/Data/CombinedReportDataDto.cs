// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SCIStorePlugin.Data;

/// <summary>
/// Simple DTO version of CombinedReportData for XML serialization without WCF proxy dependencies
/// This is a lightweight DTO that captures only essential information for XML serialization
/// </summary>
[XmlRoot("CombinedReportData")]
public class CombinedReportDataDto
{
    [XmlElement("HbExtract")]
    public string HbExtract { get; set; }

    [XmlElement("LabNumber")]
    public string LabNumber { get; set; }

    [XmlElement("TestReportID")]
    public string TestReportID { get; set; }

    [XmlElement("PatientId")]
    public string PatientId { get; set; }

    [XmlElement("ReportCount")]
    public int ReportCount { get; set; }

    [XmlElement("CreationDate")]
    public DateTime CreationDate { get; set; }

    [XmlArray("TestResults")]
    [XmlArrayItem("TestResult")]
    public List<TestResultDto> TestResults { get; set; }

    public CombinedReportDataDto()
    {
        TestResults = new List<TestResultDto>();
        CreationDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Create DTO from CombinedReportData using only simple properties, avoiding WCF proxies
    /// </summary>
    public static CombinedReportDataDto FromCombinedReportData(CombinedReportData combinedReportData)
    {
        if (combinedReportData == null)
            throw new ArgumentNullException(nameof(combinedReportData));

        var dto = new CombinedReportDataDto
        {
            HbExtract = combinedReportData.HbExtract,
            LabNumber = combinedReportData.SciStoreRecord?.LabNumber,
            TestReportID = combinedReportData.SciStoreRecord?.TestReportID,
            PatientId = combinedReportData.SciStoreRecord?.patientid,
            CreationDate = DateTime.UtcNow,
            TestResults = new List<TestResultDto>()
        };

        // Get basic test count without trying to extract complex WCF proxy data
        try
        {
            var testResults = combinedReportData.GetTestResults();
            if (testResults != null)
            {
                dto.ReportCount = testResults.Length;

                // Create simple placeholder test results without WCF proxy parsing
                dto.TestResults = testResults
                    .Select((_, index) => new TestResultDto
                    {
                        Index = index,
                        TestName = $"Test_{index + 1}",
                        ResultValue = "See source data",
                        Units = "N/A",
                        ReferenceRange = "See source data",
                        Status = "Processed"
                    })
                    .ToList();
            }
            else
            {
                dto.ReportCount = 0;
            }
        }
        catch (Exception ex)
        {
            // If we can't extract test results, create a minimal DTO
            dto.ReportCount = 0;
            dto.TestResults.Add(new TestResultDto
            {
                Index = 0,
                TestName = "Data extraction failed",
                ResultValue = ex.Message,
                Units = "Error",
                ReferenceRange = "N/A",
                Status = "Error"
            });
        }

        return dto;
    }

    /// <summary>
    /// Create a summary DTO for reporting purposes
    /// </summary>
    public static CombinedReportDataDto CreateSummary(CombinedReportData combinedReportData)
    {
        if (combinedReportData == null)
            throw new ArgumentNullException(nameof(combinedReportData));

        return new CombinedReportDataDto
        {
            HbExtract = combinedReportData.HbExtract,
            LabNumber = combinedReportData.SciStoreRecord?.LabNumber,
            TestReportID = combinedReportData.SciStoreRecord?.TestReportID,
            PatientId = combinedReportData.SciStoreRecord?.patientid,
            CreationDate = DateTime.UtcNow,
            ReportCount = combinedReportData.GetTestResults()?.Length ?? 0,
            TestResults = new List<TestResultDto>() // No detailed results for summary
        };
    }
}

/// <summary>
/// Simple DTO for individual test results
/// </summary>
public class TestResultDto
{
    [XmlAttribute("index")]
    public int Index { get; set; }

    [XmlElement("TestName")]
    public string TestName { get; set; }

    [XmlElement("ResultValue")]
    public string ResultValue { get; set; }

    [XmlElement("Units")]
    public string Units { get; set; }

    [XmlElement("ReferenceRange")]
    public string ReferenceRange { get; set; }

    [XmlElement("Status")]
    public string Status { get; set; }
}