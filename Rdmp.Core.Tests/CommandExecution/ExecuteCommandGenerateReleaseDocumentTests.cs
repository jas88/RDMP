// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.DataExport.Data;
using Tests.Common.Scenarios;

namespace Rdmp.Core.Tests.CommandExecution;

/// <summary>
/// Tests that <see cref="ExecuteCommandGenerateReleaseDocument"/> is offered for an extracted configuration whose
/// cohort is still in the cohort database, and not for one whose cohort has been deleted from it (an orphan)
/// </summary>
internal class ExecuteCommandGenerateReleaseDocumentTests : TestsRequiringACohort
{
    [Test]
    public void CohortIsInTheCohortDatabase_CommandIsPossible()
    {
        Assert.That(_extractableCohort.GetExternalData(), Is.Not.SameAs(ExternalCohortDefinitionData.Orphan));

        var cmd = new ExecuteCommandGenerateReleaseDocument(Substitute.For<IBasicActivateItems>(),
            CreateExtractedConfiguration(_extractableCohort));

        Assert.That(cmd.IsImpossible, Is.False, cmd.ReasonCommandImpossible);
    }

    [Test]
    public void CohortWasDeletedFromTheCohortDatabase_CommandIsImpossible()
    {
        var orphan = CreateOrphanCohort();
        Assert.That(orphan.GetExternalData(), Is.SameAs(ExternalCohortDefinitionData.Orphan));

        var cmd = new ExecuteCommandGenerateReleaseDocument(Substitute.For<IBasicActivateItems>(),
            CreateExtractedConfiguration(orphan));

        Assert.Multiple(() =>
        {
            Assert.That(cmd.IsImpossible);
            Assert.That(cmd.ReasonCommandImpossible, Is.EqualTo("Cohort did not exist"));
        });
    }

    /// <summary>
    /// Creates an extraction configuration for the <paramref name="cohort"/> with the audit record of one extracted
    /// dataset.  Without that record the command is impossible whatever the state of the cohort.
    /// </summary>
    private ExtractionConfiguration CreateExtractedConfiguration(IExtractableCohort cohort)
    {
        var project = new Project(DataExportRepository, nameof(ExecuteCommandGenerateReleaseDocumentTests));

        var configuration = new ExtractionConfiguration(DataExportRepository, project)
        {
            Cohort_ID = cohort.ID
        };
        configuration.SaveToDatabase();

        _ = new CumulativeExtractionResults(DataExportRepository, configuration, CustomExtractableDataSet, "SELECT 1");

        return configuration;
    }

    /// <summary>
    /// Creates a cohort and then deletes its definition from the cohort database
    /// </summary>
    private ExtractableCohort CreateOrphanCohort()
    {
        var definitions = _externalCohortTable.DiscoverDefinitionTable();

        var id = definitions.Insert(new Dictionary<string, object>
        {
            { "projectNumber", projectNumberInTestData },
            { "version", 2 },
            { "description", "Deleted after the cohort was created" }
        });

        var cohort = new ExtractableCohort(DataExportRepository, _externalCohortTable, id);

        using var con = _cohortDatabase.Server.GetConnection();
        con.Open();
        using var delete =
            _cohortDatabase.Server.GetCommand($"DELETE FROM {definitionTableName} WHERE id = {id}", con);
        Assert.That(delete.ExecuteNonQuery(), Is.EqualTo(1));

        return cohort;
    }
}
