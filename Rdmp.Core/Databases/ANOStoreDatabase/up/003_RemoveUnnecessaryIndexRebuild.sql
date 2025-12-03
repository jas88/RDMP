--Version:1.0.0.3
--Description:Remove unnecessary INDEX REBUILD from sp_substituteANOIdentifiers - SQL Server auto-maintains indexes efficiently, rebuilding after every batch is expensive and wasteful

DROP PROCEDURE IF EXISTS [dbo].[sp_substituteANOIdentifiers]
GO

CREATE PROCEDURE [dbo].[sp_substituteANOIdentifiers]
(
	@batch Batch READONLY,
	@tableName varchar(500),
	@numberOfIntegersToUseInAnonymousRepresentation int,
	@numberOfCharactersToUseInAnonymousRepresentation int,
	@suffix varchar(10)
)
AS
BEGIN

	PRINT '[sp_substituteANOIdentifiers]: @tableName='+@tableName
	PRINT '[sp_substituteANOIdentifiers]: @numberOfIntegersToUseInAnonymousRepresentation='+CONVERT(VARCHAR,@numberOfIntegersToUseInAnonymousRepresentation)
	PRINT '[sp_substituteANOIdentifiers]: @numberOfCharactersToUseInAnonymousRepresentation='+CONVERT(VARCHAR,@numberOfCharactersToUseInAnonymousRepresentation)
	PRINT '[sp_substituteANOIdentifiers]: @suffix='+@suffix

	PRINT '[sp_substituteANOIdentifiers]:  Method entered, about to call sp_allocateNewANOIdentifiers'

	EXECUTE sp_allocateNewANOIdentifiers @batch , @tableName, @numberOfIntegersToUseInAnonymousRepresentation, @numberOfCharactersToUseInAnonymousRepresentation, @suffix
	PRINT '[sp_substituteANOIdentifiers]:  Method sp_allocateNewANOIdentifiers completed'

	IF(LEFT(@tableName,3)<>'ANO')
		THROW 50002,'tablename must start with ANO',1;


	--get the column names
	DECLARE @identifiableColumn  varchar(1024) = (select name from sys.columns where OBJECT_NAME(object_id) = @tableName and column_id = 1)
	DECLARE @anonymousColumn  varchar(1024) = (select name from sys.columns where OBJECT_NAME(object_id) = @tableName and column_id = 2)

	if(@identifiableColumn <> substring(@tableName,4,LEN(@tableName)-3))
		THROW 50005, 'Expected Identifiable column in ANOTable to be the same as the table name (without the ANO) e.g. if table name is ANOGpCode then identifiable column should be called GpCode',1;

	if (@anonymousColumn <> @tableName)
		THROW 50004, 'Expected anonymousColumn in ANOTable to be the same as the table name e.g. ANOGpCode and column ANOGpCode',1;

	-- INDEX REBUILD REMOVED: SQL Server auto-maintains indexes efficiently
	-- The previous REBUILD ALL with FILLFACTOR=80 was expensive and unnecessary
	-- Indexes are rebuilt automatically by SQL Server based on fragmentation
	-- FILLFACTOR=80 also wasted 20% disk space

	--we need to ensure that the batch passed in is the correct collation - to alter this, we need to put it in a table.
	SELECT DISTINCT Identifier INTO #tempBatch from @batch WHERE Identifier IS NOT NULL

	DECLARE @sql NVARCHAR(max)

	SET @sql = N'SELECT DISTINCT ' + QUOTENAME(@identifiableColumn) + N',' + QUOTENAME(@anonymousColumn) +
	           N' FROM ' + QUOTENAME(@tableName) +
	           N' INNER JOIN #tempBatch b ON ' + QUOTENAME(@identifiableColumn) + N' = b.Identifier'

	EXECUTE sp_executesql @sql, N'@batch Batch READONLY', @batch
END
GO
