-- Compiled schema for Catalogue
-- Generated from all migration scripts on 2025-11-04 08:34:41
-- This represents the final state after applying all migrations

-- Table: dbo.AggregateConfiguration
CREATE TABLE [dbo].[AggregateConfiguration] (
    [Catalogue_ID] [int] NOT NULL,
    [Name] [nvarchar](500) NOT NULL,
    [Description] [nvarchar](max) NULL,
    [dtCreated] [datetime] NOT NULL DEFAULT (getdate()),
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [RootFilterContainer_ID] [int] NULL,
    [CountSQL] [nvarchar](1000) NULL DEFAULT ('count(*)'),
    [PivotOnDimensionID] [int] NULL,
    [IsExtractable] [bit] NOT NULL DEFAULT ((0)),
    [OverrideFiltersByUsingParentAggregateConfigurationInstead_ID] [int] NULL,
    [HavingSQL] [nvarchar](max) NULL,
    [IsDisabled] [bit] NOT NULL DEFAULT ((0)),
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_AggregateConfiguration] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.AggregateContinuousDateAxis
CREATE TABLE [dbo].[AggregateContinuousDateAxis] (
    [AggregateDimension_ID] [int] NULL,
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [StartDate] [nvarchar](500) NULL DEFAULT ('''2001-01-01'''),
    [EndDate] [nvarchar](500) NULL DEFAULT ('getdate()'),
    [AxisIncrement] [int] NOT NULL DEFAULT ((2)),
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_AggregateContinuousDateAxis] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.AggregateDimension
CREATE TABLE [dbo].[AggregateDimension] (
    [AggregateConfiguration_ID] [int] NOT NULL,
    [ExtractionInformation_ID] [int] NOT NULL,
    [SelectSQL] [nvarchar](max) NULL,
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Alias] [nvarchar](100) NULL,
    [Order] [int] NOT NULL,
    [GroupBy] [int] NULL DEFAULT ((1)),
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_AggregateDimension] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.AggregateFilter
CREATE TABLE [dbo].[AggregateFilter] (
    [FilterContainer_ID] [int] NULL,
    [WhereSQL] [nvarchar](max) NULL,
    [Description] [nvarchar](max) NULL,
    [Name] [nvarchar](1000) NOT NULL,
    [IsMandatory] [bit] NOT NULL DEFAULT ((0)),
    [AssociatedColumnInfo_ID] [int] NULL,
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Order] [int] NOT NULL DEFAULT ((0)),
    [ClonedFromExtractionFilter_ID] [int] NULL,
    [IsDisabled] [bit] NOT NULL DEFAULT ((0)),
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_AggregateFilter] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.AggregateFilterContainer
CREATE TABLE [dbo].[AggregateFilterContainer] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Operation] [nvarchar](10) NOT NULL,
    [IsDisabled] [bit] NOT NULL DEFAULT ((0)),
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_AggregateFilterContainer] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.AggregateFilterParameter
CREATE TABLE [dbo].[AggregateFilterParameter] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [AggregateFilter_ID] [int] NOT NULL,
    [ParameterSQL] [nvarchar](500) NULL,
    [Value] [nvarchar](max) NULL,
    [Comment] [nvarchar](500) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_AggregateFilterParameter] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.AggregateFilterSubContainer
CREATE TABLE [dbo].[AggregateFilterSubContainer] (
    [AggregateFilterContainer_ParentID] [int] NULL,
    [AggregateFilterContainer_ChildID] [int] NULL,
    [RowVer] [timestamp] NOT NULL
) ON [PRIMARY];

GO

-- Table: dbo.AggregateForcedJoin
CREATE TABLE [dbo].[AggregateForcedJoin] (
    [AggregateConfiguration_ID] [int] NOT NULL,
    [TableInfo_ID] [int] NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_AggregateForcedJoin] PRIMARY KEY CLUSTERED
    (
        [AggregateConfiguration_ID] ASC,
        [TableInfo_ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.AggregateTopX
CREATE TABLE [dbo].[AggregateTopX] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [AggregateConfiguration_ID] [int] NOT NULL,
    [TopX] [int] NOT NULL DEFAULT ((1)),
    [OrderByDimensionIfAny_ID] [int] NULL,
    [OrderByDirection] [varchar](100) NOT NULL DEFAULT ('Descending'),
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_AggregateTopX] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ANOTable
CREATE TABLE [dbo].[ANOTable] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [TableName] [nvarchar](500) NULL,
    [Server_ID] [int] NOT NULL,
    [NumberOfIntegersToUseInAnonymousRepresentation] [int] NOT NULL DEFAULT ((1)),
    [NumberOfCharactersToUseInAnonymousRepresentation] [int] NOT NULL DEFAULT ((1)),
    [Suffix] [nvarchar](10) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ANOTable] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.AnyTableSqlParameter
CREATE TABLE [dbo].[AnyTableSqlParameter] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [ReferencedObjectType] [nvarchar](200) NOT NULL,
    [ReferencedObjectID] [int] NOT NULL,
    [ParameterSQL] [nvarchar](500) NULL,
    [Value] [nvarchar](max) NULL,
    [Comment] [nvarchar](500) NULL,
    [ReferencedObjectRepositoryType] [nvarchar](500) NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_AnyTableSqlParameter] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.CacheFetchFailure
CREATE TABLE [dbo].[CacheFetchFailure] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [CacheProgress_ID] [int] NOT NULL,
    [FetchRequestStart] [datetime] NOT NULL,
    [FetchRequestEnd] [datetime] NOT NULL,
    [ExceptionText] [nvarchar](max) NULL,
    [LastAttempt] [datetime] NOT NULL,
    [ResolvedOn] [datetime] NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_CacheFetchFailure] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.CacheProgress
CREATE TABLE [dbo].[CacheProgress] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [LoadProgress_ID] [int] NOT NULL,
    [PermissionWindow_ID] [int] NULL,
    [CacheFillProgress] [datetime] NULL,
    [CacheLagPeriod] [varchar](10) NULL,
    [ChunkPeriod] [time](0) NOT NULL DEFAULT ('01:00:00'),
    [Pipeline_ID] [int] NULL,
    [CacheLagPeriodLoadDelay] [varchar](10) NULL,
    [Name] [nvarchar](1000) NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_CacheProgress] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.Catalogue
