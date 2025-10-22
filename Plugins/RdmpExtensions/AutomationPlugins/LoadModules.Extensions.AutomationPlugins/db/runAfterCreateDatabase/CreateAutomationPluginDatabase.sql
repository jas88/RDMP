/****** Object:  Table [dbo].[AutomateExtraction]    Script Date: 05/07/2017 08:37:46 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AutomateExtraction](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ExtractionConfiguration_ID] [int] NOT NULL,
	[AutomateExtractionSchedule_ID] [int] NOT NULL,
	[Disabled] [bit] NOT NULL,
	[BaselineDate] [datetime] NULL,
 CONSTRAINT [PK_AutomateExtraction] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[AutomateExtractionSchedule]    Script Date: 05/07/2017 08:37:46 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AutomateExtractionSchedule](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ExecutionTimescale] [varchar](50) NOT NULL,
	[UserRequestingRefresh] [varchar](500) NULL,
	[UserRequestingRefreshDate] [datetime] NULL,
	[Ticket] [varchar](500) NULL,
	[Name] [varchar](500) NOT NULL,
	[Comment] [varchar](max) NULL,
	[Disabled] [bit] NOT NULL,
	[Project_ID] [int] NOT NULL,
	[Pipeline_ID] [int] NULL,
 CONSTRAINT [PK_ExecutionSchedule] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[ReleaseIdentifiersSeen]    Script Date: 05/07/2017 08:37:46 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ReleaseIdentifiersSeen](
	[AutomateExtraction_ID] [int] NOT NULL,
	[ReleaseID] [varchar](500) NOT NULL,
 CONSTRAINT [PK_ReleaseIdentifiersSeen] PRIMARY KEY CLUSTERED 
(
	[AutomateExtraction_ID] ASC,
	[ReleaseID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[SuccessfullyExtractedResults]    Script Date: 05/07/2017 08:37:46 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SuccessfullyExtractedResults](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[SQL] [varchar](max) NOT NULL,
	[ExtractableDataSet_ID] [int] NOT NULL,
	[AutomateExtraction_ID] [int] NOT NULL,
 CONSTRAINT [PK_SuccessfullyExtractedResults] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
ALTER TABLE [dbo].[AutomateExtraction] ADD  CONSTRAINT [DF_AutomateExtraction_Disabled]  DEFAULT ((0)) FOR [Disabled]
GO
ALTER TABLE [dbo].[AutomateExtractionSchedule] ADD  CONSTRAINT [DF_ExecutionSchedule_Disabled]  DEFAULT ((0)) FOR [Disabled]
GO
ALTER TABLE [dbo].[AutomateExtraction]  WITH CHECK ADD  CONSTRAINT [FK_AutomateExtraction_AutomateExtractionSchedule] FOREIGN KEY([AutomateExtractionSchedule_ID])
REFERENCES [dbo].[AutomateExtractionSchedule] ([ID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AutomateExtraction] CHECK CONSTRAINT [FK_AutomateExtraction_AutomateExtractionSchedule]
GO
ALTER TABLE [dbo].[ReleaseIdentifiersSeen]  WITH CHECK ADD  CONSTRAINT [FK_ReleaseIdentifiersSeen_AutomateExtraction] FOREIGN KEY([AutomateExtraction_ID])
REFERENCES [dbo].[AutomateExtraction] ([ID])
GO
ALTER TABLE [dbo].[ReleaseIdentifiersSeen] CHECK CONSTRAINT [FK_ReleaseIdentifiersSeen_AutomateExtraction]
GO
ALTER TABLE [dbo].[SuccessfullyExtractedResults]  WITH CHECK ADD  CONSTRAINT [FK_SuccessfullyExtractedResults_AutomateExtraction] FOREIGN KEY([AutomateExtraction_ID])
REFERENCES [dbo].[AutomateExtraction] ([ID])
GO
ALTER TABLE [dbo].[SuccessfullyExtractedResults] CHECK CONSTRAINT [FK_SuccessfullyExtractedResults_AutomateExtraction]
GO
CREATE UNIQUE NONCLUSTERED INDEX [ix_oneResultPerConfigDataset] ON [dbo].[SuccessfullyExtractedResults]
(
	[ExtractableDataSet_ID] ASC,
	[AutomateExtraction_ID] ASC
)
GO

CREATE TABLE [dbo].[QueuedExtraction](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ExtractionConfiguration_ID] [int] NOT NULL,
	[Pipeline_ID] [int] NOT NULL,
	[DueDate] [datetime] NOT NULL,
	[Requester] [varchar](500) NOT NULL,
	[RequestDate] [datetime] NOT NULL,
 CONSTRAINT [PK_QueuedExtraction] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[QueuedExtraction] ADD  CONSTRAINT [DF_QueuedExtraction_RequestDate]  DEFAULT (getdate()) FOR [RequestDate]
GO

CREATE UNIQUE NONCLUSTERED INDEX [ix_OneSchedulePerProjectOnly] ON [dbo].[AutomateExtractionSchedule]
(
	[Project_ID] ASC
)

CREATE UNIQUE NONCLUSTERED INDEX [ix_OneSchedulePerExtractionConfigurationOnly] ON [dbo].[AutomateExtraction]
(
	[ExtractionConfiguration_ID] ASC
)