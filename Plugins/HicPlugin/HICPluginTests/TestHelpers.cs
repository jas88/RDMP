// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using FAnsi;
using FAnsi.Discovery;
using FAnsi.Discovery.QuerySyntax;
using NPOI.OpenXmlFormats.Spreadsheet;
using Rdmp.Core.Caching.Requests;
using Rdmp.Core.Caching.Requests.FetchRequestProvider;
using Rdmp.Core.CohortCreation.Execution;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Aggregation;
using Rdmp.Core.Curation.Data.Cache;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Curation.Data.Pipelines;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.DataExport.DataExtraction;
using Rdmp.Core.DataExport.DataExtraction.Commands;
using Rdmp.Core.DataExport.DataExtraction.UserPicks;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine;
using Rdmp.Core.DataLoad.Engine.DatabaseManagement.EntityNaming;
using Rdmp.Core.DataLoad.Engine.DatabaseManagement.Operations;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.Logging;
using Rdmp.Core.Logging.PastEvents;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Core.MapsDirectlyToDatabaseTable.Revertable;
using Rdmp.Core.Providers;
using Rdmp.Core.QueryBuilding;
using Rdmp.Core.QueryBuilding.Parameters;
using Rdmp.Core.Repositories;
using Rdmp.Core.ReusableLibraryCode;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.DataAccess;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Rdmp.Core.Validation;
using Rdmp.Core.Validation.Constraints.Secondary;
using SCIStorePlugin.Data;
using SCIStorePlugin.DataProvider.RetryStrategies;
using SCIStorePlugin.Repositories;
using IContainer = Rdmp.Core.Curation.Data.IContainer;

namespace HICPluginTests;

internal sealed class MockLoadProgress : ILoadProgress
{
    private readonly ILoadMetadata _lmd;

    public MockLoadProgress(ILoadMetadata lmd)
    {
        _lmd = lmd;
    }

    /// <inheritdoc />
    public void DeleteInDatabase()
    {
    }

    /// <inheritdoc />
#pragma warning disable CS0067 // Event is never used - intentionally unused in mock object
    public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

    /// <inheritdoc />
    public int ID { get; set; }

    /// <inheritdoc />
    public IRepository Repository { get; set; }

    /// <inheritdoc />
    public void SetReadOnly()
    {
    }

    /// <inheritdoc />
    public void SaveToDatabase()
    {
    }

    /// <inheritdoc />
    public void RevertToDatabaseState()
    {
    }

    /// <inheritdoc />
    public RevertableObjectReport HasLocalChanges() => null;

    /// <inheritdoc />
    public bool Exists() => false;

    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public void Check(ICheckNotifier notifier)
    {
    }

    /// <inheritdoc />
    public DateTime? OriginDate { get; set; }

    /// <inheritdoc />
    public DateTime? DataLoadProgress { get; set; }

    /// <inheritdoc />
    public int LoadMetadata_ID { get; set; }

    /// <inheritdoc />
    public ILoadMetadata LoadMetadata => _lmd;

    /// <inheritdoc />
    public ICacheProgress CacheProgress { get; }

    /// <inheritdoc />
    public bool IsDisabled { get; set; }

    /// <inheritdoc />
    public int DefaultNumberOfDaysToLoadEachTime { get; }
}
internal sealed class MockPipeline : IPipeline
{
    /// <inheritdoc />
    public void InjectKnown(IPipelineComponent[] instance)
    {
    }

    /// <inheritdoc />
    public void ClearAllInjections()
    {
    }

    /// <inheritdoc />
    public void DeleteInDatabase()
    {
    }

    /// <inheritdoc />
#pragma warning disable CS0067 // Event is never used - intentionally unused in mock object
    public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

    /// <inheritdoc />
    public int ID { get; set; }

    /// <inheritdoc />
    public IRepository Repository { get; set; }

    /// <inheritdoc />
    public void SetReadOnly()
    {
    }

    /// <inheritdoc />
    public void SaveToDatabase()
    {
    }

    /// <inheritdoc />
    public void RevertToDatabaseState()
    {
    }

    /// <inheritdoc />
    public RevertableObjectReport HasLocalChanges() => null;

    /// <inheritdoc />
    public bool Exists() => false;

    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public string Description { get; set; }

    /// <inheritdoc />
    public int? DestinationPipelineComponent_ID { get; set; }

    /// <inheritdoc />
    public int? SourcePipelineComponent_ID { get; set; }