CREATE TABLE [dbo].[Catalogue] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Acronym] [nvarchar](50) NULL,
    [Name] [nvarchar](1000) NOT NULL,
    [Description] [nvarchar](max) NULL,
    [Detail_Page_URL] [nvarchar](max) NULL,
    [Type] [nvarchar](max) NULL,
    [Periodicity] [nvarchar](50) NULL,
    [Geographical_coverage] [nvarchar](max) NULL,
    [Background_summary] [nvarchar](max) NULL,
    [Search_keywords] [nvarchar](150) NULL,
    [Update_freq] [nvarchar](50) NULL,
    [Update_sched] [nvarchar](max) NULL,
    [Time_coverage] [nvarchar](max) NULL,
    [Last_revision_date] [date] NULL,
    [Contact_details] [nvarchar](max) NULL,
    [Resource_owner] [nvarchar](max) NULL,
    [Attribution_citation] [nvarchar](max) NULL,
    [Access_options] [nvarchar](max) NULL,
    [API_access_URL] [nvarchar](max) NULL,
    [Browse_URL] [nvarchar](max) NULL,
    [Bulk_Download_URL] [nvarchar](max) NULL,
    [Query_tool_URL] [nvarchar](max) NULL,
    [Source_URL] [nvarchar](max) NULL,
    [Granularity] [nvarchar](max) NULL,
    [Country_of_origin] [nvarchar](max) NULL,
    [Data_standards] [nvarchar](max) NULL,
    [Administrative_contact_name] [nvarchar](max) NULL,
    [Administrative_contact_email] [nvarchar](max) NULL,
    [Administrative_contact_telephone] [nvarchar](max) NULL,
    [Administrative_contact_address] [nvarchar](max) NULL,
    [Explicit_consent] [bit] NULL,
    [Ethics_approver] [nvarchar](255) NULL,
    [Source_of_data_collection] [nvarchar](max) NULL,
    [SubjectNumbers] [nvarchar](max) NULL,
    [TimeCoverage_ExtractionInformation_ID] [int] NULL,
    [ValidatorXML] [nvarchar](max) NULL,
    [LoggingDataTask] [nvarchar](100) NULL,
    [Ticket] [nvarchar](20) NULL,
    [DatasetStartDate] [datetime] NULL,
    [IsDeprecated] [bit] NOT NULL DEFAULT ((0)),
    [IsInternalDataset] [bit] NOT NULL DEFAULT ((0)),
    [LiveLoggingServer_ID] [int] NULL,
    [ShortDescription] [nvarchar](250) NULL,
    [DataType] [nvarchar](255) NULL,
    [DataSubType] [nvarchar](255) NULL,
    [DataSource] [nvarchar](100) NULL,
    [DataSourceSetting] [nvarchar](100) NULL,
    [DatasetReleaseDate] [datetime] NULL,
    [StartDate] [datetime] NULL,
    [EndDate] [datetime] NULL,
    [UpdateLag] [nvarchar](255) NULL,
    [Juristiction] [nvarchar](255) NULL,
    [DataController] [nvarchar](255) NULL,
    [DataProcessor] [nvarchar](255) NULL,
    [ControlledVocabulary] [nvarchar](max) NULL,
    [AssociatedPeople] [nvarchar](max) NULL,
    [Doi] [nvarchar](50) NULL,
    [Purpose] [nvarchar](255) NULL,
    [AssociatedMedia] [nvarchar](max) NULL,
    [IsColdStorageDataset] [bit] NOT NULL DEFAULT ((0)),
    [Folder] [nvarchar](1000) NOT NULL DEFAULT ('\'),
    [PivotCategory_ExtractionInformation_ID] [int] NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_Data_Catalogue] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.CatalogueItem
CREATE TABLE [dbo].[CatalogueItem] (
    [Catalogue_ID] [int] NOT NULL,
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](256) NOT NULL,
    [Statistical_cons] [nvarchar](max) NULL,
    [Research_relevance] [nvarchar](max) NULL,
    [Description] [nvarchar](max) NULL,
    [Topic] [nvarchar](max) NULL,
    [Periodicity] [nvarchar](max) NULL,
    [Agg_method] [nvarchar](max) NULL,
    [Limitations] [nvarchar](max) NULL,
    [Comments] [nvarchar](max) NULL,
    [ColumnInfo_ID] [int] NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_Catalogue_Items] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.CatalogueItemIssue
CREATE TABLE [dbo].[CatalogueItemIssue] (
    [CatalogueItem_ID] [int] NOT NULL,
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](1000) NOT NULL,
    [Description] [nvarchar](max) NULL,
    [SQL] [nvarchar](max) NULL,
    [Ticket] [nvarchar](10) NULL,
    [Status] [nvarchar](20) NOT NULL,
    [DateCreated] [datetime] NOT NULL DEFAULT (getdate()),
    [UserWhoCreated] [nvarchar](500) NOT NULL,
    [DateOfLastStatusChange] [datetime] NULL,
    [UserWhoLastChangedStatus] [nvarchar](500) NULL,
    [Severity] [varchar](100) NOT NULL,
    [ReportedBy_ID] [int] NULL,
    [ReportedOnDate] [datetime] NULL,
    [Owner_ID] [int] NULL,
    [Action] [nvarchar](max) NULL,
    [NotesToResearcher] [nvarchar](max) NULL,
    [PathToExcelSheetWithAdditionalInformation] [nvarchar](1000) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_CatalogueItemIssue] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.CohortAggregateContainer
CREATE TABLE [dbo].[CohortAggregateContainer] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Operation] [nvarchar](20) NOT NULL,
    [Order] [int] NOT NULL,
    [Name] [nvarchar](1000) NOT NULL,
    [IsDisabled] [bit] NOT NULL DEFAULT ((0)),
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_CohortAggregateContainer] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.CohortAggregateContainer_AggregateConfiguration
CREATE TABLE [dbo].[CohortAggregateContainer_AggregateConfiguration] (
    [CohortAggregateContainer_ID] [int] NOT NULL,
    [AggregateConfiguration_ID] [int] NOT NULL,
    [Order] [int] NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_CohortAggregateContainer_AggregateConfiguration] PRIMARY KEY CLUSTERED
    (
        [AggregateConfiguration_ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.CohortAggregateSubContainer
CREATE TABLE [dbo].[CohortAggregateSubContainer] (
    [CohortAggregateContainer_ParentID] [int] NOT NULL,
    [CohortAggregateContainer_ChildID] [int] NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_CohortAggregateSubContainer] PRIMARY KEY CLUSTERED
    (
        [CohortAggregateContainer_ParentID] ASC,
        [CohortAggregateContainer_ChildID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.CohortIdentificationConfiguration
CREATE TABLE [dbo].[CohortIdentificationConfiguration] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](500) NOT NULL,
    [Ticket] [nvarchar](20) NULL,
    [Description] [nvarchar](max) NULL,
    [RootCohortAggregateContainer_ID] [int] NULL,
    [Version] [int] NULL,
    [QueryCachingServer_ID] [int] NULL,
    [Frozen] [bit] NOT NULL DEFAULT ((0)),
    [FrozenDate] [datetime] NULL,
    [FrozenBy] [nvarchar](500) NULL,
    [ClonedFrom_ID] [int] NULL,
    [RowVer] [timestamp] NOT NULL,
    [Folder] [varchar](max) NULL,
    CONSTRAINT [PK_CohortIdentificationConfiguration] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ColumnInfo
CREATE TABLE [dbo].[ColumnInfo] (
    [TableInfo_ID] [int] NOT NULL,
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Data_type] [nvarchar](50) NULL,
    [Format] [nvarchar](50) NULL,
    [Digitisation_specs] [nvarchar](255) NULL,
    [Name] [nvarchar](1000) NOT NULL,
    [Source] [nvarchar](50) NULL,
    [Description] [nvarchar](1000) NULL,
    [Status] [nvarchar](10) NULL,
    [RegexPattern] [nvarchar](255) NULL,
    [ValidationRules] [nvarchar](max) NULL,
    [IsPrimaryKey] [bit] NOT NULL DEFAULT ((0)),
    [ANOTable_ID] [int] NULL,
    [DuplicateRecordResolutionOrder] [int] NULL,
    [DuplicateRecordResolutionIsAscending] [bit] NOT NULL DEFAULT ((0)),
    [Dataset_ID] [int] NULL,
    [IsAutoIncrement] [bit] NOT NULL DEFAULT ((0)),
    [Collation] [nvarchar](100) NULL,
    [RowVer] [timestamp] NOT NULL,
    [IgnoreInLoads] [bit] NULL,
    CONSTRAINT [PK_Table_Items] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.Commit
CREATE TABLE [dbo].[Commit] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Username] [varchar](500) NOT NULL,
    [Date] [datetime] NOT NULL,
    [Transaction] [varchar](32) NOT NULL,
    [Description] [varchar](max) NOT NULL,
    CONSTRAINT [PK_Commit] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ConnectionStringKeyword
CREATE TABLE [dbo].[ConnectionStringKeyword] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [DatabaseType] [varchar](50) NOT NULL,
    [Name] [varchar](500) NOT NULL,
    [Value] [varchar](1000) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ConnectionStringKeyword] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.DashboardControl
CREATE TABLE [dbo].[DashboardControl] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [DashboardLayout_ID] [int] NOT NULL,
    [ControlType] [nvarchar](1000) NOT NULL,
    [X] [int] NOT NULL,
    [Y] [int] NOT NULL,
    [Width] [int] NOT NULL,
    [Height] [int] NOT NULL,
    [PersistenceString] [nvarchar](max) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_DashboardControl] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.DashboardLayout
