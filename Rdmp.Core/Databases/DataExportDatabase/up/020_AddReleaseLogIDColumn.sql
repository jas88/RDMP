--Version:2.13.0.1
--Description: Adds a column called ID to ReleaseLog allowing it to be created/deleted more easily.

-- Check if we need to modify the table
if(not exists (select * from sys.all_columns where name ='ID' AND OBJECT_NAME(object_id) = 'ReleaseLog'))
BEGIN
	-- Disable change tracking if enabled (required to drop/recreate PK)
	IF EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID('ReleaseLog'))
	BEGIN
		ALTER TABLE ReleaseLog DISABLE CHANGE_TRACKING
	END

	-- Drop existing PK if it exists
	if( exists (select * from sys.key_constraints where type ='PK' AND OBJECT_NAME(parent_object_id) = 'ReleaseLog'))
		ALTER TABLE ReleaseLog DROP CONSTRAINT PK_ReleaseLog;

	-- Add ID column
  	ALTER TABLE ReleaseLog ADD ID INT IDENTITY(1,1)

	-- Add new PK
  	if(not exists (select * from sys.key_constraints where type ='PK' AND OBJECT_NAME(parent_object_id) = 'ReleaseLog'))
		ALTER TABLE ReleaseLog ADD CONSTRAINT PK_ReleaseLog PRIMARY KEY (ID);

	-- Re-enable change tracking if database supports it
	-- Note: Commented out as change tracking may not be enabled at database level
	-- IF (SELECT COUNT(*) FROM sys.change_tracking_databases WHERE database_id = DB_ID()) > 0
	-- BEGIN
	--     ALTER TABLE ReleaseLog ENABLE CHANGE_TRACKING
	-- END
END

if not exists (select 1 from sys.indexes where name = 'ix_CumulativeExtractionResultsMustBeUnique')
	CREATE UNIQUE NONCLUSTERED INDEX ix_CumulativeExtractionResultsMustBeUnique ON ReleaseLog
	(
		CumulativeExtractionResults_ID ASC
	)


	--No more Software Version / Scalar Function

-- Helper procedure to drop SoftwareVersion column and its default constraint from a table
-- This handles both explicitly named and auto-generated constraint names
DECLARE @TableName nvarchar(128)
DECLARE @ConstraintName nvarchar(256)
DECLARE @SQL nvarchar(max)

-- List of tables to process (dropping SoftwareVersion column)
DECLARE @TablesToProcess TABLE (TableName nvarchar(128))
INSERT INTO @TablesToProcess VALUES
    ('CumulativeExtractionResults'),
    ('DataUser'),
    ('DeployedExtractionFilter'),
    ('DeployedExtractionFilterParameter'),
    ('ExternalCohortTable'),
    ('ExtractableCohort'),
    ('ExtractableColumn'),
    ('ExtractableDataSet'),
    ('ExtractionConfiguration'),
    ('GlobalExtractionFilterParameter'),
    ('Project'),
    ('ReleaseLog')

-- Process each table
DECLARE table_cursor CURSOR FOR SELECT TableName FROM @TablesToProcess
OPEN table_cursor
FETCH NEXT FROM table_cursor INTO @TableName

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Check if column exists
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(@TableName) AND name = 'SoftwareVersion')
    BEGIN
        -- Find any default constraint on SoftwareVersion column (name may vary)
        SELECT @ConstraintName = dc.name
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
        WHERE c.object_id = OBJECT_ID(@TableName) AND c.name = 'SoftwareVersion'

        -- Drop constraint if exists
        IF @ConstraintName IS NOT NULL
        BEGIN
            SET @SQL = 'ALTER TABLE ' + @TableName + ' DROP CONSTRAINT ' + @ConstraintName
            EXEC sp_executesql @SQL
            SET @ConstraintName = NULL
        END

        -- Drop column
        SET @SQL = 'ALTER TABLE ' + @TableName + ' DROP COLUMN SoftwareVersion'
        EXEC sp_executesql @SQL
    END

    FETCH NEXT FROM table_cursor INTO @TableName
END

CLOSE table_cursor
DEALLOCATE table_cursor

if exists (select OBJECT_NAME(object_id),* from sys.sql_modules  where OBJECT_NAME(object_id) ='GetSoftwareVersion')
	DROP FUNCTION dbo.GetSoftwareVersion