    /// <inheritdoc />
    public IList<IPipelineComponent> PipelineComponents { get; }

    /// <inheritdoc />
    public IPipelineComponent Destination { get; }

    /// <inheritdoc />
    public IPipelineComponent Source { get; }

    /// <inheritdoc />
    public Pipeline Clone() => null;
}
internal sealed class MockCacheProgress : ICacheProgress
{
    private readonly LoadMetadata _lmd;

    public MockCacheProgress(LoadMetadata lmd)
    {
        _lmd = lmd;
    }

    /// <inheritdoc />
    public void DeleteInDatabase()
    {
    }

    /// <inheritdoc />
#pragma warning disable CS0067 // Event is never used - intentionally unused in mock object
    public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

    /// <inheritdoc />
    public int ID { get; set; }

    /// <inheritdoc />
    public IRepository Repository { get; set; }

    /// <inheritdoc />
    public void SetReadOnly()
    {
    }

    /// <inheritdoc />
    public void SaveToDatabase()
    {
    }

    /// <inheritdoc />
    public void RevertToDatabaseState()
    {
    }

    /// <inheritdoc />
    public RevertableObjectReport HasLocalChanges() => null;

    /// <inheritdoc />
    public bool Exists() => false;

    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public DiscoveredServer GetDistinctLoggingDatabase() => null;

    /// <inheritdoc />
    public DiscoveredServer GetDistinctLoggingDatabase(out IExternalDatabaseServer serverChosen)
    {
        serverChosen = null;
        return null;
    }

    /// <inheritdoc />
    public string GetDistinctLoggingTask() => null;

    /// <inheritdoc />
    public IEnumerable<ArchivalDataLoadInfo> FilterRuns(IEnumerable<ArchivalDataLoadInfo> runs)
    {
        yield break;
    }

    /// <inheritdoc />
    public string CacheLagPeriodLoadDelay { get; }

    /// <inheritdoc />
    public int LoadProgress_ID { get; set; }

    /// <inheritdoc />
    public int? PermissionWindow_ID { get; set; }

    /// <inheritdoc />
    public DateTime? CacheFillProgress { get; set; }

    /// <inheritdoc />
    public string CacheLagPeriod { get; set; }

    /// <inheritdoc />
    public TimeSpan ChunkPeriod { get; set; }

    /// <inheritdoc />
    public int? Pipeline_ID { get; set; }

    /// <inheritdoc />
    public IPipeline Pipeline { get; } = new MockPipeline();

    /// <inheritdoc />
    public IPermissionWindow PermissionWindow { get; }

    /// <inheritdoc />
    public IEnumerable<ICacheFetchFailure> CacheFetchFailures { get; }

    /// <inheritdoc />
    public ILoadProgress LoadProgress => new MockLoadProgress(_lmd);

    /// <inheritdoc />
    public CacheLagPeriod GetCacheLagPeriod() => null;

    /// <inheritdoc />
    public void SetCacheLagPeriod(CacheLagPeriod cacheLagPeriod)
    {
    }

    /// <inheritdoc />
    public CacheLagPeriod GetCacheLagPeriodLoadDelay() => null;

    /// <inheritdoc />
    public void SetCacheLagPeriodLoadDelay(CacheLagPeriod cacheLagPeriod)
    {
    }

    /// <inheritdoc />
    public IEnumerable<ICacheFetchFailure> FetchPage(int start, int batchSize)
    {
        yield break;
    }

    /// <inheritdoc />
    public TimeSpan GetShortfall() => default;

    /// <inheritdoc />
    public string GetLoggingRunName() => null;
}
internal sealed class MockRepositorySupportsDateRangeQueries_CombinedReportData : IRepositorySupportsDateRangeQueries<CombinedReportData>
{
    /// <inheritdoc />
    public IEnumerable<CombinedReportData> ReadAll()
    {
        yield break;
    }

    /// <inheritdoc />
    public void Create(IEnumerable<CombinedReportData> reports, IDataLoadEventListener listener)
    {
    }

    /// <inheritdoc />
    public IEnumerable<CombinedReportData> ReadForInterval(DateTime day, TimeSpan timeSpan, IDataLoadEventListener listener,
        GracefulCancellationToken token)
    {
        yield break;
    }
}
internal sealed class MockRetryStrategy : IRetryStrategy
{
    private readonly Exception _exception;