CREATE TABLE [dbo].[DashboardLayout] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](1000) NOT NULL,
    [Username] [nvarchar](500) NOT NULL,
    [Created] [datetime] NOT NULL DEFAULT (getdate()),
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_DashboardLayout] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.DashboardObjectUse
CREATE TABLE [dbo].[DashboardObjectUse] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [DashboardControl_ID] [int] NOT NULL,
    [ReferencedObjectType] [varchar](500) NOT NULL,
    [ReferencedObjectID] [int] NOT NULL,
    [ReferencedObjectRepositoryType] [varchar](500) NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_DashboardObjectUse] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.DataAccessCredentials
CREATE TABLE [dbo].[DataAccessCredentials] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Username] [nvarchar](500) NULL,
    [Password] [nvarchar](max) NULL,
    [Name] [nvarchar](100) NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_DataAccessCredentials] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.DataAccessCredentials_TableInfo
CREATE TABLE [dbo].[DataAccessCredentials_TableInfo] (
    [TableInfo_ID] [int] NOT NULL,
    [DataAccessCredentials_ID] [int] NOT NULL,
    [Context] [varchar](30) NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_DataAccessCredentials_TableInfo] PRIMARY KEY CLUSTERED
    (
        [TableInfo_ID] ASC,
        [Context] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.Dataset
CREATE TABLE [dbo].[Dataset] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [varchar](256) NOT NULL,
    [Folder] [nvarchar](1000) NOT NULL,
    [DigitalObjectIdentifier] [varchar](256) NULL,
    [Source] [varchar](256) NULL,
    CONSTRAINT [PK_Dataset] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ExtendedProperty
CREATE TABLE [dbo].[ExtendedProperty] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [ReferencedObjectType] [varchar](500) NULL,
    [ReferencedObjectID] [int] NULL,
    [ReferencedObjectRepositoryType] [varchar](500) NULL,
    [Name] [varchar](500) NOT NULL,
    [Value] [varchar](max) NULL,
    [Type] [varchar](500) NOT NULL,
    [Description] [varchar](1000) NULL,
    CONSTRAINT [PK_ExtendedProperty] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ExternalDatabaseServer
CREATE TABLE [dbo].[ExternalDatabaseServer] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](1000) NOT NULL,
    [Database] [nvarchar](50) NULL,
    [Server] [nvarchar](50) NULL,
    [Username] [nvarchar](50) NULL,
    [Password] [nvarchar](max) NULL,
    [CreatedByAssembly] [nvarchar](500) NULL,
    [MappedDataPath] [nvarchar](1000) NULL,
    [DatabaseType] [varchar](100) NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ExternalDatabaseServer] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ExtractionFilter
CREATE TABLE [dbo].[ExtractionFilter] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [ExtractionInformation_ID] [int] NOT NULL,
    [WhereSQL] [nvarchar](max) NULL,
    [Description] [nvarchar](max) NULL,
    [Name] [nvarchar](1000) NOT NULL,
    [IsMandatory] [bit] NOT NULL DEFAULT ((0)),
    [Order] [int] NOT NULL DEFAULT ((0)),
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ExtractionFilter] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ExtractionFilterParameter
CREATE TABLE [dbo].[ExtractionFilterParameter] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [ExtractionFilter_ID] [int] NOT NULL,
    [ParameterSQL] [nvarchar](500) NULL,
    [Value] [nvarchar](max) NULL,
    [Comment] [nvarchar](500) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ExtractionFilterParameter] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ExtractionFilterParameterSet
CREATE TABLE [dbo].[ExtractionFilterParameterSet] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](max) NOT NULL,
    [Description] [nvarchar](max) NULL,
    [ExtractionFilter_ID] [int] NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ExtractionFilterParameterSet] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ExtractionFilterParameterSetValue
CREATE TABLE [dbo].[ExtractionFilterParameterSetValue] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [ExtractionFilterParameterSet_ID] [int] NOT NULL,
    [ExtractionFilterParameter_ID] [int] NOT NULL,
    [Value] [nvarchar](max) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ExtractionFilterParameterSetValue] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ExtractionInformation
CREATE TABLE [dbo].[ExtractionInformation] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [SelectSQL] [nvarchar](max) NOT NULL,
    [Order] [int] NOT NULL,
    [ExtractionCategory] [nvarchar](30) NOT NULL,
    [Alias] [nvarchar](100) NULL,
    [HashOnDataRelease] [bit] NOT NULL DEFAULT ((0)),
    [IsExtractionIdentifier] [bit] NOT NULL DEFAULT ((0)),
    [IsPrimaryKey] [bit] NOT NULL DEFAULT ((0)),
    [GroupBy] [int] NULL DEFAULT ((1)),
    [CatalogueItem_ID] [int] NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ExtractionInformation] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.Favourite
CREATE TABLE [dbo].[Favourite] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [ReferencedObjectType] [nvarchar](500) NOT NULL,
    [ReferencedObjectID] [int] NOT NULL,
    [ReferencedObjectRepositoryType] [nvarchar](500) NOT NULL,
    [Username] [nvarchar](500) NOT NULL,
    [FavouritedDate] [datetime] NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_Favourite] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.GovernanceDocument
CREATE TABLE [dbo].[GovernanceDocument] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [GovernancePeriod_ID] [int] NOT NULL,
    [URL] [nvarchar](500) NOT NULL,
    [Description] [nvarchar](max) NULL,
    [Name] [nvarchar](500) NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_GovernanceDocument] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.GovernancePeriod
CREATE TABLE [dbo].[GovernancePeriod] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](500) NOT NULL,
    [StartDate] [date] NOT NULL,
    [EndDate] [date] NULL,
    [Description] [nvarchar](max) NULL,
    [Ticket] [nvarchar](20) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_GovernancePeriod] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.GovernancePeriod_Catalogue
CREATE TABLE [dbo].[GovernancePeriod_Catalogue] (
    [Catalogue_ID] [int] NOT NULL,
    [GovernancePeriod_ID] [int] NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_GovernancePeriod_Catalogue] PRIMARY KEY CLUSTERED
    (
        [Catalogue_ID] ASC,
        [GovernancePeriod_ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.IssueSystemUser
CREATE TABLE [dbo].[IssueSystemUser] (
    [Name] [nvarchar](200) NOT NULL,
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [EmailAddress] [nvarchar](500) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_IssueSystemUser] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.JoinableCohortAggregateConfiguration
CREATE TABLE [dbo].[JoinableCohortAggregateConfiguration] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [CohortIdentificationConfiguration_ID] [int] NOT NULL,
    [AggregateConfiguration_ID] [int] NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_JoinableCohortAggregateConfiguration] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.JoinableCohortAggregateConfigurationUse
CREATE TABLE [dbo].[JoinableCohortAggregateConfigurationUse] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [JoinableCohortAggregateConfiguration_ID] [int] NOT NULL,
    [AggregateConfiguration_ID] [int] NOT NULL,
    [JoinType] [nvarchar](100) NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_JoinableCohortAggregateConfigurationUse] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.JoinInfo
