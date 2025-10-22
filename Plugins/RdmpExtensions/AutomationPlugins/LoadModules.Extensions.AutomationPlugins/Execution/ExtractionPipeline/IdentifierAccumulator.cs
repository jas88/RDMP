using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using Rdmp.Core.Logging;

namespace LoadModules.Extensions.AutomationPlugins.Execution.ExtractionPipeline;

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