    public MockRetryStrategy(Exception exception)
    {
        _exception = exception;
    }

    /// <inheritdoc />
    public IEnumerable<CombinedReportData> Fetch(DateTime dateToFetch, TimeSpan interval, IDataLoadEventListener listener,
        GracefulCancellationToken cancellationToken)
    {
        throw _exception;
    }

    /// <inheritdoc />
    public IRepositorySupportsDateRangeQueries<CombinedReportData> WebService
    {
        get => new MockRepositorySupportsDateRangeQueries_CombinedReportData();
        set { }
    }
}
internal sealed class MockCacheFetchRequestProvider : ICacheFetchRequestProvider
{
    private readonly ICacheFetchRequest _request;

    public MockCacheFetchRequestProvider(ICacheFetchRequest request=null)
    {
        _request = request ?? new CacheFetchRequest(null,DateTime.Now);
    }

    /// <inheritdoc />
    public ICacheFetchRequest Current => _request;

    /// <inheritdoc />
    public ICacheFetchRequest GetNext(IDataLoadEventListener listener) => _request;
}
internal sealed class MockReferentialIntegrityConstraint : ReferentialIntegrityConstraint
{
    private readonly Func<string,string> _failure;
    internal MockReferentialIntegrityConstraint(string failure=null)
    {
        _failure = failure is null ? null : _ => failure;
    }
    internal MockReferentialIntegrityConstraint(Func<string,string> failure)
    {
        _failure=failure;
    }
    public override ValidationFailure Validate(object value, object[] otherColumns, string[] otherColumnNames)
    {
        var message = _failure?.Invoke(value?.ToString());
        return message is null ? null : new ValidationFailure(message, this);
    }
}

internal sealed class MockSqlQueryBuilder : ISqlQueryBuilder
{
    private readonly List<QueryTimeColumn> _columns;

    public MockSqlQueryBuilder(List<QueryTimeColumn> columns)
    {
        _columns = columns;
    }

    /// <inheritdoc />
    public string SQL { get; }

    /// <inheritdoc />
    public bool SQLOutOfDate { get; set; }

    /// <inheritdoc />
    public string LimitationSQL { get; }

    /// <inheritdoc />
    public List<QueryTimeColumn> SelectColumns => _columns;

    /// <inheritdoc />
    public List<ITableInfo> TablesUsedInQuery { get; }

    /// <inheritdoc />
    public IQuerySyntaxHelper QuerySyntaxHelper { get; }

    /// <inheritdoc />
    public List<IFilter> Filters { get; }

    /// <inheritdoc />
    public List<JoinInfo> JoinsUsedInQuery { get; }

    /// <inheritdoc />
    public IContainer RootFilterContainer { get; set; }

    /// <inheritdoc />
    public bool CheckSyntax { get; set; }

    /// <inheritdoc />
    public ITableInfo PrimaryExtractionTable { get; }

    /// <inheritdoc />
    public ParameterManager ParameterManager { get; }

    /// <inheritdoc />
    public void AddColumnRange(IColumn[] columnsToAdd)
    {
    }

    /// <inheritdoc />
    public void AddColumn(IColumn col)
    {
    }

    /// <inheritdoc />
    public void RegenerateSQL()
    {
    }

    /// <inheritdoc />
    public IEnumerable<Lookup> GetDistinctRequiredLookups()
    {
        yield break;
    }

    /// <inheritdoc />
    public List<CustomLine> CustomLines { get; }

    /// <inheritdoc />
    public CustomLine AddCustomLine(string text, QueryComponent positionToInsert) => null;

    /// <inheritdoc />
    public CustomLine TopXCustomLine { get; set; }
}
internal sealed class MockExtractableCohort : IExtractableCohort
{
    /// <inheritdoc />
    public IQuerySyntaxHelper GetQuerySyntaxHelper() => null;

    /// <inheritdoc />
    public void DeleteInDatabase()
    {
    }

    /// <inheritdoc />
#pragma warning disable CS0067 // Event is never used - intentionally unused in mock object
    public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

    /// <inheritdoc />
    public int ID { get; set; }

    /// <inheritdoc />
    public IRepository Repository { get; set; }

    /// <inheritdoc />
    public void SetReadOnly()
    {
    }

    /// <inheritdoc />
    public void SaveToDatabase()
    {
    }

    /// <inheritdoc />
    public void RevertToDatabaseState()
    {
    }

    /// <inheritdoc />
    public RevertableObjectReport HasLocalChanges() => null;

