// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using FAnsi.Connections;

namespace Rdmp.Core.MapsDirectlyToDatabaseTable;

/// <summary>
/// Provides a connection scope for reusing database connections across multiple repository operations.
/// This helps reduce connection thrashing by sharing a single connection for batch operations.
/// </summary>
public class ConnectionScope : IDisposable
{
    private readonly IManagedConnection _connection;
    private readonly bool _shouldDispose;
    private bool _disposed;

    /// <summary>
    /// Creates a new connection scope using the provided connection.
    /// </summary>
    /// <param name="connection">The connection to reuse. If null, a new connection will be created.</param>
    /// <param name="connectionFactory">Factory method to create a new connection if needed.</param>
    public ConnectionScope(IManagedConnection connection, Func<IManagedConnection> connectionFactory)
    {
        _connection = connection ?? connectionFactory();
        _shouldDispose = connection == null; // Only dispose if we created the connection
    }

    /// <summary>
    /// Gets the connection for this scope.
    /// </summary>
    public IManagedConnection Connection => _connection;

    /// <summary>
    /// Creates a connection scope for the specified repository.
    /// </summary>
    /// <param name="repository">The repository to create a connection scope for.</param>
    /// <param name="existingConnection">Optional existing connection to reuse.</param>
    /// <returns>A new connection scope.</returns>
    public static ConnectionScope Create(TableRepository repository, IManagedConnection existingConnection = null)
    {
        return new ConnectionScope(existingConnection, repository.GetConnection);
    }

    /// <summary>
    /// Executes multiple operations using the same connection.
    /// </summary>
    /// <param name="repository">The repository to execute operations on.</param>
    /// <param name="operations">The operations to execute.</param>
    /// <param name="existingConnection">Optional existing connection to reuse.</param>
    public static void ExecuteWithConnection(TableRepository repository, Action<IManagedConnection> operations, IManagedConnection existingConnection = null)
    {
        using var scope = Create(repository, existingConnection);
        operations(scope.Connection);
    }

    /// <summary>
    /// Executes multiple operations and returns a result using the same connection.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="repository">The repository to execute operations on.</param>
    /// <param name="operations">The operations to execute.</param>
    /// <param name="existingConnection">Optional existing connection to reuse.</param>
    /// <returns>The result of the operations.</returns>
    public static T ExecuteWithConnection<T>(TableRepository repository, Func<IManagedConnection, T> operations, IManagedConnection existingConnection = null)
    {
        using var scope = Create(repository, existingConnection);
        return operations(scope.Connection);
    }

    public void Dispose()
    {
        if (!_disposed && _shouldDispose)
        {
            _connection?.Dispose();
            _disposed = true;
        }
    }
}