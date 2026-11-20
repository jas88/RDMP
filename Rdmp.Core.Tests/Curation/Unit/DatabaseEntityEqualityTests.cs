// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Aggregation;
using Rdmp.Core.Curation.Data.Cache;
using Rdmp.Core.Curation.Data.Cohort;
using Rdmp.Core.Curation.Data.Pipelines;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.QueryBuilding;
using Rdmp.Core.Repositories;
using Tests.Common;

namespace Rdmp.Core.Tests.Curation.Unit;

/// <summary>
/// Pins the equality that <see cref="DatabaseEntity"/> gives its subclasses: two objects are equal when they are the
/// same Type of record with the same <see cref="DatabaseEntity.ID"/>, whatever their other property values (a
/// <see cref="Rdmp.Core.MapsDirectlyToDatabaseTable.TableRepository"/> also requires the same repository).  RDMP relies
/// on this wherever it compares an object with a separately fetched copy of the same record, e.g. to check that a
/// dragged aggregate belongs to the same <see cref="CohortIdentificationConfiguration"/> as the container it is dropped on.
/// </summary>
/// <remarks>
/// The classes tested are the ones the cs/class-missing-equals code scanning rule reports, because they inherit
/// Equals from <see cref="DatabaseEntity"/> rather than declaring it.  Each copy is built the way
/// <see cref="YamlRepository"/> loads a record (parameterless constructor, then ID and Repository), so two copies share
/// no state.
/// </remarks>
public class DatabaseEntityEqualityTests : UnitTests
{
    private const int RecordId = 42;
    private const int OtherRecordId = 43;

    /// <summary>
    /// The classes tested, each with a change to one of its properties which must not affect equality
    /// </summary>
    private static readonly Dictionary<Type, Action<DatabaseEntity>> PropertyEdits = new()
    {
        { typeof(AggregateConfiguration), static o => ((AggregateConfiguration)o).Name = "Other name" },
        { typeof(AggregateDimension), static o => ((AggregateDimension)o).Alias = "Other alias" },
        { typeof(CacheProgress), static o => ((CacheProgress)o).Name = "Other name" },
        { typeof(CohortAggregateContainer), static o => ((CohortAggregateContainer)o).Name = "Other name" },
        {
            typeof(CohortIdentificationConfiguration),
            static o => ((CohortIdentificationConfiguration)o).Name = "Other name"
        },
        { typeof(DataAccessCredentials), static o => ((DataAccessCredentials)o).Name = "Other name" },
        { typeof(ExternalDatabaseServer), static o => ((ExternalDatabaseServer)o).Name = "Other name" },
        { typeof(JoinInfo), static o => ((JoinInfo)o).Collation = "Latin1_General_CS_AS" },
        { typeof(PermissionWindow), static o => ((PermissionWindow)o).Name = "Other name" },
        { typeof(Pipeline), static o => ((Pipeline)o).Name = "Other name" },
        {
            typeof(ExtractableCohort),
            static o => ((ExtractableCohort)o).OverrideReleaseIdentifierSQL = "OtherReleaseIdentifier"
        }
    };

    private static IEnumerable<Type> TypesTested() => PropertyEdits.Keys;

    [TestCaseSource(nameof(TypesTested))]
    public void CopiesOfTheSameRecord_AreEqualAndHashAlike(Type type)
    {
        var record = Load(type, RecordId);
        var copy = Load(type, RecordId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(copy, Is.Not.SameAs(record));
            Assert.That(copy.Equals(record), Is.True);
            Assert.That(record.Equals(copy), Is.True);
            Assert.That(copy.GetHashCode(), Is.EqualTo(record.GetHashCode()));
            Assert.That(new HashSet<object> { record }.Contains(copy), Is.True);
            Assert.That(new Dictionary<object, string> { { record, "found" } }.GetValueOrDefault(copy),
                Is.EqualTo("found"));
        }
    }

    [TestCaseSource(nameof(TypesTested))]
    public void CopyWithADifferentPropertyValue_IsStillTheSameRecord(Type type)
    {
        var record = Load(type, RecordId);
        var editedCopy = Load(type, RecordId);
        PropertyEdits[type](editedCopy);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(editedCopy.Equals(record), Is.True);
            Assert.That(editedCopy.GetHashCode(), Is.EqualTo(record.GetHashCode()));
        }
    }

    [TestCaseSource(nameof(TypesTested))]
    public void DifferentRecordsOfTheSameType_AreNotEqual(Type type)
    {
        var record = Load(type, RecordId);
        var otherRecord = Load(type, OtherRecordId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(otherRecord.Equals(record), Is.False);
            Assert.That(record.Equals(otherRecord), Is.False);
            Assert.That(new HashSet<object> { record }.Contains(otherRecord), Is.False);
        }
    }

    [TestCaseSource(nameof(TypesTested))]
    public void RecordsOfDifferentTypesWithTheSameID_AreNotEqual(Type type)
    {
        var record = Load(type, RecordId);
        var catalogue = Load(typeof(Catalogue), RecordId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(catalogue.Equals(record), Is.False);
            Assert.That(record.Equals(catalogue), Is.False);
            Assert.That(record.Equals(null), Is.False);
        }
    }

    [Test]
    public void CohortSummaryQueryBuilder_RefusesACopyOfTheSummaryAsTheCohort()
    {
        var summary = (AggregateConfiguration)Load(typeof(AggregateConfiguration), RecordId);
        var copyOfSummary = (AggregateConfiguration)Load(typeof(AggregateConfiguration), RecordId);

        var ex = Assert.Throws<ArgumentException>(() => _ = new CohortSummaryQueryBuilder(summary, copyOfSummary, null));
        Assert.That(ex.Message, Does.StartWith("Summary and Cohort should be different aggregates"));
    }

    [Test]
    public void JoinInfo_RefusesACopyOfItselfAsAQueryTimeComboJoin()
    {
        var join = (JoinInfo)Load(typeof(JoinInfo), RecordId);
        var copyOfJoin = (JoinInfo)Load(typeof(JoinInfo), RecordId);

        var ex = Assert.Throws<Exception>(() => join.AddQueryBuildingTimeComboJoinDiscovery(copyOfJoin));
        Assert.That(ex.Message, Is.EqualTo("A JoinInfo cannot add QueryTimeComboJoin to itself"));
    }

    private DatabaseEntity Load(Type type, int id)
    {
        var entity = (DatabaseEntity)Activator.CreateInstance(type);
        entity.ID = id;
        entity.Repository = Repository;
        return entity;
    }
}
