// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Data;

namespace SCIStorePlugin.Data;

/// <summary>
/// Dataset ID provider that executes a stored procedure to generate new dataset identifiers based on data source, retrieval date, department, and transfer mechanism.
/// </summary>
public class StoredProcedureDatasetIdProvider : IDatasetIdProvider
{
    private readonly string _spName;
    private readonly DatabaseHelper _dh;
    private readonly string _dataSource;
    private readonly string _connectionName;
    private readonly string _dept;
    private readonly DateTime _retrievedOn;

    public StoredProcedureDatasetIdProvider(string spName, DatabaseHelper dh, string dataSource, string connectionName, string dept, DateTime retrievedOn)
    {
        _spName = spName;
        _dh = dh;
        _dataSource = dataSource;
        _connectionName = connectionName;
        _dept = dept;
        _retrievedOn = retrievedOn;
    }

    public int CreateDatasetId()
    {
        var cmd = _dh.CreateStoredProcedure(_spName);
        cmd.Parameters.AddWithValue("@filename", _dataSource);
        cmd.Parameters.AddWithValue("@description", $"For Date : {_retrievedOn:yyyy-MM-dd} from: {_connectionName}");
        cmd.Parameters.AddWithValue("@dsID", _dept);
        cmd.Parameters.AddWithValue("@transferMechanism", "Web Services Version ??");
        cmd.Parameters.Add("@myId", SqlDbType.Int).Direction = ParameterDirection.Output;

        var numRowsAffected = _dh.ExecuteNonQueryCommand(cmd);

        return numRowsAffected == 1 ? Convert.ToInt32(cmd.Parameters["@myId"].Value) : -1;
    }
}