    /// <inheritdoc />
    public bool Exists() => false;

    /// <inheritdoc />
    public bool IsDeprecated { get; set; }

    /// <inheritdoc />
    public IHasDependencies[] GetObjectsThisDependsOn() => Array.Empty<IHasDependencies>();

    /// <inheritdoc />
    public IHasDependencies[] GetObjectsDependingOnThis() => Array.Empty<IHasDependencies>();

    /// <inheritdoc />
    public int Count { get; }

    /// <inheritdoc />
    public int CountDistinct { get; }

    /// <inheritdoc />
    public int GetCountDistinctFromDatabase(int timeout = -1) => 0;

    /// <inheritdoc />
    public int ExternalCohortTable_ID { get; }

    /// <inheritdoc />
    public int OriginID { get; }

    /// <inheritdoc />
    public string OverrideReleaseIdentifierSQL { get; set; }

    /// <inheritdoc />
    public string AuditLog { get; set; }

    /// <inheritdoc />
    public IExternalCohortTable ExternalCohortTable { get; }

    /// <inheritdoc />
    public DataTable FetchEntireCohort() => null;

    /// <inheritdoc />
    public string GetPrivateIdentifier(bool runtimeName = false) => "PrivateID";

    /// <inheritdoc />
    public string GetReleaseIdentifier(bool runtimeName = false) => "ReleaseID";

    /// <inheritdoc />
    public string WhereSQL() => null;

    /// <inheritdoc />
    public IExternalCohortDefinitionData GetExternalData(int timeout = -1) => null;

    /// <inheritdoc />
    public string GetPrivateIdentifierDataType() => null;

    /// <inheritdoc />
    public string GetReleaseIdentifierDataType() => null;

    /// <inheritdoc />
    public DiscoveredDatabase GetDatabaseServer() => null;

    /// <inheritdoc />
    public void ReverseAnonymiseDataTable(DataTable toProcess, IDataLoadEventListener listener, bool allowCaching)
    {
    }
}

internal sealed class MockExtractionDirectory : IExtractionDirectory
{
    private readonly DirectoryInfo _dir;

    public MockExtractionDirectory(DirectoryInfo dir)
    {
        _dir = dir;
    }

    /// <inheritdoc />
    public DirectoryInfo GetDirectoryForDataset(IExtractableDataSet dataset) => _dir;

    /// <inheritdoc />
    public DirectoryInfo GetGlobalsDirectory() => null;

    /// <inheritdoc />
    public DirectoryInfo GetDirectoryForCohortCustomData() => null;
}
internal sealed class MockExtractableDataSet : IExtractableDataSet
{
    /// <inheritdoc />
    public void DeleteInDatabase()
    {
    }

    /// <inheritdoc />
#pragma warning disable CS0067 // Event is never used - intentionally unused in mock object
    public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

    /// <inheritdoc />
    public int ID { get; set; }

    /// <inheritdoc />
    public IRepository Repository { get; set; }

    /// <inheritdoc />
    public void SetReadOnly()
    {
    }

    /// <inheritdoc />
    public void SaveToDatabase()
    {
    }

    /// <inheritdoc />
    public void RevertToDatabaseState()
    {
    }

    /// <inheritdoc />
    public RevertableObjectReport HasLocalChanges() => null;

    /// <inheritdoc />
    public bool Exists() => false;

    /// <inheritdoc />
    public int Catalogue_ID { get; set; }

    /// <inheritdoc />
    public bool DisableExtraction { get; set; }

    /// <inheritdoc />
    public ICatalogue Catalogue { get; }

    /// <inheritdoc />
    public int? Project_ID { get; set; }

    /// <inheritdoc />
    public bool IsCatalogueDeprecated { get; }
    public List<IProject> Projects { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}
internal sealed class MockExtractableDatasetBundle : IExtractableDatasetBundle
{
    private readonly IExtractableDataSet _dataSet=new MockExtractableDataSet();

    /// <inheritdoc />
    public IExtractableDataSet DataSet => _dataSet;

    /// <inheritdoc />
    public List<SupportingDocument> Documents { get; }

    /// <inheritdoc />
    public List<SupportingSQLTable> SupportingSQL { get; }

    /// <inheritdoc />
    public List<IBundledLookupTable> LookupTables { get; }

    /// <inheritdoc />
    public void DropContent(object toDrop)
    {
    }

