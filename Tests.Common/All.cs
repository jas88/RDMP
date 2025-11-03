// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using FAnsi;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Tests.Common;

public class All
{
    /// <summary>
    /// <see cref="TestCaseSourceAttribute"/> for tests that should run on all configured DBMS
    /// Only returns database types that are actually configured in TestDatabases.txt
    /// </summary>
    public static DatabaseType[] DatabaseTypes
    {
        get
        {
            var types = new List<DatabaseType> { DatabaseType.MicrosoftSQLServer };

            if (DatabaseTests.TestDatabaseSettings?.MySql != null)
                types.Add(DatabaseType.MySql);

            if (DatabaseTests.TestDatabaseSettings?.Oracle != null)
                types.Add(DatabaseType.Oracle);

            if (DatabaseTests.TestDatabaseSettings?.PostgreSql != null)
                types.Add(DatabaseType.PostgreSql);

            return types.ToArray();
        }
    }

    /// <summary>
    /// <see cref="TestCaseSourceAttribute"/> for tests that should run on all configured DBMS
    /// with both permutations of true/false.  Matches exhaustively method signature (DatabaseType,bool)
    /// </summary>
    public static object[] DatabaseTypesWithBoolFlags
    {
        get
        {
            var types = new List<object>
            {
                new object[] { DatabaseType.MicrosoftSQLServer, true },
                new object[] { DatabaseType.MicrosoftSQLServer, false }
            };

            if (DatabaseTests.TestDatabaseSettings?.MySql != null)
            {
                types.Add(new object[] { DatabaseType.MySql, true });
                types.Add(new object[] { DatabaseType.MySql, false });
            }

            if (DatabaseTests.TestDatabaseSettings?.Oracle != null)
            {
                types.Add(new object[] { DatabaseType.Oracle, true });
                types.Add(new object[] { DatabaseType.Oracle, false });
            }

            if (DatabaseTests.TestDatabaseSettings?.PostgreSql != null)
            {
                types.Add(new object[] { DatabaseType.PostgreSql, true });
                types.Add(new object[] { DatabaseType.PostgreSql, false });
            }

            return types.ToArray();
        }
    }
}