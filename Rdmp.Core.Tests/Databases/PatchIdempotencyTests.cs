// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FAnsi.Discovery;
using NUnit.Framework;
using Rdmp.Core.Databases;
using Rdmp.Core.MapsDirectlyToDatabaseTable.Versioning;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Tests.Common;

namespace Rdmp.Core.Tests.Databases;

/// <summary>
/// Tests to verify that:
/// (a) All migration patches are idempotent (can be re-applied safely)
/// (b) Applying all patches in sequence produces same result as compiled script
///
/// <para>These tests ensure new migrations maintain quality and compiled scripts stay in sync</para>
/// </summary>
[Category("Database")]
[Explicit("Long-running tests that create multiple databases for schema validation")]
public class PatchIdempotencyTests : DatabaseTests
{
    /// <summary>
    /// Verifies that all migration patches are idempotent - they can be applied twice
    /// without errors or unintended schema changes
    /// </summary>
    [Test]
    public void AllPatchesMustBeIdempotent()
    {
        var patchers = new (IPatcher patcher, string dbName)[]
        {
            (new CataloguePatcher(), "CatalogueIdempotencyTest"),
            (new DataExportPatcher(), "DataExportIdempotencyTest"),
            (new LoggingDatabasePatcher(), "LoggingIdempotencyTest")
        };

        var allPatchResults = new List<(string Database, string Patch, bool IsIdempotent, string Error)>();

        foreach (var (patcher, dbName) in patchers)
        {
            var db = GetCleanedServer(FAnsi.DatabaseType.MicrosoftSQLServer, dbName);

            if (db.Exists())
                db.Drop();

            // Create database with initial create script and all patches
            var executor = new MasterDatabaseScriptExecutor(db);
            executor.CreateAndPatchDatabase(patcher, new AcceptAllCheckNotifier());

            // Get all patches
            var patches = patcher.GetAllPatchesInAssembly(db);

            // Skip if using compiled scripts (no patches to verify)
            if (patches.Count == 0)
            {
                TestContext.Out.WriteLine($"{dbName}: Using compiled script (no migration patches)");
                continue;
            }

            TestContext.Out.WriteLine($"\n=== {dbName} ({patches.Count} patches) ===");

            foreach (var patch in patches)
            {
                var patchName = patch.Key;
                TestContext.Out.Write($"  {patchName}...");

                try
                {
                    // Get schema hash before
                    var hashBefore = GetSchemaHash(db);

                    // Apply patch again
                    ExecutePatchSQL(db, patch.Value);

                    // Get schema hash after
                    var hashAfter = GetSchemaHash(db);

                    var isIdempotent = hashBefore == hashAfter;
                    var status = isIdempotent ? "✓ idempotent" : "⚠️  schema changed";

                    TestContext.Out.WriteLine($" {status}");
                    allPatchResults.Add((dbName, patchName, isIdempotent, null));
                }
                catch (Exception ex)
                {
                    TestContext.Out.WriteLine($" ✗ FAILED: {ex.Message}");
                    allPatchResults.Add((dbName, patchName, false, ex.Message));
                }
            }
        }

        // Report results
        var failed = allPatchResults.Where(static r => r.Error != null).ToList();
        var schemaChanging = allPatchResults.Where(static r => r.Error == null && !r.IsIdempotent).ToList();
        var idempotent = allPatchResults.Where(static r => r.IsIdempotent).ToList();

        TestContext.Out.WriteLine($"\n=== SUMMARY ===");
        TestContext.Out.WriteLine($"✓ Idempotent: {idempotent.Count}");
        TestContext.Out.WriteLine($"⚠️  Schema-changing: {schemaChanging.Count}");
        TestContext.Out.WriteLine($"✗ Failed: {failed.Count}");

        if (failed.Any())
        {
            TestContext.Out.WriteLine($"\nFailing patches:");
            foreach (var (db, patch, _, error) in failed)
            {
                TestContext.Out.WriteLine($"  {db}/{patch}: {error}");
            }
        }

        Assert.That(failed, Is.Empty,
            $"{failed.Count} patches are not idempotent and fail when re-applied. " +
            $"All patches must be safe to re-apply. See test output for details.");
    }

