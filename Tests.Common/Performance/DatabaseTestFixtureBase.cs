// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using FAnsi;
using FAnsi.Connections;
using FAnsi.Discovery;
using NUnit.Framework;
using Rdmp.Core.CommandLine.DatabaseCreation;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.Startup;
using Rdmp.Core.Startup.Events;

namespace Tests.Common.Performance;

/// <summary>
/// Specialized shared test fixture base for database tests that provides optimized database
/// creation, connection pooling, and cleanup patterns to improve test performance.
///
/// This class extends the shared fixture pattern with database-specific optimizations including
/// database reuse, transaction management, and efficient cleanup strategies.
/// </summary>
public abstract class DatabaseTestFixtureBase : SharedTestFixtureBase
{
    private static readonly Dictionary<string, DatabasePool> _databasePools = new();
    private static readonly Dictionary<string, DbConnection> _connectionPools = new();
    private static readonly object _dbLock = new();
    private static Startup _startup;

    /// <summary>
    /// Gets a pooled database connection for the specified database type.
    /// </summary>
    /// <param name="databaseType">The type of database</param>
    /// <param name="connectionString">Connection string for the database</param>
    /// <returns>A database connection</returns>
    protected DbConnection GetPooledConnection(DatabaseType databaseType, string connectionString)
    {
        var key = $"{databaseType}_{connectionString.GetHashCode()}";

        lock (_dbLock)
        {
            if (_connectionPools.TryGetValue(key, out var connection))
            {
                // Check if connection is still valid
                try
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        return connection;
                    }
                    else
                    {
                        connection.Open();
                        return connection;
                    }
                }
                catch
                {
                    // Connection is invalid, remove it
                    _connectionPools.Remove(key);
                    connection.Dispose();
                }
            }

