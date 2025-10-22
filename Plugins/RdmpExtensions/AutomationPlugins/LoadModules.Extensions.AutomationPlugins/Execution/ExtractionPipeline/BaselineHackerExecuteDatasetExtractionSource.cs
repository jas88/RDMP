using System;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using Rdmp.Core.DataExport.DataExtraction.Pipeline.Sources;
using Rdmp.Core.Repositories;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.AutomationPlugins.Execution.ExtractionPipeline;

public class BaselineHackerExecuteDatasetExtractionSource : ExecuteDatasetExtractionSource
{
    public override string HackExtractionSQL(string sql, IDataLoadEventListener listener)
    {
        var finder = new AutomateExtractionRepositoryFinder(new RepositoryProvider(Request.DataExportRepository));
        var repository = (AutomateExtractionRepository)finder.GetRepositoryIfAny() ?? throw new Exception(
            "AutomateExtractionRepositoryFinder returned null, are you missing an AutomationPlugins database");
        var hacker = new DeltaHacker(repository,Request);

        //hacking allowed?
        if (hacker.ExecuteHackIfAllowed(listener, out var hackSql) != BaselineHackEvaluation.Allowed) return sql;
        var newSql = sql + hackSql;
        listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information,
            $"Full Hacked Query is now:{Environment.NewLine}{newSql}"));

        return newSql;
    }
}