CREATE TABLE [dbo].[JoinInfo] (
    [ForeignKey_ID] [int] NOT NULL,
    [PrimaryKey_ID] [int] NOT NULL,
    [ExtractionJoinType] [nvarchar](100) NOT NULL,
    [Collation] [nvarchar](50) NULL,
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_JoinInfo] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.LoadMetadata
CREATE TABLE [dbo].[LoadMetadata] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [LocationOfForLoadingDirectory] [varchar](3000) NULL,
    [LocationOfForArchivingDirectory] [varchar](3000) NULL,
    [LocationOfExecutablesDirectory] [varchar](3000) NULL,
    [LocationOfCacheDirectory] [varchar](3000) NULL,
    [AnonymisationEngineClass] [nvarchar](50) NULL,
    [Name] [nvarchar](500) NOT NULL,
    [Description] [nvarchar](max) NULL,
    [CacheArchiveType] [int] NOT NULL,
    [AllowReservedPrefix] [bit] NOT NULL DEFAULT ((0)),
    [RootLoadMetadata_ID] [int] NULL,
    [LastLoadTime] [datetime] NULL,
    [OverrideRAWServer_ID] [int] NULL,
    [RowVer] [timestamp] NOT NULL,
    [IgnoreTrigger] [bit] NULL,
    [Folder] [nvarchar](max) NULL,
    CONSTRAINT [PK_LoadMetadata] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.LoadMetadataCatalogueLinkage
CREATE TABLE [dbo].[LoadMetadataCatalogueLinkage] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [LoadMetadataID] [int] NOT NULL,
    [CatalogueID] [int] NOT NULL,
    CONSTRAINT [PK_LoadMetadataCatalogueLinkage] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.LoadModuleAssembly
CREATE TABLE [dbo].[LoadModuleAssembly] (
    [Bin] [varbinary](max) NOT NULL,
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Committer] [nvarchar](2000) NULL,
    [UploadDate] [datetime] NOT NULL DEFAULT (getdate()),
    [Plugin_ID] [int] NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_LoadModuleAssembly] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.LoadProgress
CREATE TABLE [dbo].[LoadProgress] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](500) NOT NULL,
    [OriginDate] [datetime] NULL,
    [DataLoadProgress] [datetime] NULL,
    [LoadMetadata_ID] [int] NOT NULL,
    [LoadPeriodicity] [varchar](10) NOT NULL DEFAULT (''),
    [IsDisabled] [bit] NOT NULL DEFAULT ((0)),
    [DefaultNumberOfDaysToLoadEachTime] [int] NOT NULL DEFAULT ((5)),
    [AllowAutomation] [bit] NOT NULL DEFAULT ((0)),
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_LoadSchedule] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.Lookup
CREATE TABLE [dbo].[Lookup] (
    [Description_ID] [int] NOT NULL,
    [ForeignKey_ID] [int] NOT NULL,
    [PrimaryKey_ID] [int] NOT NULL,
    [ExtractionJoinType] [nvarchar](100) NOT NULL,
    [Collation] [nvarchar](50) NULL,
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_Lookup] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.LookupCompositeJoinInfo
CREATE TABLE [dbo].[LookupCompositeJoinInfo] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [OriginalLookup_ID] [int] NOT NULL,
    [ForeignKey_ID] [int] NOT NULL,
    [PrimaryKey_ID] [int] NOT NULL,
    [Collation] [nvarchar](50) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_LookupCompositeJoinInfo] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.Memento
CREATE TABLE [dbo].[Memento] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [ReferencedObjectType] [varchar](500) NOT NULL,
    [ReferencedObjectID] [int] NOT NULL,
    [ReferencedObjectRepositoryType] [varchar](500) NOT NULL,
    [BeforeYaml] [varchar](max) NULL,
    [AfterYaml] [varchar](max) NULL,
    [Commit_ID] [int] NOT NULL,
    [Type] [varchar](20) NOT NULL,
    CONSTRAINT [PK_Memento] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ObjectExport
CREATE TABLE [dbo].[ObjectExport] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [ReferencedObjectType] [varchar](500) NOT NULL,
    [ReferencedObjectID] [int] NOT NULL,
    [SharingUID] [varchar](36) NOT NULL,
    [ReferencedObjectRepositoryType] [varchar](500) NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ObjectShares] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ObjectImport
CREATE TABLE [dbo].[ObjectImport] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [SharingUID] [varchar](36) NOT NULL,
    [ReferencedObjectID] [int] NOT NULL,
    [ReferencedObjectType] [varchar](500) NOT NULL,
    [ReferencedObjectRepositoryType] [varchar](500) NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ObjectImports] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.PasswordEncryptionKeyLocation
CREATE TABLE [dbo].[PasswordEncryptionKeyLocation] (
    [Path] [nvarchar](max) NULL,
    [Lock] [char](1) NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_T1] PRIMARY KEY CLUSTERED
    (
        [Lock] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.PermissionWindow
CREATE TABLE [dbo].[PermissionWindow] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [PermissionPeriodConfig] [nvarchar](max) NULL,
    [RequiresSynchronousAccess] [bit] NOT NULL DEFAULT ((1)),
    [Name] [nvarchar](1000) NOT NULL,
    [Description] [nvarchar](max) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_PermissionWindow] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.Pipeline
CREATE TABLE [dbo].[Pipeline] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](500) NOT NULL,
    [DestinationPipelineComponent_ID] [int] NULL,
    [SourcePipelineComponent_ID] [int] NULL,
    [Description] [nvarchar](max) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_Pipeline] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.PipelineComponent
CREATE TABLE [dbo].[PipelineComponent] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Order] [int] NOT NULL,
    [Pipeline_ID] [int] NOT NULL,
    [Name] [nvarchar](500) NOT NULL,
    [Class] [nvarchar](500) NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_PipelineComponent] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.PipelineComponentArgument
CREATE TABLE [dbo].[PipelineComponentArgument] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [PipelineComponent_ID] [int] NOT NULL,
    [Name] [nvarchar](500) NOT NULL,
    [Value] [nvarchar](max) NULL,
    [Type] [nvarchar](500) NOT NULL,
    [Description] [nvarchar](1000) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_PipelineComponentArgument] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.Plugin
CREATE TABLE [dbo].[Plugin] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](500) NOT NULL,
    [UploadedFromDirectory] [nvarchar](max) NOT NULL,
    [PluginVersion] [nvarchar](50) NOT NULL,
    [RdmpVersion] [nvarchar](50) NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_Plugin] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.PreLoadDiscardedColumn
CREATE TABLE [dbo].[PreLoadDiscardedColumn] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [TableInfo_ID] [int] NOT NULL,
    [Destination] [int] NOT NULL,
    [RuntimeColumnName] [nvarchar](500) NOT NULL,
    [SqlDataType] [nvarchar](50) NULL,
    [DuplicateRecordResolutionOrder] [int] NULL,
    [DuplicateRecordResolutionIsAscending] [bit] NOT NULL DEFAULT ((0)),
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_PreLoadDiscardedColumn] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ProcessTask
CREATE TABLE [dbo].[ProcessTask] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [LoadMetadata_ID] [int] NOT NULL,
    [Path] [nvarchar](500) NULL,
    [ProcessTaskType] [nchar](50) NOT NULL,
    [LoadStage] [nchar](50) NOT NULL,
    [Name] [nvarchar](500) NOT NULL,
    [Order] [int] NOT NULL,
    [RelatesSolelyToCatalogue_ID] [int] NULL,
    [IsDisabled] [bit] NOT NULL DEFAULT ((0)),
    [SerialisableConfiguration] [varchar](max) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ProcessTask] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ProcessTaskArgument
CREATE TABLE [dbo].[ProcessTaskArgument] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [ProcessTask_ID] [int] NOT NULL,
    [Name] [nvarchar](500) NOT NULL,
    [Value] [nvarchar](max) NULL,
    [Type] [nvarchar](500) NOT NULL,
    [Description] [nvarchar](1000) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ProcessTaskArgument] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.RegexRedaction
CREATE TABLE [dbo].[RegexRedaction] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [RedactionConfiguration_ID] [int] NOT NULL,
    [ColumnInfo_ID] [int] NOT NULL,
    [StartingIndex] [int] NOT NULL,
    [RedactedValue] [nvarchar](250) NULL,
    [ReplacementValue] [nvarchar](250) NOT NULL,
    CONSTRAINT [PK_RegexRedaction] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.RegexRedactionConfiguration
