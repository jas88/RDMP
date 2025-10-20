// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Linq;
using FAnsi;
using NUnit.Framework;

namespace Tests.Common;

public class All
{
    /// <summary>
    /// <see cref="TestCaseSourceAttribute"/> for tests that should run on all configured DBMS.
    /// This property dynamically filters to only include database types that are configured
    /// in TestDatabases.txt, preventing slow test case generation and "Inconclusive" results
    /// for unconfigured databases.
    /// </summary>
    public static DatabaseType[] DatabaseTypes =>
        Enum.GetValues<DatabaseType>()
            .Where(IsConfigured)
            .ToArray();

    /// <summary>
    /// <see cref="TestCaseSourceAttribute"/> for tests that should run on all configured DBMS
    /// with both permutations of true/false. Matches exhaustively method signature (DatabaseType,bool).
    /// This property dynamically filters to only include database types that are configured.
    /// </summary>
    public static object[] DatabaseTypesWithBoolFlags =>
        Enum.GetValues<DatabaseType>()
            .Where(IsConfigured)
            .SelectMany(type => new[]
            {
                new object[] { type, true },
                new object[] { type, false }
            })
            .ToArray();

    /// <summary>
    /// Checks if a database type is configured in TestDatabases.txt without attempting to connect.
    /// SQL Server is always considered configured as it's required. Other types are checked against
    /// the TestDatabaseSettings.
    /// </summary>
    private static bool IsConfigured(DatabaseType type)
    {
        // SQL Server is always required and configured
        if (type == DatabaseType.MicrosoftSQLServer)
            return true;

        // Check if the connection string is configured for optional database types
        var settings = TestDatabaseSettings.GetSettings();
        return type switch
        {
            DatabaseType.MySql => settings.MySql != null,
            DatabaseType.Oracle => settings.Oracle != null,
            DatabaseType.PostgreSql => settings.PostgreSql != null,
            _ => false
        };
    }
}