// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using FAnsi;
using FAnsi.Discovery;
using NUnit.Framework;
using Rdmp.Core.CohortCommitting;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Tests.Common;

namespace Rdmp.Core.Tests.CohortCommitting;

/// <summary>
/// Tests for <see cref="PrimaryKeyDataTypes"/> and for <see cref="CreateNewCohortDatabaseWizard"/> refusing private
/// identifier types which cannot be part of the primary key of the cohort table.  None of these need a database.
/// </summary>
public class PrimaryKeyDataTypesTests : UnitTests
{
    [TestCase(DatabaseType.MicrosoftSQLServer, "text", "SQL Server")]
    [TestCase(DatabaseType.MicrosoftSQLServer, "TEXT", "SQL Server")]
    [TestCase(DatabaseType.MicrosoftSQLServer, "ntext", "SQL Server")]
    [TestCase(DatabaseType.MicrosoftSQLServer, "image", "SQL Server")]
    [TestCase(DatabaseType.MicrosoftSQLServer, "xml", "SQL Server")]
    [TestCase(DatabaseType.MicrosoftSQLServer, "json", "SQL Server")]
    [TestCase(DatabaseType.MicrosoftSQLServer, "varchar(max)", "(max)")]
    [TestCase(DatabaseType.MicrosoftSQLServer, "nvarchar(MAX)", "(max)")]
    [TestCase(DatabaseType.MicrosoftSQLServer, "varbinary( max )", "(max)")]
    [TestCase(DatabaseType.MySql, "text", "MySQL")]
    [TestCase(DatabaseType.MySql, "TEXT", "MySQL")]
    [TestCase(DatabaseType.MySql, "text(100)", "MySQL")]
    [TestCase(DatabaseType.MySql, "text character set utf8mb4", "MySQL")]
    [TestCase(DatabaseType.MySql, "tinytext", "MySQL")]
    [TestCase(DatabaseType.MySql, "mediumtext", "MySQL")]
    [TestCase(DatabaseType.MySql, "longtext", "MySQL")]
    [TestCase(DatabaseType.MySql, "tinyblob", "MySQL")]
    [TestCase(DatabaseType.MySql, "blob", "MySQL")]
    [TestCase(DatabaseType.MySql, "mediumblob", "MySQL")]
    [TestCase(DatabaseType.MySql, "longblob", "MySQL")]
    [TestCase(DatabaseType.MySql, "json", "MySQL")]
    [TestCase(DatabaseType.MySql, "long", "MySQL")]
    [TestCase(DatabaseType.MySql, "long varchar", "MySQL")]
    [TestCase(DatabaseType.MySql, "long varbinary", "MySQL")]
    [TestCase(DatabaseType.MySql, "varchar(max)", "(max)")]
    [TestCase(DatabaseType.Oracle, "CLOB", "Oracle")]
    [TestCase(DatabaseType.Oracle, "NCLOB", "Oracle")]
    [TestCase(DatabaseType.Oracle, "BLOB", "Oracle")]
    [TestCase(DatabaseType.Oracle, "BFILE", "Oracle")]
    [TestCase(DatabaseType.Oracle, "LONG", "Oracle")]
    [TestCase(DatabaseType.Oracle, "LONG RAW", "Oracle")]
    [TestCase(DatabaseType.Oracle, "XMLTYPE", "Oracle")]
    [TestCase(DatabaseType.PostgreSql, "json", "PostgreSQL")]
    [TestCase(DatabaseType.PostgreSql, "xml", "PostgreSQL")]
    [TestCase(DatabaseType.PostgreSql, "varchar(max)", "(max)")]
    public void GetReasonCannotBePrimaryKey_TypeCannotBeKeyed_SaysWhy(DatabaseType databaseType, string sqlType,
        string expectedInReason) =>
        Assert.That(PrimaryKeyDataTypes.GetReasonCannotBePrimaryKey(databaseType, sqlType),
            Does.Contain(expectedInReason));

