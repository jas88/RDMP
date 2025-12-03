--Version:1.9.0.0
--Description:Adds PermissionWindow table
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PermissionWindow')
BEGIN

CREATE TABLE [dbo].[PermissionWindow](
	[ID] [int] NOT NULL,
	[PermissionPeriodConfig] [varchar](max) NOT NULL,
	[RequiresSynchronousAccess] [bit] NOT NULL,
 CONSTRAINT [PK_PermissionWindow] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]


END

-- Add default for 'RequiresSynchronousAccess' (true) - check for ANY default on this column
IF NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.PermissionWindow')
    AND c.name = 'RequiresSynchronousAccess'
)
BEGIN

ALTER TABLE [dbo].[PermissionWindow] ADD CONSTRAINT [DF_PermissionWindow_RequiresSynchronousAccess] DEFAULT ((1)) FOR [RequiresSynchronousAccess]

END