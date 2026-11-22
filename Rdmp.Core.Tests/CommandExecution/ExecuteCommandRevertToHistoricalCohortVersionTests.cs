// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.Curation.Data.Aggregation;
using Rdmp.Core.Curation.Data.Cohort;

namespace Rdmp.Core.Tests.CommandExecution;

/// <summary>
/// Tests for what <see cref="ExecuteCommandRevertToHistoricalCohortVersion"/> does with what the configuration had before
/// it was reverted
/// </summary>
internal class ExecuteCommandRevertToHistoricalCohortVersionTests : CommandCliTests
{
    [Test]
    public void Execute_GivesTheConfigurationACopyOfTheContainersOfTheVersion_AndDeletesThoseItHad()
    {
        var root = WhenIHaveA<CohortAggregateContainer>();
        var cic = root.GetCohortIdentificationConfiguration();
        var subContainer = new CohortAggregateContainer(Repository, SetOperation.EXCEPT);
        root.AddChild(subContainer);
        var cohortSets = new List<AggregateConfiguration> { WhenIHaveACohortSetIn(root), WhenIHaveACohortSetIn(subContainer) };
        var version = WhenIHaveAVersionOf(cic);
        var versionRoot = version.RootCohortAggregateContainer;

        // since the version was saved, the subcontainer has been changed and a cohort set has been added
        subContainer.Operation = SetOperation.INTERSECT;
        subContainer.SaveToDatabase();
        cohortSets.Add(WhenIHaveACohortSetIn(root));
        var containersBefore = Repository.GetAllObjects<CohortAggregateContainer>().Length;

        new ExecuteCommandRevertToHistoricalCohortVersion(GetActivator(), cic, version).Execute();

        var newRoot = cic.RootCohortAggregateContainer;
        Assert.Multiple(() =>
        {
            // the configuration has a copy of what the version has, in containers of its own
            Assert.That(newRoot.ID, Is.Not.EqualTo(root.ID).And.Not.EqualTo(versionRoot.ID));
            Assert.That(newRoot.GetAggregateConfigurations(), Has.Length.EqualTo(1));
            Assert.That(newRoot.GetSubContainers().Select(static c => c.Operation),
                Is.EqualTo(new[] { SetOperation.EXCEPT }));
            Assert.That(newRoot.GetAllAggregateConfigurationsRecursively(), Has.Count.EqualTo(2));
            Assert.That(newRoot.GetAllAggregateConfigurationsRecursively().Intersect(cohortSets), Is.Empty);

            // the containers it had are deleted, so there are as many containers as before
            Assert.That(root.Exists(), Is.False);
            Assert.That(subContainer.Exists(), Is.False);
            Assert.That(Repository.GetAllObjects<CohortAggregateContainer>(), Has.Length.EqualTo(containersBefore));

            // their cohort sets are left as orphans, like those of any container that is deleted
            Assert.That(cohortSets.Select(static s => s.Exists()), Is.All.True);
            Assert.That(cohortSets.Select(static s => s.GetCohortAggregateContainerIfAny()), Is.All.Null);

            // the version is as it was
            Assert.That(version.RootCohortAggregateContainer_ID, Is.EqualTo(versionRoot.ID));
            Assert.That(versionRoot.GetAllAggregateConfigurationsRecursively(), Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void Execute_ForAVersionSavedBeforeTheConfigurationHadARootContainer_LeavesItWithNone()
    {
        var cic = WhenIHaveA<CohortIdentificationConfiguration>();
        Assert.That(cic.RootCohortAggregateContainer_ID, Is.Null);
        var version = WhenIHaveAVersionOf(cic);

        // opening the configuration in the cohort builder gives it a root container, to which cohort sets are added
        cic.CreateRootContainerIfNotExists();
        var root = cic.RootCohortAggregateContainer;
        var cohortSet = WhenIHaveACohortSetIn(root);

        new ExecuteCommandRevertToHistoricalCohortVersion(GetActivator(), cic, version).Execute();

        Assert.Multiple(() =>
        {
            Assert.That(cic.RootCohortAggregateContainer_ID, Is.Null);
            Assert.That(root.Exists(), Is.False);

            // the cohort set is left as an orphan, like that of any container that is deleted
            Assert.That(cohortSet.Exists());
            Assert.That(cohortSet.GetCohortAggregateContainerIfAny(), Is.Null);
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Execute_KeepsTheContainersTheConfigurationHad_IfAnotherConfigurationUsesOneAsItsRootContainer(
        bool theSubContainer)
    {
        var root = WhenIHaveA<CohortAggregateContainer>();
        var cic = root.GetCohortIdentificationConfiguration();
        var subContainer = new CohortAggregateContainer(Repository, SetOperation.EXCEPT);
        root.AddChild(subContainer);
        var cohortSet = WhenIHaveACohortSetIn(subContainer);
        var version = WhenIHaveAVersionOf(cic);

        // RDMP does not share containers between configurations, but nothing in the database prevents it
        var other = new CohortIdentificationConfiguration(Repository, "Other")
        {
            RootCohortAggregateContainer_ID = (theSubContainer ? subContainer : root).ID
        };
        other.SaveToDatabase();

        new ExecuteCommandRevertToHistoricalCohortVersion(GetActivator(), cic, version).Execute();

        Assert.Multiple(() =>
        {
            Assert.That(cic.RootCohortAggregateContainer_ID, Is.Not.EqualTo(root.ID));

            // in the database, deleting a root container deletes the configuration that uses it, so they are all left
            Assert.That(root.Exists());
            Assert.That(subContainer.Exists());
            Assert.That(subContainer.GetAggregateConfigurations(), Is.EqualTo(new[] { cohortSet }));
        });
    }

    /// <summary>
    /// Saves the current state of <paramref name="cic"/> as a version of it, and returns the version
    /// </summary>
    private CohortIdentificationConfiguration WhenIHaveAVersionOf(CohortIdentificationConfiguration cic)
    {
        var versionsBefore = cic.GetVersions();

        new ExecuteCommandCreateVersionOfCohortConfiguration(GetActivator(), cic).Execute();

        return cic.GetVersions().Except(versionsBefore).Single();
    }

    /// <summary>
    /// Adds a new cohort set to the end of <paramref name="container"/>, and returns it
    /// </summary>
    private AggregateConfiguration WhenIHaveACohortSetIn(CohortAggregateContainer container)
    {
        var cohortSet = WhenIHaveA<AggregateConfiguration>();

        // a cohort set can be cloned, as it is into a version and back, only when it is clear which of its Catalogue's
        // columns identifies the patient
        var identifier = cohortSet.AggregateDimensions.Single().ExtractionInformation;
        identifier.IsExtractionIdentifier = true;
        identifier.SaveToDatabase();

        container.AddChild(cohortSet, container.GetOrderedContents().Count() + 1);
        return cohortSet;
    }
}
