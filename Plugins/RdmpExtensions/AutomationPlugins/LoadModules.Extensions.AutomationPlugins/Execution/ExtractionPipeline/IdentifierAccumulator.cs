// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using Rdmp.Core.Logging;

namespace LoadModules.Extensions.AutomationPlugins.Execution.ExtractionPipeline;

/// <summary>
/// Thread-safe singleton that collects release identifiers seen during an extraction and commits them to the ReleaseIdentifiersSeen table to track which cohort members have been extracted in baseline runs.
/// </summary>
public class IdentifierAccumulator
{
    private static readonly Dictionary<int, IdentifierAccumulator> Accumulators =
        new Dictionary<int, IdentifierAccumulator>();

    private static readonly object oAccumulatorsLock = new object();

    public static IdentifierAccumulator GetInstance(DataLoadInfo dataLoadInfo)
    {
        lock (oAccumulatorsLock)
        {
            if (!Accumulators.ContainsKey(dataLoadInfo.ID))
                Accumulators.Add(dataLoadInfo.ID, new IdentifierAccumulator());

            return Accumulators[dataLoadInfo.ID];
        }
    }

    private IdentifierAccumulator()
    {
        _commitTblName = $"Temp{Guid.NewGuid().ToString().Replace("-", "")}";
    }

    HashSet<string>  identifiers = new HashSet<string>();
    private string _commitTblName;

    public void AddIdentifierIfNotSee(string identifier)
    {
        identifiers.Add(identifier);
    }

    public void CommitCurrentState(AutomateExtractionRepository repository, AutomateExtraction automateExtraction)
    {
        //only clar/commit on one thread at once!
        lock (oAccumulatorsLock)
        {
            var dt = new DataTable();

            dt.Columns.Add("AutomateExtraction_ID", typeof(int));
            dt.Columns.Add("ReleaseID", typeof(string));

            var id = automateExtraction.ID;

            foreach (var s in identifiers)
                dt.Rows.Add(id, s);

            //clear old history
            var tempTable = repository.DiscoveredServer.GetCurrentDatabase().ExpectTable(_commitTblName);

            using (var con = repository.DiscoveredServer.GetConnection())
            {
                con.Open();
                var query = $"SELECT TOP 0 * INTO {tempTable.GetFullyQualifiedName()} FROM ReleaseIdentifiersSeen";
                repository.DiscoveredServer.GetCommand(query, con).ExecuteNonQuery();
            }

            using(var bulk = tempTable.BeginBulkInsert())
            {
                bulk.Upload(dt);
            }

            //clear old history
            using (var con = new SqlConnection(repository.ConnectionString))
            {
                con.Open();
                var sql = $@"INSERT ReleaseIdentifiersSeen (AutomateExtraction_ID, ReleaseID)  
SELECT AutomateExtraction_ID, ReleaseID   
FROM {_commitTblName}
WHERE NOT EXISTS (SELECT 1 FROM ReleaseIdentifiersSeen A2 WHERE
A2.AutomateExtraction_ID = {_commitTblName}.AutomateExtraction_ID 
AND
A2.ReleaseID = {_commitTblName}.ReleaseID )";
                var cmd = new SqlCommand(sql, con);
                cmd.ExecuteNonQuery();
            }


            tempTable.Drop();
        }
    }
}