// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using NUnit.Framework;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.DataLoad.Modules.Attachers;
using System;
using FAnsi;
using FAnsi.Discovery.QuerySyntax;
using FAnsi.Implementations.MicrosoftSQL;
using FAnsi.Implementations.MySql;
using FAnsi.Implementations.Oracle;
using FAnsi.Implementations.PostgreSql;

namespace Rdmp.Core.Tests.DataLoad.Engine.Integration;

public class RemoteAttacherTests
{

    [Test]
    [TestCase(AttacherHistoricalDurations.Past24Hours, "DAY")]
    [TestCase(AttacherHistoricalDurations.Past7Days, "WEEK")]
    [TestCase(AttacherHistoricalDurations.PastMonth, "MONTH")]
    [TestCase(AttacherHistoricalDurations.PastYear, "YEAR")]
    public void TestRemoteAttacherParameter(AttacherHistoricalDurations duration, string convertTime)
    {
        var attacher = new RemoteAttacher
        {
            HistoricalFetchDuration = duration,
            RemoteTableDateColumn = "date"
        };
        var lmd = new LoadMetadata();
        // Updated: No longer casting column to Date - preserves time component for accurate filtering
        Assert.That(attacher.SqlHistoricalDataFilter(lmd, DatabaseType.MicrosoftSQLServer,attacher.RemoteTableDateColumn), Is.EqualTo($" WHERE date > CAST(DATEADD({convertTime}, -1, GETUTCDATE()) as Date)"));
    }
    [Test]
    public void TestRemoteAttacherParameterSinceLastUse()
    {
        var attacher = new RemoteAttacher
        {
            HistoricalFetchDuration = AttacherHistoricalDurations.SinceLastUse,
            RemoteTableDateColumn = "date"
        };
        var lmd = new LoadMetadata
        {
            LastLoadTime = DateTime.Now
        };
        // Updated: No longer casting column to Date - preserves time component
        Assert.That(attacher.SqlHistoricalDataFilter(lmd, DatabaseType.MicrosoftSQLServer,attacher.RemoteTableDateColumn), Is.EqualTo($" WHERE date > convert(Date,'{lmd.LastLoadTime.GetValueOrDefault():yyyy-MM-dd HH:mm:ss.fff}')"));
    }
    [Test]
    public void TestRemoteAttacherParameterSinceLastUse_NULL()
    {
        var attacher = new RemoteAttacher
        {
            HistoricalFetchDuration = AttacherHistoricalDurations.SinceLastUse,
            RemoteTableDateColumn = "date"
        };
        var lmd = new LoadMetadata();
        Assert.That(attacher.SqlHistoricalDataFilter(lmd, DatabaseType.MicrosoftSQLServer, attacher.RemoteTableDateColumn), Is.EqualTo(""));
    }
    [Test]
    public void TestRemoteAttacherParameterCustomRange()
    {
        var attacher = new RemoteAttacher
        {
            HistoricalFetchDuration = AttacherHistoricalDurations.Custom,
            RemoteTableDateColumn = "date",
            CustomFetchDurationStartDate = DateTime.Now,
            CustomFetchDurationEndDate = DateTime.Now
        };
        var lmd = new LoadMetadata
        {
            LastLoadTime = DateTime.Now
        };
        // Updated: No longer casting column to Date - preserves time component
        Assert.That(attacher.SqlHistoricalDataFilter(lmd, DatabaseType.MicrosoftSQLServer, attacher.RemoteTableDateColumn), Is.EqualTo($" WHERE date >= convert(Date,'{attacher.CustomFetchDurationStartDate:yyyy-MM-dd HH:mm:ss.fff}') AND date <= convert(Date,'{attacher.CustomFetchDurationEndDate:yyyy-MM-dd HH:mm:ss.fff}')"));
    }
    [Test]
    public void TestRemoteAttacherParameterCustomRangeNoStart()
    {
        var attacher = new RemoteAttacher
        {
            HistoricalFetchDuration = AttacherHistoricalDurations.Custom,
            RemoteTableDateColumn = "date",
            CustomFetchDurationEndDate = DateTime.Now
        };
        var lmd = new LoadMetadata
        {
            LastLoadTime = DateTime.Now
        };
        // Updated: No longer casting column to Date
        Assert.That(attacher.SqlHistoricalDataFilter(lmd, DatabaseType.MicrosoftSQLServer, attacher.RemoteTableDateColumn), Is.EqualTo($" WHERE date <= convert(Date,'{attacher.CustomFetchDurationEndDate:yyyy-MM-dd HH:mm:ss.fff}')"));
    }
    [Test]
    public void TestRemoteAttacherParameterCustomRangeNoEnd()
    {
        var attacher = new RemoteAttacher
        {
            HistoricalFetchDuration = AttacherHistoricalDurations.Custom,
            RemoteTableDateColumn = "date",
            CustomFetchDurationStartDate = DateTime.Now
        };
        var lmd = new LoadMetadata
        {
            LastLoadTime = DateTime.Now
        };
        // Updated: No longer casting column to Date
        Assert.That(attacher.SqlHistoricalDataFilter(lmd, DatabaseType.MicrosoftSQLServer, attacher.RemoteTableDateColumn), Is.EqualTo($" WHERE date >= convert(Date,'{attacher.CustomFetchDurationStartDate:yyyy-MM-dd HH:mm:ss.fff}')"));
    }
    [Test]
    public void TestRemoteAttacherParameterCustomRangeNoDates()
    {
        var attacher = new RemoteAttacher
        {
            HistoricalFetchDuration = AttacherHistoricalDurations.Custom,
            RemoteTableDateColumn = "date"
        };
        var lmd = new LoadMetadata
        {
            LastLoadTime = DateTime.Now
        };
        Assert.That(attacher.SqlHistoricalDataFilter(lmd, DatabaseType.MicrosoftSQLServer, attacher.RemoteTableDateColumn), Is.EqualTo(""));
    }
    [Test]
    public void TestRemoteAttacherParameterDeltaReading()
    {
        var attacher = new RemoteAttacher
        {
            HistoricalFetchDuration = AttacherHistoricalDurations.DeltaReading,
            DeltaReadingLookBackDays = 1,
            DeltaReadingLookForwardDays = 1,
            DeltaReadingStartDate = DateTime.Now,
            RemoteTableDateColumn = "date"
        };
        var lmd = new LoadMetadata
        {
            LastLoadTime = DateTime.Now
        };
        // Updated: No longer casting column to Date - preserves time component
        Assert.That(attacher.SqlHistoricalDataFilter(lmd, DatabaseType.MicrosoftSQLServer,attacher.RemoteTableDateColumn), Is.EqualTo($" WHERE date >= convert(Date,'{attacher.DeltaReadingStartDate.AddDays(-attacher.DeltaReadingLookBackDays):yyyy-MM-dd HH:mm:ss.fff}') AND date < convert(Date,'{attacher.DeltaReadingStartDate.AddDays(attacher.DeltaReadingLookForwardDays):yyyy-MM-dd HH:mm:ss.fff}')"));
    }
    [Test]
    public void TestRemoteAttacherParameterDeltaReading_NoLookBack()
    {
        var attacher = new RemoteAttacher
        {
            HistoricalFetchDuration = AttacherHistoricalDurations.DeltaReading,
            DeltaReadingLookForwardDays = 1,
            DeltaReadingStartDate = DateTime.Now,
            RemoteTableDateColumn = "date"
        };
        var lmd = new LoadMetadata
        {
            LastLoadTime = DateTime.Now
        };
        // Updated: No longer casting column to Date
        Assert.That(attacher.SqlHistoricalDataFilter(lmd, DatabaseType.MicrosoftSQLServer,attacher.RemoteTableDateColumn), Is.EqualTo($" WHERE date >= convert(Date,'{attacher.DeltaReadingStartDate:yyyy-MM-dd HH:mm:ss.fff}') AND date < convert(Date,'{attacher.DeltaReadingStartDate.AddDays(attacher.DeltaReadingLookForwardDays):yyyy-MM-dd HH:mm:ss.fff}')"));
    }
    [Test]
    public void TestRemoteAttacherParameterDeltaReading_NoLookForward()
    {
        var attacher = new RemoteAttacher
        {
            HistoricalFetchDuration = AttacherHistoricalDurations.DeltaReading,
            DeltaReadingLookBackDays = 1,
            DeltaReadingStartDate = DateTime.Now,
            RemoteTableDateColumn = "date"
        };
        var lmd = new LoadMetadata
        {
            LastLoadTime = DateTime.Now
        };
        // Updated: No longer casting column to Date
        Assert.That(attacher.SqlHistoricalDataFilter(lmd, DatabaseType.MicrosoftSQLServer,attacher.RemoteTableDateColumn), Is.EqualTo($" WHERE date >= convert(Date,'{attacher.DeltaReadingStartDate.AddDays(-attacher.DeltaReadingLookBackDays):yyyy-MM-dd HH:mm:ss.fff}') AND date < convert(Date,'{attacher.DeltaReadingStartDate:yyyy-MM-dd HH:mm:ss.fff}')"));
    }
    [Test]
    public void TestRemoteAttacherParameterDeltaReadingNoDates()
    {
        var attacher = new RemoteAttacher
        {
            HistoricalFetchDuration = AttacherHistoricalDurations.DeltaReading,
            RemoteTableDateColumn = "date"
        };
        var lmd = new LoadMetadata();
        Assert.That(attacher.SqlHistoricalDataFilter(lmd, DatabaseType.MicrosoftSQLServer, attacher.RemoteTableDateColumn), Is.EqualTo(""));
    }