CREATE TABLE [dbo].[RegexRedactionConfiguration] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](250) NOT NULL,
    [Description] [nvarchar](250) NULL,
    [RegexPattern] [nvarchar](250) NOT NULL,
    [RedactionString] [nvarchar](250) NOT NULL,
    CONSTRAINT [PK_RegexRedactionConfiguration] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.RegexRedactionKey
CREATE TABLE [dbo].[RegexRedactionKey] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [RegexRedaction_ID] [int] NOT NULL,
    [ColumnInfo_ID] [int] NOT NULL,
    [Value] [nvarchar](max) NULL,
    CONSTRAINT [PK_RegexRedactionKey] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.RemoteRDMP
CREATE TABLE [dbo].[RemoteRDMP] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [URL] [nvarchar](1024) NOT NULL,
    [Name] [nvarchar](100) NOT NULL,
    [Username] [nvarchar](500) NULL,
    [Password] [nvarchar](max) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_RemoteRDMP] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ServerDefaults
CREATE TABLE [dbo].[ServerDefaults] (
    [DefaultType] [varchar](500) NOT NULL,
    [ExternalDatabaseServer_ID] [int] NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ServerDefaults] PRIMARY KEY CLUSTERED
    (
        [DefaultType] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.Setting
CREATE TABLE [dbo].[Setting] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Key] [varchar](450) NOT NULL,
    [Value] [varchar](max) NOT NULL,
    CONSTRAINT [PK_SettingKey] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.StandardRegex
CREATE TABLE [dbo].[StandardRegex] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [ConceptName] [nvarchar](500) NOT NULL,
    [Regex] [nvarchar](max) NOT NULL,
    [Description] [nvarchar](max) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_StandardRegex] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.SupportingDocument
CREATE TABLE [dbo].[SupportingDocument] (
    [Catalogue_ID] [int] NOT NULL,
    [URL] [nvarchar](500) NULL,
    [Description] [nvarchar](2000) NULL,
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](1000) NOT NULL,
    [Extractable] [bit] NOT NULL DEFAULT ((0)),
    [Ticket] [nvarchar](10) NULL,
    [IsGlobal] [bit] NOT NULL DEFAULT ((0)),
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_SupportingDocument] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.SupportingSQLTable
CREATE TABLE [dbo].[SupportingSQLTable] (
    [Catalogue_ID] [int] NOT NULL,
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Description] [nvarchar](2000) NULL,
    [Name] [nvarchar](1000) NOT NULL,
    [Extractable] [bit] NOT NULL DEFAULT ((0)),
    [SQL] [nvarchar](max) NULL,
    [IsGlobal] [bit] NOT NULL DEFAULT ((0)),
    [Ticket] [nvarchar](10) NULL,
    [ExternalDatabaseServer_ID] [int] NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_SupportingSQLTable] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.sysdiagrams
CREATE TABLE [dbo].[sysdiagrams] (
    [name] [sysname] NOT NULL,
    [principal_id] [int] NOT NULL,
    [diagram_id] [int] IDENTITY(1,1) NOT NULL,
    [version] [int] NULL,
    [definition] [varbinary](max) NULL,
    CONSTRAINT [PK__sysdiagr__C2B05B611CBC2A53] PRIMARY KEY CLUSTERED
    (
        [diagram_id] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.TableInfo
CREATE TABLE [dbo].[TableInfo] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [DatabaseType] [varchar](100) NOT NULL DEFAULT ('MicrosoftSQLServer'),
    [Server] [nvarchar](50) NULL,
    [Database] [nvarchar](500) NULL,
    [Name] [nvarchar](1000) NOT NULL,
    [State] [nvarchar](50) NULL,
    [ValidationXml] [nvarchar](max) NULL,
    [IsPrimaryExtractionTable] [bit] NOT NULL DEFAULT ((0)),
    [IsTableValuedFunction] [bit] NOT NULL DEFAULT ((0)),
    [IdentifierDumpServer_ID] [int] NULL,
    [Schema] [nvarchar](500) NULL,
    [IsView] [bit] NOT NULL DEFAULT ((0)),
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_Data_Tables] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.TicketingSystemConfiguration
CREATE TABLE [dbo].[TicketingSystemConfiguration] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](1000) NOT NULL,
    [IsActive] [bit] NOT NULL,
    [Url] [nvarchar](max) NULL,
    [Type] [nvarchar](500) NULL,
    [DataAccessCredentials_ID] [int] NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_TicketingSystemConfiguration] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.TicketingSystemReleaseStatus
CREATE TABLE [dbo].[TicketingSystemReleaseStatus] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Status] [nvarchar](250) NOT NULL,
    [TicketingSystemConfigurationID] [int] NOT NULL,
    CONSTRAINT [PK_TicketingSystemReleaseStatus] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.WindowLayout
CREATE TABLE [dbo].[WindowLayout] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](1000) NOT NULL,
    [LayoutData] [nvarchar](max) NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_Layout] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Foreign Keys

ALTER TABLE [dbo].[AggregateConfiguration]
    ADD CONSTRAINT [FK_AggregateConfiguration_AggregateDimension] FOREIGN KEY ([PivotOnDimensionID]) REFERENCES [dbo].[AggregateDimension] ([ID]);
GO

ALTER TABLE [dbo].[AggregateConfiguration]
    ADD CONSTRAINT [FK_AggregateConfiguration_AggregateFilterContainer] FOREIGN KEY ([RootFilterContainer_ID]) REFERENCES [dbo].[AggregateFilterContainer] ([ID]) ON DELETE SET NULL;
GO

ALTER TABLE [dbo].[AggregateConfiguration]
    ADD CONSTRAINT [FK_AggregateConfiguration_Catalogue] FOREIGN KEY ([Catalogue_ID]) REFERENCES [dbo].[Catalogue] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[AggregateConfiguration]
    ADD CONSTRAINT [FK_OverrideRootFilterContainerToUseParents] FOREIGN KEY ([OverrideFiltersByUsingParentAggregateConfigurationInstead_ID]) REFERENCES [dbo].[AggregateConfiguration] ([ID]);
GO

ALTER TABLE [dbo].[AggregateContinuousDateAxis]
    ADD CONSTRAINT [FK_AggregateContinuousDateAxis_AggregateDimension] FOREIGN KEY ([AggregateDimension_ID]) REFERENCES [dbo].[AggregateDimension] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[AggregateDimension]
    ADD CONSTRAINT [FK_AggregateDimension_AggregateConfiguration] FOREIGN KEY ([AggregateConfiguration_ID]) REFERENCES [dbo].[AggregateConfiguration] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[AggregateDimension]
    ADD CONSTRAINT [FK_AggregateDimension_ExtractionInformation] FOREIGN KEY ([ExtractionInformation_ID]) REFERENCES [dbo].[ExtractionInformation] ([ID]);
GO

ALTER TABLE [dbo].[AggregateFilter]
    ADD CONSTRAINT [FK_AggregateFilter_AggregateFilterContainer] FOREIGN KEY ([FilterContainer_ID]) REFERENCES [dbo].[AggregateFilterContainer] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[AggregateFilterParameter]
    ADD CONSTRAINT [FK_AggregateFilterParameter_AggregateFilter] FOREIGN KEY ([AggregateFilter_ID]) REFERENCES [dbo].[AggregateFilter] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[AggregateFilterSubContainer]
    ADD CONSTRAINT [FK_AggregateFilterSubContainer_AggregateFilterContainer] FOREIGN KEY ([AggregateFilterContainer_ParentID]) REFERENCES [dbo].[AggregateFilterContainer] ([ID]);
GO

