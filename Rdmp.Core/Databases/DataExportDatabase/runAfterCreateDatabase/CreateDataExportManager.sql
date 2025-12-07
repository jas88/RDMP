-- Compiled schema for DataExport
-- Generated from all migration scripts on 2025-11-04 08:34:42
-- This represents the final state after applying all migrations

-- Table: dbo.ConfigurationProperties
CREATE TABLE [dbo].[ConfigurationProperties] (
    [Property] [varchar](500) NOT NULL,
    [Value] [nvarchar](max) NULL,
    [Description] [nvarchar](max) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ConfigurationProperties] PRIMARY KEY CLUSTERED
    (
        [Property] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.CumulativeExtractionResults
CREATE TABLE [dbo].[CumulativeExtractionResults] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [ExtractionConfiguration_ID] [int] NOT NULL,
    [ExtractableDataSet_ID] [int] NOT NULL,
    [DateOfExtraction] [datetime] NOT NULL DEFAULT (getdate()),
    [DestinationDescription] [nvarchar](max) NULL,
    [RecordsExtracted] [int] NOT NULL DEFAULT (0),
    [DistinctReleaseIdentifiersEncountered] [int] NOT NULL DEFAULT (0),
    [FiltersUsed] [nvarchar](max) NULL,
    [Exception] [nvarchar](max) NULL,
    [SQLExecuted] [nvarchar](max) NULL,
    [CohortExtracted] [int] NULL,
    [DestinationType] [nvarchar](500) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_CumulativeExtractionResults] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.DataUser
CREATE TABLE [dbo].[DataUser] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Forename] [nvarchar](50) NOT NULL,
    [Surname] [nvarchar](50) NOT NULL,
    [Email] [nvarchar](100) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_DataUser] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.DeployedExtractionFilter
CREATE TABLE [dbo].[DeployedExtractionFilter] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [WhereSQL] [nvarchar](max) NULL,
    [Description] [nvarchar](max) NULL,
    [Name] [nvarchar](100) NOT NULL,
    [FilterContainer_ID] [int] NULL,
    [IsMandatory] [bit] NOT NULL DEFAULT ((0)),
    [Order] [int] NOT NULL DEFAULT ((0)),
    [ClonedFromExtractionFilter_ID] [int] NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ExtractionFilter] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.DeployedExtractionFilterParameter
CREATE TABLE [dbo].[DeployedExtractionFilterParameter] (
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

-- Table: dbo.ExternalCohortTable
CREATE TABLE [dbo].[ExternalCohortTable] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](1000) NOT NULL,
    [Server] [nvarchar](50) NULL,
    [Database] [nvarchar](250) NULL,
    [TableName] [nvarchar](500) NULL,
    [DefinitionTableName] [nvarchar](500) NULL,
    [PrivateIdentifierField] [nvarchar](1000) NULL,
    [ReleaseIdentifierField] [nvarchar](1000) NULL,
    [DefinitionTableForeignKeyField] [nvarchar](1000) NULL,
    [Username] [nvarchar](500) NULL,
    [Password] [nvarchar](max) NULL,
    [DatabaseType] [nvarchar](100) NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ExternalCohortTable] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ExtractableCohort