    /// <summary>
    /// Verifies that applying all migration patches in sequence produces the same final schema
    /// as the compiled script. This ensures compiled scripts stay in sync with migrations.
    /// </summary>
    [Test]
    public void CompiledScriptMustMatchMigrationSequence()
    {
        var databases = new (string folder, string dbName, IPatcher patcher)[]
        {
            ("CatalogueDatabase", "Catalogue", new CataloguePatcher()),
            ("DataExportDatabase", "DataExport", new DataExportPatcher()),
            ("LoggingDatabase", "Logging", new LoggingDatabasePatcher())
        };

        foreach (var (folder, dbBaseName, patcher) in databases)
        {
            TestContext.Out.WriteLine($"\n=== Comparing {dbBaseName} ===");

            // Create database using compiled script
            var compiledDbName = $"{dbBaseName}_Compiled";
            var compiledDb = GetCleanedServer(FAnsi.DatabaseType.MicrosoftSQLServer, compiledDbName);

            var compiledExecutor = new MasterDatabaseScriptExecutor(compiledDb);
            compiledExecutor.CreateAndPatchDatabase(patcher, new AcceptAllCheckNotifier());

            var compiledHash = GetSchemaHash(compiledDb);
            TestContext.Out.WriteLine($"  Compiled script hash: {compiledHash.Substring(0, 16)}...");

            // Create database using migration sequence (replay original scripts)
            var migratedDbName = $"{dbBaseName}_Migrated";
            var migratedDb = GetCleanedServer(FAnsi.DatabaseType.MicrosoftSQLServer, migratedDbName);

            var migratedExecutor = new MasterDatabaseScriptExecutor(migratedDb);

            // Get initial create script from original file (not Patcher which may use programmatic creation)
            var createScriptPath = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                $"../../../../Rdmp.Core/Databases/{folder}/runAfterCreateDatabase/*.sql");

            var createScripts = Directory.GetFiles(
                Path.GetDirectoryName(createScriptPath),
                Path.GetFileName(createScriptPath))
                .Where(f => !f.EndsWith(".original")) // Skip backup files
                .OrderBy(f => f)
                .ToArray();

            Assert.That(createScripts, Is.Not.Empty,
                $"No create script found at {createScriptPath}");

            var initialScript = File.ReadAllText(createScripts[0]);
            var initialPatch = new Patch("Initial Creation", initialScript);

            migratedExecutor.CreateDatabase(initialPatch, new AcceptAllCheckNotifier());

            // Apply all migration patches from /up/ folder
            var upPath = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                $"../../../../Rdmp.Core/Databases/{folder}/up");

            if (Directory.Exists(upPath))
            {
                var migrationFiles = Directory.GetFiles(upPath, "*.sql")
                    .OrderBy(f => f)
                    .ToArray();

                TestContext.Out.WriteLine($"  Applying {migrationFiles.Length} migration patches...");

                foreach (var migrationFile in migrationFiles)
                {
                    var sql = File.ReadAllText(migrationFile);
                    var migrationPatch = new Patch(Path.GetFileName(migrationFile), sql);
                    ExecutePatchSQL(migratedDb, migrationPatch);
                }
            }

            var migratedHash = GetSchemaHash(migratedDb);
            TestContext.Out.WriteLine($"  Migrated sequence hash: {migratedHash.Substring(0, 16)}...");

            // Compare schemas
            if (compiledHash == migratedHash)
            {
                TestContext.Out.WriteLine($"  ✓ Schemas match!");
            }
            else
            {
                TestContext.Out.WriteLine($"  ✗ SCHEMA MISMATCH!");

                // Get detailed comparison
                var differences = CompareSchemas(compiledDb, migratedDb);
                foreach (var diff in differences.Take(20))
                {
                    TestContext.Out.WriteLine($"    {diff}");
                }

                if (differences.Count > 20)
                    TestContext.Out.WriteLine($"    ... and {differences.Count - 20} more differences");
            }

