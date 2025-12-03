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

	DECLARE @tableName VARCHAR(100) = 'ID_' + @liveTableName

	-- Build column list using FOR XML PATH (no cursor!)
	DECLARE @columns VARCHAR(MAX), @pkColumns VARCHAR(MAX)

	SELECT @columns = STUFF((
		SELECT ',' + RuntimeName + ' ' + DataType + ' NOT NULL'
		FROM @primaryKeys
		FOR XML PATH(''), TYPE
	).value('.', 'VARCHAR(MAX)'), 1, 1, '')

	SELECT @pkColumns = STUFF((
		SELECT ',' + RuntimeName + ' ASC'
		FROM @primaryKeys
		FOR XML PATH(''), TYPE
	).value('.', 'VARCHAR(MAX)'), 1, 1, '')

	-- Create table with primary key columns
	DECLARE @sqlCreateTable VARCHAR(MAX)
	SET @sqlCreateTable = 'IF OBJECT_ID(''' + @tableName + ''') IS NULL CREATE TABLE ' + @tableName + ' (' + @columns + ')'
	EXEC(@sqlCreateTable)

	-- Add primary key constraint
	DECLARE @sqlCreatePKConstraint VARCHAR(MAX)
	SET @sqlCreatePKConstraint = 'ALTER TABLE ' + @tableName + ' ADD CONSTRAINT PK_' + @tableName + ' PRIMARY KEY NONCLUSTERED (' + @pkColumns + ')'
	EXEC(@sqlCreatePKConstraint)

	-- Add additional dump identifier columns (still need cursor here due to ALTER TABLE per column)
	DECLARE @fieldName VARCHAR(500), @dataType VARCHAR(100)
	DECLARE @sqlOtherFields VARCHAR(MAX)

	DECLARE fieldCursor CURSOR FOR SELECT RuntimeName, DataType FROM @dumpIdentifiers
	OPEN fieldCursor
	FETCH NEXT FROM fieldCursor INTO @fieldName, @dataType

	WHILE @@FETCH_STATUS = 0
	BEGIN
		SET @sqlOtherFields = 'ALTER TABLE ' + @tableName + ' ADD ' + @fieldName + ' ' + @dataType
		EXEC(@sqlOtherFields)
		FETCH NEXT FROM fieldCursor INTO @fieldName, @dataType
	END

	CLOSE fieldCursor
	DEALLOCATE fieldCursor

	SET NOCOUNT OFF;
END
GO
