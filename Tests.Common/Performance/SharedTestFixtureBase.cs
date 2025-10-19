// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Threading;
using FAnsi;
using FAnsi.Discovery;
using NUnit.Framework;
using Rdmp.Core.CommandLine.DatabaseCreation;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.Repositories;
using Rdmp.Core.Startup;
using Tests.Common.Performance;

namespace Tests.Common.Performance;

/// <summary>
/// Base class for shared test fixtures that provides expensive one-time setup operations
/// and lazy initialization of commonly used resources to improve test performance.
///
/// This class implements the Shared Fixture pattern where expensive resources are created
/// once per test run and reused across multiple test classes, reducing setup overhead.
/// </summary>
public abstract class SharedTestFixtureBase
{
    protected static RepositoryPool RepositoryPool => RepositoryPool.Instance;
    protected static TestObjectCache ObjectCache => TestObjectCache.Instance;

    private static readonly object _lock = new();
    private static bool _oneTimeSetupCompleted;
    private static readonly Dictionary<string, Lazy<object>> _lazyResources = new();

    /// <summary>
    /// Gets the shared repository locator, creating it if necessary.
    /// </summary>
    protected IRDMPPlatformRepositoryServiceLocator SharedRepositoryLocator =>
        GetOrCreateSharedRepositoryLocator();

    /// <summary>
    /// Gets a lazy-initialized test database that can be reused across tests.
    /// </summary>
    protected Lazy<DiscoveredDatabase> SharedTestDatabase =>
        GetLazyResource<DiscoveredDatabase>("SharedTestDatabase", CreateSharedTestDatabase);

    /// <summary>
    /// Gets a lazy-initialized test table that can be reused across tests.
    /// </summary>
    protected Lazy<DiscoveredTable> SharedTestTable =>
        GetLazyResource<DiscoveredTable>("SharedTestTable", CreateSharedTestTable);

    /// <summary>
    /// Gets a lazy-initialized sample catalogue that can be reused across tests.
    /// </summary>
    protected Lazy<ICatalogue> SharedSampleCatalogue =>
        GetLazyResource<ICatalogue>("SharedSampleCatalogue", CreateSharedSampleCatalogue);

