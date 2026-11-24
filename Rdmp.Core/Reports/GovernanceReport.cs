// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Governance;
using Rdmp.Core.Repositories;

namespace Rdmp.Core.Reports;

/// <summary>
/// Processes all GovernancePeriod and Catalogues into a CSV report about which datasets are covered by which governance periods, which periods have expired (and there
/// is no corresponding follow on GovernancePeriod) and which Catalogues are not covered by any governance.
/// </summary>
public class GovernanceReport : DocXHelper
{
    private readonly IDetermineDatasetTimespan _timespanCalculator;
    private readonly ICatalogueRepository _repository;

    /// <summary>
    /// The report has sections whose rows differ in length, which the writer rejects unless told otherwise
    /// </summary>
    private static readonly CsvWriterOptions CsvOptions = new()
    {
        ValidateFieldCount = false
    };

    /// <summary>
    /// The line ending the <see cref="CsvWriter"/> uses by default, used for the blank lines between sections
    /// </summary>
    private const string NewLine = "\r\n";

    public GovernanceReport(IDetermineDatasetTimespan timespanCalculator, ICatalogueRepository repository)
    {
        _timespanCalculator = timespanCalculator;
        _repository = repository;
    }

    public void GenerateReport()
    {
        var f = GetUniqueFilenameInWorkArea("GovernanceReport", ".csv");

        using (var s = new StreamWriter(f.FullName))
        {
            using var writer = CsvWriter.Create(s, CsvOptions);
            writer.WriteField("Extractable Datasets");
            writer.EndRecord();

            writer.WriteField("Folder");
            writer.WriteField("Catalogue");
            writer.WriteField("Current Governance");
            writer.WriteField("Dataset Period");
            writer.WriteField("Description");
            writer.EndRecord();


            var govs = _repository.GetAllObjects<GovernancePeriod>()
                .ToDictionary(period => period, period => period.GovernedCatalogues.ToArray());

            foreach (var catalogue in _repository.GetAllObjects<Catalogue>()
                         .Where(c => c.CatalogueItems.Any(ci => ci.ExtractionInformation != null))
                         .OrderBy(c => c.Name))
            {
                if (catalogue.IsDeprecated || catalogue.IsInternalDataset)
                    continue;

                //get active governances
                var activeGovs = govs.Where(kvp => kvp.Value.Contains(catalogue) && !kvp.Key.IsExpired())
                    .Select(g => g.Key).ToArray();
                var expiredGovs = govs.Where(kvp => kvp.Value.Contains(catalogue) && kvp.Key.IsExpired())
                    .Select(g => g.Key).ToArray();

                string relevantGovernance = null;

                if (activeGovs.Any())
                    relevantGovernance = string.Join(",", activeGovs.Select(gov => gov.Name));
                else if (expiredGovs.Any())
                    relevantGovernance =
                        $"No Current Governance (Expired Governances: {string.Join(",", expiredGovs.Select(gov => gov.Name))})";
                else
                    relevantGovernance = "No Governance Required";


                //write the results out to Excel
                writer.WriteField(catalogue.Folder);
                writer.WriteField(catalogue.Name);
                writer.WriteField(relevantGovernance);
                writer.WriteField(_timespanCalculator.GetHumanReadableTimespanIfKnownOf(catalogue, true, out _));
                writer.WriteField(ShortenDescription(catalogue.Description));

                writer.EndRecord();
            }

            WriteBlankLine(writer, s);

            // next section header
            writer.WriteField("Active Governance");

            OutputGovernanceList(govs, writer, false);

            WriteBlankLine(writer, s);

            // next section header
            writer.WriteField("Expired Governance");

            OutputGovernanceList(govs, writer, true);
        }

        ShowFile(f);
    }

    /// <summary>
    /// Writes an empty line to <paramref name="underlying"/>, the stream that <paramref name="writer"/> writes to.  The
    /// <see cref="CsvWriter"/> cannot do this itself: a record needs at least one field, and an empty field is written as "".
    /// </summary>
    private static void WriteBlankLine(CsvWriter writer, StreamWriter underlying)
    {
        // the writer buffers its records, so they must reach the stream before the blank line does
        writer.Flush();
        underlying.Write(NewLine);
    }

    private static string ShortenDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return description;

        description = description.Replace("\r\n", " ");
        description = description.Replace("\n", " ");

        return description.Length >= 100 ? $"{description[..100]}..." : description;
    }

    /// <summary>
    /// Pass false for active or true for expired
    /// </summary>
    /// <param name="govs"></param>
    /// <param name="writer"></param>
    /// <param name="expired"></param>
    private static void OutputGovernanceList(Dictionary<GovernancePeriod, ICatalogue[]> govs, CsvWriter writer,
        bool expired)
    {
        //headers for this section
        writer.WriteField("Governance");
        writer.WriteField("Period Name");
        writer.WriteField("Catalogues");
        writer.WriteField("Approval Start");
        writer.WriteField("Approval End");
        writer.WriteField("Documents");
        writer.EndRecord();

        foreach (var kvp in govs)
        {
            //if governance period does not have any Catalogues associated with it skip it
            if (!kvp.Value.Any())
                continue;

            //if it is expiry does not match the callers desired expiry state then skip it
            if (kvp.Key.IsExpired() != expired)
                continue;

            writer.WriteField(kvp.Key.Name);
            writer.WriteField(string.Join(",", kvp.Value.Select(cata => cata.Name)));
            writer.WriteField(kvp.Key.StartDate);

            // written as a date (not EndDate.ToString()) so that it has the same format as the start date
            if (kvp.Key.EndDate == null)
                writer.WriteField("Never Expires");
            else
                writer.WriteField(kvp.Key.EndDate.Value);

            writer.WriteField(string.Join(",", kvp.Key.GovernanceDocuments.Select(doc => doc.GetFilenameOnly())));

            writer.EndRecord();
        }
    }
}