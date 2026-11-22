// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Linq;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Aggregation;
using Rdmp.Core.Curation.Data.Cohort;
using Rdmp.Core.Curation.Data.Cohort.Joinables;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Tests.Common;

namespace Rdmp.Core.Tests.CohortCreation;

/// <summary>
/// Tests for cloning, merging, importing and listing the tables of a <see cref="CohortIdentificationConfiguration"/> that
/// has no root container. A configuration has none until
/// <see cref="CohortIdentificationConfiguration.CreateRootContainerIfNotExists"/> is called, which creating one with the
/// NewObject command or its constructor does not do
/// </summary>
internal class CohortIdentificationConfigurationWithNoRootContainerTests : UnitTests
{
    [Test]
    public void CreateClone_CopiesThePatientIndexTablesAndParameters_AndHasNoRootContainerEither()
    {
        var joinable = WhenIHaveA<JoinableCohortAggregateConfiguration>();
        var cic = WhenIHaveAConfigurationWithNoRootContainer(joinable.CohortIdentificationConfiguration);
        new AnyTableSqlParameter(Repository, cic, "DECLARE @age AS int") { Value = "5" }.SaveToDatabase();

        var clone = cic.CreateClone(ThrowImmediatelyCheckNotifier.Quiet);

        Assert.Multiple(() =>
        {
            Assert.That(clone.RootCohortAggregateContainer_ID, Is.Null);
            Assert.That(clone.ClonedFrom_ID, Is.EqualTo(cic.ID));
            Assert.That(clone.GetAllParameters().Select(static p => p.ParameterSQL),
                Is.EqualTo(new[] { "DECLARE @age AS int" }));

            // the clone has a copy of the patient index table, not the original
            Assert.That(clone.GetAllJoinables(), Has.Length.EqualTo(1));
            Assert.That(clone.GetAllJoinables().Select(static j => j.AggregateConfiguration_ID),
                Has.None.EqualTo(joinable.AggregateConfiguration_ID));
        });
    }

    [Test]
    public void CloneIntoExistingConfiguration_LeavesTheTargetWithNoRootContainerEither()
    {
        // as when a configuration is reverted to a version of it that was saved before it had a root container
        var source = WhenIHaveAConfigurationWithNoRootContainer();
        var target = WhenIHaveA<CohortAggregateContainer>().GetCohortIdentificationConfiguration();

        var result = source.CloneIntoExistingConfiguration(ThrowImmediatelyCheckNotifier.Quiet, target, false);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(target));
            Assert.That(target.RootCohortAggregateContainer_ID, Is.Null);
        });
    }

    [Test]
    public void Merge_TreatsItLikeAConfigurationWithAnEmptyRootContainer()
    {
        var withRoot = WhenIHaveA<CohortAggregateContainer>().GetCohortIdentificationConfiguration();
        var withoutRoot = WhenIHaveAConfigurationWithNoRootContainer();
        var configurationsBefore = Repository.GetAllObjects<CohortIdentificationConfiguration>().Length;

        var merged = new CohortIdentificationConfigurationMerger(Repository)
            .Merge([withRoot, withoutRoot], SetOperation.UNION);

        Assert.Multiple(() =>
        {
            // one subcontainer for each configuration that was merged
            Assert.That(merged.RootCohortAggregateContainer.GetSubContainers(), Has.Length.EqualTo(2));

            // the merge works on clones: the original is as it was, and the clones are gone
            Assert.That(withoutRoot.RootCohortAggregateContainer_ID, Is.Null);
            Assert.That(Repository.GetAllObjects<CohortIdentificationConfiguration>(),
                Has.Length.EqualTo(configurationsBefore + 1));
        });
    }

    [Test]
    public void Import_AddsAnEmptyContainerForIt()
    {
        var into = WhenIHaveA<CohortAggregateContainer>();
        var withoutRoot = WhenIHaveAConfigurationWithNoRootContainer();
        var configurationsBefore = Repository.GetAllObjects<CohortIdentificationConfiguration>().Length;

        new CohortIdentificationConfigurationMerger(Repository).Import([withoutRoot], into);

        Assert.Multiple(() =>
        {
            Assert.That(into.GetSubContainers(), Has.Length.EqualTo(1));
            Assert.That(into.GetAllAggregateConfigurationsRecursively(), Is.Empty);

            // the import works on a clone: the original is as it was, and the clone is gone
            Assert.That(withoutRoot.RootCohortAggregateContainer_ID, Is.Null);
            Assert.That(Repository.GetAllObjects<CohortIdentificationConfiguration>(),
                Has.Length.EqualTo(configurationsBefore));
        });
    }

    [Test]
    public void GetDistinctTableInfos_FindsNoTables()
    {
        Assert.That(WhenIHaveAConfigurationWithNoRootContainer().GetDistinctTableInfos(), Is.Empty);
    }

    [Test]
    public void GetDistinctTableInfos_WithARootContainer_FindsTheTablesOfItsCohortSets()
    {
        var container = WhenIHaveA<CohortAggregateContainer>();
        var cohortSet = WhenIHaveA<AggregateConfiguration>();
        container.AddChild(cohortSet, 0);

        var tables = container.GetCohortIdentificationConfiguration().GetDistinctTableInfos();

        Assert.That(tables, Is.EqualTo(new[] { cohortSet.AggregateDimensions.Single().ColumnInfo.TableInfo }));
    }

    /// <summary>
    /// Returns <paramref name="cic"/> (or a new configuration) after checking that it has no root container
    /// </summary>
    private CohortIdentificationConfiguration WhenIHaveAConfigurationWithNoRootContainer(
        CohortIdentificationConfiguration cic = null)
    {
        cic ??= WhenIHaveA<CohortIdentificationConfiguration>();
        Assert.That(cic.RootCohortAggregateContainer_ID, Is.Null);
        return cic;
    }
}