    /// <summary>
    /// Performs one-time setup operations that are expensive and should be shared across tests.
    /// </summary>
    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        lock (_lock)
        {
            if (_oneTimeSetupCompleted) return;

            try
            {
                PerformOneTimeSetup();
                _oneTimeSetupCompleted = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to perform one-time setup for {GetType().Name}", ex);
            }
        }
    }

    /// <summary>
    /// Performs cleanup of shared resources after all tests are complete.
    /// </summary>
    [OneTimeTearDown]
    public virtual void OneTimeTearDown()
    {
        lock (_lock)
        {
            try
            {
                PerformOneTimeTeardown();

                // Clear lazy resources
                foreach (var lazyResource in _lazyResources.Values)
                {
                    if (lazyResource.IsValueCreated && lazyResource.Value is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _lazyResources.Clear();
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid interfering with test cleanup
                TestContext.Out.WriteLine($"Error during one-time teardown: {ex.Message}");
            }
            finally
            {
                _oneTimeSetupCompleted = false;
            }
        }
    }

    /// <summary>
    /// Override this method to perform expensive one-time setup operations.
    /// This method is called only once per test run, regardless of how many test classes inherit from this base.
    /// </summary>
    protected virtual void PerformOneTimeSetup()
    {
        // Initialize FAnsi implementations
        FAnsi.Implementation.ImplementationManager.Load<FAnsi.Implementations.MicrosoftSQL.MicrosoftSQLImplementation>();
        FAnsi.Implementation.ImplementationManager.Load<FAnsi.Implementations.MySql.MySqlImplementation>();
        FAnsi.Implementation.ImplementationManager.Load<FAnsi.Implementations.Oracle.OracleImplementation>();
        FAnsi.Implementation.ImplementationManager.Load<FAnsi.Implementations.PostgreSql.PostgreSqlImplementation>();

        // Initialize repository pool
        RepositoryPool.GetOrCreateRepository(GetDefaultDatabaseOptions(), false);
    }

    /// <summary>
    /// Override this method to perform cleanup of shared resources.
    /// </summary>
    protected virtual void PerformOneTimeTeardown()
    {
        // Clear caches
        ObjectCache.ClearCache();
        RepositoryPool.ClearAll();
    }

    /// <summary>
    /// Override this method to provide default database creation options for shared repositories.
    /// </summary>
    /// <returns>Platform database creation options</returns>
    protected abstract PlatformDatabaseCreationOptions GetDefaultDatabaseOptions();

    /// <summary>
    /// Gets or creates a lazy-initialized resource with the specified key.
    /// </summary>
    /// <typeparam name="T">The type of resource</typeparam>
    /// <param name="key">The unique key for the resource</param>
    /// <param name="factory">Factory method to create the resource</param>
    /// <returns>Lazy-initialized resource</returns>
    protected static Lazy<T> GetLazyResource<T>(string key, Func<T> factory) where T : class
    {
        lock (_lock)
        {
            if (!_lazyResources.TryGetValue(key, out var lazyResource))
            {
                lazyResource = new Lazy<T>(factory, LazyThreadSafetyMode.ExecutionAndPublication);
                _lazyResources[key] = lazyResource;
            }

            return (Lazy<T>)lazyResource;
        }
    }

    private IRDMPPlatformRepositoryServiceLocator GetOrCreateSharedRepositoryLocator()
    {
        return RepositoryPool.GetOrCreateRepository(GetDefaultDatabaseOptions(), false);
    }

    private DiscoveredDatabase CreateSharedTestDatabase()
    {
        var repository = SharedRepositoryLocator;
        var server = repository.CatalogueRepository is ICatalogueRepository cataRepo
            ? cataRepo.DiscoveredServer
            : throw new InvalidOperationException("Cannot get server from repository");

        var dbName = $"SharedTestDb_{Guid.NewGuid():N}";
        var database = server.ExpectDatabase(dbName);

        if (!database.Exists())
        {
            database.Create(true);
        }

        return database;
    }

    private DiscoveredTable CreateSharedTestTable()
    {
        var database = SharedTestDatabase.Value;
        var tableName = $"SharedTestTable_{Guid.NewGuid():N}";

        using var dt = new System.Data.DataTable();
        dt.Columns.Add("ID", typeof(int));
        dt.Columns.Add("Name", typeof(string));
        dt.Columns.Add("CreatedDate", typeof(DateTime));

        dt.Rows.Add(1, "Test Row 1", DateTime.Now);
        dt.Rows.Add(2, "Test Row 2", DateTime.Now.AddDays(-1));

        return database.CreateTable(tableName, dt);
    }

    private ICatalogue CreateSharedSampleCatalogue()
    {
        var repository = SharedRepositoryLocator.CatalogueRepository;
        var catalogue = new Catalogue(repository, "SharedSampleCatalogue");
        catalogue.SaveToDatabase();
        return catalogue;
    }

    /// <summary>
    /// Creates a clean database for testing, reusing connections where possible.
    /// </summary>
    /// <param name="databaseType">The type of database to create</param>
    /// <param name="databaseName">Optional name for the database</param>
    /// <returns>A clean discovered database</returns>
    protected DiscoveredDatabase GetCleanDatabase(DatabaseType databaseType, string databaseName = null)
    {
        var repository = SharedRepositoryLocator;
        var server = repository.CatalogueRepository is ICatalogueRepository cataRepo
            ? cataRepo.DiscoveredServer
            : throw new InvalidOperationException("Cannot get server from repository");

        databaseName ??= $"CleanDb_{Guid.NewGuid():N}";
        var database = server.ExpectDatabase(databaseName);

        if (database.Exists())
        {
            // Clean existing database
            foreach (var table in database.DiscoverTables())
            {
                table.Drop();
            }
        }
        else
        {
            database.Create(true);
        }

        return database;
    }

    /// <summary>
    /// Executes SQL script on the shared database server.
    /// </summary>
    /// <param name="sql">The SQL script to execute</param>
    /// <param name="parameters">Optional parameters for the SQL command</param>
    protected void ExecuteSql(string sql, params (string name, object value)[] parameters)
    {
        var repository = SharedRepositoryLocator;
        var server = repository.CatalogueRepository is ICatalogueRepository cataRepo
            ? cataRepo.DiscoveredServer
            : throw new InvalidOperationException("Cannot get server from repository");

        using var connection = server.GetConnection();
        connection.Open();

        using var command = server.GetCommand(sql, connection);

        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Gets performance statistics for the shared resources.
    /// </summary>
    /// <returns>Dictionary containing performance statistics</returns>
    protected Dictionary<string, object> GetPerformanceStatistics()
    {
        return new Dictionary<string, object>
        {
            ["RepositoryPool"] = RepositoryPool.GetStatistics(),
            ["ObjectCache"] = ObjectCache.GetStatistics(),
            ["LazyResourceCount"] = _lazyResources.Count,
            ["OneTimeSetupCompleted"] = _oneTimeSetupCompleted
        };
    }
}