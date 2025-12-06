// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using FAnsi;
using NUnit.Framework;
using Rdmp.Core.Databases;
using System;
using System.Linq;
using Rdmp.Core.MapsDirectlyToDatabaseTable.Versioning;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Tests.Common;

namespace Rdmp.Core.Tests.Databases;

internal class MasterDatabaseScriptExecutorTests : DatabaseTests
{
    [Test]
    public void TestCreatingSchemaTwice()
    {
        var db = GetCleanedServer(DatabaseType.MicrosoftSQLServer);

        var mds = new MasterDatabaseScriptExecutor(db);
        //setup as DQE
        mds.CreateAndPatchDatabase(new DataQualityEnginePatcher(), new AcceptAllCheckNotifier());

        //now try to setup same db as Logging
        var ex = Assert.Throws<Exception>(() =>
            mds.CreateAndPatchDatabase(new LoggingDatabasePatcher(), new AcceptAllCheckNotifier()));

        Assert.That(
            ex.InnerException.Message, Does.Contain("is already set up as a platform database for another schema (it has the 'ScriptsRun' table)"));
    }

    [Test]
    public void TestAtomicDatabaseCreation_WithFunctions_Success()
    {
        // Test that database initialization with CREATE FUNCTION statements works atomically
        var db = GetCleanedServer(DatabaseType.MicrosoftSQLServer);

        var mds = new MasterDatabaseScriptExecutor(db);

        // Create and patch a database that includes CREATE FUNCTION statements (ANOStore has functions)
        mds.CreateAndPatchDatabase(new ANOStorePatcher(), new AcceptAllCheckNotifier());

        // Verify database was created successfully
        Assert.That(db.Exists(), Is.True, "Database should exist after successful creation");

        // Verify key tables exist - ANOStore creates Numbers table plus RoundhousE tracking tables
        var tables = db.DiscoverTables(false).Select(t => t.GetRuntimeName()).ToArray();
        Assert.That(tables, Does.Contain("Numbers"),
            "ANOStore Numbers table should exist");

        // Verify functions were created (ANOStore has GetAlpha, GetNumeric, etc.)
        using var con = db.Server.GetConnection();
        con.Open();
        var cmd = db.Server.GetCommand(
            "SELECT COUNT(*) FROM sys.objects WHERE type IN ('FN', 'IF', 'TF') AND name LIKE 'Get%'", con);
        var functionCount = (int)cmd.ExecuteScalar();
        Assert.That(functionCount, Is.GreaterThan(0), "Functions should have been created");
    }

    [Test]
    public void TestAtomicDatabaseCreation_RollbackOnFailure()
    {
        // Test that failures during initialization are handled gracefully
        var db = GetCleanedServer(DatabaseType.MicrosoftSQLServer);

        // Create a custom patcher that will fail midway through
        var badPatcher = new FailingTestPatcher();

        var mds = new MasterDatabaseScriptExecutor(db);

        // Attempt to create and patch - this should fail
        var notifier = ThrowImmediatelyCheckNotifier.Quiet;
        Assert.Throws<Exception>(() => mds.CreateAndPatchDatabase(badPatcher, notifier),
            "Should throw exception from intentionally failing script");

        // The database may or may not exist depending on where the failure occurred
        // The important thing is that an exception was thrown and we didn't get partial state
        // If the database exists, verify the test table from the failing script doesn't exist
        // (since the failure should have prevented successful creation)
        if (db.Exists())
        {
            var tables = db.DiscoverTables(false).Select(t => t.GetRuntimeName()).ToArray();
            // TestTable1 should not exist because the script failed before completing
            Assert.That(tables, Does.Not.Contain("TestTable1"),
                "Partial table creation should have been rolled back");
        }
    }

    /// <summary>
    /// Test patcher that intentionally fails during script execution
    /// </summary>
    private class FailingTestPatcher : IPatcher
    {
        public string ResourceSubdirectory => "TestPatches";
        public int Tier => 1;
        public string Name => "FailingTest";
        public string LegacyName => null;
        public bool SqlServerOnly => true;

        public Patch GetInitialCreateScriptContents(FAnsi.Discovery.DiscoveredDatabase db)
        {
            // Simple script that creates a table, then fails
            var script = @"--Version:1.0.0.0
--Description:Failing test patch
CREATE TABLE TestTable1 (ID int PRIMARY KEY, Name varchar(100))
GO
INSERT INTO TestTable1 VALUES (1, 'Test')
GO
-- This will cause a syntax error
THIS IS INTENTIONALLY BROKEN SQL TO CAUSE FAILURE
GO
";
            return new Patch("FailingTest.sql", script);
        }

        public System.Collections.Generic.SortedDictionary<string, Patch> GetAllPatchesInAssembly(
            FAnsi.Discovery.DiscoveredDatabase db)
        {
            return new System.Collections.Generic.SortedDictionary<string, Patch>();
        }

        public System.Reflection.Assembly GetDbAssembly() => GetType().Assembly;
    }
}