    /// <inheritdoc />
    public Dictionary<object, ExtractCommandState> States { get; }
}
internal sealed class MockExtractDatasetCommand : IExtractDatasetCommand
{
    private readonly ICatalogue _catalogue;
    private readonly IExtractionDirectory _dir;
    private readonly List<IColumn> _columns;
    private readonly ISqlQueryBuilder _queryBuilder;
    private readonly IExtractableDatasetBundle _datasetBundle;

    public MockExtractDatasetCommand(ICatalogue catalogue, IExtractionDirectory dir, List<IColumn> columns, ISqlQueryBuilder queryBuilder)
    {
        _catalogue = catalogue;
        _dir = dir;
        _columns = columns;
        _queryBuilder = queryBuilder;
        _datasetBundle = new MockExtractableDatasetBundle();
    }

    /// <inheritdoc />
    public DirectoryInfo GetExtractionDirectory() => null;

    /// <inheritdoc />
    public IExtractionConfiguration Configuration { get; }

    /// <inheritdoc />
    public string DescribeExtractionImplementation() => null;

    /// <inheritdoc />
    public ExtractCommandState State { get; }

    /// <inheritdoc />
    public void ElevateState(ExtractCommandState newState)
    {
    }

    /// <inheritdoc />
    public bool IsBatchResume { get; set; }

    /// <inheritdoc />
    public ISelectedDataSets SelectedDataSets { get; }

    /// <inheritdoc />
    public IExtractableCohort ExtractableCohort
    {
        get => new MockExtractableCohort();
        set { }
    }

    /// <inheritdoc />
    public ICatalogue Catalogue => _catalogue;

    /// <inheritdoc />
    public IExtractionDirectory Directory
    {
        get => _dir;
        set { }
    }

    /// <inheritdoc />
    public IExtractableDatasetBundle DatasetBundle => _datasetBundle;

    /// <inheritdoc />
    public List<IColumn> ColumnsToExtract
    {
        get => _columns;
        set { }
    }

    /// <inheritdoc />
    public IProject Project { get; }

    /// <inheritdoc />
    public void GenerateQueryBuilder()
    {
    }

    /// <inheritdoc />
    public ISqlQueryBuilder QueryBuilder
    {
        get => _queryBuilder;
        set { }
    }

    /// <inheritdoc />
    public ICumulativeExtractionResults CumulativeExtractionResults { get; }

    /// <inheritdoc />
    public int TopX { get; set; }

    /// <inheritdoc />
    public DateTime? BatchStart { get; set; }

    /// <inheritdoc />
    public DateTime? BatchEnd { get; set; }

    /// <inheritdoc />
    public DiscoveredServer GetDistinctLiveDatabaseServer() => null;
}

internal sealed class MockCatalogue : ICatalogue
{
    private readonly LoadMetadata _loadMetadata;

    public MockCatalogue(LoadMetadata loadMetadata)
    {
        _loadMetadata = loadMetadata;
    }

    /// <inheritdoc />
    public IHasDependencies[] GetObjectsThisDependsOn()
    {
        return new IHasDependencies[] { };
    }

    /// <inheritdoc />
    public IHasDependencies[] GetObjectsDependingOnThis()
    {
        return new IHasDependencies[] { };
    }

    /// <inheritdoc />
    public IQuerySyntaxHelper GetQuerySyntaxHelper() => null;

    /// <inheritdoc />
    public void DeleteInDatabase()
    {
    }

    /// <inheritdoc />
#pragma warning disable CS0067 // Event is never used - intentionally unused in mock object
    public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

    /// <inheritdoc />
    public int ID { get; set; }

    /// <inheritdoc />
    public IRepository Repository { get; set; }

    /// <inheritdoc />
    public void SetReadOnly()
    {
    }

    /// <inheritdoc />
    public void SaveToDatabase()
    {
    }

    /// <inheritdoc />
    public void RevertToDatabaseState()
    {
    }

    /// <inheritdoc />
    public RevertableObjectReport HasLocalChanges() => null;

    /// <inheritdoc />
    public bool Exists() => false;

    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public bool IsDeprecated { get; set; }

    /// <inheritdoc />
    public void ClearAllInjections()
    {
    }

    /// <inheritdoc />
    public void Check(ICheckNotifier notifier)
    {
    }

    /// <inheritdoc />
    public string Folder { get; set; }

    /// <inheritdoc />
    public ICatalogueRepository CatalogueRepository { get; }

