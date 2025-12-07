--Version:1.91.0
--Description:Make LoadPeriodicity column nullable (obsolete column, C# converts empty string to NULL)

-- Drop existing default constraint if present
IF EXISTS (SELECT 1 FROM sys.default_constraints dc
           JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
           WHERE c.name = 'LoadPeriodicity' AND OBJECT_NAME(c.object_id) = 'LoadProgress')
BEGIN
    DECLARE @constraintName NVARCHAR(256)
    SELECT @constraintName = dc.name FROM sys.default_constraints dc
           JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
           WHERE c.name = 'LoadPeriodicity' AND OBJECT_NAME(c.object_id) = 'LoadProgress'
    EXEC('ALTER TABLE [dbo].[LoadProgress] DROP CONSTRAINT ' + @constraintName)
END
GO

-- Make the column nullable (it's obsolete and C# code converts empty string to NULL on UPDATE)
IF EXISTS (SELECT 1 FROM sys.columns c
           WHERE c.name = 'LoadPeriodicity' AND OBJECT_NAME(c.object_id) = 'LoadProgress' AND c.is_nullable = 0)
BEGIN
    ALTER TABLE [dbo].[LoadProgress] ALTER COLUMN [LoadPeriodicity] [varchar](10) NULL
END
GO
