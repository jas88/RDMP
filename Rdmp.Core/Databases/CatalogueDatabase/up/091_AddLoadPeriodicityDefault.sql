--Version:1.91.0
--Description:Add DEFAULT constraint to LoadPeriodicity column to allow inserts without explicit value

-- Add default constraint to LoadPeriodicity (the column is obsolete but still NOT NULL)
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc
               JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
               WHERE c.name = 'LoadPeriodicity' AND OBJECT_NAME(c.object_id) = 'LoadProgress')
BEGIN
    ALTER TABLE [dbo].[LoadProgress] ADD CONSTRAINT [DF_LoadProgress_LoadPeriodicity] DEFAULT ('') FOR [LoadPeriodicity]
END
GO
