// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using NUnit.Framework;
using Rdmp.Core.Curation.Data.Aggregation;
using Rdmp.Core.Curation.Data.Cohort;
using Rdmp.Core.QueryBuilding;
using Tests.Common;

namespace Rdmp.Core.Tests.CohortCreation.QueryTests;

/// <summary>
/// Tests for what <see cref="CohortQueryBuilder"/> and <see cref="CohortQueryBuilderHelper"/> say when they are not given
/// what they need to build a query
/// </summary>
internal class CohortQueryBuilderGuardTests : UnitTests
{
    [Test]
    public void Constructor_WithoutAConfiguration_Throws()
    {
        var ex = Assert.Throws<QueryBuildingException>(() =>
            _ = new CohortQueryBuilder((CohortIdentificationConfiguration)null, null));

        Assert.That(ex?.Message, Is.EqualTo("Configuration has not been set yet"));
    }

    [Test]
    public void Constructor_ForAConfigurationWithNoRootContainer_Throws()
    {
        var cic = WhenIHaveA<CohortIdentificationConfiguration>();
        Assert.That(cic.RootCohortAggregateContainer_ID, Is.Null);

        var ex = Assert.Throws<QueryBuildingException>(() => _ = new CohortQueryBuilder(cic, null));

        Assert.That(ex?.Message, Is.EqualTo($"Root container not set on CohortIdentificationConfiguration {cic}"));
    }

    [Test]
    public void GetSQLForAggregate_WithoutArgs_Throws()
    {
        var aggregate = WhenIHaveA<AggregateConfiguration>();

        var ex = Assert.Throws<ArgumentNullException>(() =>
            CohortQueryBuilderHelper.GetSQLForAggregate(aggregate, null));

        Assert.That(ex?.ParamName, Is.EqualTo("args"));
    }
}
