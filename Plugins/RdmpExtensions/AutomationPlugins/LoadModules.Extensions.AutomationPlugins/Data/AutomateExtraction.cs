using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.Startup;
using Microsoft.Data.SqlClient;

namespace LoadModules.Extensions.AutomationPlugins.Data;

public class AutomateExtraction : DatabaseEntity
{
    private readonly AutomateExtractionRepository _repository;

    #region Database Properties

    private int _extractionConfigurationId;
    private int _automateExtractionScheduleId;
    private bool _disabled;
    private DateTime? _baselineDate;
    private bool _refreshCohort;
    private bool _release;

    public int ExtractionConfigurationId
    {
        get => _extractionConfigurationId;
        set => SetField(ref _extractionConfigurationId, value);
    }
    public int AutomateExtractionScheduleId
    {
        get => _automateExtractionScheduleId;
        set => SetField(ref _automateExtractionScheduleId, value);
    }
    public bool Disabled
    {
        get => _disabled;
        set => SetField(ref _disabled, value);
    }
    public DateTime? BaselineDate
    {
        get => _baselineDate;
        set => SetField(ref _baselineDate, value);
    }

    public bool RefreshCohort
    {
        get => _refreshCohort;
        set => SetField(ref _refreshCohort , value);
    }

    public bool Release
    {
        get => _release;
        set => SetField(ref _release , value);
    }

    #endregion

    #region Relationships

    [NoMappingToDatabase]
    public IExtractionConfiguration ExtractionConfiguration => _repository.DataExportRepository.GetObjectByID<ExtractionConfiguration>(ExtractionConfigurationId);

    [NoMappingToDatabase]
    public AutomateExtractionSchedule AutomateExtractionSchedule => _repository.GetObjectByID<AutomateExtractionSchedule>(AutomateExtractionScheduleId);

    #endregion

    public AutomateExtraction(PluginRepository repository, AutomateExtractionSchedule schedule, IExtractionConfiguration config)
    {
        _repository = (AutomateExtractionRepository) repository;
        repository.InsertAndHydrate(this, new Dictionary<string, object>
        {
            {"AutomateExtractionSchedule_ID",schedule.ID},
            {"ExtractionConfiguration_ID",config.ID},
            {"RefreshCohort",false},
            {"Release",false}

        });

        if (ID == 0 || Repository != repository)
            throw new ArgumentException("Repository failed to properly hydrate this class");
    }
    public AutomateExtraction(PluginRepository repository, DbDataReader r)
        : base(repository, r)
    {
        _repository = (AutomateExtractionRepository) repository;
        ExtractionConfigurationId = Convert.ToInt32(r["ExtractionConfiguration_ID"]);
        AutomateExtractionScheduleId = Convert.ToInt32(r["AutomateExtractionSchedule_ID"]);
        Disabled = Convert.ToBoolean(r["Disabled"]);
        BaselineDate = ObjectToNullableDateTime(r["BaselineDate"]);

        RefreshCohort = Convert.ToBoolean(r["RefreshCohort"]);
        Release = Convert.ToBoolean(r["Release"]);
    }

    private ExtractionConfiguration _cachedExtractionConfiguration;


    public override string ToString()
    {
        _cachedExtractionConfiguration ??=
            _repository.DataExportRepository.GetObjectByID<ExtractionConfiguration>(ExtractionConfigurationId);

        return _cachedExtractionConfiguration.Name;
    }

    public DataTable GetIdentifiersTable()
    {
        var dt = new DataTable();
        dt.BeginLoadData();

        var repo = (TableRepository)Repository;
        var server = repo.DiscoveredServer;

        using var con = server.GetConnection();
        con.Open();
        var cmd = server.GetCommand("Select ReleaseID from ReleaseIdentifiersSeen", con);
        var da = server.GetDataAdapter(cmd);
        da.Fill(dt);
        dt.EndLoadData();

        return dt;
    }

    public SuccessfullyExtractedResults GetSuccessIfAnyFor(IExtractableDataSet ds)
    {
        return _repository.GetAllObjects<SuccessfullyExtractedResults>(
            $@"WHERE ExtractableDataSet_ID  = {ds.ID} AND AutomateExtraction_ID = {ID}").SingleOrDefault();
    }

    public void ClearBaselines()
    {
        using (var con = _repository.DiscoveredServer.GetConnection())
        {
            con.Open();
            new SqlCommand($@"Delete From 
  [ReleaseIdentifiersSeen]
  where
  AutomateExtraction_ID = {ID}", (SqlConnection) con).ExecuteNonQuery();
        }

        foreach (var r in _repository.GetAllObjectsWithParent<SuccessfullyExtractedResults>(this))
            r.DeleteInDatabase();

        BaselineDate = null;
        SaveToDatabase();
    }
}