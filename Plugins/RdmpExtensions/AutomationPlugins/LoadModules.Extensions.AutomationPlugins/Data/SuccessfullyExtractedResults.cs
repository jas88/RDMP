using System;
using System.Collections.Generic;
using System.Data.Common;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.Startup;

namespace LoadModules.Extensions.AutomationPlugins.Data;

public class SuccessfullyExtractedResults : DatabaseEntity
{
    #region Database Properties

    private string _sQL;
    private int _extractableDataSet_ID;
    private int _automateExtraction_ID;

    public string SQL
    {
        get => _sQL;
        set => SetField(ref _sQL, value);
    }
    public int ExtractableDataSet_ID
    {
        get => _extractableDataSet_ID;
        set => SetField(ref _extractableDataSet_ID, value);
    }
    public int AutomateExtraction_ID
    {
        get => _automateExtraction_ID;
        set => SetField(ref _automateExtraction_ID, value);
    }
    #endregion

    public SuccessfullyExtractedResults(PluginRepository repository, string sql, AutomateExtraction parent, IExtractableDataSet dataset)
    {
        repository.InsertAndHydrate(this, new Dictionary<string, object>
        {
            {"SQL",sql},
            {"ExtractableDataSet_ID",dataset.ID},
            {"AutomateExtraction_ID",parent.ID}
        });

        if (ID == 0 || Repository != repository)
            throw new ArgumentException("Repository failed to properly hydrate this class");
    }
    public SuccessfullyExtractedResults(PluginRepository repository, DbDataReader r)
        : base(repository, r)
    {
        SQL = r["SQL"].ToString();
        ExtractableDataSet_ID = Convert.ToInt32(r["ExtractableDataSet_ID"]);
        AutomateExtraction_ID = Convert.ToInt32(r["AutomateExtraction_ID"]);
    }
}