// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Tests.Common;

namespace Rdmp.Core.Tests.Curation.Unit;

/// <summary>
/// Pins that sorting catalogue objects is not the same as comparing them.  <see cref="ColumnInfo"/>,
/// <see cref="TableInfo"/> and <see cref="CatalogueItem"/> sort by name and <see cref="ConcreteColumn"/> by
/// <see cref="ConcreteColumn.Order"/>, while Equals (inherited from <see cref="DatabaseEntity"/>) compares the record.
/// Two different records which sort alike must stay distinct, e.g. as keys in the child provider's dictionaries.
/// </summary>
public class SortOrderIsNotIdentityTests : UnitTests
{
    private static IEnumerable<TestCaseData> SameSortKeyDifferentRecords()
    {
        yield return Case<ColumnInfo>(static o => o.Name = "[db]..[tbl].[col]");
        yield return Case<TableInfo>(static o => o.Name = "[db]..[tbl]");
        yield return Case<CatalogueItem>(static o => o.Name = "col");
        yield return Case<ExtractionInformation>(static o => o.Order = 3);
    }

    private static TestCaseData Case<T>(Action<T> setSortKey) where T : DatabaseEntity, IComparable, new() =>
        new TestCaseData((Func<DatabaseEntity>)(static () => new T()), (Action<DatabaseEntity>)(o => setSortKey((T)o)))
            .SetArgDisplayNames(typeof(T).Name);

    [TestCaseSource(nameof(SameSortKeyDifferentRecords))]
    public void RecordsWhichSortAlike_AreStillDifferentRecords(Func<DatabaseEntity> create,
        Action<DatabaseEntity> setSortKey)
    {
        var first = Load(create, 1, setSortKey);
        var second = Load(create, 2, setSortKey);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(((IComparable)first).CompareTo(second), Is.Zero);
            Assert.That(first.Equals(second), Is.False);
            Assert.That(new HashSet<object> { first, second }, Has.Count.EqualTo(2));
        }
    }

    [TestCaseSource(nameof(SameSortKeyDifferentRecords))]
    public void CopiesOfTheSameRecord_SortAlikeAndAreEqual(Func<DatabaseEntity> create,
        Action<DatabaseEntity> setSortKey)
    {
        var record = Load(create, 1, setSortKey);
        var copy = Load(create, 1, setSortKey);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(((IComparable)record).CompareTo(copy), Is.Zero);
            Assert.That(record.Equals(copy), Is.True);
        }
    }

    private static IEnumerable<TestCaseData> SortedByName()
    {
        yield return new TestCaseData((Func<DatabaseEntity>)(static () => new ColumnInfo())).SetArgDisplayNames(nameof(ColumnInfo));
        yield return new TestCaseData((Func<DatabaseEntity>)(static () => new TableInfo())).SetArgDisplayNames(nameof(TableInfo));
        yield return new TestCaseData((Func<DatabaseEntity>)(static () => new CatalogueItem())).SetArgDisplayNames(nameof(CatalogueItem));
        yield return new TestCaseData((Func<DatabaseEntity>)(static () => new Catalogue())).SetArgDisplayNames(nameof(Catalogue));
    }

    [TestCaseSource(nameof(SortedByName))]
    public void SortingByName_IsAToZ(Func<DatabaseEntity> create)
    {
        var apple = Load(create, 1, static o => ((INamed)o).Name = "Apple");
        var banana = Load(create, 2, static o => ((INamed)o).Name = "Banana");
        var sorted = new List<IComparable> { (IComparable)banana, (IComparable)apple };
        sorted.Sort();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(((IComparable)apple).CompareTo(banana), Is.Negative);
            Assert.That(((IComparable)banana).CompareTo(apple), Is.Positive);
            Assert.That(sorted[0], Is.SameAs(apple));
        }
    }

    private DatabaseEntity Load(Func<DatabaseEntity> create, int id, Action<DatabaseEntity> setSortKey)
    {
        // built the way YamlRepository loads a record, so no repository events fire when the sort key is set
        var entity = create();
        entity.ID = id;
        entity.Repository = Repository;
        setSortKey(entity);
        return entity;
    }
}
