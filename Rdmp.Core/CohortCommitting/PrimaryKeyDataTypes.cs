// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Text.RegularExpressions;
using FAnsi;
using FAnsi.Discovery.QuerySyntax;

namespace Rdmp.Core.CohortCommitting;

/// <summary>
/// Decides whether a column of a given SQL data type can be part of a primary key on a given DBMS, so that a table which
/// needs such a key (e.g. the cohort table created by <see cref="CreateNewCohortDatabaseWizard"/>) can be refused before
/// anything is created.
/// <para>Refuses (max) types, the large object, JSON and XML types of each DBMS and, on MySQL, string types too wide for
/// the key length limit.</para>
/// <para>The FAnsi type translater reports whether a string type has a maximum width, but that does not say whether it can be
/// in a key: PostgreSQL can key text, which FAnsi reports as unbounded, MySQL cannot key text, which FAnsi reports as
/// 65,536 wide, and FAnsi reports MySQL enum and set as unbounded, though they can be keyed.  So the types which cannot be
/// keyed are listed for each DBMS and the translater is only used for the declared width of a string type.</para>
/// <para>Only MySQL has its key length limit checked, because it counts the declared width of every column in a key and so
/// will not create a key which could be too long.  SQL Server creates a key whose variable width columns could exceed its
/// 900 byte limit and only refuses the values which do, PostgreSQL only refuses values too long for an index, and an Oracle
/// key of the widest standard string type fits the limit of the usual block size.</para>
/// </summary>
internal static partial class PrimaryKeyDataTypes
{
    /// <summary>
    /// The most bytes MySQL (and MariaDB) allows in a key of a table with the DYNAMIC or COMPRESSED row format (the
    /// default since MySQL 5.7 and MariaDB 10.2), counting the declared width of every column in it
    /// </summary>
    private const int MySqlMaxKeyBytes = 3072;

    /// <summary>
    /// The most bytes a character takes in utf8mb4, the default character set of MySQL 8 and MariaDB 11.6.  The
    /// character set of a column is not known before it is created, so this is assumed for every string column
    /// </summary>
    private const int MySqlMaxBytesPerCharacter = 4;

    /// <summary>
    /// Returns why a column of type <paramref name="sqlType"/> cannot be part of a primary key on
    /// <paramref name="databaseType"/>, or null if it can.
    /// </summary>
    /// <param name="databaseType">The DBMS which will create the key</param>
    /// <param name="sqlType">The proprietary SQL type of the column e.g. varchar(10)</param>
    /// <param name="otherKeyColumnBytes">The bytes taken by the other columns of the key, which count towards the MySQL
    /// key length limit (the only key length limit checked)</param>
    /// <returns>A clause saying why the type cannot be part of a primary key e.g. "SQL Server cannot use text, ntext, image,
    /// xml or json columns as key columns", or null if it can be</returns>
    public static string GetReasonCannotBePrimaryKey(DatabaseType databaseType, string sqlType,
        int otherKeyColumnBytes = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sqlType);
        ArgumentOutOfRangeException.ThrowIfNegative(otherKeyColumnBytes);

        // SQL Server's large object types, which are not valid types on the other DBMS at all
        if (MaxWidthRegex().IsMatch(sqlType))
            return "(max) types have no maximum length so cannot be part of a primary key";

        var name = GetTypeName(sqlType);

        return databaseType switch
        {
            DatabaseType.MicrosoftSQLServer when name is "text" or "ntext" or "image" or "xml" or "json" =>
                "SQL Server cannot use text, ntext, image, xml or json columns as key columns",

            // "long" is MySQL's name for mediumtext, as are "long varchar" and "long char varying", and "long varbinary" is mediumblob
            DatabaseType.MySql when name is "tinytext" or "text" or "mediumtext" or "longtext" or "tinyblob" or "blob"
                    or "mediumblob" or "longblob" or "long" or "json" =>
                "MySQL can only index text and blob columns by a prefix, and json columns not at all, so cannot use them in a primary key",

            DatabaseType.MySql => GetReasonTooWideForMySqlKey(sqlType, otherKeyColumnBytes),

            // "long" includes "long raw"
            DatabaseType.Oracle when name is "clob" or "nclob" or "blob" or "bfile" or "long" or "xmltype" =>
                "Oracle cannot use LOB, BFILE, LONG or XMLTYPE columns in a primary key",

            DatabaseType.PostgreSql when name is "json" or "xml" =>
                "PostgreSQL has no btree operator class for json or xml, which a primary key needs (jsonb and text can be used)",

            _ => null
        };
    }

    /// <summary>
    /// Returns why a MySQL key containing a string column of type <paramref name="sqlType"/> could be longer than
    /// <see cref="MySqlMaxKeyBytes"/>, or null if it cannot be (or the type is not a string type with a width)
    /// </summary>
    private static string GetReasonTooWideForMySqlKey(string sqlType, int otherKeyColumnBytes)
    {
        var width = QuerySyntaxHelperFactory.Create(DatabaseType.MySql).TypeTranslater.GetLengthIfString(sqlType);

        // -1 is not a string type and int.MaxValue a string type FAnsi has no width for (e.g. enum and set, as the
        // unbounded types have been refused already)
        if (width <= 0 || width == int.MaxValue)
            return null;

        if ((long)width * MySqlMaxBytesPerCharacter + otherKeyColumnBytes <= MySqlMaxKeyBytes)
            return null;

        var widest = Math.Max(0, MySqlMaxKeyBytes - otherKeyColumnBytes) / MySqlMaxBytesPerCharacter;
        var besideOtherColumns = otherKeyColumnBytes > 0 ? $" beside the other {otherKeyColumnBytes} bytes of the key" : "";

        return
            $"MySQL limits a primary key to {MySqlMaxKeyBytes} bytes and a character can take {MySqlMaxBytesPerCharacter} bytes (in utf8mb4, MySQL's default character set), which leaves room for at most varchar({widest}){besideOtherColumns}";
    }

    /// <summary>
    /// Returns the name of <paramref name="sqlType"/> in lower case, without the width or any other words after it
    /// e.g. "varchar" for "VARCHAR(10)" and "long" for "LONG RAW"
    /// </summary>
    private static string GetTypeName(string sqlType)
    {
        var trimmed = sqlType.Trim();
        var end = trimmed.IndexOfAny([' ', '\t', '\r', '\n', '(']);

        return (end == -1 ? trimmed : trimmed[..end]).ToLowerInvariant();
    }

    [GeneratedRegex(@"\(\s*max\s*\)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MaxWidthRegex();
}