CREATE TABLE [dbo].[ExtractableCohort] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [OverrideReleaseIdentifierSQL] [nvarchar](500) NULL,
    [ExternalCohortTable_ID] [int] NOT NULL,
    [OriginID] [int] NOT NULL,
    [AuditLog] [nvarchar](max) NULL,
    [IsDeprecated] [bit] NOT NULL DEFAULT ((0)),
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_Cohort] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ExtractableColumn
CREATE TABLE [dbo].[ExtractableColumn] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [ExtractableDataSet_ID] [int] NOT NULL,
    [SelectSQL] [nvarchar](max) NULL,
    [Order] [int] NULL,
    [Alias] [nvarchar](100) NULL,
    [ExtractionConfiguration_ID] [int] NOT NULL,
    [CatalogueExtractionInformation_ID] [int] NULL,
    [HashOnDataRelease] [bit] NOT NULL DEFAULT ((0)),
    [IsExtractionIdentifier] [bit] NOT NULL DEFAULT ((0)),
    [IsPrimaryKey] [bit] NOT NULL DEFAULT ((0)),
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ExtractableColumn] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ExtractableDataSet
CREATE TABLE [dbo].[ExtractableDataSet] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Catalogue_ID] [int] NOT NULL,
    [DisableExtraction] [bit] NOT NULL DEFAULT ((0)),
    [Project_ID] [int] NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ExtractableDataSet] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ExtractableDataSetPackage
CREATE TABLE [dbo].[ExtractableDataSetPackage] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](1000) NOT NULL,
    [Creator] [nvarchar](500) NOT NULL,
    [CreationDate] [datetime] NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ExtractableDataSetPackage] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ExtractableDataSetPackage_ExtractableDataSet
CREATE TABLE [dbo].[ExtractableDataSetPackage_ExtractableDataSet] (
    [ExtractableDataSet_ID] [int] NOT NULL,
    [ExtractableDataSetPackage_ID] [int] NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ExtractableDataSetPackage_ExtractableDataSet] PRIMARY KEY CLUSTERED
    (
        [ExtractableDataSet_ID] ASC,
        [ExtractableDataSetPackage_ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ExtractableDataSetProject
CREATE TABLE [dbo].[ExtractableDataSetProject] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Project_ID] [int] NOT NULL,
    [ExtractableDataSet_ID] [int] NOT NULL,
    CONSTRAINT [PK_ExtractableDataSetProject] PRIMARY KEY CLUSTERED
    (
        [Project_ID] ASC,
        [ExtractableDataSet_ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ExtractionConfiguration
CREATE TABLE [dbo].[ExtractionConfiguration] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [dtCreated] [datetime] NULL,
    [Project_ID] [int] NULL,
    [Username] [nvarchar](50) NULL,
    [Cohort_ID] [int] NULL,
    [RequestTicket] [nvarchar](10) NULL,
    [ReleaseTicket] [nvarchar](10) NULL,
    [Separator] [nvarchar](3) NOT NULL DEFAULT (','),
    [Description] [nvarchar](max) NULL,
    [IsReleased] [bit] NOT NULL DEFAULT ((0)),
    [ClonedFrom_ID] [int] NULL,
    [Name] [nvarchar](1000) NOT NULL,
    [DefaultPipeline_ID] [int] NULL,
    [CohortIdentificationConfiguration_ID] [int] NULL,
    [CohortRefreshPipeline_ID] [int] NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ExtractionConfiguration] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ExtractionProgress
CREATE TABLE [dbo].[ExtractionProgress] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [varchar](max) NOT NULL,
    [SelectedDataSets_ID] [int] NOT NULL,
    [ProgressDate] [datetime] NULL,
    [ExtractionInformation_ID] [int] NOT NULL,
    [StartDate] [datetime] NULL,
    [EndDate] [datetime] NULL,
    [NumberOfDaysPerBatch] [int] NOT NULL,
    [Retry] [varchar](100) NOT NULL,
    CONSTRAINT [PK_ExtractionProgress] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.FilterContainer
CREATE TABLE [dbo].[FilterContainer] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Operation] [nvarchar](10) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_FilterContainer] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.FilterContainerSubcontainers
CREATE TABLE [dbo].[FilterContainerSubcontainers] (
    [FilterContainer_ParentID] [int] NOT NULL,
    [FilterContainerChildID] [int] NOT NULL,
    [RowVer] [timestamp] NOT NULL
) ON [PRIMARY];

GO