    /// <inheritdoc />
    public int? LoadMetadata_ID { get; set; }

    /// <inheritdoc />
    public string LoggingDataTask { get; set; }

    /// <inheritdoc />
    public int? LiveLoggingServer_ID { get; set; }

    /// <inheritdoc />
    public string ValidatorXML { get; set; }

    /// <inheritdoc />
    public int? TimeCoverage_ExtractionInformation_ID { get; set; }

    /// <inheritdoc />
    public int? PivotCategory_ExtractionInformation_ID { get; set; }

    /// <inheritdoc />
    public bool IsInternalDataset { get; set; }

    /// <inheritdoc />
    public bool IsColdStorageDataset { get; set; }

    /// <inheritdoc />
    public string Time_coverage { get; set; }

    /// <inheritdoc />
    public Catalogue.CataloguePeriodicity Periodicity { get; set; }

    /// <inheritdoc />
    public string Description { get; set; }

    /// <inheritdoc />
    public DateTime? DatasetStartDate { get; set; }

    /// <inheritdoc />
    public ExtractionInformation TimeCoverage_ExtractionInformation { get; }

    /// <inheritdoc />
    public ExtractionInformation PivotCategory_ExtractionInformation { get; }

    /// <inheritdoc />
    public LoadMetadata LoadMetadata => _loadMetadata;

    /// <inheritdoc />
    public CatalogueItem[] CatalogueItems { get; }

    /// <inheritdoc />
    public AggregateConfiguration[] AggregateConfigurations { get; }

    /// <inheritdoc />
    public ExternalDatabaseServer LiveLoggingServer { get; }

    /// <inheritdoc />
    public string Acronym { get; set; }
    public string ShortDescription { get ; set;}
    public string DataType { get ; set;}
    public string DataSubtype { get ; set;}
    public DateTime? DatasetReleaseDate { get ; set;}
    public DateTime? StartDate { get ; set;}
    public DateTime? EndDate { get ; set;}
    public string Juristiction { get ; set;}
    public string DataController { get ; set;}
    public string DataProcessor { get ; set;}
    public string ControlledVocabulary { get ; set;}
    public string AssociatedPeople { get ; set;}
    public string AssociatedMedia { get ; set;}
    public string Doi { get ; set;}
    public string DataSubType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    /// <inheritdoc />
    public ITableInfo[] GetTableInfoList(bool includeLookupTables)
    {
        return new ITableInfo[] { };
    }

    /// <inheritdoc />
    public ITableInfo[] GetLookupTableInfoList()
    {
        return new ITableInfo[] { };
    }

    /// <inheritdoc />
    public void GetTableInfos(out List<ITableInfo> normalTables, out List<ITableInfo> lookupTables)
    {
        normalTables = null;
        lookupTables = null;
    }

    /// <inheritdoc />
    public void GetTableInfos(ICoreChildProvider provider, out List<ITableInfo> normalTables, out List<ITableInfo> lookupTables)
    {
        normalTables = null;
        lookupTables = null;
    }

    /// <inheritdoc />
    public DiscoveredServer GetDistinctLiveDatabaseServer(DataAccessContext context, bool setInitialDatabase,
        out IDataAccessPoint distinctAccessPoint)
    {
        distinctAccessPoint = null;
        return null;
    }

    /// <inheritdoc />
    public DiscoveredServer GetDistinctLiveDatabaseServer(DataAccessContext context, bool setInitialDatabase) => null;

    /// <inheritdoc />
    public ITableInfo[] GetTableInfosIdeallyJustFromMainTables()
    {
        return new ITableInfo[] { };
    }

    /// <inheritdoc />
    public SupportingSQLTable[] GetAllSupportingSQLTablesForCatalogue(FetchOptions fetch)
    {
        return new SupportingSQLTable[] { };
    }

    /// <inheritdoc />
    public ExtractionInformation[] GetAllExtractionInformation(ExtractionCategory category)
    {
        return new ExtractionInformation[] { };
    }

    /// <inheritdoc />
    public ExtractionInformation[] GetAllExtractionInformation()
    {
        return new ExtractionInformation[] { };
    }

    /// <inheritdoc />
    public SupportingDocument[] GetAllSupportingDocuments(FetchOptions fetch)
    {
        return new SupportingDocument[] { };
    }

    /// <inheritdoc />
    public ExtractionFilter[] GetAllMandatoryFilters()
    {
        return new ExtractionFilter[] { };
    }

