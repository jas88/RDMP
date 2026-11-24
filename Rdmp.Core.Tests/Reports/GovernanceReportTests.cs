// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Governance;
using Rdmp.Core.Reports;
using Rdmp.Core.Repositories;
using Tests.Common;

namespace Rdmp.Core.Tests.Reports;

internal class GovernanceReportTests : UnitTests
{
    private sealed class FixedTimespan : IDetermineDatasetTimespan
    {
        public string GetHumanReadableTimespanIfKnownOf(Catalogue catalogue, bool discardOutliers,
            out DateTime? accurateAsOf)
        {
            accurateAsOf = null;
            return "2001-2002";
        }

        public Tuple<DateTime?, DateTime?> GetMachineReadableTimespanIfKnownOf(Catalogue catalogue,
            bool discardOutliers, out DateTime? accurateAsOf)
        {
            accurateAsOf = null;
            return Tuple.Create<DateTime?, DateTime?>(null, null);
        }
    }

    private static string WriteReport(ICatalogueRepository repository)
    {
        using var sw = new StringWriter();
        new GovernanceReport(new FixedTimespan(), repository).WriteReport(sw);
        return sw.ToString();
    }

    [Test]
    public void WriteReport_NoCataloguesOrGovernance_WritesSectionsSeparatedByBlankLines()
    {
        // the repository of the fixture is shared by its tests, so use an empty one
        Assert.That(WriteReport(new MemoryDataExportRepository().CatalogueRepository), Is.EqualTo(
            "Extractable Datasets\r\n" +
            "Folder,Catalogue,Current Governance,Dataset Period,Description\r\n" +
            "\r\n" +
            "Active Governance,Governance,Period Name,Catalogues,Approval Start,Approval End,Documents\r\n" +
            "\r\n" +
            "Expired Governance,Governance,Period Name,Catalogues,Approval Start,Approval End,Documents\r\n"));
    }

    [Test]
    public void WriteReport_ActiveAndExpiredGovernance_DoesNotDependOnCulture()
    {
        var catalogue = WhenIHaveA<ExtractionInformation>().CatalogueItem.Catalogue;
        catalogue.Name = "My, Catalogue";
        catalogue.Description = "Line one\r\nline two";
        catalogue.SaveToDatabase();

        var active = new GovernancePeriod(Repository.CatalogueRepository, "Active one")
        {
            StartDate = new DateTime(2001, 2, 3)
        };
        active.SaveToDatabase();
        active.CreateGovernanceRelationshipTo(catalogue);

        var expired = new GovernancePeriod(Repository.CatalogueRepository, "Expired one")
        {
            StartDate = new DateTime(2001, 2, 3),
            EndDate = new DateTime(2002, 12, 31)
        };
        expired.SaveToDatabase();
        expired.CreateGovernanceRelationshipTo(catalogue);

        var before = CultureInfo.CurrentCulture;
        string report;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            report = WriteReport(Repository.CatalogueRepository);
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }

        Assert.That(report, Is.EqualTo(
            "Extractable Datasets\r\n" +
            "Folder,Catalogue,Current Governance,Dataset Period,Description\r\n" +
            "\\,\"My, Catalogue\",Active one,2001-2002,Line one line two\r\n" +
            "\r\n" +
            "Active Governance,Governance,Period Name,Catalogues,Approval Start,Approval End,Documents\r\n" +
            "Active one,\"My, Catalogue\",2001-02-03T00:00:00.0000000,Never Expires,\r\n" +
            "\r\n" +
            "Expired Governance,Governance,Period Name,Catalogues,Approval Start,Approval End,Documents\r\n" +
            "Expired one,\"My, Catalogue\",2001-02-03T00:00:00.0000000,2002-12-31T00:00:00.0000000,\r\n"));
    }
}
