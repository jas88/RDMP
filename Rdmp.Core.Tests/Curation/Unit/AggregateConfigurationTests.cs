// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using FAnsi.Discovery.QuerySyntax.Aggregation;
using NUnit.Framework;
using Rdmp.Core.Curation.Data.Aggregation;
using Rdmp.Core.QueryBuilding;
using System.Data;
using Rdmp.Core.ReusableLibraryCode.Settings;
using Tests.Common;

namespace Rdmp.Core.Tests.Curation.Unit;

internal class AggregateConfigurationTests : UnitTests
{
    [Test]
    public void TestStripZeroSeries_EmptyTable()
    {
        var dt = new DataTable();
        dt.Columns.Add("col1");
        dt.Columns.Add("col2");

        UserSettings.IncludeZeroSeriesInGraphs = false;

        // empty tables should not get nuked
        AggregateConfiguration.AdjustGraphDataTable(dt);
        Assert.That(dt.Columns, Has.Count.EqualTo(2));

        dt.Dispose();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void TestStripZeroSeries_Nulls(bool includeZeroSeries)
    {
        var dt = new DataTable();
        dt.Columns.Add("date");
        dt.Columns.Add("col1");
        dt.Columns.Add("col2");

        dt.Rows.Add("2001", 0, 12);
        dt.Rows.Add("2002", null, 333);

        UserSettings.IncludeZeroSeriesInGraphs = includeZeroSeries;

        AggregateConfiguration.AdjustGraphDataTable(dt);

        if (includeZeroSeries)
        {
            Assert.That(dt.Columns, Has.Count.EqualTo(3));
        }
        else
        {
            // col1 should have been gotten rid of
            Assert.That(dt.Columns, Has.Count.EqualTo(2));
            dt.Columns.Contains("date");
            dt.Columns.Contains("col2");
        }

        dt.Dispose();
    }

    [Test]
    public void GetQueryBuilder_PivotAndAxisOnTheSameDimension_IsRefused()
    {
        var config = WhenIHaveA(Repository, out var dateEi, out _);
        var dateDimension = new AggregateDimension(Repository, dateEi, config);
        _ = new AggregateContinuousDateAxis(Repository, dateDimension);
        config.PivotOnDimensionID = dateDimension.ID;
        config.SaveToDatabase();

        var ex = Assert.Throws<QueryBuildingException>(() => _ = config.GetQueryBuilder().SQL);
        Assert.That(ex.Message, Does.Contain("is both a PIVOT and has an AXIS configured on it"));
    }

    [TestCase(null, null)]
    [TestCase("'2001-01-01'", null)]
    [TestCase(null, "'2002-02-02'")]
    [TestCase("'2001-01-01'", "'2002-02-02'")]
    public void GetQueryBuilder_UsesTheAxisDatesUnlessTheyAreOverridden(string startOverride, string endOverride)
    {
        // A graph with an axis may only have the axis column and the count column, which is what this gives
        var axis = WhenIHaveA<AggregateContinuousDateAxis>();
        axis.StartDate = "'1990-01-01'";
        axis.EndDate = "'1995-01-01'";
        axis.AxisIncrement = AxisIncrement.Year;
        axis.SaveToDatabase();

        // WhenIHaveA swapped the graph's column for the axis column after the graph had cached its columns
        var config = axis.AggregateDimension.AggregateConfiguration;
        config.ClearAllInjections();

        var builder = config.GetQueryBuilder();
        builder.AxisStartDateOverride = startOverride;
        builder.AxisEndDateOverride = endOverride;
        var sql = builder.SQL;

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain($"SET @startDate = {startOverride ?? "'1990-01-01'"}"));
            Assert.That(sql, Does.Contain($"SET @endDate = {endOverride ?? "'1995-01-01'"}"));
        });
    }

    [TestCase("'2001-01-01'", null)]
    [TestCase(null, "'2002-02-02'")]
    public void GetQueryBuilder_WithAxisDateOverrideButNoAxis_Throws(string startOverride, string endOverride)
    {
        var builder = WhenIHaveA<AggregateConfiguration>().GetQueryBuilder();
        builder.AxisStartDateOverride = startOverride;
        builder.AxisEndDateOverride = endOverride;

        var ex = Assert.Throws<QueryBuildingException>(() => _ = builder.SQL);
        Assert.That(ex?.Message, Does.Contain("can only be set when the query has an AggregateContinuousDateAxis"));
    }
}