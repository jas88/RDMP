// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FAnsi.Discovery;
using Microsoft.Data.SqlClient;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.Databases;
using Rdmp.Core.MapsDirectlyToDatabaseTable.Versioning;
using Rdmp.Core.Repositories;
using Rdmp.Core.ReusableLibraryCode.Checks;

namespace Rdmp.Core.CommandLine.DatabaseCreation;

/// <summary>
/// Creates RDMP core databases (logging, DQE, Catalogue, DataExport) in the given database server.  Also creates initial
/// pipelines for common activities.
/// </summary>
public class PlatformDatabaseCreation
{
    public const string DefaultCatalogueDatabaseName = "Catalogue";
    public const string DefaultDataExportDatabaseName = "DataExport";
    public const string DefaultDQEDatabaseName = "DQE";
    public const string DefaultLoggingDatabaseName = "Logging";

    /// <summary>
    /// Creates new databases on the given server for RDMP platform databases
    /// </summary>
    /// <param name="options"></param>
    public static void CreatePlatformDatabases(PlatformDatabaseCreationOptions options)
    {
        DiscoveredServerHelper.CreateDatabaseTimeoutInSeconds = options.CreateDatabaseTimeout;

        // Build list of databases to create
        var databasesToCreate = new List<(string name, IPatcher patcher)>
        {
            (DefaultCatalogueDatabaseName, new CataloguePatcher()),
            (DefaultDataExportDatabaseName, new DataExportPatcher()),
            (DefaultDQEDatabaseName, new DataQualityEnginePatcher())
        };

        if (options.CreateLoggingServer)
        {
            databasesToCreate.Add((DefaultLoggingDatabaseName, new LoggingDatabasePatcher()));
        }

        // Create all databases in parallel with controlled concurrency
        // Limit to 4 concurrent operations to avoid overwhelming the database server
        var connectionStrings = new Dictionary<string, SqlConnectionStringBuilder>();
        var lockObj = new object();

        Parallel.ForEach(databasesToCreate,
            new ParallelOptions { MaxDegreeOfParallelism = 4 },
            db =>
            {
                var builder = Create(db.name, db.patcher, options);
                lock (lockObj)
                {
                    connectionStrings[db.name] = builder;
                }
            });

        // Extract connection strings for downstream use
        var dqe = connectionStrings[DefaultDQEDatabaseName];
        var logging = options.CreateLoggingServer ? connectionStrings[DefaultLoggingDatabaseName] : null;

        CatalogueRepository.SuppressHelpLoading = true;

        var repo = new PlatformDatabaseCreationRepositoryFinder(options);

        if (!options.SkipPipelines)
        {
            var creator = new CataloguePipelinesAndReferencesCreation(repo, logging, dqe);
            creator.Create(options);
        }

        if (options.ExampleDatasets || options.Nightmare)
        {
            var examples = new ExampleDatasetsCreation(new ThrowImmediatelyActivator(repo, null), repo);
            var server = new DiscoveredServer(options.GetBuilder("ExampleData"));

            examples.Create(server.GetCurrentDatabase(), ThrowImmediatelyCheckNotifier.Quiet, options);
        }
    }

    private static readonly object ConsoleLock = new();

    private static SqlConnectionStringBuilder Create(string databaseName, IPatcher patcher,
        PlatformDatabaseCreationOptions options)
    {
        var builder = options.GetBuilder(databaseName);

        var db = new DiscoveredServer(builder).ExpectDatabase(builder.InitialCatalog);

        if (options.DropDatabases && db.Exists())
        {
            lock (ConsoleLock)
            {
                Console.WriteLine($"Dropping Database:{builder.InitialCatalog}");
            }
            db.Drop();
        }

        var executor = new MasterDatabaseScriptExecutor(db)
        {
            Collation = options.Collation??(options.BinaryCollation? "Latin1_General_BIN2" : null)
        };
        executor.CreateAndPatchDatabase(patcher, new AcceptAllCheckNotifier());

        lock (ConsoleLock)
        {
            Console.WriteLine($"Created {builder.InitialCatalog} on server {builder.DataSource}");
        }

        return builder;
    }
}
