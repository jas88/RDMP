// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using NUnit.Framework;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.Curation.Data.Aggregation;
using Rdmp.Core.Curation.Data.Cohort;
using Rdmp.Core.Curation.Data.Cohort.Joinables;

namespace Rdmp.Core.Tests.CommandExecution;

internal class ExecuteCommandMakePatientIndexTableIntoRegularCohortIdentificationSetAgainTests : CommandCliTests
{
    [Test]
    public void Constructor_ForAnAggregateOutsideAnyCohortIdentificationConfiguration_IsImpossible()
    {
        var aggregate = WhenIHaveA<AggregateConfiguration>();
        var container = WhenIHaveA<CohortAggregateContainer>();

        var cmd = new ExecuteCommandMakePatientIndexTableIntoRegularCohortIdentificationSetAgain(GetMockActivator(),
            aggregate, container);

        Assert.That(cmd.ReasonCommandImpossible, Does.Contain("not a patient index table"));
    }

    [Test]
    public void Constructor_ForACohortSetThatIsNotAPatientIndexTable_IsImpossible()
    {
        // Execute would take the set out of its container and then fail to delete its (missing) joinable declaration
        var container = WhenIHaveA<CohortAggregateContainer>();
        var aggregate = WhenIHaveA<AggregateConfiguration>();
        container.GetCohortIdentificationConfiguration().EnsureNamingConvention(aggregate);
        container.AddChild(aggregate, 0);

        var cmd = new ExecuteCommandMakePatientIndexTableIntoRegularCohortIdentificationSetAgain(GetMockActivator(),
            aggregate, container);

        Assert.That(cmd.ReasonCommandImpossible, Does.Contain("not a patient index table"));
    }

    [Test]
    public void Constructor_ForAPatientIndexTableOfAnotherConfiguration_IsImpossible()
    {
        var joinable = WhenIHaveA<JoinableCohortAggregateConfiguration>();
        var containerOfAnotherConfiguration = WhenIHaveA<CohortAggregateContainer>();

        var cmd = new ExecuteCommandMakePatientIndexTableIntoRegularCohortIdentificationSetAgain(GetMockActivator(),
            joinable.AggregateConfiguration, containerOfAnotherConfiguration);

        Assert.That(cmd.ReasonCommandImpossible, Does.Contain("different CohortIdentificationConfiguration"));
    }

    [Test]
    public void Execute_ForAPatientIndexTable_MakesItACohortSetOfTheContainer()
    {
        var joinable = WhenIHaveA<JoinableCohortAggregateConfiguration>();
        var aggregate = joinable.AggregateConfiguration;
        var cic = joinable.CohortIdentificationConfiguration;
        cic.CreateRootContainerIfNotExists();
        var container = cic.RootCohortAggregateContainer;

        var cmd = new ExecuteCommandMakePatientIndexTableIntoRegularCohortIdentificationSetAgain(GetMockActivator(),
            aggregate, container);
        Assert.That(cmd.IsImpossible, Is.False, cmd.ReasonCommandImpossible);

        cmd.Execute();

        Assert.Multiple(() =>
        {
            Assert.That(container.GetAggregateConfigurations(), Does.Contain(aggregate));
            Assert.That(cic.GetAllJoinables(), Is.Empty, "the aggregate should no longer be a patient index table");
        });
    }
}
