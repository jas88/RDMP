// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Data;
using Microsoft.Data.SqlClient;
using FAnsi.Discovery;

namespace SCIStorePlugin.Repositories;

public class DataTableSchemaFromDatabase : IDataTableSchemaSource
{
    private readonly DiscoveredDatabase _destinationDatabase;

    public DataTableSchemaFromDatabase(DiscoveredDatabase destinationDatabase)
    {
        _destinationDatabase = destinationDatabase;
    }

    public void SetSchema(DataTable dataTable)
    {
        if (string.IsNullOrWhiteSpace(dataTable.TableName))
            throw new Exception("The DataTable must have a TableName in order for its schema to be set this way");

        var cmdText = $"SELECT TOP 0 * FROM [{dataTable.TableName}]";
            
        var server = _destinationDatabase.Server;
        server.ChangeDatabase(_destinationDatabase.GetRuntimeName());

        using var con = server.GetConnection();
        using var sqlAdapter = new SqlDataAdapter(cmdText, (SqlConnection)con);
        sqlAdapter.Fill(dataTable);
    }
}