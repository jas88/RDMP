--Version:1.28.0
--Description:Add DEFAULT constraint to RecordsExtracted and DistinctReleaseIdentifiersEncountered columns

-- Add default constraint to RecordsExtracted (value is set later via CompleteAudit but needs default for initial insert)
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc
               JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
               WHERE c.name = 'RecordsExtracted' AND OBJECT_NAME(c.object_id) = 'CumulativeExtractionResults')
BEGIN
    ALTER TABLE [dbo].[CumulativeExtractionResults] ADD CONSTRAINT [DF_CumulativeExtractionResults_RecordsExtracted] DEFAULT (0) FOR [RecordsExtracted]
END
GO

-- Also add default for DistinctReleaseIdentifiersEncountered which has the same issue
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc
               JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
               WHERE c.name = 'DistinctReleaseIdentifiersEncountered' AND OBJECT_NAME(c.object_id) = 'CumulativeExtractionResults')
BEGIN
    ALTER TABLE [dbo].[CumulativeExtractionResults] ADD CONSTRAINT [DF_CumulativeExtractionResults_DistinctReleaseIdentifiersEncountered] DEFAULT (0) FOR [DistinctReleaseIdentifiersEncountered]
END
GO