-- Table: dbo.GlobalExtractionFilterParameter
CREATE TABLE [dbo].[GlobalExtractionFilterParameter] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [ExtractionConfiguration_ID] [int] NULL,
    [Value] [nvarchar](max) NULL,
    [ParameterSQL] [nvarchar](500) NULL,
    [Comment] [nvarchar](500) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_GlobalExtractionFilterParameter] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.Project
CREATE TABLE [dbo].[Project] (
    [Name] [nvarchar](1000) NOT NULL,
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [MasterTicket] [nvarchar](10) NULL,
    [ExtractionDirectory] [nvarchar](300) NULL,
    [ProjectNumber] [int] NULL,
    [RowVer] [timestamp] NOT NULL,
    [Folder] [nvarchar](max) NULL,
    CONSTRAINT [PK_Project] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.Project_DataUser
CREATE TABLE [dbo].[Project_DataUser] (
    [Project_ID] [int] NOT NULL,
    [DataUser_ID] [int] NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_Project_DataUser] PRIMARY KEY CLUSTERED
    (
        [Project_ID] ASC,
        [DataUser_ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ProjectCohortIdentificationConfigurationAssociation
CREATE TABLE [dbo].[ProjectCohortIdentificationConfigurationAssociation] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Project_ID] [int] NOT NULL,
    [CohortIdentificationConfiguration_ID] [int] NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ProjectCohortIdentificationConfigurationAssociation] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ReleaseLog
CREATE TABLE [dbo].[ReleaseLog] (
    [CumulativeExtractionResults_ID] [int] NOT NULL,
    [Username] [nvarchar](50) NOT NULL,
    [DateOfRelease] [datetime] NOT NULL DEFAULT (getdate()),
    [MD5OfDatasetFile] [nvarchar](256) NOT NULL,
    [DatasetState] [nvarchar](100) NOT NULL,
    [EnvironmentState] [nvarchar](500) NOT NULL,
    [IsPatch] [bit] NOT NULL DEFAULT ((0)),
    [ReleaseFolder] [nvarchar](max) NOT NULL,
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_ReleaseLog] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.SelectedDataSets
CREATE TABLE [dbo].[SelectedDataSets] (
    [ExtractionConfiguration_ID] [int] NOT NULL,
    [ExtractableDataSet_ID] [int] NOT NULL,
    [RootFilterContainer_ID] [int] NULL,
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_SelectedDataSets] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.SelectedDataSetsForcedJoin
CREATE TABLE [dbo].[SelectedDataSetsForcedJoin] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [SelectedDataSets_ID] [int] NOT NULL,
    [TableInfo_ID] [int] NOT NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_SelectedDataSetsForcedJoin] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.SupplementalExtractionResults
CREATE TABLE [dbo].[SupplementalExtractionResults] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [CumulativeExtractionResults_ID] [int] NULL,
    [ExtractionConfiguration_ID] [int] NULL,
    [DestinationDescription] [nvarchar](max) NULL,
    [RecordsExtracted] [int] NULL,
    [DateOfExtraction] [datetime] NOT NULL DEFAULT (getdate()),
    [Exception] [nvarchar](max) NULL,
    [SQLExecuted] [nvarchar](max) NULL,
    [ExtractedName] [nvarchar](max) NULL,
    [ReferencedObjectType] [nvarchar](max) NULL,
    [ReferencedObjectID] [int] NULL,
    [ReferencedObjectRepositoryType] [nvarchar](max) NULL,
    [DestinationType] [nvarchar](500) NULL,
    [RowVer] [timestamp] NOT NULL,
    CONSTRAINT [PK_SupplementalExtractionResults] PRIMARY KEY CLUSTERED
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
    CONSTRAINT [PK__sysdiagr__C2B05B6168654B81] PRIMARY KEY CLUSTERED
    (
        [diagram_id] ASC
    )
) ON [PRIMARY];

GO

-- Foreign Keys

