/****** Object:  Table [dbo].[WebdavAutomationAudit]    Script Date: 17/10/2017 12:12:08 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[WebdavAutomationAudit](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[FileHref] [varchar](256) NOT NULL,
	[FileResult] [smallint] NOT NULL,
	[Message] [varchar](256) NOT NULL,
	[Created] [datetime] NOT NULL,
	[Updated] [datetime] NOT NULL,
 CONSTRAINT [PK_WebdavAutomationAudit] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO


