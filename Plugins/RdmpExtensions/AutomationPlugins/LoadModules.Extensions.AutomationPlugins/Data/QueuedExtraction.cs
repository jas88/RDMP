using System;
using System.Collections.Generic;
using System.Data.Common;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Pipelines;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.Startup;

namespace LoadModules.Extensions.AutomationPlugins.Data;

public class QueuedExtraction : DatabaseEntity
{
    #region Database Properties

    private int _extractionConfiguration_ID;
    private int _pipeline_ID;
    private DateTime _dueDate;
    private string _requester;
    private DateTime _requestDate;


    public int ExtractionConfiguration_ID
    {
        get => _extractionConfiguration_ID;
        set => SetField(ref _extractionConfiguration_ID, value);
    }
    public int Pipeline_ID
    {
        get => _pipeline_ID;
        set => SetField(ref _pipeline_ID, value);
    }
    public DateTime DueDate
    {
        get => _dueDate;
        set => SetField(ref _dueDate, value);
    }
    public string Requester
    {
        get => _requester;
        set => SetField(ref _requester, value);
    }
    public DateTime RequestDate
    {
        get => _requestDate;
        set => SetField(ref _requestDate, value);
    }
    #endregion

    #region Relationships
    [NoMappingToDatabase]
    public IExtractionConfiguration ExtractionConfiguration => ((AutomateExtractionRepository)Repository).DataExportRepository.GetObjectByID<ExtractionConfiguration>(ExtractionConfiguration_ID);

    [NoMappingToDatabase]
    public Pipeline Pipeline => ((AutomateExtractionRepository)Repository).CatalogueRepository.GetObjectByID<Pipeline>(Pipeline_ID);

    #endregion

    public QueuedExtraction(PluginRepository repository, ExtractionConfiguration configuration, IPipeline extractionPipeline, DateTime dueDate)
    {
        repository.InsertAndHydrate(this, new Dictionary<string, object>
        {
            {"ExtractionConfiguration_ID",configuration.ID},
            {"Pipeline_ID",extractionPipeline.ID},
            {"DueDate",dueDate},
            {"Requester",Environment.UserName}
        });

        if (ID == 0 || Repository != repository)
            throw new ArgumentException("Repository failed to properly hydrate this class");
    }
    public QueuedExtraction(PluginRepository repository, DbDataReader r)
        : base(repository, r)
    {
        ExtractionConfiguration_ID = Convert.ToInt32(r["ExtractionConfiguration_ID"]);
        Pipeline_ID = Convert.ToInt32(r["Pipeline_ID"]);
        DueDate = Convert.ToDateTime(r["DueDate"]);
        Requester = r["Requester"].ToString();
        RequestDate = Convert.ToDateTime(r["RequestDate"]);
    }

    public bool IsDue()
    {
        return DateTime.Now > DueDate;
    }
}