ALTER TABLE [dbo].[CumulativeExtractionResults]
    ADD CONSTRAINT [FK_CumulativeExtractionResults_ExtractableDataSet] FOREIGN KEY ([ExtractableDataSet_ID]) REFERENCES [dbo].[ExtractableDataSet] ([ID]);
GO

ALTER TABLE [dbo].[CumulativeExtractionResults]
    ADD CONSTRAINT [FK_CumulativeExtractionResults_ExtractionConfiguration] FOREIGN KEY ([ExtractionConfiguration_ID]) REFERENCES [dbo].[ExtractionConfiguration] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[DeployedExtractionFilter]
    ADD CONSTRAINT [FK_ExtractionFilter_FilterContainer] FOREIGN KEY ([FilterContainer_ID]) REFERENCES [dbo].[FilterContainer] ([ID]) ON DELETE SET NULL;
GO

ALTER TABLE [dbo].[DeployedExtractionFilterParameter]
    ADD CONSTRAINT [FK_ExtractionFilterParameter_ExtractionFilter] FOREIGN KEY ([ExtractionFilter_ID]) REFERENCES [dbo].[DeployedExtractionFilter] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ExtractableCohort]
    ADD CONSTRAINT [FK_ExtractableCohort_ExternalCohortTable] FOREIGN KEY ([ExternalCohortTable_ID]) REFERENCES [dbo].[ExternalCohortTable] ([ID]);
GO

ALTER TABLE [dbo].[ExtractableColumn]
    ADD CONSTRAINT [FK_ExtractableColumn_ExtractableDataSet] FOREIGN KEY ([ExtractableDataSet_ID]) REFERENCES [dbo].[ExtractableDataSet] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ExtractableColumn]
    ADD CONSTRAINT [FK_ExtractableColumn_ExtractionConfiguration] FOREIGN KEY ([ExtractionConfiguration_ID]) REFERENCES [dbo].[ExtractionConfiguration] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ExtractableDataSetPackage_ExtractableDataSet]
    ADD CONSTRAINT [FK_ExtractableDataSetPackage_ExtractableDataSet_ExtractableDataSet] FOREIGN KEY ([ExtractableDataSet_ID]) REFERENCES [dbo].[ExtractableDataSet] ([ID]);
GO

ALTER TABLE [dbo].[ExtractableDataSetPackage_ExtractableDataSet]
    ADD CONSTRAINT [FK_ExtractableDataSetPackage_ExtractableDataSet_ExtractableDataSetPackage] FOREIGN KEY ([ExtractableDataSetPackage_ID]) REFERENCES [dbo].[ExtractableDataSetPackage] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ExtractableDataSetProject]
    ADD CONSTRAINT [fk_ExtractableDataSetProject] FOREIGN KEY ([ExtractableDataSet_ID]) REFERENCES [dbo].[ExtractableDataSet] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ExtractionConfiguration]
    ADD CONSTRAINT [FK_ExtractionConfiguration_Cohort] FOREIGN KEY ([Cohort_ID]) REFERENCES [dbo].[ExtractableCohort] ([ID]);
GO

ALTER TABLE [dbo].[ExtractionConfiguration]
    ADD CONSTRAINT [FK_ExtractionConfiguration_Project] FOREIGN KEY ([Project_ID]) REFERENCES [dbo].[Project] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ExtractionProgress]
    ADD CONSTRAINT [FK_ExtractionProgress_SelectedDataSets] FOREIGN KEY ([SelectedDataSets_ID]) REFERENCES [dbo].[SelectedDataSets] ([ID]) ON DELETE CASCADE ON UPDATE CASCADE;
GO

ALTER TABLE [dbo].[FilterContainerSubcontainers]
    ADD CONSTRAINT [FK_FilterContainerSubcontainers_FilterContainer] FOREIGN KEY ([FilterContainer_ParentID]) REFERENCES [dbo].[FilterContainer] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[GlobalExtractionFilterParameter]
    ADD CONSTRAINT [FK_GlobalExtractionFilterParameter_ExtractionConfiguration] FOREIGN KEY ([ExtractionConfiguration_ID]) REFERENCES [dbo].[ExtractionConfiguration] ([ID]);