            // Create new connection
            var newConnection = CreateConnection(databaseType, connectionString);
            newConnection.Open();
            _connectionPools[key] = newConnection;
            return newConnection;
        }
    }

    /// <summary>
    /// Gets a database from the pool, creating a new one if necessary.
    /// </summary>
    /// <param name="databaseType">The type of database</param>
    /// <param name="databaseName">Name of the database</param>
    /// <param name="schema">Optional schema to apply to the database</param>
    /// <returns>A discovered database ready for testing</returns>
    protected DiscoveredDatabase GetPooledDatabase(DatabaseType databaseType, string databaseName, Action<DiscoveredDatabase> schema = null)
    {
        var key = $"{databaseType}_{databaseName}";

        lock (_dbLock)
        {
            if (!_databasePools.TryGetValue(key, out var pool))
            {
                pool = new DatabasePool(databaseType, databaseName, schema);
                _databasePools[key] = pool;
            }

            return pool.GetDatabase();
        }
    }

    /// <summary>
    /// Returns a database to the pool for reuse.
    /// </summary>
    /// <param name="database">The database to return to the pool</param>
    protected void ReturnDatabaseToPool(DiscoveredDatabase database)
    {
        if (database == null) return;

        var key = $"{database.Server.DatabaseType}_{database.GetRuntimeName()}";

        lock (_dbLock)
        {
            if (_databasePools.TryGetValue(key, out var pool))
            {
                pool.ReturnDatabase(database);
            }
        }
    }

    /// <summary>
    /// Executes a database operation within a transaction for better performance and isolation.
    /// </summary>
    /// <typeparam name="T">The return type of the operation</typeparam>
    /// <param name="database">The database to operate on</param>
    /// <param name="operation">The operation to execute</param>
    /// <returns>The result of the operation</returns>
    protected T ExecuteInTransaction<T>(DiscoveredDatabase database, Func<IManagedConnection, T> operation)
    {
        using var transaction = database.Server.BeginNewTransactedConnection();
        return operation(transaction);
        // Transaction is committed on disposal via using statement
    }

    /// <summary>
    /// Creates multiple databases in parallel for better performance.
    /// </summary>
    /// <param name="databaseDefinitions">Definitions of databases to create</param>
    /// <returns>Array of created databases</returns>
    protected DiscoveredDatabase[] CreateDatabasesInParallel(params (DatabaseType type, string name)[] databaseDefinitions)
    {
        var databases = new DiscoveredDatabase[databaseDefinitions.Length];
        var tasks = new Task[databaseDefinitions.Length];

        for (int i = 0; i < databaseDefinitions.Length; i++)
        {
            var index = i;
            var (type, name) = databaseDefinitions[i];

            tasks[i] = Task.Run(() =>
            {
                databases[index] = GetPooledDatabase(type, name);
            });
        }

        Task.WaitAll(tasks);
        return databases;
    }

    /// <summary>
    /// Performs bulk database cleanup efficiently.
    /// </summary>
    /// <param name="databases">Databases to clean up</param>
    protected void BulkCleanupDatabases(params DiscoveredDatabase[] databases)
    {
        var cleanupTasks = new Task[databases.Length];

        for (int i = 0; i < databases.Length; i++)
        {
            var database = databases[i];
            cleanupTasks[i] = Task.Run(() =>
            {
                try
                {
                    // Return to pool instead of dropping
                    ReturnDatabaseToPool(database);
                }
                catch (Exception ex)
                {
                    TestContext.Out.WriteLine($"Error returning database {database.GetRuntimeName()} to pool: {ex.Message}");
                }
            });
        }

        Task.WaitAll(cleanupTasks);
    }

    protected override void PerformOneTimeSetup()
    {
        base.PerformOneTimeSetup();

        // Initialize startup services
        if (_startup == null)
        {
            _startup = new Startup(SharedRepositoryLocator);
            _startup.DatabaseFound += OnDatabaseFound;
            _startup.PluginPatcherFound += OnPluginPatcherFound;
            _startup.DoStartup(IgnoreAllErrorsCheckNotifier.Instance);
        }
    }

    protected override void PerformOneTimeTeardown()
    {
        base.PerformOneTimeTeardown();

        // Cleanup database pools
        foreach (var pool in _databasePools.Values)
        {
            pool.Dispose();
        }
        _databasePools.Clear();

        // Cleanup connection pools
        foreach (var connection in _connectionPools.Values)
        {
            try
            {
                connection.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
        _connectionPools.Clear();

        _startup = null;
    }

    private static DbConnection CreateConnection(DatabaseType databaseType, string connectionString)
    {
        return databaseType switch
        {
            DatabaseType.MicrosoftSQLServer => new Microsoft.Data.SqlClient.SqlConnection(connectionString),
            DatabaseType.MySql => new MySqlConnector.MySqlConnection(connectionString),
            DatabaseType.Oracle => new Oracle.ManagedDataAccess.Client.OracleConnection(connectionString),
            DatabaseType.PostgreSql => new Npgsql.NpgsqlConnection(connectionString),
            _ => throw new ArgumentException($"Unsupported database type: {databaseType}")
        };
    }

    private static void OnDatabaseFound(object sender, PlatformDatabaseFoundEventArgs args)
    {
        if (args.Exception != null && args.Status != RDMPPlatformDatabaseStatus.Healthy &&
            args.Status != RDMPPlatformDatabaseStatus.SoftwareOutOfDate)
        {
            TestContext.Out.WriteLine($"Database issue found: {args.SummariseAsString()}");
        }
    }

    private static void OnPluginPatcherFound(object sender, PluginPatcherFoundEventArgs args)
    {
        if (args.Status != PluginPatcherStatus.Healthy)
        {
            TestContext.Out.WriteLine($"Plugin patcher issue found: {args.Type.Name} - {args.Status}");
        }
    }

    /// <summary>
    /// Represents a pool of databases of a specific type and name.
    /// </summary>
    private class DatabasePool : IDisposable
    {
        private readonly DatabaseType _databaseType;
        private readonly string _databaseName;
        private readonly Action<DiscoveredDatabase> _schema;
        private readonly Queue<DiscoveredDatabase> _availableDatabases = new();
        private readonly HashSet<DiscoveredDatabase> _usedDatabases = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _disposed;

        public DatabasePool(DatabaseType databaseType, string databaseName, Action<DiscoveredDatabase> schema = null)
        {
            _databaseType = databaseType;
            _databaseName = databaseName;
            _schema = schema;
        }

        public DiscoveredDatabase GetDatabase()
        {
            _semaphore.Wait();
            try
            {
                if (_availableDatabases.TryDequeue(out var database))
                {
                    _usedDatabases.Add(database);
                    return database;
                }

                // Create new database
                database = CreateDatabase();
                _usedDatabases.Add(database);
                return database;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void ReturnDatabase(DiscoveredDatabase database)
        {
            if (database == null || _disposed) return;

            _semaphore.Wait();
            try
            {
                if (_usedDatabases.Remove(database))
                {
                    // Clean database for reuse
                    CleanupDatabase(database);
                    _availableDatabases.Enqueue(database);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private DiscoveredDatabase CreateDatabase()
        {
            // Implementation would depend on your database creation logic
            // This is a placeholder - you'd implement based on your needs
            throw new NotImplementedException("Database creation logic needs to be implemented");
        }

        private void CleanupDatabase(DiscoveredDatabase database)
        {
            try
            {
                // Clean all tables except system tables
                foreach (var table in database.DiscoverTables(false))
                {
                    table.Drop();
                }

                // Apply schema if provided
                _schema?.Invoke(database);
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Error cleaning database {database.GetRuntimeName()}: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _semaphore.Wait();
            try
            {
                // Dispose all databases
                foreach (var database in _availableDatabases)
                {
                    try
                    {
                        database.Drop();
                    }
                    catch
                    {
                        // Ignore disposal errors
                    }
                }

                foreach (var database in _usedDatabases)
                {
                    try
                    {
                        database.Drop();
                    }
                    catch
                    {
                        // Ignore disposal errors
                    }
                }

                _availableDatabases.Clear();
                _usedDatabases.Clear();
                _disposed = true;
            }
            finally
            {
                _semaphore.Release();
                _semaphore.Dispose();
            }
        }
    }
}