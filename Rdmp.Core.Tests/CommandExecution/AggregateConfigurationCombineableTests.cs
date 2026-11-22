// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using NUnit.Framework;
using Rdmp.Core.CommandExecution.Combining;
using Rdmp.Core.Curation.Data.Aggregation;
using Rdmp.Core.Curation.Data.Cohort;
using Rdmp.Core.Curation.Data.Cohort.Joinables;
using Tests.Common;

namespace Rdmp.Core.Tests.CommandExecution;

/// <summary>
/// Tests for what <see cref="AggregateConfigurationCombineable"/> finds out about its aggregate
/// </summary>
internal class AggregateConfigurationCombineableTests : UnitTests
{
    [Test]
    public void Constructor_ForAPatientIndexTableOfAConfigurationWithNoRootContainer_FindsNoContainers()
    {
        // A configuration has no root container until CreateRootContainerIfNotExists is called, which creating one
        // with the NewObject command or its constructor does not do
        var joinable = WhenIHaveA<JoinableCohortAggregateConfiguration>();
        var cic = joinable.CohortIdentificationConfiguration;
        Assert.That(cic.RootCohortAggregateContainer_ID, Is.Null);

        var combineable = new AggregateConfigurationCombineable(joinable.AggregateConfiguration);

        Assert.Multiple(() =>
        {
            Assert.That(combineable.CohortIdentificationConfigurationIfAny, Is.EqualTo(cic));
            Assert.That(combineable.JoinableDeclarationIfAny, Is.EqualTo(joinable));
            Assert.That(combineable.AllContainersInTreeIfPartOfOne, Is.Empty);
        });
    }

    [Test]
    public void Constructor_ForACohortSet_FindsEveryContainerOfItsConfiguration()
    {
        var root = WhenIHaveA<CohortAggregateContainer>();
        var subContainer = new CohortAggregateContainer(Repository, SetOperation.INTERSECT);
        root.AddChild(subContainer);
        var subSubContainer = new CohortAggregateContainer(Repository, SetOperation.EXCEPT);
        subContainer.AddChild(subSubContainer);

        var aggregate = WhenIHaveA<AggregateConfiguration>();
        subContainer.AddChild(aggregate, 0);

        var combineable = new AggregateConfigurationCombineable(aggregate);

        Assert.Multiple(() =>
        {
            Assert.That(combineable.ContainerIfAny, Is.EqualTo(subContainer));
            Assert.That(combineable.AllContainersInTreeIfPartOfOne,
                Is.EquivalentTo(new[] { root, subContainer, subSubContainer }));
        });
    }
}