ALTER TABLE [dbo].[AggregateFilterSubContainer]
    ADD CONSTRAINT [FK_AggregateFilterSubContainer_AggregateFilterContainer1] FOREIGN KEY ([AggregateFilterContainer_ChildID]) REFERENCES [dbo].[AggregateFilterContainer] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[AggregateForcedJoin]
    ADD CONSTRAINT [FK_AggregateForcedJoin_AggregateConfiguration] FOREIGN KEY ([AggregateConfiguration_ID]) REFERENCES [dbo].[AggregateConfiguration] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[AggregateForcedJoin]
    ADD CONSTRAINT [FK_AggregateForcedJoin_TableInfo] FOREIGN KEY ([TableInfo_ID]) REFERENCES [dbo].[TableInfo] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[AggregateTopX]
    ADD CONSTRAINT [FK_AggregateTopX_AggregateConfiguration] FOREIGN KEY ([AggregateConfiguration_ID]) REFERENCES [dbo].[AggregateConfiguration] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[AggregateTopX]
    ADD CONSTRAINT [FK_AggregateTopX_AggregateDimension] FOREIGN KEY ([OrderByDimensionIfAny_ID]) REFERENCES [dbo].[AggregateDimension] ([ID]);
GO

ALTER TABLE [dbo].[ANOTable]
    ADD CONSTRAINT [FK_ANOTable_ExternalDatabaseServer] FOREIGN KEY ([Server_ID]) REFERENCES [dbo].[ExternalDatabaseServer] ([ID]);
GO

ALTER TABLE [dbo].[CacheFetchFailure]
    ADD CONSTRAINT [FK_CacheFetchFailure_CacheProgress] FOREIGN KEY ([CacheProgress_ID]) REFERENCES [dbo].[CacheProgress] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[CacheProgress]
    ADD CONSTRAINT [FK_CacheProgress_LoadSchedule] FOREIGN KEY ([LoadProgress_ID]) REFERENCES [dbo].[LoadProgress] ([ID]);
GO

ALTER TABLE [dbo].[CacheProgress]
    ADD CONSTRAINT [FK_CacheProgress_PermissionWindow] FOREIGN KEY ([PermissionWindow_ID]) REFERENCES [dbo].[PermissionWindow] ([ID]);
GO

ALTER TABLE [dbo].[Catalogue]
    ADD CONSTRAINT [FK_Catalogue_ExternalDatabaseServer] FOREIGN KEY ([LiveLoggingServer_ID]) REFERENCES [dbo].[ExternalDatabaseServer] ([ID]);
GO

ALTER TABLE [dbo].[Catalogue]
    ADD CONSTRAINT [FK_PivotCategory_ExtractionInformation_ID] FOREIGN KEY ([PivotCategory_ExtractionInformation_ID]) REFERENCES [dbo].[ExtractionInformation] ([ID]);
GO

ALTER TABLE [dbo].[Catalogue]
    ADD CONSTRAINT [FK_TimeCoverageCategory_ExtractionInformation_ID] FOREIGN KEY ([TimeCoverage_ExtractionInformation_ID]) REFERENCES [dbo].[ExtractionInformation] ([ID]);
GO

ALTER TABLE [dbo].[CatalogueItem]
    ADD CONSTRAINT [FK_Catalogue_Items_Data_Catalogue] FOREIGN KEY ([Catalogue_ID]) REFERENCES [dbo].[Catalogue] ([ID]) ON DELETE CASCADE ON UPDATE CASCADE;
GO

ALTER TABLE [dbo].[CatalogueItem]
    ADD CONSTRAINT [FK_CatalogueItem_ColumnInfo] FOREIGN KEY ([ColumnInfo_ID]) REFERENCES [dbo].[ColumnInfo] ([ID]) ON DELETE SET NULL;
GO

ALTER TABLE [dbo].[CatalogueItemIssue]
    ADD CONSTRAINT [FK_CatalogueItemIssue_CatalogueItem] FOREIGN KEY ([CatalogueItem_ID]) REFERENCES [dbo].[CatalogueItem] ([ID]);
GO

ALTER TABLE [dbo].[CatalogueItemIssue]
    ADD CONSTRAINT [FK_CatalogueItemIssue_Owner_IssueSystemUser] FOREIGN KEY ([Owner_ID]) REFERENCES [dbo].[IssueSystemUser] ([ID]);
GO

ALTER TABLE [dbo].[CatalogueItemIssue]
    ADD CONSTRAINT [FK_CatalogueItemIssue_Reporter_IssueSystemUser] FOREIGN KEY ([ReportedBy_ID]) REFERENCES [dbo].[IssueSystemUser] ([ID]);
GO

ALTER TABLE [dbo].[CohortAggregateContainer_AggregateConfiguration]
    ADD CONSTRAINT [FK_CohortAggregateContainer_AggregateConfiguration_AggregateConfiguration] FOREIGN KEY ([AggregateConfiguration_ID]) REFERENCES [dbo].[AggregateConfiguration] ([ID]);
GO

ALTER TABLE [dbo].[CohortAggregateContainer_AggregateConfiguration]
    ADD CONSTRAINT [FK_CohortAggregateContainer_AggregateConfiguration_CohortAggregateContainer] FOREIGN KEY ([CohortAggregateContainer_ID]) REFERENCES [dbo].[CohortAggregateContainer] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[CohortAggregateSubContainer]
    ADD CONSTRAINT [FK_CohortAggregateSubContainer_CohortAggregateContainer_ChildID] FOREIGN KEY ([CohortAggregateContainer_ChildID]) REFERENCES [dbo].[CohortAggregateContainer] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[CohortAggregateSubContainer]
    ADD CONSTRAINT [FK_CohortAggregateSubContainer_CohortAggregateContainer_ParentID] FOREIGN KEY ([CohortAggregateContainer_ParentID]) REFERENCES [dbo].[CohortAggregateContainer] ([ID]);
GO

ALTER TABLE [dbo].[CohortIdentificationConfiguration]
    ADD CONSTRAINT [FK_CohortAggregateContainer_CohortAggregateContainer] FOREIGN KEY ([RootCohortAggregateContainer_ID]) REFERENCES [dbo].[CohortAggregateContainer] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[CohortIdentificationConfiguration]
    ADD CONSTRAINT [FK_CohortIdentificationConfiguration_ExternalDatabaseServer] FOREIGN KEY ([QueryCachingServer_ID]) REFERENCES [dbo].[ExternalDatabaseServer] ([ID]);
GO

ALTER TABLE [dbo].[ColumnInfo]
    ADD CONSTRAINT [FK_Column_Info_Dataset] FOREIGN KEY ([Dataset_ID]) REFERENCES [dbo].[Dataset] ([ID]) ON DELETE CASCADE ON UPDATE CASCADE;
GO

ALTER TABLE [dbo].[ColumnInfo]
    ADD CONSTRAINT [FK_ColumnInfo_ANOTable] FOREIGN KEY ([ANOTable_ID]) REFERENCES [dbo].[ANOTable] ([ID]);
GO

ALTER TABLE [dbo].[ColumnInfo]
    ADD CONSTRAINT [FK_Table_Items_Data_Tables] FOREIGN KEY ([TableInfo_ID]) REFERENCES [dbo].[TableInfo] ([ID]) ON DELETE CASCADE ON UPDATE CASCADE;
GO

ALTER TABLE [dbo].[DashboardControl]
    ADD CONSTRAINT [FK_DashboardControl_DashboardLayout] FOREIGN KEY ([DashboardLayout_ID]) REFERENCES [dbo].[DashboardLayout] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[DashboardObjectUse]
    ADD CONSTRAINT [FK_DashboardObjectUsage_DashboardControl] FOREIGN KEY ([DashboardControl_ID]) REFERENCES [dbo].[DashboardControl] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[DataAccessCredentials_TableInfo]
    ADD CONSTRAINT [FK_DataAccessCredentials_TableInfo_DataAccessCredentials] FOREIGN KEY ([DataAccessCredentials_ID]) REFERENCES [dbo].[DataAccessCredentials] ([ID]);
GO