    [TestCase(DatabaseType.MicrosoftSQLServer, "varchar(10)")]
    [TestCase(DatabaseType.MicrosoftSQLServer, "char(10)")]
    [TestCase(DatabaseType.MicrosoftSQLServer, "int")]
    [TestCase(DatabaseType.MicrosoftSQLServer, "bigint")]
    [TestCase(DatabaseType.MicrosoftSQLServer, "uniqueidentifier")]
    [TestCase(DatabaseType.MicrosoftSQLServer, "varbinary(16)")]
    [TestCase(DatabaseType.MicrosoftSQLServer, "sql_variant")]
    [TestCase(DatabaseType.MicrosoftSQLServer, "datetime2")]
    [TestCase(DatabaseType.MySql, "varchar(10)")]
    [TestCase(DatabaseType.MySql, "char(255)")]
    [TestCase(DatabaseType.MySql, "nvarchar(10)")]
    [TestCase(DatabaseType.MySql, "varchar(10) character set latin1")]
    [TestCase(DatabaseType.MySql, "int")]
    [TestCase(DatabaseType.MySql, "bigint(20)")]
    [TestCase(DatabaseType.MySql, "decimal(10,0)")]
    [TestCase(DatabaseType.MySql, "enum('a','b')")]
    [TestCase(DatabaseType.MySql, "set('a','b')")]
    [TestCase(DatabaseType.MySql, "binary(16)")]
    [TestCase(DatabaseType.MySql, "varbinary(16)")]
    [TestCase(DatabaseType.MySql, "datetime")]
    [TestCase(DatabaseType.Oracle, "varchar2(10)")]
    [TestCase(DatabaseType.Oracle, "nvarchar2(10)")]
    [TestCase(DatabaseType.Oracle, "CHAR(10)")]
    [TestCase(DatabaseType.Oracle, "NUMBER(10)")]
    [TestCase(DatabaseType.Oracle, "RAW(16)")]
    [TestCase(DatabaseType.Oracle, "DATE")]
    [TestCase(DatabaseType.PostgreSql, "text")]
    [TestCase(DatabaseType.PostgreSql, "bytea")]
    [TestCase(DatabaseType.PostgreSql, "jsonb")]
    [TestCase(DatabaseType.PostgreSql, "varchar")]
    [TestCase(DatabaseType.PostgreSql, "character varying(10)")]
    [TestCase(DatabaseType.PostgreSql, "integer")]
    [TestCase(DatabaseType.PostgreSql, "uuid")]
    public void GetReasonCannotBePrimaryKey_TypeCanBeKeyed_ReturnsNull(DatabaseType databaseType, string sqlType) =>
        Assert.That(PrimaryKeyDataTypes.GetReasonCannotBePrimaryKey(databaseType, sqlType), Is.Null);

    /// <summary>
    /// MySQL counts 4 bytes a character (utf8mb4) for every string column in a key, and the whole key must fit in 3072 bytes
    /// </summary>
    [TestCase("varchar(767)", 4, true)]
    [TestCase("varchar(768)", 4, false)]
    [TestCase("VARCHAR(768)", 0, true)]
    [TestCase("varchar(769)", 0, false)]
    [TestCase("nvarchar(768)", 4, false)]
    [TestCase("varchar(4000)", 4, false)]
    [TestCase("char(255)", 4, true)]
    public void GetReasonCannotBePrimaryKey_MySqlKeyLengthLimit(string sqlType, int otherKeyColumnBytes,
        bool expectCanBeKeyed)
    {
        var reason = PrimaryKeyDataTypes.GetReasonCannotBePrimaryKey(DatabaseType.MySql, sqlType, otherKeyColumnBytes);

        if (expectCanBeKeyed)
            Assert.That(reason, Is.Null);
        else
            Assert.That(reason, Does.Contain("3072 bytes")
                .And.Contain($"at most varchar({(3072 - otherKeyColumnBytes) / 4})"));
    }

    /// <summary>
    /// MySQL counts a byte for each of the declared width of a binary column in a key.  It does not allow binary columns
    /// wider than 255 bytes, so only varbinary can be too wide in practice, but binary is counted the same way
    /// </summary>
    [TestCase("varbinary(3068)", 4, true)]
    [TestCase("varbinary(3069)", 4, false)]
    [TestCase("VARBINARY(3072)", 0, true)]
    [TestCase("varbinary( 3073 )", 0, false)]
    [TestCase("varbinary(65535)", 4, false)]
    [TestCase("binary(3068)", 4, true)]
    [TestCase("binary(3069)", 4, false)]
    [TestCase("binary(255)", 4, true)]
    [TestCase("binary", 4, true)]
    public void GetReasonCannotBePrimaryKey_MySqlKeyLengthLimit_Binary(string sqlType, int otherKeyColumnBytes,
        bool expectCanBeKeyed)
    {
        var reason = PrimaryKeyDataTypes.GetReasonCannotBePrimaryKey(DatabaseType.MySql, sqlType, otherKeyColumnBytes);

        if (expectCanBeKeyed)
            Assert.That(reason, Is.Null);
        else
            Assert.That(reason, Does.Contain("3072 bytes")
                .And.Contain($"at most varbinary({3072 - otherKeyColumnBytes})"));
    }

