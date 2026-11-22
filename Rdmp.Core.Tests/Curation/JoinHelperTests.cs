// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using NSubstitute;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.DataHelper;
using Tests.Common;

namespace Rdmp.Core.Tests.Curation;

internal class JoinHelperTests : UnitTests
{
    [Test]
    public void GetJoinSQLPrimaryKeySideOnly_WithAliasButNoPrimaryKey_DescribesTheHalfSetUpJoin()
    {
        // a join can be described before its primary key has been chosen
        ColumnInfo noPrimaryKey = null;
        var join = Substitute.For<IJoin>();
        join.ForeignKey.Returns(WhenIHaveA<ColumnInfo>());
        join.PrimaryKey.Returns(noPrimaryKey);
        join.GetSupplementalJoins().Returns([]);

        var sql = JoinHelper.GetJoinSQLPrimaryKeySideOnly(join, 1);

        Assert.That(sql, Does.Contain("AS lookup_1"));
    }

    [Test]
    public void GetJoinSQLPrimaryKeySideOnly_WithAlias_JoinsOnThePrimaryKeyOfTheAliasedTable()
    {
        var lookupTable = new TableInfo(Repository, "[MyDb]..[MyLookup]");
        lookupTable.SaveToDatabase();

        var join = Substitute.For<IJoin>();
        join.ForeignKey.Returns(WhenIHaveA<ColumnInfo>());
        join.PrimaryKey.Returns(new ColumnInfo(Repository, "[MyDb]..[MyLookup].[Code]", "varchar(10)", lookupTable));
        join.GetSupplementalJoins().Returns([]);

        var sql = JoinHelper.GetJoinSQLPrimaryKeySideOnly(join, 1);

        Assert.That(sql, Does.EndWith(" JOIN [MyDb]..[MyLookup] AS lookup_1 ON My_Col = lookup_1.[Code]"));
    }
}