ALTER TABLE [dbo].[DataAccessCredentials_TableInfo]
    ADD CONSTRAINT [FK_DataAccessCredentials_TableInfo_TableInfo] FOREIGN KEY ([TableInfo_ID]) REFERENCES [dbo].[TableInfo] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ExtractionFilter]
    ADD CONSTRAINT [FK_ExtractionFilter_ExtractionInformation] FOREIGN KEY ([ExtractionInformation_ID]) REFERENCES [dbo].[ExtractionInformation] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ExtractionFilterParameter]
    ADD CONSTRAINT [FK_ExtractionFilterParameter_ExtractionFilter] FOREIGN KEY ([ExtractionFilter_ID]) REFERENCES [dbo].[ExtractionFilter] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ExtractionFilterParameterSet]
    ADD CONSTRAINT [FK_ExtractionFilterParameterSet_ExtractionFilter] FOREIGN KEY ([ExtractionFilter_ID]) REFERENCES [dbo].[ExtractionFilter] ([ID]);
GO

ALTER TABLE [dbo].[ExtractionFilterParameterSetValue]
    ADD CONSTRAINT [FK_ExtractionFilterParameterSetValue_ExtractionFilterParameter] FOREIGN KEY ([ExtractionFilterParameter_ID]) REFERENCES [dbo].[ExtractionFilterParameter] ([ID]);
GO

ALTER TABLE [dbo].[ExtractionFilterParameterSetValue]
    ADD CONSTRAINT [FK_ExtractionFilterParameterSetValue_ExtractionFilterParameterSet] FOREIGN KEY ([ExtractionFilterParameterSet_ID]) REFERENCES [dbo].[ExtractionFilterParameterSet] ([ID]);
GO

ALTER TABLE [dbo].[ExtractionInformation]
    ADD CONSTRAINT [FK_ExtractionInformation_CatalogueItem] FOREIGN KEY ([CatalogueItem_ID]) REFERENCES [dbo].[CatalogueItem] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[GovernanceDocument]
    ADD CONSTRAINT [FK_GovernanceDocument_GovernancePeriod] FOREIGN KEY ([GovernancePeriod_ID]) REFERENCES [dbo].[GovernancePeriod] ([ID]);
GO

ALTER TABLE [dbo].[GovernancePeriod_Catalogue]
    ADD CONSTRAINT [FK_GovernancePeriod_Catalogue_Catalogue] FOREIGN KEY ([Catalogue_ID]) REFERENCES [dbo].[Catalogue] ([ID]);
GO

ALTER TABLE [dbo].[GovernancePeriod_Catalogue]
    ADD CONSTRAINT [FK_GovernancePeriod_Catalogue_GovernancePeriod] FOREIGN KEY ([GovernancePeriod_ID]) REFERENCES [dbo].[GovernancePeriod] ([ID]);
GO

ALTER TABLE [dbo].[JoinableCohortAggregateConfiguration]
    ADD CONSTRAINT [FK_JoinableCohortAggregateConfiguration_AggregateConfiguration] FOREIGN KEY ([AggregateConfiguration_ID]) REFERENCES [dbo].[AggregateConfiguration] ([ID]);
GO

ALTER TABLE [dbo].[JoinableCohortAggregateConfiguration]
    ADD CONSTRAINT [FK_JoinableCohortAggregateConfiguration_CohortIdentificationConfiguration] FOREIGN KEY ([CohortIdentificationConfiguration_ID]) REFERENCES [dbo].[CohortIdentificationConfiguration] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[JoinableCohortAggregateConfigurationUse]
    ADD CONSTRAINT [FK_JoinableCohortAggregateConfigurationUse_AggregateConfiguration] FOREIGN KEY ([AggregateConfiguration_ID]) REFERENCES [dbo].[AggregateConfiguration] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[JoinableCohortAggregateConfigurationUse]
    ADD CONSTRAINT [FK_JoinableCohortAggregateConfigurationUse_JoinableCohortAggregateConfiguration] FOREIGN KEY ([JoinableCohortAggregateConfiguration_ID]) REFERENCES [dbo].[JoinableCohortAggregateConfiguration] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[JoinInfo]
    ADD CONSTRAINT [FK_JoinInfo_ColumnInfo_JoinKey1] FOREIGN KEY ([ForeignKey_ID]) REFERENCES [dbo].[ColumnInfo] ([ID]);
GO

ALTER TABLE [dbo].[JoinInfo]
    ADD CONSTRAINT [FK_JoinInfo_ColumnInfo_JoinKey2] FOREIGN KEY ([PrimaryKey_ID]) REFERENCES [dbo].[ColumnInfo] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[LoadMetadata]
    ADD CONSTRAINT [fk_loadMetadataRootReference] FOREIGN KEY ([RootLoadMetadata_ID]) REFERENCES [dbo].[LoadMetadata] ([ID]);
GO

ALTER TABLE [dbo].[LoadMetadata]
    ADD CONSTRAINT [FK_OverrideRAWServer_ID] FOREIGN KEY ([OverrideRAWServer_ID]) REFERENCES [dbo].[ExternalDatabaseServer] ([ID]);
GO

ALTER TABLE [dbo].[LoadMetadataCatalogueLinkage]
    ADD CONSTRAINT [FK__LoadMetad__Catal__1DB06A4F] FOREIGN KEY ([CatalogueID]) REFERENCES [dbo].[Catalogue] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[LoadMetadataCatalogueLinkage]
    ADD CONSTRAINT [FK__LoadMetad__LoadM__1CBC4616] FOREIGN KEY ([LoadMetadataID]) REFERENCES [dbo].[LoadMetadata] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[LoadModuleAssembly]
    ADD CONSTRAINT [FK_LoadModuleAssembly_Plugin] FOREIGN KEY ([Plugin_ID]) REFERENCES [dbo].[Plugin] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[LoadProgress]
    ADD CONSTRAINT [FK_LoadSchedule_LoadMetadata] FOREIGN KEY ([LoadMetadata_ID]) REFERENCES [dbo].[LoadMetadata] ([ID]);
GO

ALTER TABLE [dbo].[Lookup]
    ADD CONSTRAINT [FK_Lookup_ColumnInfo] FOREIGN KEY ([Description_ID]) REFERENCES [dbo].[ColumnInfo] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[Lookup]
    ADD CONSTRAINT [FK_Lookup_ColumnInfo1] FOREIGN KEY ([ForeignKey_ID]) REFERENCES [dbo].[ColumnInfo] ([ID]);
GO

ALTER TABLE [dbo].[Lookup]
    ADD CONSTRAINT [FK_Lookup_ColumnInfo2] FOREIGN KEY ([PrimaryKey_ID]) REFERENCES [dbo].[ColumnInfo] ([ID]);
GO

ALTER TABLE [dbo].[LookupCompositeJoinInfo]
    ADD CONSTRAINT [FK_LookupCompositeJoinInfo_ColumnInfo] FOREIGN KEY ([PrimaryKey_ID]) REFERENCES [dbo].[ColumnInfo] ([ID]);
GO

ALTER TABLE [dbo].[LookupCompositeJoinInfo]
    ADD CONSTRAINT [FK_LookupCompositeJoinInfo_ColumnInfo_FK] FOREIGN KEY ([ForeignKey_ID]) REFERENCES [dbo].[ColumnInfo] ([ID]);
GO

ALTER TABLE [dbo].[LookupCompositeJoinInfo]
    ADD CONSTRAINT [FK_LookupCompositeJoinInfo_Lookup] FOREIGN KEY ([OriginalLookup_ID]) REFERENCES [dbo].[Lookup] ([ID]) ON DELETE CASCADE ON UPDATE CASCADE;
GO

ALTER TABLE [dbo].[Memento]
    ADD CONSTRAINT [FK_Memento_Commit] FOREIGN KEY ([Commit_ID]) REFERENCES [dbo].[Commit] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[Pipeline]
    ADD CONSTRAINT [FK_Pipeline_PipelineComponent] FOREIGN KEY ([DestinationPipelineComponent_ID]) REFERENCES [dbo].[PipelineComponent] ([ID]);
