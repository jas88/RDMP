-- Compiled schema for Logging
-- Generated from all migration scripts on 2025-11-04 08:34:42
-- This represents the final state after applying all migrations

-- Table: dbo.DataLoadRun
CREATE TABLE [dbo].[DataLoadRun] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [description] [nvarchar](max) NOT NULL,
    [startTime] [datetime] NOT NULL DEFAULT (getdate()),
    [endTime] [datetime] NULL,
    [dataLoadTaskID] [int] NOT NULL,
    [isTest] [bit] NOT NULL DEFAULT ((0)),
    [packageName] [nvarchar](750) NOT NULL,
    [userAccount] [nvarchar](500) NOT NULL,
    [suggestedRollbackCommand] [nvarchar](max) NULL,
    CONSTRAINT [PK_DataLoad] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.DataLoadTask
CREATE TABLE [dbo].[DataLoadTask] (
    [ID] [int] NOT NULL,
    [description] [nvarchar](max) NOT NULL,
    [name] [nvarchar](1000) NOT NULL,
    [createTime] [datetime] NOT NULL DEFAULT (getdate()),
    [userAccount] [nvarchar](500) NOT NULL,
    [statusID] [int] NOT NULL,
    [isTest] [bit] NOT NULL DEFAULT ((0)),
    [dataSetID] [nvarchar](450) NOT NULL,
    CONSTRAINT [PK_DataTask] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.DataSet
CREATE TABLE [dbo].[DataSet] (
    [dataSetID] [nvarchar](450) NOT NULL,
    [name] [nvarchar](2000) NULL,
    [description] [nvarchar](max) NULL,
    [time_period] [nvarchar](64) NULL,
    [SLA_required] [nvarchar](3) NULL,
    [supplier_name] [nvarchar](32) NULL,
    [supplier_tel_no] [nvarchar](32) NULL,
    [supplier_email] [nvarchar](64) NULL,
    [contact_name] [nvarchar](64) NULL,
    [contact_position] [nvarchar](64) NULL,
    [currentContactInstitutions] [nvarchar](64) NULL,
    [contact_tel_no] [nvarchar](32) NULL,
    [contact_email] [nvarchar](64) NULL,
    [frequency] [nvarchar](32) NULL,
    [method] [nvarchar](16) NULL,
    CONSTRAINT [PK_DataSet] PRIMARY KEY CLUSTERED
    (
        [dataSetID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.DataSource
CREATE TABLE [dbo].[DataSource] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [source] [nvarchar](max) NOT NULL,
    [tableLoadRunID] [int] NULL,
    [archive] [nvarchar](max) NULL,
    [originDate] [date] NULL,
    [MD5] [binary](128) NULL,
    CONSTRAINT [PK_DataSource] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.FatalError
CREATE TABLE [dbo].[FatalError] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [time] [datetime] NOT NULL DEFAULT (getdate()),
    [source] [nvarchar](max) NULL,
    [description] [nvarchar](max) NOT NULL,
    [explanation] [nvarchar](max) NULL,
    [dataLoadRunID] [int] NULL,
    [statusID] [int] NULL,
    [interestingToOthers] [bit] NULL,
    CONSTRAINT [PK_FatalError] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.ProgressLog
CREATE TABLE [dbo].[ProgressLog] (
    [dataLoadRunID] [int] NOT NULL,
    [eventType] [nvarchar](50) NULL,
    [description] [nvarchar](max) NULL,
    [source] [nvarchar](max) NULL,
    [time] [datetime] NULL,
    [ID] [int] IDENTITY(1,1) NOT NULL,
    CONSTRAINT [PK_ProgressLog] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.RowError
CREATE TABLE [dbo].[RowError] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [tableLoadRunID] [int] NOT NULL,
    [rowErrorTypeID] [int] NULL,
    [description] [nvarchar](max) NOT NULL,
    [locationOfRow] [nvarchar](max) NOT NULL,
    [requiresReloading] [bit] NOT NULL DEFAULT ((0)),
    [columnName] [nvarchar](max) NULL,
    CONSTRAINT [PK_RowErrors] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.TableLoadRun
CREATE TABLE [dbo].[TableLoadRun] (
    [startTime] [datetime] NOT NULL DEFAULT (getdate()),
    [endTime] [datetime] NULL,
    [dataLoadRunID] [int] NOT NULL,
    [targetTable] [varchar](200) NOT NULL,
    [expectedInserts] [bigint] NULL,
    [inserts] [bigint] NULL,
    [updates] [bigint] NULL,
    [deletes] [bigint] NULL,
    [errorRows] [bigint] NULL,
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [duplicates] [bigint] NULL,
    [notes] [varchar](8000) NULL,
    [suggestedRollbackCommand] [varchar](max) NULL,
    CONSTRAINT [PK_TableLoadRun] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.z_DataLoadTaskStatus
CREATE TABLE [dbo].[z_DataLoadTaskStatus] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [status] [varchar](50) NULL,
    [description] [varchar](max) NULL,
    CONSTRAINT [PK_z_DataLoadTaskStatus] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.z_FatalErrorStatus
CREATE TABLE [dbo].[z_FatalErrorStatus] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [status] [varchar](20) NOT NULL,
    CONSTRAINT [PK_z_FatalErrorsStatus] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Table: dbo.z_RowErrorType
CREATE TABLE [dbo].[z_RowErrorType] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [type] [varchar](20) NOT NULL,
    CONSTRAINT [PK_z_RowErrorsType] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    )
) ON [PRIMARY];

GO

-- Foreign Keys

ALTER TABLE [dbo].[DataLoadRun]
    ADD CONSTRAINT [FK_DataLoadRun_DataLoadTask] FOREIGN KEY ([dataLoadTaskID]) REFERENCES [dbo].[DataLoadTask] ([ID]) ON UPDATE CASCADE;
GO

ALTER TABLE [dbo].[DataLoadTask]
    ADD CONSTRAINT [FK_DataLoadTask_DataSet] FOREIGN KEY ([dataSetID]) REFERENCES [dbo].[DataSet] ([dataSetID]);
GO

ALTER TABLE [dbo].[DataLoadTask]
    ADD CONSTRAINT [FK_DataLoadTask_z_DataLoadTaskStatus] FOREIGN KEY ([statusID]) REFERENCES [dbo].[z_DataLoadTaskStatus] ([ID]);
GO

ALTER TABLE [dbo].[DataSource]
    ADD CONSTRAINT [FK_DataSource_TableLoadRun] FOREIGN KEY ([tableLoadRunID]) REFERENCES [dbo].[TableLoadRun] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[FatalError]
    ADD CONSTRAINT [FK_FatalError_DataLoadRun] FOREIGN KEY ([dataLoadRunID]) REFERENCES [dbo].[DataLoadRun] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[FatalError]
    ADD CONSTRAINT [FK_FatalErrors_z_FatalErrorsStatus] FOREIGN KEY ([statusID]) REFERENCES [dbo].[z_FatalErrorStatus] ([ID]);
GO

ALTER TABLE [dbo].[ProgressLog]
    ADD CONSTRAINT [FK_ProgressLog_DataLoadRun] FOREIGN KEY ([dataLoadRunID]) REFERENCES [dbo].[DataLoadRun] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[RowError]
    ADD CONSTRAINT [FK_RowErrors_TableLoadRun] FOREIGN KEY ([tableLoadRunID]) REFERENCES [dbo].[TableLoadRun] ([ID]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[RowError]
    ADD CONSTRAINT [FK_RowErrors_z_RowErrorsType] FOREIGN KEY ([rowErrorTypeID]) REFERENCES [dbo].[z_RowErrorType] ([ID]);
GO

ALTER TABLE [dbo].[TableLoadRun]
    ADD CONSTRAINT [FK_TableLoadRun_DataLoadRun] FOREIGN KEY ([dataLoadRunID]) REFERENCES [dbo].[DataLoadRun] ([ID]) ON DELETE CASCADE;
GO

-- Indexes

CREATE NONCLUSTERED INDEX [ixFatalError_DataLoadRun]
    ON [dbo].[FatalError] ([dataLoadRunID] ASC);
GO

CREATE NONCLUSTERED INDEX [ixProgressLog_DataLoadRun]
    ON [dbo].[ProgressLog] ([dataLoadRunID] ASC);
GO

CREATE NONCLUSTERED INDEX [ixTableLoadRun_DataLoadRun]
    ON [dbo].[TableLoadRun] ([dataLoadRunID] ASC);
GO

-- Reference Data (for z_ tables)

INSERT INTO [dbo].[z_DataLoadTaskStatus] ([ID], [status], [description])
    VALUES (1, N'Open', NULL);
INSERT INTO [dbo].[z_DataLoadTaskStatus] ([ID], [status], [description])
    VALUES (2, N'Ready', NULL);
INSERT INTO [dbo].[z_DataLoadTaskStatus] ([ID], [status], [description])
    VALUES (3, N'Committed', NULL);
GO

INSERT INTO [dbo].[z_FatalErrorStatus] ([ID], [status])
    VALUES (1, N'Outstanding');
INSERT INTO [dbo].[z_FatalErrorStatus] ([ID], [status])
    VALUES (2, N'Resolved');
INSERT INTO [dbo].[z_FatalErrorStatus] ([ID], [status])
    VALUES (3, N'Blocked');
GO

INSERT INTO [dbo].[z_RowErrorType] ([ID], [type])
    VALUES (1, N'LoadRow');
INSERT INTO [dbo].[z_RowErrorType] ([ID], [type])
    VALUES (2, N'Duplication');
INSERT INTO [dbo].[z_RowErrorType] ([ID], [type])
    VALUES (3, N'Validation');
INSERT INTO [dbo].[z_RowErrorType] ([ID], [type])
    VALUES (4, N'DatabaseOperation');
INSERT INTO [dbo].[z_RowErrorType] ([ID], [type])
    VALUES (5, N'Unknown');
GO

