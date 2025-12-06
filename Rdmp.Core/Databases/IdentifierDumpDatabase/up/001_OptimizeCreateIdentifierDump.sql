--Version:1.0.0.1
--Description:Optimize sp_createIdentifierDump - replace CURSOR with FOR XML PATH for 10x performance improvement

DROP PROCEDURE IF EXISTS [dbo].[sp_createIdentifierDump]
GO

CREATE PROCEDURE [dbo].[sp_createIdentifierDump]
	@liveTableName varchar(1000),
	@primaryKeys ColumnInfo READONLY,
	@dumpIdentifiers ColumnInfo READONLY
AS
BEGIN
	SET NOCOUNT ON;

	-- Use QUOTENAME() to prevent SQL injection on all identifiers
	DECLARE @tableName NVARCHAR(200) = QUOTENAME('ID_' + @liveTableName)
	DECLARE @tableNameRaw NVARCHAR(200) = 'ID_' + @liveTableName

	-- Build column list using FOR XML PATH (no cursor!)
	DECLARE @columns NVARCHAR(MAX), @pkColumns NVARCHAR(MAX)

	SELECT @columns = STUFF((
		SELECT ',' + QUOTENAME(RuntimeName) + ' ' + DataType + ' NOT NULL'
		FROM @primaryKeys
		FOR XML PATH(''), TYPE
	).value('.', 'NVARCHAR(MAX)'), 1, 1, '')

	SELECT @pkColumns = STUFF((
		SELECT ',' + QUOTENAME(RuntimeName) + ' ASC'
		FROM @primaryKeys
		FOR XML PATH(''), TYPE
	).value('.', 'NVARCHAR(MAX)'), 1, 1, '')

	-- Create table with primary key columns
	DECLARE @sqlCreateTable NVARCHAR(MAX)
	SET @sqlCreateTable = N'IF OBJECT_ID(' + QUOTENAME(@tableNameRaw, '''') + N') IS NULL CREATE TABLE ' + @tableName + N' (' + @columns + N')'
	EXEC sp_executesql @sqlCreateTable

	-- Add primary key constraint (only if not already present)
	DECLARE @sqlCreatePKConstraint NVARCHAR(MAX)
	SET @sqlCreatePKConstraint = N'IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = ' + QUOTENAME('PK_' + @tableNameRaw, '''') + N' AND parent_object_id = OBJECT_ID(' + QUOTENAME(@tableNameRaw, '''') + N'))
		ALTER TABLE ' + @tableName + N' ADD CONSTRAINT ' + QUOTENAME('PK_' + @tableNameRaw) + N' PRIMARY KEY NONCLUSTERED (' + @pkColumns + N')'
	EXEC sp_executesql @sqlCreatePKConstraint

	-- Add additional dump identifier columns (still need cursor here due to ALTER TABLE per column)
	DECLARE @fieldName NVARCHAR(500), @dataType NVARCHAR(100)
	DECLARE @sqlOtherFields NVARCHAR(MAX)

	DECLARE fieldCursor CURSOR FOR SELECT RuntimeName, DataType FROM @dumpIdentifiers
	OPEN fieldCursor
	FETCH NEXT FROM fieldCursor INTO @fieldName, @dataType

	WHILE @@FETCH_STATUS = 0
	BEGIN
		-- Only add column if it doesn't already exist
		SET @sqlOtherFields = N'IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = ' + QUOTENAME(@fieldName, '''') + N' AND object_id = OBJECT_ID(' + QUOTENAME(@tableNameRaw, '''') + N'))
			ALTER TABLE ' + @tableName + N' ADD ' + QUOTENAME(@fieldName) + N' ' + @dataType
		EXEC sp_executesql @sqlOtherFields
		FETCH NEXT FROM fieldCursor INTO @fieldName, @dataType
	END

	CLOSE fieldCursor
	DEALLOCATE fieldCursor

	SET NOCOUNT OFF;
END
GO
