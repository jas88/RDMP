// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using FAnsi;

namespace Tests.Common;

public static class TestDatabaseNames
{
    public static string Prefix;
    private const int OracleMaxIdentifierLength = 30;

    public static string GetConsistentName(string databaseName) => Prefix + databaseName;

    /// <summary>
    /// Returns a database name that is safe for the specified database type, truncating if necessary for Oracle's 30-character limit.
    /// </summary>
    /// <param name="databaseName">The base database name</param>
    /// <param name="databaseType">The database type to generate a name for</param>
    /// <returns>A database name appropriate for the specified database type</returns>
    public static string GetConsistentName(string databaseName, DatabaseType databaseType)
    {
        var fullName = Prefix + databaseName;

        // Oracle has a 30-character limit for identifiers
        if (databaseType == DatabaseType.Oracle && fullName.Length > OracleMaxIdentifierLength)
        {
            // Try to preserve meaningful parts by truncating the databaseName part first
            var prefixLength = Prefix.Length;
            var availableForDbName = OracleMaxIdentifierLength - prefixLength;

            if (availableForDbName > 0)
            {
                return Prefix + databaseName.Substring(0, Math.Min(availableForDbName, databaseName.Length));
            }
            else
            {
                // If even the prefix is too long, truncate everything
                return fullName.Substring(0, OracleMaxIdentifierLength);
            }
        }

        return fullName;
    }
}
