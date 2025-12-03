--Version:3.2.0
--Description: Changes all varchar(x) columns that involve user provided values into nvarchar(x)

if exists (select 1 from sys.indexes where name = 'idxGovernancePeriodNameMustBeUnique')
	DROP INDEX [idxGovernancePeriodNameMustBeUnique] ON [dbo].[GovernancePeriod]

if exists (select 1 from sys.indexes where name = 'ix_ConceptNamesMustBeUnique')
	DROP INDEX [ix_ConceptNamesMustBeUnique] ON [dbo].[StandardRegex]

if exists (select 1 from sys.indexes where name = 'ix_PreventDuplicateParameterNamesOnSameEntity')
	DROP INDEX [ix_PreventDuplicateParameterNamesOnSameEntity] ON [dbo].[AnyTableSqlParameter]

if exists (select 1 from sys.indexes where name = 'ix_PluginNameAndVersionMustBeUnique')
	DROP INDEX [ix_PluginNameAndVersionMustBeUnique] ON [dbo].[Plugin]

if exists (select 1 from sys.indexes where name = 'idx_ANOTableNamesMustBeUnique')
	DROP INDEX [idx_ANOTableNamesMustBeUnique] ON [dbo].[ANOTable]

if exists (select 1 from sys.indexes where name = 'ix_suffixMustBeUnique')
	DROP INDEX [ix_suffixMustBeUnique] ON [dbo].[ANOTable]

if exists (select 1 from sys.indexes where name = 'ix_preventDuplicatePipelineNames')
	DROP INDEX [ix_preventDuplicatePipelineNames] ON [dbo].[Pipeline]

if exists (select 1 from sys.indexes where name = 'ix_NamesMustBeUnique')
	DROP INDEX [ix_NamesMustBeUnique] ON [dbo].[DataAccessCredentials]

if exists (select 1 from sys.indexes where name = 'IX_RemoteRDMP_NameMustBeUnique')
	DROP INDEX [IX_RemoteRDMP_NameMustBeUnique] ON [dbo].[RemoteRDMP]


-- Drop any existing default constraint on AggregateConfiguration.CountSQL
DECLARE @constraintName1 NVARCHAR(200)
SELECT @constraintName1 = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.AggregateConfiguration') AND c.name = 'CountSQL'
IF @constraintName1 IS NOT NULL
	EXEC('ALTER TABLE [dbo].[AggregateConfiguration] DROP CONSTRAINT [' + @constraintName1 + ']')

-- Drop any existing default constraint on AggregateContinuousDateAxis.EndDate
DECLARE @constraintName2 NVARCHAR(200)
SELECT @constraintName2 = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.AggregateContinuousDateAxis') AND c.name = 'EndDate'
IF @constraintName2 IS NOT NULL
	EXEC('ALTER TABLE [dbo].[AggregateContinuousDateAxis] DROP CONSTRAINT [' + @constraintName2 + ']')

-- Drop any existing default constraint on AggregateContinuousDateAxis.StartDate
DECLARE @constraintName3 NVARCHAR(200)
SELECT @constraintName3 = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.AggregateContinuousDateAxis') AND c.name = 'StartDate'
IF @constraintName3 IS NOT NULL
	EXEC('ALTER TABLE [dbo].[AggregateContinuousDateAxis] DROP CONSTRAINT [' + @constraintName3 + ']')

-- Drop any existing default constraint on Catalogue.Folder
DECLARE @constraintName4 NVARCHAR(200)
SELECT @constraintName4 = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.Catalogue') AND c.name = 'Folder'
IF @constraintName4 IS NOT NULL
	EXEC('ALTER TABLE [dbo].[Catalogue] DROP CONSTRAINT [' + @constraintName4 + ']')

GO


