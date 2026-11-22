// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Aggregation;
using Rdmp.Core.Curation.Data.Cohort;
using Rdmp.Core.Repositories;
using Tests.Common;

namespace Rdmp.Core.Tests.Repositories;

/// <summary>
/// Tests which aggregates the cohort container manager of a <see cref="CatalogueRepository"/> will look up the order
/// of: those of its own repository, and those of any other repository object that has the same connection string.
/// Only a database repository has this manager, so these are Inconclusive against a file system repository.
/// </summary>
internal class CohortContainerManagerTests : DatabaseTests
{
    private const int OrderInContainer = 7;

    [Test]
    public void GetOrderIfExistsFor_AggregateOfTheSameRepository_ReturnsItsOrder()
    {
        var repository = CatalogueTableRepository;
        var aggregate = CreateAggregateInAContainer(repository);

        Assert.That(repository.CohortContainerManager.GetOrderIfExistsFor(aggregate), Is.EqualTo(OrderInContainer));
    }

    [Test]
    public void GetOrderIfExistsFor_AggregateOfAnotherRepositoryWithTheSameConnectionString_ReturnsItsOrder()
    {
        var repository = CatalogueTableRepository;
        var aggregate = CreateAggregateInAContainer(repository);

        using var sameConnectionString = new CatalogueRepository(repository.ConnectionStringBuilder);
        var fetched = sameConnectionString.GetObjectByID<AggregateConfiguration>(aggregate.ID);

        Assert.Multiple(() =>
        {
            Assert.That(fetched.Repository, Is.Not.SameAs(repository));
            Assert.That(repository.CohortContainerManager.GetOrderIfExistsFor(fetched), Is.EqualTo(OrderInContainer));
        });
    }

    [Test]
    public void GetOrderIfExistsFor_AggregateOfARepositoryWithADifferentConnectionString_IsNotSupported()
    {
        var repository = CatalogueTableRepository;
        var aggregate = CreateAggregateInAContainer(repository);

        // The same database: the connection strings are compared as text, and only the Application Name differs
        using var differentConnectionString = new CatalogueRepository(
            new SqlConnectionStringBuilder(repository.ConnectionString)
            {
                ApplicationName = nameof(CohortContainerManagerTests)
            });
        var fetched = differentConnectionString.GetObjectByID<AggregateConfiguration>(aggregate.ID);

        var ex = Assert.Throws<NotSupportedException>(() =>
            repository.CohortContainerManager.GetOrderIfExistsFor(fetched));
        Assert.That(ex.Message, Does.Contain("different connection string"));
    }

    /// <summary>
    /// Creates an aggregate that is at <see cref="OrderInContainer"/> in a new cohort container
    /// </summary>
    private static AggregateConfiguration CreateAggregateInAContainer(CatalogueRepository repository)
    {
        var catalogue = new Catalogue(repository, nameof(CohortContainerManagerTests));
        var aggregate = new AggregateConfiguration(repository, catalogue, nameof(CohortContainerManagerTests));

        var container = new CohortAggregateContainer(repository, SetOperation.UNION);
        container.AddChild(aggregate, OrderInContainer);

        return aggregate;
    }
}
