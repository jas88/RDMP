// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.
// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using NUnit.Framework;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.CommandLine.Interactive;
using Rdmp.Core.Curation.Data.Cohort;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.Tests.CohortCreation.QueryTests;
using System;
using System.Linq;

namespace Rdmp.Core.Tests.CohortCreation;

public class CohortVersioningTests : CohortQueryBuilderWithCacheTests
{

    private CohortIdentificationConfiguration GenerateCIC()
    {
        var cic = new CohortIdentificationConfiguration(CatalogueRepository, "mycic")
        {
            QueryCachingServer_ID = externalDatabaseServer.ID
        };
        cic.SaveToDatabase();
        cic.CreateRootContainerIfNotExists();
        return cic;
    }

    [Test]
    public void TestCreationOfNewVersionOfCohort()
    {
        var activator = new ConsoleInputManager(RepositoryLocator, ThrowImmediatelyCheckNotifier.Quiet)
        { DisallowInput = true };
        var cic = GenerateCIC();
        var cmd = new ExecuteCommandCreateVersionOfCohortConfiguration(activator, cic);
        Assert.DoesNotThrow(() => cmd.Execute());
        var newCic = CatalogueRepository.GetAllObjectsWhere<CohortIdentificationConfiguration>("ClonedFrom_ID", cic.ID);
        Assert.That(newCic, Has.Length.EqualTo(1));
        Assert.That(newCic.First().Version, Is.EqualTo(1));

        Assert.DoesNotThrow(() => cmd.Execute());
        newCic = CatalogueRepository.GetAllObjectsWhere<CohortIdentificationConfiguration>("ClonedFrom_ID", cic.ID);
        Assert.That(newCic, Has.Length.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(newCic.First().Version, Is.EqualTo(1));
            Assert.That(newCic.Last().Version, Is.EqualTo(2));
        });
    }

    [Test]
    public void TestCreationOfNewVersionOfCohortWithDescription()
    {
        var activator = new ConsoleInputManager(RepositoryLocator, ThrowImmediatelyCheckNotifier.Quiet)
        { DisallowInput = true };
        var cic = GenerateCIC();
        var cmd = new ExecuteCommandCreateVersionOfCohortConfiguration(activator, cic,"Name","Description");
        Assert.DoesNotThrow(() => cmd.Execute());
        var newCic = CatalogueRepository.GetAllObjectsWhere<CohortIdentificationConfiguration>("ClonedFrom_ID", cic.ID);
        Assert.That(newCic, Has.Length.EqualTo(1));
        Assert.That(newCic.First().Version, Is.EqualTo(1));
        Assert.That(newCic.First().Name, Is.EqualTo("Name"));
        Assert.That(newCic.First().Description, Is.EqualTo("Description"));


    }

    [Test]
    public void TestCreatingCohortVersionWithName()
    {
        var activator = new ConsoleInputManager(RepositoryLocator, ThrowImmediatelyCheckNotifier.Quiet)
        { DisallowInput = true };
        var cic = GenerateCIC();
        var cmd = new ExecuteCommandCreateVersionOfCohortConfiguration(activator, cic, "MyName!");
        Assert.DoesNotThrow(() => cmd.Execute());
        var newCic = CatalogueRepository.GetAllObjectsWhere<CohortIdentificationConfiguration>("ClonedFrom_ID", cic.ID);
        Assert.That(newCic, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(newCic.First().Version, Is.EqualTo(1));
            Assert.That(newCic.First().Name, Is.EqualTo("MyName!"));
        });
    }

    [Test]
    public void TestRestoreACohortVersion()
    {
        var activator = new ConsoleInputManager(RepositoryLocator, ThrowImmediatelyCheckNotifier.Quiet)
        { DisallowInput = true };
        var cic = GenerateCIC();
        cic.SaveToDatabase();
        var agg = new CohortAggregateContainer(RepositoryLocator.CatalogueRepository, SetOperation.UNION);
        agg.SaveToDatabase();
        cic.RootCohortAggregateContainer_ID = agg.ID;
        cic.SaveToDatabase();
        var cmd = new ExecuteCommandCreateVersionOfCohortConfiguration(activator, cic, "MyName!");
        Assert.DoesNotThrow(() => cmd.Execute());
        var newCic = CatalogueRepository.GetAllObjectsWhere<CohortIdentificationConfiguration>("ClonedFrom_ID", cic.ID);
        Assert.That(newCic, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(newCic.First().Version, Is.EqualTo(1));
            Assert.That(newCic.First().Name, Is.EqualTo("MyName!"));
        });
        cic.RootCohortAggregateContainer_ID = null;
        cic.SaveToDatabase();
        var fetchedCic = CatalogueRepository.GetAllObjectsWhere<CohortIdentificationConfiguration>("ID", cic.ID);
        Assert.That(fetchedCic.First().RootCohortAggregateContainer_ID, Is.Null);
        var restoreCmd = new ExecuteCommandRevertToHistoricalCohortVersion(activator, fetchedCic.First(), newCic.First());
        Assert.DoesNotThrow(()=> restoreCmd.Execute());
        fetchedCic = CatalogueRepository.GetAllObjectsWhere<CohortIdentificationConfiguration>("ID", cic.ID);
        Assert.That(fetchedCic.First().RootCohortAggregateContainer_ID, Is.Not.Null);

    }
    [Test]
    public void TestRestoreACohortVersion_DeletesTheContainersTheConfigurationHad()
    {
        var activator = new ConsoleInputManager(RepositoryLocator, ThrowImmediatelyCheckNotifier.Quiet)
        { DisallowInput = true };

        // a cohort set in the root container, and another in a subcontainer
        var cic = cohortIdentificationConfiguration;
        rootcontainer.AddChild(aggregate1, 1);
        rootcontainer.AddChild(container1);
        container1.AddChild(aggregate2, 1);

        new ExecuteCommandCreateVersionOfCohortConfiguration(activator, cic, "MyName!").Execute();
        var version = cic.GetVersions().Single();

        new ExecuteCommandRevertToHistoricalCohortVersion(activator, cic, version).Execute();

        // in the database, deleting a root container deletes the configuration that uses it
        var reverted = CatalogueRepository.GetAllObjectsWhere<CohortIdentificationConfiguration>("ID", cic.ID);
        Assert.That(reverted, Has.Length.EqualTo(1));
        var newRoot = reverted.Single().RootCohortAggregateContainer;
        Assert.Multiple(() =>
        {
            // the configuration has a copy of what the version has, in containers of its own
            Assert.That(newRoot.ID, Is.Not.EqualTo(rootcontainer.ID).And.Not.EqualTo(version.RootCohortAggregateContainer_ID));
            Assert.That(newRoot.GetAggregateConfigurations(), Has.Length.EqualTo(1));
            Assert.That(newRoot.GetSubContainers().Single().GetAggregateConfigurations(), Has.Length.EqualTo(1));

            // the containers it had are deleted
            Assert.That(rootcontainer.Exists(), Is.False);
            Assert.That(container1.Exists(), Is.False);

            // their cohort sets are left as orphans, like those of any container that is deleted
            Assert.That(aggregate1.Exists());
            Assert.That(aggregate2.Exists());
            Assert.That(aggregate1.GetCohortAggregateContainerIfAny(), Is.Null);
            Assert.That(aggregate2.GetCohortAggregateContainerIfAny(), Is.Null);

            // the version is as it was
            Assert.That(version.RootCohortAggregateContainer.GetAllAggregateConfigurationsRecursively(), Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void TestRestoreACohortVersion_ReplacesTheParametersTheConfigurationHad()
    {
        var activator = new ConsoleInputManager(RepositoryLocator, ThrowImmediatelyCheckNotifier.Quiet)
        { DisallowInput = true };
        var cic = cohortIdentificationConfiguration;
        var age = new AnyTableSqlParameter(CatalogueRepository, cic, "DECLARE @age AS int") { Value = "5" };
        age.SaveToDatabase();

        new ExecuteCommandCreateVersionOfCohortConfiguration(activator, cic, "MyName!").Execute();
        var version = cic.GetVersions().Single();

        // since the version was saved, the value of the parameter has been changed and another parameter has been added
        age.Value = "6";
        age.SaveToDatabase();
        new AnyTableSqlParameter(CatalogueRepository, cic, "DECLARE @sex AS varchar(1)") { Value = "'F'" }.SaveToDatabase();

        // the database does not let the configuration have a second parameter with the declaration of @age
        new ExecuteCommandRevertToHistoricalCohortVersion(activator, cic, version).Execute();

        Assert.That(cic.GetAllParameters().Select(static p => $"{p.ParameterSQL} = {p.Value}"),
            Is.EqualTo(new[] { "DECLARE @age AS int = 5" }));
    }

    [Test]
    public void TestRestoreACohortVersion_Bad()
    {
        var activator = new ConsoleInputManager(RepositoryLocator, ThrowImmediatelyCheckNotifier.Quiet)
        { DisallowInput = true };
        var cic = GenerateCIC();
        cic.SaveToDatabase();
        var agg = new CohortAggregateContainer(RepositoryLocator.CatalogueRepository, SetOperation.UNION);
        agg.SaveToDatabase();
        cic.RootCohortAggregateContainer_ID = agg.ID;
        cic.SaveToDatabase();
        var cmd = new ExecuteCommandCreateVersionOfCohortConfiguration(activator, cic, "MyName!");
        Assert.DoesNotThrow(() => cmd.Execute());
        var newCic = CatalogueRepository.GetAllObjectsWhere<CohortIdentificationConfiguration>("ClonedFrom_ID", cic.ID);
        Assert.That(newCic, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(newCic.First().Version, Is.EqualTo(1));
            Assert.That(newCic.First().Name, Is.EqualTo("MyName!"));
        });
        cic.RootCohortAggregateContainer_ID = null;
        cic.SaveToDatabase();
        var fetchedCic = CatalogueRepository.GetAllObjectsWhere<CohortIdentificationConfiguration>("ID", cic.ID);
        Assert.That(fetchedCic.First().RootCohortAggregateContainer_ID, Is.Null);
        var restoreCmd = new ExecuteCommandRevertToHistoricalCohortVersion(activator, newCic.First(), fetchedCic.First());
        var ex = Assert.Throws<Exception>(() => restoreCmd.Execute());
        Assert.That(ex.Message, Is.EqualTo("Historical configuration is not derived from this cohort configuration"));

    }
}