alter table [CohortAggregateContainer] alter column [Operation] nvarchar(20)NOT NULL
alter table [CohortAggregateContainer] alter column [Name] nvarchar(1000)NOT NULL
alter table [CohortIdentificationConfiguration] alter column [Name] nvarchar(500)NOT NULL
alter table [CohortIdentificationConfiguration] alter column [Ticket] nvarchar(20)NULL
alter table [CohortIdentificationConfiguration] alter column [Description] nvarchar(max)NULL
alter table [CohortIdentificationConfiguration] alter column [FrozenBy] nvarchar(500)NULL
alter table [GovernanceDocument] alter column [URL] nvarchar(500)NOT NULL
alter table [GovernanceDocument] alter column [Description] nvarchar(max)NULL
alter table [GovernanceDocument] alter column [Name] nvarchar(500)NOT NULL
alter table [GovernancePeriod] alter column [Name] nvarchar(500)NOT NULL
alter table [GovernancePeriod] alter column [Description] nvarchar(max)NULL
alter table [GovernancePeriod] alter column [Ticket] nvarchar(20)NULL
alter table [StandardRegex] alter column [ConceptName] nvarchar(500)NOT NULL
alter table [StandardRegex] alter column [Regex] nvarchar(max)NOT NULL
alter table [StandardRegex] alter column [Description] nvarchar(max)NULL
alter table [AnyTableSqlParameter] alter column [ReferencedObjectType] nvarchar(200)NOT NULL
alter table [AnyTableSqlParameter] alter column [ParameterSQL] nvarchar(500)NULL
alter table [AnyTableSqlParameter] alter column [Value] nvarchar(max)NULL
alter table [AnyTableSqlParameter] alter column [Comment] nvarchar(500)NULL
alter table [AnyTableSqlParameter] alter column [ReferencedObjectRepositoryType] nvarchar(500)NOT NULL
alter table [PasswordEncryptionKeyLocation] alter column [Path] nvarchar(max)NULL
alter table [Plugin] alter column [Name] nvarchar(500)NOT NULL
alter table [Plugin] alter column [UploadedFromDirectory] nvarchar(max)NOT NULL
alter table [Plugin] alter column [PluginVersion] nvarchar(50)NOT NULL
alter table [Plugin] alter column [RdmpVersion] nvarchar(50)NOT NULL
alter table [ANOTable] alter column [TableName] nvarchar(500)NULL
alter table [ANOTable] alter column [Suffix] nvarchar(10)NULL
alter table [AggregateConfiguration] alter column [Name] nvarchar(500)NOT NULL
alter table [AggregateConfiguration] alter column [Description] nvarchar(max)NULL
alter table [AggregateConfiguration] alter column [CountSQL] nvarchar(1000)NULL
alter table [AggregateConfiguration] alter column [HavingSQL] nvarchar(max)NULL
alter table [AggregateContinuousDateAxis] alter column [StartDate] nvarchar(500)NULL
alter table [AggregateContinuousDateAxis] alter column [EndDate] nvarchar(500)NULL
alter table [AggregateDimension] alter column [SelectSQL] nvarchar(max)NULL
alter table [AggregateDimension] alter column [Alias] nvarchar(100)NULL
alter table [AggregateFilter] alter column [WhereSQL] nvarchar(max)NULL
alter table [AggregateFilter] alter column [Description] nvarchar(max)NULL
alter table [AggregateFilter] alter column [Name] nvarchar(1000)NOT NULL
alter table [AggregateFilterContainer] alter column [Operation] nvarchar(10)NOT NULL
alter table [AggregateFilterParameter] alter column [ParameterSQL] nvarchar(500)NULL
alter table [AggregateFilterParameter] alter column [Value] nvarchar(max)NULL
alter table [AggregateFilterParameter] alter column [Comment] nvarchar(500)NULL
alter table [Catalogue] alter column [Acronym] nvarchar(50)NULL
alter table [Catalogue] alter column [Name] nvarchar(1000)NOT NULL
alter table [Catalogue] alter column [Description] nvarchar(max)NULL
alter table [Catalogue] alter column [Detail_Page_URL] nvarchar(150)NULL
alter table [Catalogue] alter column [Type] nvarchar(50)NULL
alter table [Catalogue] alter column [Periodicity] nvarchar(50)NULL
alter table [Catalogue] alter column [Geographical_coverage] nvarchar(150)NULL
alter table [Catalogue] alter column [Background_summary] nvarchar(max)NULL
alter table [Catalogue] alter column [Search_keywords] nvarchar(150)NULL
alter table [Catalogue] alter column [Update_freq] nvarchar(50)NULL
alter table [Catalogue] alter column [Update_sched] nvarchar(50)NULL
alter table [Catalogue] alter column [Time_coverage] nvarchar(50)NULL
alter table [Catalogue] alter column [Contact_details] nvarchar(50)NULL
alter table [Catalogue] alter column [Resource_owner] nvarchar(50)NULL
alter table [Catalogue] alter column [Attribution_citation] nvarchar(500)NULL
alter table [Catalogue] alter column [Access_options] nvarchar(150)NULL
alter table [Catalogue] alter column [API_access_URL] nvarchar(150)NULL
alter table [Catalogue] alter column [Browse_URL] nvarchar(150)NULL
alter table [Catalogue] alter column [Bulk_Download_URL] nvarchar(150)NULL
alter table [Catalogue] alter column [Query_tool_URL] nvarchar(150)NULL
alter table [Catalogue] alter column [Source_URL] nvarchar(150)NULL
alter table [Catalogue] alter column [Granularity] nvarchar(50)NULL
alter table [Catalogue] alter column [Country_of_origin] nvarchar(150)NULL
alter table [Catalogue] alter column [Data_standards] nvarchar(500)NULL
alter table [Catalogue] alter column [Administrative_contact_name] nvarchar(50)NULL
alter table [Catalogue] alter column [Administrative_contact_email] nvarchar(255)NULL
alter table [Catalogue] alter column [Administrative_contact_telephone] nvarchar(50)NULL
alter table [Catalogue] alter column [Administrative_contact_address] nvarchar(500)NULL
alter table [Catalogue] alter column [Ethics_approver] nvarchar(255)NULL
alter table [Catalogue] alter column [Source_of_data_collection] nvarchar(500)NULL
alter table [Catalogue] alter column [SubjectNumbers] nvarchar(50)NULL
alter table [Catalogue] alter column [ValidatorXML] nvarchar(max)NULL
alter table [Catalogue] alter column [LoggingDataTask] nvarchar(100)NULL
alter table [Catalogue] alter column [Ticket] nvarchar(20)NULL
alter table [Catalogue] alter column [Folder] nvarchar(1000)NOT NULL
alter table [CatalogueItem] alter column [Name] nvarchar(256)NOT NULL
alter table [CatalogueItem] alter column [Topic] nvarchar(50)NULL
alter table [CatalogueItem] alter column [Periodicity] nvarchar(50)NULL
alter table [CatalogueItem] alter column [Agg_method] nvarchar(255)NULL
alter table [CatalogueItem] alter column [Statistical_cons] nvarchar(max) NULL
alter table [CatalogueItem] alter column [Research_relevance] nvarchar(max) NULL
alter table [CatalogueItem] alter column [Description] nvarchar(max) NULL
alter table [CatalogueItem] alter column [Limitations] nvarchar(max) NULL
alter table [CatalogueItem] alter column [Comments] nvarchar(max) NULL
alter table [CatalogueItemIssue] alter column [Name] nvarchar(1000)NOT NULL
alter table [CatalogueItemIssue] alter column [Description] nvarchar(max)NULL
alter table [CatalogueItemIssue] alter column [SQL] nvarchar(max)NULL
alter table [CatalogueItemIssue] alter column [Ticket] nvarchar(10)NULL
alter table [CatalogueItemIssue] alter column [Status] nvarchar(20)NOT NULL
alter table [CatalogueItemIssue] alter column [UserWhoCreated] nvarchar(500)NOT NULL
alter table [CatalogueItemIssue] alter column [UserWhoLastChangedStatus] nvarchar(500)NULL
alter table [CatalogueItemIssue] alter column [Action] nvarchar(max)NULL
alter table [CatalogueItemIssue] alter column [NotesToResearcher] nvarchar(max)NULL
alter table [CatalogueItemIssue] alter column [PathToExcelSheetWithAdditionalInformation] nvarchar(1000)NULL
alter table [ColumnInfo] alter column [Data_type] nvarchar(50)NULL
alter table [ColumnInfo] alter column [Format] nvarchar(50)NULL
alter table [ColumnInfo] alter column [Digitisation_specs] nvarchar(255)NULL
alter table [ColumnInfo] alter column [Name] nvarchar(1000)NOT NULL
alter table [ColumnInfo] alter column [Source] nvarchar(50)NULL
alter table [ColumnInfo] alter column [Description] nvarchar(1000)NULL
alter table [ColumnInfo] alter column [Status] nvarchar(10)NULL
alter table [ColumnInfo] alter column [RegexPattern] nvarchar(255)NULL
alter table [ColumnInfo] alter column [ValidationRules] nvarchar(max)NULL
alter table [ColumnInfo] alter column [Collation] nvarchar(100)NULL
alter table [JoinableCohortAggregateConfigurationUse] alter column [JoinType] nvarchar(100)NOT NULL
alter table [ExternalDatabaseServer] alter column [Name] nvarchar(1000)NOT NULL
alter table [ExternalDatabaseServer] alter column [Database] nvarchar(50)NULL
alter table [ExternalDatabaseServer] alter column [Server] nvarchar(50)NULL
alter table [ExternalDatabaseServer] alter column [Username] nvarchar(50)NULL
alter table [ExternalDatabaseServer] alter column [Password] nvarchar(max)NULL
alter table [ExternalDatabaseServer] alter column [CreatedByAssembly] nvarchar(500)NULL
alter table [ExternalDatabaseServer] alter column [MappedDataPath] nvarchar(1000)NULL
alter table [ExtractionFilter] alter column [WhereSQL] nvarchar(max)NULL
alter table [ExtractionFilter] alter column [Description] nvarchar(max)NULL
alter table [ExtractionFilter] alter column [Name] nvarchar(1000)NOT NULL
alter table [ExtractionFilterParameter] alter column [ParameterSQL] nvarchar(500)NULL
alter table [ExtractionFilterParameter] alter column [Value] nvarchar(max)NULL
alter table [ExtractionFilterParameter] alter column [Comment] nvarchar(500)NULL
alter table [ExtractionInformation] alter column [SelectSQL] nvarchar(max)NOT NULL
alter table [ExtractionInformation] alter column [ExtractionCategory] nvarchar(30)NOT NULL
alter table [ExtractionInformation] alter column [Alias] nvarchar(100)NULL
alter table [IssueSystemUser] alter column [Name] nvarchar(200)NOT NULL
alter table [IssueSystemUser] alter column [EmailAddress] nvarchar(500)NULL
alter table [JoinInfo] alter column [ExtractionJoinType] nvarchar(100)NOT NULL
alter table [JoinInfo] alter column [Collation] nvarchar(50)NULL
alter table [ExtractionFilterParameterSet] alter column [Name] nvarchar(max)NOT NULL
alter table [ExtractionFilterParameterSet] alter column [Description] nvarchar(max)NULL
alter table [LoadMetadata] alter column [AnonymisationEngineClass] nvarchar(50)NULL
alter table [LoadMetadata] alter column [Name] nvarchar(500)NOT NULL
alter table [LoadMetadata] alter column [Description] nvarchar(max)NULL
alter table [ExtractionFilterParameterSetValue] alter column [Value] nvarchar(max)NULL
alter table [LoadModuleAssembly] alter column [Committer] nvarchar(2000)NULL
alter table [LoadProgress] alter column [Name] nvarchar(500)NOT NULL
alter table [Favourite] alter column [ReferencedObjectType] nvarchar(500)NOT NULL
alter table [Favourite] alter column [ReferencedObjectRepositoryType] nvarchar(500)NOT NULL
alter table [Favourite] alter column [Username] nvarchar(500)NOT NULL
alter table [Pipeline] alter column [Name] nvarchar(500)NOT NULL
alter table [Pipeline] alter column [Description] nvarchar(max)NULL
alter table [Lookup] alter column [ExtractionJoinType] nvarchar(100)NOT NULL
alter table [Lookup] alter column [Collation] nvarchar(50)NULL
alter table [PipelineComponent] alter column [Name] nvarchar(500)NOT NULL
alter table [PipelineComponent] alter column [Class] nvarchar(500)NOT NULL
alter table [LookupCompositeJoinInfo] alter column [Collation] nvarchar(50)NULL
alter table [PipelineComponentArgument] alter column [Name] nvarchar(500)NOT NULL
alter table [PipelineComponentArgument] alter column [Value] nvarchar(max)NULL
alter table [PipelineComponentArgument] alter column [Type] nvarchar(500)NOT NULL
alter table [PipelineComponentArgument] alter column [Description] nvarchar(1000)NULL
alter table [PreLoadDiscardedColumn] alter column [RuntimeColumnName] nvarchar(500)NOT NULL
alter table [PreLoadDiscardedColumn] alter column [SqlDataType] nvarchar(50)NULL
alter table [ProcessTask] alter column [Path] nvarchar(500)NULL
alter table [ProcessTask] alter column [Name] nvarchar(500)NOT NULL
alter table [DashboardLayout] alter column [Name] nvarchar(1000)NOT NULL
alter table [DashboardLayout] alter column [Username] nvarchar(500)NOT NULL
alter table [ProcessTaskArgument] alter column [Name] nvarchar(500)NOT NULL
alter table [ProcessTaskArgument] alter column [Value] nvarchar(max)NULL
alter table [ProcessTaskArgument] alter column [Type] nvarchar(500)NOT NULL
alter table [ProcessTaskArgument] alter column [Description] nvarchar(1000)NULL
alter table [DashboardControl] alter column [ControlType] nvarchar(1000)NOT NULL
alter table [DashboardControl] alter column [PersistenceString] nvarchar(max)NULL
alter table [DataAccessCredentials] alter column [Username] nvarchar(500)NULL
alter table [DataAccessCredentials] alter column [Password] nvarchar(max)NULL
alter table [DataAccessCredentials] alter column [Name] nvarchar(100)NOT NULL
alter table [SupportingDocument] alter column [URL] nvarchar(500)NULL
alter table [SupportingDocument] alter column [Description] nvarchar(2000)NULL
alter table [SupportingDocument] alter column [Name] nvarchar(1000)NOT NULL
alter table [SupportingDocument] alter column [Ticket] nvarchar(10)NULL
alter table [SupportingSQLTable] alter column [Description] nvarchar(2000)NULL
alter table [SupportingSQLTable] alter column [Name] nvarchar(1000)NOT NULL
alter table [SupportingSQLTable] alter column [SQL] nvarchar(max)NULL
alter table [SupportingSQLTable] alter column [Ticket] nvarchar(10)NULL
alter table [TableInfo] alter column [Server] nvarchar(50)NULL
alter table [TableInfo] alter column [Database] nvarchar(500)NULL
alter table [TableInfo] alter column [Name] nvarchar(1000)NOT NULL
alter table [TableInfo] alter column [State] nvarchar(50)NULL
alter table [TableInfo] alter column [ValidationXml] nvarchar(max)NULL
alter table [TableInfo] alter column [Schema] nvarchar(500)NULL
alter table [RemoteRDMP] alter column [URL] nvarchar(1024)NOT NULL
alter table [RemoteRDMP] alter column [Name] nvarchar(100)NOT NULL
alter table [RemoteRDMP] alter column [Username] nvarchar(500)NULL
alter table [RemoteRDMP] alter column [Password] nvarchar(max)NULL
alter table [CacheProgress] alter column [Name] nvarchar(1000)NOT NULL
alter table [WindowLayout] alter column [Name] nvarchar(1000)NOT NULL
alter table [WindowLayout] alter column [LayoutData] nvarchar(max)NOT NULL
alter table [PermissionWindow] alter column [PermissionPeriodConfig] nvarchar(max)NULL
alter table [PermissionWindow] alter column [Name] nvarchar(1000)NOT NULL
alter table [PermissionWindow] alter column [Description] nvarchar(max)NULL
alter table [TicketingSystemConfiguration] alter column [Name] nvarchar(1000)NOT NULL
alter table [TicketingSystemConfiguration] alter column [Url] nvarchar(max)NULL
alter table [TicketingSystemConfiguration] alter column [Type] nvarchar(500)NULL
alter table [CacheFetchFailure] alter column [ExceptionText] nvarchar(max)NULL

