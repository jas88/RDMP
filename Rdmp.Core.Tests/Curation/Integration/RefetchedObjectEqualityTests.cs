// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Cohort;
using Rdmp.Core.Curation.Data.Pipelines;
using Rdmp.Core.Repositories;
using Tests.Common;

namespace Rdmp.Core.Tests.Curation.Integration;

/// <summary>
/// Pins the equality a <see cref="Rdmp.Core.MapsDirectlyToDatabaseTable.TableRepository"/> gives the objects it reads:
/// fetching a record again gives a new object, which is equal to the first and hashes alike.  RDMP relies on this to
/// compare an object with a fresh copy of the same record (see <see cref="Unit.DatabaseEntityEqualityTests"/>, which
/// covers every class the cs/class-missing-equals code scanning rule reports, without needing a database).
/// </summary>
public class RefetchedObjectEqualityTests : DatabaseTests
{
    private const string ObjectName = nameof(RefetchedObjectEqualityTests);

    private static readonly Dictionary<string, Func<ICatalogueRepository, DatabaseEntity>> Factories = new()
    {
        { nameof(CohortAggregateContainer), static r => new CohortAggregateContainer(r, SetOperation.UNION) },
        { nameof(CohortIdentificationConfiguration), static r => new CohortIdentificationConfiguration(r, ObjectName) },
        { nameof(DataAccessCredentials), static r => new DataAccessCredentials(r, ObjectName) },
        { nameof(ExternalDatabaseServer), static r => new ExternalDatabaseServer(r, ObjectName, null) },
        { nameof(PermissionWindow), static r => new PermissionWindow(r) },
        { nameof(Pipeline), static r => new Pipeline(r, ObjectName) }
    };

    private static IEnumerable<string> TypeNames() => Factories.Keys;

    [TestCaseSource(nameof(TypeNames))]
    public void RefetchedCopy_IsEqualAndHashesAlike(string typeName)
    {
        // only a TableRepository builds a new object each time a record is fetched
        var repository = CatalogueTableRepository;
        var original = Factories[typeName](repository);

        try
        {
            var refetched = repository.GetObjectByID(original.GetType(), original.ID);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refetched, Is.Not.SameAs(original));
                Assert.That(refetched.Equals(original), Is.True);
                Assert.That(original.Equals(refetched), Is.True);
                Assert.That(refetched.GetHashCode(), Is.EqualTo(original.GetHashCode()));
                Assert.That(new HashSet<object> { original }.Contains(refetched), Is.True);
            }
        }
        finally
        {
            original.DeleteInDatabase();
        }
    }
}