    [Test]
    public void GetReasonCannotBePrimaryKey_MySqlKeyLengthLimit_CountsTheOtherKeyColumns() =>
        Assert.That(PrimaryKeyDataTypes.GetReasonCannotBePrimaryKey(DatabaseType.MySql, "varchar(4000)", 4),
            Does.EndWith("at most varchar(767) beside the other 4 bytes of the key"));

    /// <summary>
    /// SQL Server, Oracle and PostgreSQL create keys of these widths (SQL Server and PostgreSQL only refuse the values
    /// which are too long for a key)
    /// </summary>
    [TestCase(DatabaseType.MicrosoftSQLServer, "varchar(8000)")]
    [TestCase(DatabaseType.MicrosoftSQLServer, "nvarchar(4000)")]
    [TestCase(DatabaseType.Oracle, "varchar2(4000)")]
    [TestCase(DatabaseType.Oracle, "nvarchar2(2000)")]
    [TestCase(DatabaseType.PostgreSql, "varchar(10485760)")]
    public void GetReasonCannotBePrimaryKey_KeyLengthOnlyCheckedForMySql(DatabaseType databaseType, string sqlType) =>
        Assert.That(PrimaryKeyDataTypes.GetReasonCannotBePrimaryKey(databaseType, sqlType, 4), Is.Null);

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void GetReasonCannotBePrimaryKey_NoType_Throws(string sqlType) =>
        Assert.That(() => PrimaryKeyDataTypes.GetReasonCannotBePrimaryKey(DatabaseType.MySql, sqlType),
            Throws.InstanceOf<ArgumentException>());

    [Test]
    public void GetReasonCannotBePrimaryKey_NegativeOtherKeyColumnBytes_Throws() =>
        Assert.That(() => PrimaryKeyDataTypes.GetReasonCannotBePrimaryKey(DatabaseType.MySql, "int", -1),
            Throws.InstanceOf<ArgumentOutOfRangeException>());

    [TestCase(DatabaseType.MicrosoftSQLServer, "nvarchar(max)")]
    [TestCase(DatabaseType.MicrosoftSQLServer, "ntext")]
    [TestCase(DatabaseType.MySql, "text")]
    [TestCase(DatabaseType.MySql, "varchar(4000)")]
    [TestCase(DatabaseType.MySql, "varchar(768)")]
    [TestCase(DatabaseType.MySql, "varbinary(3069)")]
    [TestCase(DatabaseType.Oracle, "CLOB")]
    [TestCase(DatabaseType.PostgreSql, "json")]
    public void CreateDatabase_IdentifierCannotBeKeyed_RefusedBeforeConnecting(DatabaseType databaseType,
        string sqlType)
    {
        // Nothing resolves under .invalid, so the wizard cannot have created anything if it refuses with this message
        // rather than failing to connect
        var db = new DiscoveredServer("cohort-store.invalid", "cohorts", databaseType, "user", "password")
            .ExpectDatabase("cohorts");
        var wizard = new CreateNewCohortDatabaseWizard(db, Repository, Repository, false);

        var ex = Assert.Throws<Exception>(() =>
            wizard.CreateDatabase(new PrivateIdentifierPrototype("chi", sqlType), ThrowImmediatelyCheckNotifier.Quiet));

        // the cohort definition id (an int) is the rest of the key, which is why varchar(768) and varbinary(3069) are too
        // wide on MySQL
        var reason = PrimaryKeyDataTypes.GetReasonCannotBePrimaryKey(databaseType, sqlType, 4);

        Assert.That(ex.Message, Is.EqualTo(
            $"Private identifier datatype '{sqlType}' cannot be used for a cohort store, because the private identifier is part of the primary key of the cohort table and {reason}. Choose a type which can be part of a primary key and is no wider than the identifiers need, e.g. varchar(10)"));
    }
}