    private static IQuerySyntaxHelper Syntax(DatabaseType type) => type switch
    {
        DatabaseType.MicrosoftSQLServer => MicrosoftQuerySyntaxHelper.Instance,
        DatabaseType.MySql => MySqlQuerySyntaxHelper.Instance,
        DatabaseType.Oracle => OracleQuerySyntaxHelper.Instance,
        DatabaseType.PostgreSql => PostgreSqlSyntaxHelper.Instance,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    // A bare name is replaced by the real column name, wrapped, so case-folding DBMS find it
    [TestCase(DatabaseType.MicrosoftSQLServer, "dateColumn", "dateColumn", "[dateColumn]")]
    [TestCase(DatabaseType.MySql, "dateColumn", "dateColumn", "`dateColumn`")]
    [TestCase(DatabaseType.PostgreSql, "dateColumn", "dateColumn", "\"dateColumn\"")]
    [TestCase(DatabaseType.PostgreSql, "DateColumn", "datecolumn", "\"datecolumn\"")]
    [TestCase(DatabaseType.Oracle, "date", "DATE", "\"DATE\"")]
    [TestCase(DatabaseType.Oracle, "MyDate", "MYDATE", "\"MYDATE\"")]
    public void ResolveColumnSql_BareName_UsesRealColumnName(DatabaseType type, string configured, string actual,
        string expected)
    {
        Assert.That(RemoteAttacher.ResolveColumnSql(Syntax(type), configured, new[] { "id", actual }),
            Is.EqualTo(expected));
    }

    // Wrapped or qualified names, expressions, unknown and ambiguous names are left exactly as configured
    [TestCase(DatabaseType.PostgreSql, "dateColumn::timestamp")]
    [TestCase(DatabaseType.PostgreSql, "\"dateColumn\"")]
    [TestCase(DatabaseType.PostgreSql, "t.dateColumn")]
    [TestCase(DatabaseType.PostgreSql, "CAST(dateColumn AS date)")]
    [TestCase(DatabaseType.MicrosoftSQLServer, "[dateColumn]")]
    [TestCase(DatabaseType.MicrosoftSQLServer, "date column")]
    [TestCase(DatabaseType.MySql, "`dateColumn`")]
    [TestCase(DatabaseType.Oracle, "otherColumn")]
    public void ResolveColumnSql_NotABareKnownName_Unchanged(DatabaseType type, string configured)
    {
        Assert.That(RemoteAttacher.ResolveColumnSql(Syntax(type), configured, new[] { "dateColumn", "date column" }),
            Is.EqualTo(configured));
    }

    [Test]
    public void ResolveColumnSql_CaseOnlyDifferences_PrefersExactThenGivesUp()
    {
        var columns = new[] { "DateColumn", "datecolumn" };
        var syntax = PostgreSqlSyntaxHelper.Instance;

        Assert.Multiple(() =>
        {
            Assert.That(RemoteAttacher.ResolveColumnSql(syntax, "datecolumn", columns), Is.EqualTo("\"datecolumn\""));
            Assert.That(RemoteAttacher.ResolveColumnSql(syntax, "DATECOLUMN", columns), Is.EqualTo("DATECOLUMN"));
        });
    }
}
