--Version:1.0.0.1
--Description:Increases the length of the [DataSet].[dataSetID] column to match the length of [DataLoadTask].[name]

-- First drop the FK in DataLoadTask (handle both old and new names)
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DataLoadTask_DataLoadTask')
	ALTER TABLE [DataLoadTask] DROP CONSTRAINT FK_DataLoadTask_DataLoadTask

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DataLoadTask_DataSet')
	ALTER TABLE [DataLoadTask] DROP CONSTRAINT FK_DataLoadTask_DataSet

-- Drop PK on DataSet to allow column modification
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_DataSet' AND type = 'PK')
	ALTER TABLE [DataSet] DROP CONSTRAINT PK_DataSet

-- Now update the columns. First the column in DataSet.
ALTER TABLE [DataSet] ALTER COLUMN dataSetID varchar(255) not null

-- Re-add PK on DataSet
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_DataSet')
	ALTER TABLE [DataSet] ADD CONSTRAINT PK_DataSet PRIMARY KEY (dataSetID)

-- And then the FK column in DataLoadTask
ALTER TABLE [DataLoadTask] ALTER COLUMN dataSetID varchar(255) not null

-- Finally re-add the FK in DataLoadTask (with corrected name)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DataLoadTask_DataSet')
	ALTER TABLE [DataLoadTask] ADD CONSTRAINT FK_DataLoadTask_DataSet FOREIGN KEY (dataSetID) REFERENCES DataSet (dataSetID)