GO

ALTER TABLE [dbo].[Pipeline]
    ADD CONSTRAINT [FK_Pipeline_SourcePipelineComponent_ID_PipelineComponent] FOREIGN KEY ([SourcePipelineComponent_ID]) REFERENCES [dbo].[PipelineComponent] ([ID]);
GO

ALTER TABLE [dbo].[PipelineComponent]
    ADD CONSTRAINT [FK_PipelineComponent_Pipeline] FOREIGN KEY ([Pipeline_ID]) REFERENCES [dbo].[Pipeline] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[PipelineComponentArgument]
    ADD CONSTRAINT [FK_PipelineComponentArgument_PipelineComponent] FOREIGN KEY ([PipelineComponent_ID]) REFERENCES [dbo].[PipelineComponent] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[PreLoadDiscardedColumn]
    ADD CONSTRAINT [FK_PreLoadDiscardedColumn_TableInfo] FOREIGN KEY ([TableInfo_ID]) REFERENCES [dbo].[TableInfo] ([ID]);
GO

ALTER TABLE [dbo].[ProcessTask]
    ADD CONSTRAINT [FK_ProcessTask_Catalogue] FOREIGN KEY ([RelatesSolelyToCatalogue_ID]) REFERENCES [dbo].[Catalogue] ([ID]);
GO

ALTER TABLE [dbo].[ProcessTask]
    ADD CONSTRAINT [FK_ProcessTask_LoadMetadata] FOREIGN KEY ([LoadMetadata_ID]) REFERENCES [dbo].[LoadMetadata] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ProcessTaskArgument]
    ADD CONSTRAINT [FK_ProcessTaskArgument_ProcessTask] FOREIGN KEY ([ProcessTask_ID]) REFERENCES [dbo].[ProcessTask] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[RegexRedaction]
    ADD CONSTRAINT [FK_Redaction_ColumnInfo_ID] FOREIGN KEY ([ColumnInfo_ID]) REFERENCES [dbo].[ColumnInfo] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[RegexRedaction]
    ADD CONSTRAINT [FK_Redaction_RedactionConfiguration_ID] FOREIGN KEY ([RedactionConfiguration_ID]) REFERENCES [dbo].[RegexRedactionConfiguration] ([ID]);
GO

ALTER TABLE [dbo].[RegexRedactionKey]
    ADD CONSTRAINT [FK_RedactionKey_ColumnInfo_ID] FOREIGN KEY ([ColumnInfo_ID]) REFERENCES [dbo].[ColumnInfo] ([ID]);
GO

ALTER TABLE [dbo].[RegexRedactionKey]
    ADD CONSTRAINT [FK_RedactionKey_Redaction_ID] FOREIGN KEY ([RegexRedaction_ID]) REFERENCES [dbo].[RegexRedaction] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ServerDefaults]
    ADD CONSTRAINT [FK_ServerDefaults_ExternalDatabaseServer] FOREIGN KEY ([ExternalDatabaseServer_ID]) REFERENCES [dbo].[ExternalDatabaseServer] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[SupportingDocument]
    ADD CONSTRAINT [FK_SupportingDocument_Catalogue] FOREIGN KEY ([Catalogue_ID]) REFERENCES [dbo].[Catalogue] ([ID]);
GO

ALTER TABLE [dbo].[SupportingSQLTable]
    ADD CONSTRAINT [FK_SupportingSQLTable_Catalogue] FOREIGN KEY ([Catalogue_ID]) REFERENCES [dbo].[Catalogue] ([ID]);
GO

ALTER TABLE [dbo].[SupportingSQLTable]
    ADD CONSTRAINT [FK_SupportingSQLTable_ExternalDatabaseServer] FOREIGN KEY ([ExternalDatabaseServer_ID]) REFERENCES [dbo].[ExternalDatabaseServer] ([ID]);
GO

ALTER TABLE [dbo].[TableInfo]
    ADD CONSTRAINT [FK_TableInfo_ExternalDatabaseServer] FOREIGN KEY ([IdentifierDumpServer_ID]) REFERENCES [dbo].[ExternalDatabaseServer] ([ID]);
GO

ALTER TABLE [dbo].[TicketingSystemConfiguration]
    ADD CONSTRAINT [FK_TicketingSystemConfiguration_DataAccessCredentials] FOREIGN KEY ([DataAccessCredentials_ID]) REFERENCES [dbo].[DataAccessCredentials] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[TicketingSystemReleaseStatus]
    ADD CONSTRAINT [FK__Ticketing__Ticke__324172E1] FOREIGN KEY ([TicketingSystemConfigurationID]) REFERENCES [dbo].[TicketingSystemConfiguration] ([ID]) ON DELETE CASCADE;
GO

-- Indexes

CREATE UNIQUE NONCLUSTERED INDEX [ix_OneTopXPerAggregateConfiguration]
    ON [dbo].[AggregateTopX] ([AggregateConfiguration_ID] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [idx_ANOTableNamesMustBeUnique]
    ON [dbo].[ANOTable] ([TableName] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [ix_suffixMustBeUnique]
    ON [dbo].[ANOTable] ([Suffix] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [ix_PreventDuplicateParameterNamesOnSameEntity]
    ON [dbo].[AnyTableSqlParameter] ([ReferencedObjectType] ASC, [ReferencedObjectID] ASC, [ParameterSQL] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [ix_LoadProgressRelationshipIs0To1]
    ON [dbo].[CacheProgress] ([LoadProgress_ID] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [ix_DashboardControlObjectUseNoDuplicatesAllowed]
    ON [dbo].[DashboardObjectUse] ([DashboardControl_ID] ASC, [ReferencedObjectType] ASC, [ReferencedObjectID] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [ix_NamesMustBeUnique]
    ON [dbo].[DataAccessCredentials] ([Name] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [ix_preventMultipleExtractionInformationsPerCatalogueItem]
    ON [dbo].[ExtractionInformation] ([CatalogueItem_ID] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [idxGovernancePeriodNameMustBeUnique]
    ON [dbo].[GovernancePeriod] ([Name] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [ix_eachAggregateCanOnlyBeJoinableOnOneProject]
    ON [dbo].[JoinableCohortAggregateConfiguration] ([AggregateConfiguration_ID] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [ix_eachAggregateCanOnlyHaveOneJoinable]
    ON [dbo].[JoinableCohortAggregateConfigurationUse] ([AggregateConfiguration_ID] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [ix_JoinColumnsMustBeUnique]
    ON [dbo].[JoinInfo] ([ForeignKey_ID] ASC, [PrimaryKey_ID] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [ix_OneBinaryPerPlugin]
    ON [dbo].[LoadModuleAssembly] ([Plugin_ID] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [ix_YouCanExportEachObjectOnlyOnce]
    ON [dbo].[ObjectExport] ([ReferencedObjectType] ASC, [ReferencedObjectID] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [ix_YouCanImportEachObjectOnlyOnce]
    ON [dbo].[ObjectImport] ([SharingUID] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [ix_preventDuplicatePipelineNames]
    ON [dbo].[Pipeline] ([Name] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [ix_PluginNameAndVersionMustBeUnique]
    ON [dbo].[Plugin] ([Name] ASC, [PluginVersion] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_RemoteRDMP_NameMustBeUnique]
    ON [dbo].[RemoteRDMP] ([Name] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UNIQUE_SettingKey]
    ON [dbo].[Setting] ([Key] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [ix_ConceptNamesMustBeUnique]
    ON [dbo].[StandardRegex] ([ConceptName] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UK_principal_name]
    ON [dbo].[sysdiagrams] ([principal_id] ASC, [name] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [idx_CanOnlyBeOneActiveTicketingSystemConfiguration]
    ON [dbo].[TicketingSystemConfiguration] ([IsActive] ASC);
GO

-- Reference Data (for z_ tables)