GO

if not exists (select 1 from sys.indexes where name = 'idxGovernancePeriodNameMustBeUnique')
	CREATE UNIQUE NONCLUSTERED INDEX [idxGovernancePeriodNameMustBeUnique] ON [dbo].[GovernancePeriod]
	(
		[Name] ASC
	)

if not exists (select 1 from sys.indexes where name = 'ix_ConceptNamesMustBeUnique')
	CREATE UNIQUE NONCLUSTERED INDEX [ix_ConceptNamesMustBeUnique] ON [dbo].[StandardRegex]
	(
		[ConceptName] ASC
	)

if not exists (select 1 from sys.indexes where name = 'ix_PreventDuplicateParameterNamesOnSameEntity')
	CREATE UNIQUE NONCLUSTERED INDEX [ix_PreventDuplicateParameterNamesOnSameEntity] ON [dbo].[AnyTableSqlParameter]
	(
		[ReferencedObjectType] ASC,
		[ReferencedObjectID] ASC,
		[ParameterSQL] ASC
	)

if not exists (select 1 from sys.indexes where name = 'ix_PluginNameAndVersionMustBeUnique')
	CREATE UNIQUE NONCLUSTERED INDEX [ix_PluginNameAndVersionMustBeUnique] ON [dbo].[Plugin]
	(
		[Name] ASC,
		[PluginVersion] ASC
	)

