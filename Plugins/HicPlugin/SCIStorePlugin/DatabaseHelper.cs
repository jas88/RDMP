// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Data;
using Microsoft.Data.SqlClient;

namespace SCIStorePlugin;

/// <summary>
/// Helper class for SQL Server database operations, providing simplified methods for creating commands, executing queries, and bulk inserting data with configurable authentication and timeout settings.
/// </summary>
public class DatabaseHelper
{
    public string Database { get; set; }

    public string Server { get; private set; }

    public string Username { get; }

    public string Password { get; }

    private readonly int _timeout;
    private readonly bool _integratedSecurity;
    private readonly SqlConnectionStringBuilder _builder;

    // TODO: Construction needs refactored

    public DatabaseHelper(string server, string database, int timeout = 30)
    {
        Server = server;
        Database = database;
        _timeout = timeout;
        _integratedSecurity = true;
    }

    public DatabaseHelper(string server, string database, string username, string password, int timeout = 30)
    {
        Server = server;
        Database = database;
        _timeout = timeout;
        _integratedSecurity = false;
        Username = username;
        Password = password;
    }

    public DatabaseHelper(SqlConnectionStringBuilder builder)
    {
        _builder = builder;
        Database = _builder.InitialCatalog;
    }

    public string ConnectionString()
    {
        if (_builder != null)
            return _builder.ConnectionString;

        var sb = new SqlConnectionStringBuilder
        {
            DataSource = Server,
            InitialCatalog = Database,
            IntegratedSecurity = _integratedSecurity
        };

        if (!_integratedSecurity)
        {
            sb.UserID = Username;
            sb.Password = Password;
        }

        return sb.ConnectionString;
    }

    public SqlCommand CreateCommand(string sql)
    {
        return new SqlCommand
        {
            Connection = new SqlConnection(ConnectionString()),
            CommandText = sql,
            CommandTimeout = _timeout
        };
    }

    public SqlCommand CreateCommand(SqlConnection conn, string sql)
    {
        return new SqlCommand
        {
            Connection = conn,
            CommandText = sql,
            CommandTimeout = _timeout
        };
    }

    public SqlCommand CreateStoredProcedure(string sp)
    {
        return new SqlCommand(sp)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = _timeout
        };
    }

    public object ExecuteScalarObject(string sql)
    {
        using var conn = new SqlConnection(ConnectionString());
        conn.Open();
        var command = CreateCommand(conn, sql);
        return command.ExecuteScalar();
    }

    public int ExecuteNonQuery(string sql)
    {
        using var conn = new SqlConnection(ConnectionString());
        conn.Open();
        var command = CreateCommand(conn, sql);
        return command.ExecuteNonQuery();
    }

    public SqlDataReader ExecuteReader(string sql)
    {
        using var conn = new SqlConnection(ConnectionString());
        conn.Open();
        var command = CreateCommand(conn, sql);
        return command.ExecuteReader();
    }

    public int ExecuteNonQueryCommand(SqlCommand cmd)
    {
        using var conn = new SqlConnection(ConnectionString());
        conn.Open();
        return cmd.ExecuteNonQuery();
    }

    public DataTable GetDataTableFor(string tableName)
    {
        using var cmd = CreateCommand($"SELECT TOP 0 * FROM {tableName}");
        var dt = new DataTable(tableName);
        var da = new SqlDataAdapter
        {
            SelectCommand = cmd,
            MissingSchemaAction = MissingSchemaAction.AddWithKey
        };
                
        da.Fill(dt);

        return dt;
    }
}