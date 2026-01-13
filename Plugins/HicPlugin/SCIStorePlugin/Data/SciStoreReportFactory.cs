// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using Rdmp.Core.Validation.Constraints.Secondary;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace SCIStorePlugin.Data;

/// <summary>
/// Factory for creating SciStoreReport objects from CombinedReportData, validating read codes and handling bad data according to configuration.
/// </summary>
public class SciStoreReportFactory
{
    private readonly ReferentialIntegrityConstraint _readCodeConstraint;
    public bool IgnoreBadData { get; set; }

    public SciStoreReportFactory(ReferentialIntegrityConstraint readCodeConstraint)
    {
        _readCodeConstraint = readCodeConstraint;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="combinedReport"></param>
    /// <param name="listener"></param>
    /// <returns></returns>
    /// <exception cref="BadCombinedReportDataException">False to issue an Exception, True to issue warning to <paramref name="listener"/> and return null </exception>
    public SciStoreReport Create(CombinedReportData combinedReport,IDataLoadEventListener listener)
    {
        var sampleFactory = new SciStoreSampleFactory(_readCodeConstraint);

        try
        {
            var report = new SciStoreReport
            {
                Header = SciStoreHeaderFactory.Create(combinedReport),
                Samples = new HashSet<SciStoreSample>()
            };

            // bail out if we don't see any samples in this report
            var serviceResults = combinedReport.GetServiceResults();
            if (serviceResults == null || !serviceResults.Any())
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                    $"No samples found in report {report.Header.LabNumber}-{report.Header.TestReportID}"));
                return report;
            }

            foreach (var serviceResult in combinedReport.GetServiceResults())
            foreach (var testResultSet in serviceResult.TestResultSets)
                report.Samples.Add(sampleFactory.Create(report.Header, serviceResult, testResultSet, listener));

            return report;
        }
        catch (Exception e)
        {
            //if we are not ignoring bad data throw
            if (!IgnoreBadData)
                throw new BadCombinedReportDataException(this, combinedReport, e);

            //we are ignoring bad data
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, "Ignoring bad data", e));
            return null;
        }
    }

}