            Assert.That(migratedHash, Is.EqualTo(compiledHash),
                $"Compiled script for {dbBaseName} does not match migration sequence. " +
                $"Run: dotnet-script scripts/DumpCompiledSchema.csx to regenerate compiled scripts.");
        }
    }

    /// <summary>
    /// Computes a hash of the database schema for comparison purposes
    /// </summary>
    private string GetSchemaHash(DiscoveredDatabase db)
    {
        using var con = db.Server.GetConnection();
        con.Open();

        var sql = @"
            SELECT
                t.name AS TableName,
                c.name AS ColumnName,
                TYPE_NAME(c.user_type_id) AS DataType,
                c.max_length,
                c.precision,
                c.scale,
                c.is_nullable,
                c.is_identity,
                ISNULL(dc.definition, '') AS DefaultValue
            FROM sys.tables t
            INNER JOIN sys.columns c ON t.object_id = c.object_id
            LEFT JOIN sys.default_constraints dc ON c.default_object_id = dc.object_id
            WHERE t.is_ms_shipped = 0
            AND SCHEMA_NAME(t.schema_id) = 'dbo'
            ORDER BY t.name, c.column_id";

        var cmd = db.Server.GetCommand(sql, con);
        var reader = cmd.ExecuteReader();

        var sb = new StringBuilder();
        while (reader.Read())
        {
            sb.AppendLine($"{reader["TableName"]}.{reader["ColumnName"]}|" +
                         $"{reader["DataType"]}|{reader["max_length"]}|" +
                         $"{reader["precision"]}|{reader["scale"]}|" +
                         $"{reader["is_nullable"]}|{reader["is_identity"]}|" +
                         $"{reader["DefaultValue"]}");
        }

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return BitConverter.ToString(hash).Replace("-", "");
    }

    /// <summary>
    /// Executes a patch SQL script against a database
    /// </summary>
    private void ExecutePatchSQL(DiscoveredDatabase db, Patch patch)
    {
        using var con = db.Server.GetConnection();
        con.Open();

        var batches = System.Text.RegularExpressions.Regex.Split(
            patch.GetScriptBody(),
            @"^\s*GO\s*$",
            System.Text.RegularExpressions.RegexOptions.Multiline |
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (var batch in batches)
        {
            var trimmed = batch.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            var cmd = db.Server.GetCommand(trimmed, con);
            cmd.CommandTimeout = 300;
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Gets detailed schema differences between two databases
    /// </summary>
    private List<string> CompareSchemas(DiscoveredDatabase db1, DiscoveredDatabase db2)
    {
        var differences = new List<string>();

        var schema1 = GetSchemaDetails(db1);
        var schema2 = GetSchemaDetails(db2);

        // Compare columns
        var missingCols = schema2.Keys.Except(schema1.Keys).ToList();
        var extraCols = schema1.Keys.Except(schema2.Keys).ToList();

        differences.AddRange(missingCols.Select(col => $"- MISSING: {schema2[col]}"));
        differences.AddRange(extraCols.Select(col => $"+ EXTRA: {schema1[col]}"));

        var common = schema1.Keys.Intersect(schema2.Keys);
        differences.AddRange(common
            .Where(col => schema1[col] != schema2[col])
            .Select(col => $"≠ DIFFERENT: {col}\n    Compiled: {schema1[col]}\n    Migrated: {schema2[col]}"));

        return differences;
    }

    private Dictionary<string, string> GetSchemaDetails(DiscoveredDatabase db)
    {
        using var con = db.Server.GetConnection();
        con.Open();

        var sql = @"
            SELECT
                SCHEMA_NAME(t.schema_id) + '.' + t.name AS TableName,
                c.name AS ColumnName,
                TYPE_NAME(c.user_type_id) AS DataType,
                c.max_length,
                c.precision,
                c.scale,
                c.is_nullable,
                c.is_identity
            FROM sys.tables t
            INNER JOIN sys.columns c ON t.object_id = c.object_id
            WHERE t.is_ms_shipped = 0
            ORDER BY TableName, c.column_id";

        var cmd = db.Server.GetCommand(sql, con);
        var reader = cmd.ExecuteReader();

        var result = new Dictionary<string, string>();
        while (reader.Read())
        {
            var key = $"{reader["TableName"]}.{reader["ColumnName"]}";
            var value = $"{reader["DataType"]}({reader["max_length"]},{reader["precision"]},{reader["scale"]}) " +
                       $"NULL={reader["is_nullable"]} IDENTITY={reader["is_identity"]}";
            result[key] = value;
        }

        return result;
    }
}