    /// <inheritdoc />
    public ExtractionFilter[] GetAllFilters()
    {
        return new ExtractionFilter[] { };
    }

    /// <inheritdoc />
    public DatabaseType? GetDistinctLiveDatabaseServerType() => null;

    /// <inheritdoc />
    public CatalogueExtractabilityStatus GetExtractabilityStatus(IDataExportRepository dataExportRepository) => null;

    /// <inheritdoc />
    public ICatalogue ShallowClone() => null;

    /// <inheritdoc />
    public bool IsApiCall() => false;

    /// <inheritdoc />
    public bool IsApiCall(out IPluginCohortCompiler plugin)
    {
        plugin = null;
        return false;
    }

    /// <inheritdoc />
    public bool IsProjectSpecific(IDataExportRepository dataExportRepository) => false;
}

internal sealed class MockColumn : IColumn
{
    private readonly string _name;
    private readonly bool _isExtraction;

    public MockColumn(string name, bool isExtraction=false)
    {
        _name = name;
        _isExtraction = isExtraction;
    }

    /// <inheritdoc />
    public string GetRuntimeName() => _name;

    /// <inheritdoc />
    public void Check(ICheckNotifier notifier)
    {
    }

    /// <inheritdoc />
    public int Order { get; set; }

    /// <inheritdoc />
    public void DeleteInDatabase()
    {
    }

    /// <inheritdoc />
#pragma warning disable CS0067 // Event is never used - intentionally unused in mock object
    public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

    /// <inheritdoc />
    public int ID { get; set; }

    /// <inheritdoc />
    public IRepository Repository { get; set; }

    /// <inheritdoc />
    public void SetReadOnly()
    {
    }

    /// <inheritdoc />
    public ColumnInfo ColumnInfo { get; }

    /// <inheritdoc />
    public string SelectSQL { get; set; }

    /// <inheritdoc />
    public string Alias { get; }

    /// <inheritdoc />
    public bool HashOnDataRelease { get; }

    /// <inheritdoc />
    public bool IsExtractionIdentifier => _isExtraction;

    /// <inheritdoc />
    public bool IsPrimaryKey { get; }
}
internal sealed class MockDataLoadJob : IDataLoadJob
{
    internal MockDataLoadJob(int jobId)
    {
        JobID = jobId;
    }

    /// <inheritdoc />
    public void OnNotify(object sender, NotifyEventArgs e)
    {
    }

    /// <inheritdoc />
    public void OnProgress(object sender, ProgressEventArgs e)
    {
    }

    /// <inheritdoc />
    public void LoadCompletedSoDispose(ExitCodeType exitCode, IDataLoadEventListener postLoadEventsListener)
    {
    }

    /// <inheritdoc />
    public void StartLogging()
    {
    }

    /// <inheritdoc />
    public void CloseLogging()
    {
    }

    /// <inheritdoc />
    public void CreateTablesInStage(DatabaseCloner cloner, LoadBubble stage)
    {
    }

    /// <inheritdoc />
    public void PushForDisposal(IDisposeAfterDataLoad disposeable)
    {
    }

    /// <inheritdoc />
    public ColumnInfo[] GetAllColumns()
    {
        return new ColumnInfo[] { };
    }

    /// <inheritdoc />
    public void CrashAtEnd(NotifyEventArgs because)
    {
    }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public IDataLoadInfo DataLoadInfo { get; }

    /// <inheritdoc />
    public ILoadDirectory LoadDirectory { get; set; }

    /// <inheritdoc />
    public int JobID { get; set; }

    /// <inheritdoc />
    public ILoadMetadata LoadMetadata { get; }

    /// <inheritdoc />
    public string ArchiveFilepath { get; }

    /// <inheritdoc />
    public object Payload { get; set; }

    /// <inheritdoc />
    public IReadOnlyCollection<NotifyEventArgs> CrashAtEndMessages { get; }

    /// <inheritdoc />
    public List<ITableInfo> RegularTablesToLoad { get; }

    /// <inheritdoc />
    public List<ITableInfo> LookupTablesToLoad { get; }

    /// <inheritdoc />
    public IRDMPPlatformRepositoryServiceLocator RepositoryLocator { get; }

    /// <inheritdoc />
    public HICDatabaseConfiguration Configuration { get; }

    /// <inheritdoc />
    public bool PersistentRaw { get; set; }
}