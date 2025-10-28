// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using SCIStore.SciStoreServices81;

namespace SCIStorePlugin.Data;

/// <summary>
/// Container for SCIStore report data combining the healthboard extract indicator, SCIStore record metadata, and the full investigation report with service results and clinical information.
/// </summary>
public class CombinedReportData
{
    public string HbExtract { get; set; } // This field is not in the SciStore data, it is only available at time of extraction
    public SciStoreRecord SciStoreRecord { get; set; }
    public InvestigationReport InvestigationReport { get; set; }

    public TEST_RESULT_TYPE[] GetTestResults()
    {
        var testResults = new List<TEST_RESULT_TYPE>();
        foreach (var result in InvestigationReport.ReportData.ServiceResult)
        {
            foreach (var testSample in result.TestResultSets)
            {
                testResults.AddRange(testSample.TestResults);
            }
        }

        return testResults.ToArray();
    }

    public CLINICAL_CIRCUMSTANCE_TYPE[] GetSampleTypes()
    {
        var serviceResults = InvestigationReport.ReportData.ServiceResult.ToList();
            
        if (serviceResults.Count == 0)
            throw new Exception("This is unlikely to be valid, but investigate and add logic/remove throw if it is");

        if (serviceResults.Count > 1)
            throw new Exception("This may be valid but the code doesn't deal with this case. Investigate and add logic.");

        var serviceResult = serviceResults[0]; // todo: not sure if this is valid, but handlers above will capture other cases
        return serviceResult.SampleDetails.SampleName;
    }

    public ResultSuite[] GetServiceResults()
    {
        return InvestigationReport.ReportData.ServiceResult;
    }
}