if not exists (select 1 from sys.indexes where name = 'idx_ANOTableNamesMustBeUnique')
	CREATE UNIQUE NONCLUSTERED INDEX [idx_ANOTableNamesMustBeUnique] ON [dbo].[ANOTable]
	(
		[TableName] ASC
	)

if not exists (select 1 from sys.indexes where name = 'ix_suffixMustBeUnique')
	CREATE UNIQUE NONCLUSTERED INDEX [ix_suffixMustBeUnique] ON [dbo].[ANOTable]
	(
		[Suffix] ASC
	)

if not exists (select 1 from sys.indexes where name = 'ix_preventDuplicatePipelineNames')
	CREATE UNIQUE NONCLUSTERED INDEX [ix_preventDuplicatePipelineNames] ON [dbo].[Pipeline]
	(
		[Name] ASC
	)

if not exists (select 1 from sys.indexes where name = 'ix_NamesMustBeUnique')
	CREATE UNIQUE NONCLUSTERED INDEX [ix_NamesMustBeUnique] ON [dbo].[DataAccessCredentials]
	(
		[Name] ASC
	)

if not exists (select 1 from sys.indexes where name = 'IX_RemoteRDMP_NameMustBeUnique')
	CREATE UNIQUE NONCLUSTERED INDEX [IX_RemoteRDMP_NameMustBeUnique] ON [dbo].[RemoteRDMP]
	(
		[Name] ASC
	)

