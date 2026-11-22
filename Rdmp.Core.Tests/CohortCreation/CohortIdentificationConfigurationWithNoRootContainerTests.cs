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
using Rdmp.Core.Providers;
using Rdmp.Core.QueryBuilding;
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
    public void CloneIntoExistingConfiguration_DeletesTheContainersTheTargetHad_ButNotTheirCohortSets()
    {
        var source = WhenIHaveAConfigurationWithNoRootContainer();
        var root = WhenIHaveA<CohortAggregateContainer>();
        var target = root.GetCohortIdentificationConfiguration();
        var subContainer = new CohortAggregateContainer(Repository, SetOperation.INTERSECT);
        root.AddChild(subContainer);
        var cohortSet = WhenIHaveA<AggregateConfiguration>();
        subContainer.AddChild(cohortSet, 1);

        source.CloneIntoExistingConfiguration(ThrowImmediatelyCheckNotifier.Quiet, target, false);

        Assert.Multiple(() =>
        {
            // nothing uses the containers any more, and there is nowhere they could be seen or deleted
            Assert.That(root.Exists(), Is.False);
            Assert.That(subContainer.Exists(), Is.False);

            // the cohort set is left as an orphan, like that of any container that is deleted
            Assert.That(cohortSet.Exists());
            Assert.That(cohortSet.GetCohortAggregateContainerIfAny(), Is.Null);
        });
    }

    [Test]
    public void Merge_LeavesItOut()
    {
        var withRoot = WhenIHaveAConfigurationWithTwoCohortSets();
        var withoutRoot = WhenIHaveAConfigurationWithNoRootContainer();
        var configurationsBefore = Repository.GetAllObjects<CohortIdentificationConfiguration>().Length;

        var merged = new CohortIdentificationConfigurationMerger(Repository)
            .Merge([withRoot, withoutRoot], SetOperation.UNION);

        var mergedRoot = merged.RootCohortAggregateContainer;
        var problems = new CatalogueProblemProvider();
        problems.RefreshProblems(new CatalogueChildProvider(Repository, null, ThrowImmediatelyCheckNotifier.Quiet, null));

        Assert.Multiple(() =>
        {
            // the only subcontainer is the root container of the configuration that has one
            Assert.That(mergedRoot.GetSubContainers(), Has.Length.EqualTo(1));
            Assert.That(mergedRoot.GetAllAggregateConfigurationsRecursively(), Has.Count.EqualTo(2));

            // so the cohort builder can run the merged configuration: an empty subcontainer is a problem to it, and stops
            // the query being built
            Assert.That(
                mergedRoot.GetAllSubContainersRecursively().Prepend(mergedRoot)
                    .Select(container => problems.DescribeProblem(container)), Is.All.Null);
            Assert.That(() => new CohortQueryBuilder(merged, null).SQL, Does.Contain("UNION"));

            // the merge works on clones: the original is as it was, and the clones are gone
            Assert.That(withoutRoot.RootCohortAggregateContainer_ID, Is.Null);
            Assert.That(Repository.GetAllObjects<CohortIdentificationConfiguration>(),
                Has.Length.EqualTo(configurationsBefore + 1));
        });
    }

    [Test]
    public void Import_LeavesItOut()
    {
        var into = WhenIHaveA<CohortAggregateContainer>();
        var withRoot = WhenIHaveAConfigurationWithTwoCohortSets();
        var patientIndexTable = WhenIHaveA<JoinableCohortAggregateConfiguration>();
        var withoutRoot = WhenIHaveAConfigurationWithNoRootContainer(patientIndexTable.CohortIdentificationConfiguration);
        var configurationsBefore = Repository.GetAllObjects<CohortIdentificationConfiguration>().Length;
        var aggregatesBefore = Repository.GetAllObjects<AggregateConfiguration>().Length;
        var patientIndexTablesBefore = Repository.GetAllObjects<JoinableCohortAggregateConfiguration>().Length;

        new CohortIdentificationConfigurationMerger(Repository).Import([withoutRoot, withRoot], into);

        Assert.Multiple(() =>
        {
            // the only subcontainer is the root container of the configuration that has one
            Assert.That(into.GetSubContainers(), Has.Length.EqualTo(1));
            Assert.That(into.GetAllAggregateConfigurationsRecursively(), Has.Count.EqualTo(2));

            // nothing is cloned for the configuration that is left out, so nothing of it is left behind
            Assert.That(Repository.GetAllObjects<AggregateConfiguration>(), Has.Length.EqualTo(aggregatesBefore + 2));
            Assert.That(Repository.GetAllObjects<JoinableCohortAggregateConfiguration>(),
                Has.Length.EqualTo(patientIndexTablesBefore));
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
    /// Returns a new configuration whose root container has two cohort sets, which the cohort builder has no problem with
    /// </summary>
    private CohortIdentificationConfiguration WhenIHaveAConfigurationWithTwoCohortSets()
    {
        var root = WhenIHaveA<CohortAggregateContainer>();
        var cic = root.GetCohortIdentificationConfiguration();

        for (var order = 1; order <= 2; order++)
        {
            var cohortSet = WhenIHaveA<AggregateConfiguration>();

            // a cohort set can be cloned only when it is clear which of its Catalogue's columns identifies the patient
            var identifier = cohortSet.AggregateDimensions.Single().ExtractionInformation;
            identifier.IsExtractionIdentifier = true;
            identifier.SaveToDatabase();

            // and cohort sets can be combined in one query only when their tables are known to be on the same server
            var table = identifier.ColumnInfo.TableInfo;
            table.Server = "localhost";
            table.Database = "MyDb";
            table.SaveToDatabase();

            cic.EnsureNamingConvention(cohortSet);
            root.AddChild(cohortSet, order);
        }

        return cic;
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