GO

ALTER TABLE [dbo].[Project_DataUser]
    ADD CONSTRAINT [FK_Project_DataUser_DataUser] FOREIGN KEY ([DataUser_ID]) REFERENCES [dbo].[DataUser] ([ID]);
GO

ALTER TABLE [dbo].[Project_DataUser]
    ADD CONSTRAINT [FK_Project_DataUser_Project] FOREIGN KEY ([Project_ID]) REFERENCES [dbo].[Project] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ProjectCohortIdentificationConfigurationAssociation]
    ADD CONSTRAINT [FK_ProjectCohortIdentificationConfigurationAssociation_Project] FOREIGN KEY ([Project_ID]) REFERENCES [dbo].[Project] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ReleaseLog]
    ADD CONSTRAINT [FK_ReleaseLog_CumulativeExtractionResults] FOREIGN KEY ([CumulativeExtractionResults_ID]) REFERENCES [dbo].[CumulativeExtractionResults] ([ID]);
GO

ALTER TABLE [dbo].[SelectedDataSets]
    ADD CONSTRAINT [FK_SelectedDataSets_ExtractableDataSet] FOREIGN KEY ([ExtractableDataSet_ID]) REFERENCES [dbo].[ExtractableDataSet] ([ID]);
GO

ALTER TABLE [dbo].[SelectedDataSets]
    ADD CONSTRAINT [FK_SelectedDataSets_ExtractionConfiguration] FOREIGN KEY ([ExtractionConfiguration_ID]) REFERENCES [dbo].[ExtractionConfiguration] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[SelectedDataSets]
    ADD CONSTRAINT [FK_SelectedDataSets_FilterContainer] FOREIGN KEY ([RootFilterContainer_ID]) REFERENCES [dbo].[FilterContainer] ([ID]) ON DELETE SET NULL;
GO

ALTER TABLE [dbo].[SelectedDataSetsForcedJoin]
    ADD CONSTRAINT [FK_SelectedDataSetsForcedJoin_SelectedDataSets] FOREIGN KEY ([SelectedDataSets_ID]) REFERENCES [dbo].[SelectedDataSets] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[SupplementalExtractionResults]
    ADD CONSTRAINT [FK_SupplementalExtractionResults_CumulativeExtractionResults] FOREIGN KEY ([CumulativeExtractionResults_ID]) REFERENCES [dbo].[CumulativeExtractionResults] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[SupplementalExtractionResults]
    ADD CONSTRAINT [FK_SupplementalExtractionResults_ExtractionConfiguration] FOREIGN KEY ([ExtractionConfiguration_ID]) REFERENCES [dbo].[ExtractionConfiguration] ([ID]);
GO

-- Indexes

CREATE UNIQUE NONCLUSTERED INDEX [PreventDoubleAddingCatalogueIdx]
    ON [dbo].[ExtractableDataSet] ([Catalogue_ID] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [ix_OneExtractionProgressPerDataset]
    ON [dbo].[ExtractionProgress] ([SelectedDataSets_ID] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [ix_NoCicProjectDuplicates]
    ON [dbo].[ProjectCohortIdentificationConfigurationAssociation] ([Project_ID] ASC, [CohortIdentificationConfiguration_ID] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [ix_CumulativeExtractionResultsMustBeUnique]
    ON [dbo].[ReleaseLog] ([CumulativeExtractionResults_ID] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [ix_SelectedDataSetsForceJoinsMustBeUnique]
    ON [dbo].[SelectedDataSetsForcedJoin] ([SelectedDataSets_ID] ASC, [TableInfo_ID] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UK_principal_name]
    ON [dbo].[sysdiagrams] ([principal_id] ASC, [name] ASC);
GO

-- Reference Data (for z_ tables)