-- Add default for AggregateConfiguration.CountSQL if no default exists on this column
IF NOT EXISTS (
    SELECT 1 FROM sys.default_constraints dc
    JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.AggregateConfiguration') AND c.name = 'CountSQL'
)
	ALTER TABLE [dbo].[AggregateConfiguration] ADD CONSTRAINT [DF_AggregateConfiguration_CountSQL] DEFAULT ('count(*)') FOR [CountSQL]

-- Add default for AggregateContinuousDateAxis.StartDate if no default exists on this column
IF NOT EXISTS (
    SELECT 1 FROM sys.default_constraints dc
    JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.AggregateContinuousDateAxis') AND c.name = 'StartDate'
)
	ALTER TABLE [dbo].[AggregateContinuousDateAxis] ADD CONSTRAINT [DF_AggregateContinuousDateAxis_StartDate] DEFAULT ('''2001-01-01''') FOR [StartDate]

-- Add default for AggregateContinuousDateAxis.EndDate if no default exists on this column
IF NOT EXISTS (
    SELECT 1 FROM sys.default_constraints dc
    JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.AggregateContinuousDateAxis') AND c.name = 'EndDate'
)
	ALTER TABLE [dbo].[AggregateContinuousDateAxis] ADD CONSTRAINT [DF_AggregateContinuousDateAxis_EndDate] DEFAULT ('getdate()') FOR [EndDate]

-- Add default for Catalogue.Folder if no default exists on this column
IF NOT EXISTS (
    SELECT 1 FROM sys.default_constraints dc
    JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.Catalogue') AND c.name = 'Folder'
)
	ALTER TABLE [dbo].[Catalogue] ADD CONSTRAINT [DF_Folder] DEFAULT ('\') FOR [Folder]