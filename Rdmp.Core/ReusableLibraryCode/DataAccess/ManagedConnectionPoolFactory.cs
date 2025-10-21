// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading;
using FAnsi.Connections;
using FAnsi.Discovery;
using NLog;

namespace Rdmp.Core.ReusableLibraryCode.DataAccess;

/// <summary>
/// Provides thread-local connection pooling for DiscoveredServer instances to eliminate
/// ephemeral connection churn. Maintains one long-lived connection per thread per server.
///
/// <para>NOTE: This is a temporary implementation for RDMP. Once FAnsi natively supports
/// GetPooledConnection, this extension will be removed. Check FAnsi first before using.</para>
/// </summary>
public static class ManagedConnectionPoolFactory
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private static readonly bool _fAnsiHasPooling;

    /// <summary>
    /// Thread-local storage for connections, keyed by connection string.
    /// Each thread maintains its own set of connections to different servers.
    /// </summary>
    private static readonly ThreadLocal<ConcurrentDictionary<string, IManagedConnection>> _threadLocalConnections =
        new(() => new ConcurrentDictionary<string, IManagedConnection>(), trackAllValues: true);

    static ManagedConnectionPoolFactory()
    {
        // Check if FAnsi already has GetPooledConnection method
        _fAnsiHasPooling = typeof(DiscoveredServer).GetMethod("GetPooledConnection") != null;

        if (_fAnsiHasPooling)
        {
            _logger.Info("FAnsi already has GetPooledConnection - RDMP extension will delegate to native implementation");
        }
        else
        {
            _logger.Info("Using RDMP ManagedConnectionPoolFactory extension for connection pooling");
        }
    }

    /// <summary>
    /// Gets a managed connection for the specified server. Returns a thread-local long-lived
    /// connection if available, otherwise creates a new one.
    ///
    /// If FAnsi has native GetPooledConnection support, delegates to that. Otherwise uses
    /// RDMP's implementation.
    /// </summary>
    /// <param name="server">The discovered server to connect to</param>
    /// <param name="transaction">Optional transaction to use (if specified, bypasses pooling)</param>
    /// <returns>A managed connection that should not be disposed (CloseOnDispose = false)</returns>
    public static IManagedConnection GetPooledConnection(this DiscoveredServer server, IManagedTransaction transaction = null)
    {
        // If FAnsi has native pooling, use that instead
        if (_fAnsiHasPooling)
        {
            // Use reflection to call the native method (will be optimized away when FAnsi is updated)
            var method = typeof(DiscoveredServer).GetMethod("GetPooledConnection");
            return (IManagedConnection)method.Invoke(server, new object[] { transaction });
        }

        // Otherwise use RDMP's implementation
        // If we have a transaction, use the standard non-pooled connection
        if (transaction != null)
            return server.GetManagedConnection(transaction);

        var connectionKey = server.Builder.ConnectionString;
        var threadConnections = _threadLocalConnections.Value;

        // Try to get existing connection for this server on this thread
        if (threadConnections.TryGetValue(connectionKey, out var existingConnection))
        {
            // Verify connection is still valid
            if (existingConnection?.Connection.State == ConnectionState.Open)
            {
                // Return a non-disposing wrapper
                var wrapper = existingConnection.Clone();
                wrapper.CloseOnDispose = false;
                return wrapper;
            }

            // Connection is dead, remove it
            threadConnections.TryRemove(connectionKey, out _);
        }

        // Create new long-lived connection for this thread
        var newConnection = server.GetManagedConnection(null);
        newConnection.CloseOnDispose = false; // Don't close on dispose - we manage the lifetime

        // Store it
        threadConnections[connectionKey] = newConnection;

        // Return a non-disposing wrapper
        var returnWrapper = newConnection.Clone();
        returnWrapper.CloseOnDispose = false;
        return returnWrapper;
    }

    /// <summary>
    /// Clears all pooled connections for the current thread.
    /// Useful for cleanup or when you want to force new connections.
    /// </summary>
    public static void ClearCurrentThreadConnections()
    {
        var threadConnections = _threadLocalConnections.Value;
        if (threadConnections == null) return;

        foreach (var kvp in threadConnections)
        {
            try
            {
                if (kvp.Value?.Connection.State == ConnectionState.Open)
                {
                    kvp.Value.Connection.Close();
                    kvp.Value.Connection.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, $"Error closing pooled connection for {kvp.Key}");
            }
        }

        threadConnections.Clear();
    }

    /// <summary>
    /// Clears all pooled connections across all threads.
    /// Should be called during application shutdown or when disposing repository objects.
    /// </summary>
    public static void ClearAllConnections()
    {
        if (_threadLocalConnections?.Values == null) return;

        foreach (var threadConnections in _threadLocalConnections.Values)
        {
            if (threadConnections == null) continue;

            foreach (var kvp in threadConnections)
            {
                try
                {
                    if (kvp.Value?.Connection.State == ConnectionState.Open)
                    {
                        kvp.Value.Connection.Close();
                        kvp.Value.Connection.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, $"Error closing pooled connection for {kvp.Key}");
                }
            }

            threadConnections.Clear();
        }
    }

    /// <summary>
    /// Gets statistics about the current connection pool state.
    /// Useful for monitoring and debugging.
    /// </summary>
    public static ConnectionPoolStatistics GetStatistics()
    {
        var stats = new ConnectionPoolStatistics
        {
            TotalThreadsWithConnections = _threadLocalConnections?.Values?.Count ?? 0
        };

        if (_threadLocalConnections?.Values != null)
        {
            foreach (var threadConnections in _threadLocalConnections.Values)
            {
                if (threadConnections == null) continue;

                stats.TotalPooledConnections += threadConnections.Count;
                foreach (var kvp in threadConnections)
                {
                    if (kvp.Value?.Connection.State == ConnectionState.Open)
                        stats.OpenConnections++;
                    else
                        stats.ClosedConnections++;
                }
            }
        }

        return stats;
    }

    /// <summary>
    /// Statistics about the connection pool state
    /// </summary>
    public class ConnectionPoolStatistics
    {
        public int TotalThreadsWithConnections { get; set; }
        public int TotalPooledConnections { get; set; }
        public int OpenConnections { get; set; }
        public int ClosedConnections { get; set; }

        public override string ToString() =>
            $"Threads: {TotalThreadsWithConnections}, Total: {TotalPooledConnections}, Open: {OpenConnections}, Closed: {ClosedConnections}";
    }
}
