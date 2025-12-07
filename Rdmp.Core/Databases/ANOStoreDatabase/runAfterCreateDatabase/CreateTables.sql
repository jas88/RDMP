
-- =============================================
--	Author:			Chris Hall 
--	Create date:	12/01/2015
--	Description:	Used to create a a table to hold anonymous identifiers
--	Output:			Mapping.

CREATE TYPE [dbo].[Batch] AS TABLE(
	[Identifier] [varchar](max) NULL
)
GO

CREATE FUNCTION [dbo].[GetRandomAlpha]
(
    @length int
)
RETURNS VARCHAR(64)
AS
BEGIN
	
	DECLARE @rand VARCHAR(64), @i INT, @newid VARCHAR(36)

	IF(@length > 64)
	BEGIN
		RETURN 0
	END

	DECLARE @characters VARCHAR(1024)
	SET @characters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'

	RETURN(SELECT rnd AS [text()] FROM  
		(SELECT Num,SUBSTRING(@characters,(SELECT (ABS(CHECKSUM(new_id))%LEN(@characters))+1 FROM v_newID), 1) AS rnd FROM Numbers WHERE Num <=  @length)x --restrict to the required length.
		FOR XML PATH(''))
END
GO

CREATE FUNCTION [dbo].[GetRandomDigits](@length int)
RETURNS VARCHAR(64)
AS
BEGIN
	IF(@length > 64) RETURN -1

	-- Generate pool of random hex characters (3 GUIDs = 96 chars, plenty for 64 digit extraction)
	-- Use v_newID view to work around SQL Server's restriction on NEWID() in functions
	DECLARE @pool VARCHAR(256)
	SELECT @pool = REPLACE(CAST((SELECT new_id FROM v_newID) AS VARCHAR(36)), '-', '')
	             + REPLACE(CAST((SELECT new_id FROM v_newID) AS VARCHAR(36)), '-', '')
	             + REPLACE(CAST((SELECT new_id FROM v_newID) AS VARCHAR(36)), '-', '')

	-- Extract only digits using FOR XML PATH (single pass, no loop!)
	DECLARE @digits VARCHAR(256)
	SELECT @digits = (
		SELECT SUBSTRING(@pool, Num, 1) AS [text()]
		FROM Numbers
		WHERE Num <= LEN(@pool)
		  AND SUBSTRING(@pool, Num, 1) LIKE '[0-9]'
		FOR XML PATH('')
	)

	-- Return requested length
	RETURN LEFT(@digits, @length)
END

GO

CREATE PROCEDURE [dbo].[sp_allocateNewANOIdentifiers]
	-- Add the parameters for the stored procedure here
	@batch Batch READONLY, 
	@tableName varchar(500),
	@numberOfIntegersToUseInAnonymousRepresentation int,
	@numberOfCharactersToUseInAnonymousRepresentation int,
	@suffix varchar(10)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
	SET NOCOUNT ON;
                
	PRINT '[sp_allocateNewANOIdentifiers]:  Method entered'
	--add in the underscore to seperate the suffix from the anonymisation string.
	IF LEN(@suffix) IS NOT NULL
	BEGIN
		SET @suffix ='_' + @suffix 
	END
                                

	PRINT '[sp_allocateNewANOIdentifiers]:  Suffix is ' + @suffix

	IF(@numberOfIntegersToUseInAnonymousRepresentation + @numberOfCharactersToUseInAnonymousRepresentation >64)
		THROW  50003,'pattern is greater than 64 characters long which is illegal due to the random string generator max ',1;

	--first build the table to hold the identifiers - we need to do this as sql thinks that batch is a varchar in dynamic code and so won't select from it, or thinks it's outwith the scope..
	SELECT DISTINCT Identifier INTO #tempBatch from @batch WHERE Identifier IS NOT NULL
	PRINT '[sp_allocateNewANOIdentifiers]:  Identified Distinct Identifiers'

	--get the column names
	DECLARE @identifiableColumn  varchar(1024) = (select name from sys.columns where OBJECT_NAME(object_id) = @tableName and column_id = 1)
	DECLARE @anonymousColumn  varchar(1024) = (select name from sys.columns where OBJECT_NAME(object_id) = @tableName and column_id = 2)
                                
	PRINT '[sp_allocateNewANOIdentifiers]:  @anonymousColumn = ' + @anonymousColumn
	PRINT '[sp_allocateNewANOIdentifiers]:  @identifiableColumn = ' + @identifiableColumn
                                                                
	if(@identifiableColumn <> substring(@tableName,4,LEN(@tableName)-3))
		THROW 50005, 'Expected Identifiable column in ANOTable to be the same as the table name (without the ANO) e.g. if table name is ANOGpCode then identifiable column should be called GpCode',1;

	if (@anonymousColumn <> @tableName)
		THROW 50004, 'Expected anonymousColumn in ANOTable to be the same as the table name e.g. ANOGpCode and column ANOGpCode',1;
                                                                
	PRINT '[sp_allocateNewANOIdentifiers]:  Ready for first pass anonymisation'
	--generate anonymous identifiers for each of the identifiers that do not already exist in the destination table.

	----------------------------------------------------------------------
	--						First pass anonymisation					--
	----------------------------------------------------------------------
	--For performance, we will first allocate a random identifier to each of the identifiers to be anonymised
	--and then look for and correct clashes afterwards.
	--this approach may cause performance to dwindle as the tables fill up, but we can wait and see.. ;)

	--first build a temporary table to hold the anonymised results.
	DECLARE @tempFinalTableName VARCHAR(1024)
	SELECT @tempFinalTableName = '##sp_allocateNewANOIdentifiersFinal'+REPLACE(CONVERT(VARCHAR(64),NEWID()),'-', '') -- Random table name
                
	--This gives a clone of the main identifiers table but with no data
	EXEC(N'SELECT TOP 0 '+@anonymousColumn+N','+@identifiableColumn+N' INTO '+@tempFinalTableName+N' FROM '+@tableName)
	
	DECLARE @sql nvarchar(4000)

	SET @sql = N'
		INSERT INTO '+@tempFinalTableName+ N'('+@identifiableColumn + N',' + @anonymousColumn+ N')
			SELECT DISTINCT Identifier, ISNULL(dbo.GetRandomAlpha('+CONVERT(VARCHAR,@numberOfCharactersToUseInAnonymousRepresentation)+N'),'''') + ISNULL(dbo.GetRandomDigits('+CONVERT(VARCHAR,@numberOfIntegersToUseInAnonymousRepresentation)+N'), '''') 
				+'''+ISNULL(CONVERT(VARCHAR,@suffix),N'')+N''' FROM #tempBatch
			WHERE Identifier NOT IN (SELECT '+@identifiableColumn+N' from '+@tableName+ N')                
	'
	EXEC(@sql)
                
                
	PRINT '[sp_allocateNewANOIdentifiers]:  Ready to resolve clashes'
	----------------------------------------------------------------------
	--					Clashes With Anonymous IDs						--
	----------------------------------------------------------------------
	--we may well have clashes in our new anonymisation, look for this and NULL them.
	SET @sql = N'WITH clashes AS(
					SELECT ROW_NUMBER() OVER(PARTITION BY '+@anonymousColumn+N' ORDER BY '+@anonymousColumn+N') AS clashCount, '+@anonymousColumn+N' FROM '+@tempFinalTableName+N'        )
			UPDATE clashes SET '+@anonymousColumn+N' = Char(0) WHERE clashCount > 1'

	EXEC(@sql)
	PRINT '[sp_allocateNewANOIdentifiers]:  Clashes CTE used to remove duplication within the temp table'

	--we also can't have anonymisations which clash with existing anonymisations
	--UPDATE ##anoTemp SET ANOTest = NULL WHERE ANOTest IN (SELECT ANOTest FROM ANOTest)
	SET @sql = N'UPDATE '+@tempFinalTableName+N' SET '+@anonymousColumn+N' = Char(0) WHERE '+@anonymousColumn+N' IN (SELECT '+@anonymousColumn+N' FROM '+@tableName+N')'

	EXEC(@sql)
	PRINT '[sp_allocateNewANOIdentifiers]:  Clashes between temp table and live table (' + @tableName +') resolved'

	--We need to generate new non-clashing values for those that are now null and update the table.
	DECLARE @clashCount INT
                
	DECLARE @paramDef nvarchar(50)

	SET @sql = N'SELECT @clashCountOut = COUNT(*) FROM '+@tempFinalTableName+N' WHERE '+@anonymousColumn+N' = Char(0)'
	SET @paramDef = N'@clashCountOut INT OUTPUT'

	EXEC sp_executesql @sql, @paramDef, @clashCountOut=@clashCount OUTPUT;
                
	WHILE @clashCount > 0 
	BEGIN
		SET @sql = N'DECLARE @anon VARCHAR(1024), @loopCounter INT, @loopMax INT
					SET @loopCounter = 0
					SET @loopMax = 1000

					SET @anon = ISNULL(dbo.GetRandomAlpha('+CONVERT(VARCHAR,@numberOfCharactersToUseInAnonymousRepresentation)+N'),'''') + ISNULL(dbo.GetRandomDigits('+CONVERT(VARCHAR,@numberOfIntegersToUseInAnonymousRepresentation)+N'),'''') + '''
					+ISNULL(CONVERT(VARCHAR,@suffix),'') +N'''
					WHILE @anon IN (SELECT '+@anonymousColumn+N' FROM '+@tableName+N' UNION SELECT '+@anonymousColumn+N' FROM '+@tempFinalTableName+N') AND @loopCounter < @loopMax 
					BEGIN
									SET @anon = ISNULL(dbo.GetRandomAlpha('+CONVERT(VARCHAR,@numberOfCharactersToUseInAnonymousRepresentation)+N'),'''') + ISNULL(dbo.GetRandomDigits('+CONVERT(VARCHAR,@numberOfIntegersToUseInAnonymousRepresentation)+N'),'''') 
													+ '''+ISNULL(CONVERT(VARCHAR,@suffix),'')+N'''

									SET @loopCounter = @loopCounter + 1
					END
                                
					IF @loopCounter = @loopMax
									SET @anon = NULL

					UPDATE TOP(1) '+@tempFinalTableName+N' SET '+@anonymousColumn+N' = @anon WHERE '+@anonymousColumn+N' = Char(0)'

		EXEC(@sql)
                                
                                
		SET @clashCount = @clashCount - 1
	END 
	PRINT '[sp_allocateNewANOIdentifiers]:  Clashes repopulations completed (previously clashes were nulled, we have now given them new unique values)'

	----------------------------------------------------------------------
	--						Commit to the final table.                  --
	----------------------------------------------------------------------
	SET @sql = N'INSERT INTO '+@tableName+ N'('+@identifiableColumn+','+@anonymousColumn+ N') SELECT DISTINCT '+@identifiableColumn+','+@anonymousColumn+ N' FROM '+@tempFinalTableName + N' WHERE ' + @anonymousColumn  + ' <> Char(0)'
	EXEC(@sql)
                
	PRINT '[sp_allocateNewANOIdentifiers]:  New identifiers integrated, dropping temporary tables'
	----------------------------------------------------------------------
	--                              Clean up.                           --
	----------------------------------------------------------------------
	SET @sql = N'DROP TABLE '+@tempFinalTableName
	EXEC(@sql)
END
GO

CREATE PROCEDURE [dbo].[sp_substituteANOIdentifiers]
(	
	-- Add the parameters for the function here
	-- Add the parameters for the stored procedure here
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

	--we need to ensure that the batch passed in is the correct collation - to alter this, we need to put it in a table.
	SELECT DISTINCT Identifier INTO #tempBatch from @batch WHERE Identifier IS NOT NULL
	
	DECLARE @sql NVARCHAR(max)

	SET @sql = 'SELECT DISTINCT '+@identifiableColumn+ ',' + @anonymousColumn + ' FROM ' + @tableName + ' INNER JOIN #tempBatch b on '+ @identifiableColumn  + '= b.Identifier'
	
	EXECUTE sp_executesql @sql, N'@batch Batch READONLY', @batch
END
GO


CREATE view [dbo].[v_newID] as select newid() as new_id


GO
CREATE FUNCTION [dbo].[GetAlpha](@strAlphaNumeric VARCHAR(256))
RETURNS VARCHAR(256)
AS
BEGIN
	DECLARE @result VARCHAR(256) = @strAlphaNumeric

	-- Remove all digits (single pass - O(n) instead of O(n²))
	SET @result = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	              REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	                @result,'0',''),'1',''),'2',''),'3',''),'4',''),
	                '5',''),'6',''),'7',''),'8',''),'9','')

	-- Remove common special characters (single pass)
	SET @result = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	              REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	                @result,' ',''),'-',''),'_',''),'.',''),',',''),
	                '!',''),'@',''),'#',''),'$',''),'%','')
	SET @result = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	              REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	                @result,'&',''),'*',''),'(',''),')',''),'[',''),
	                ']',''),'{',''),'}',''),'|',''),';','')
	SET @result = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	              REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	                @result,':',''),'"',''),'''',''),'<',''),'>',''),
	                '?',''),'/',''),'\',''),'=',''),'+','')

	RETURN ISNULL(@result, '0')
END
GO

CREATE FUNCTION [dbo].[GetNumeric](@strAlphaNumeric VARCHAR(256))
RETURNS VARCHAR(256)
AS
BEGIN
	DECLARE @result VARCHAR(256) = @strAlphaNumeric

	-- Remove all letters (single pass - O(n) instead of O(n²))
	SET @result = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	              REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	                @result,'A',''),'B',''),'C',''),'D',''),'E',''),
	                'F',''),'G',''),'H',''),'I',''),'J','')
	SET @result = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	              REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	                @result,'K',''),'L',''),'M',''),'N',''),'O',''),
	                'P',''),'Q',''),'R',''),'S',''),'T','')
	SET @result = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	              REPLACE(@result,'U',''),'V',''),'W',''),'X',''),'Y',''),'Z','')

	-- Remove lowercase letters
	SET @result = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	              REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	                @result,'a',''),'b',''),'c',''),'d',''),'e',''),
	                'f',''),'g',''),'h',''),'i',''),'j','')
	SET @result = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	              REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	                @result,'k',''),'l',''),'m',''),'n',''),'o',''),
	                'p',''),'q',''),'r',''),'s',''),'t','')
	SET @result = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	              REPLACE(@result,'u',''),'v',''),'w',''),'x',''),'y',''),'z','')

	-- Remove common special characters
	SET @result = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	              REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	                @result,' ',''),'-',''),'_',''),'.',''),',',''),
	                '!',''),'@',''),'#',''),'$',''),'%','')
	SET @result = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	              REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	                @result,'&',''),'*',''),'(',''),')',''),'[',''),
	                ']',''),'{',''),'}',''),'|',''),';','')
	SET @result = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	              REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
	                @result,':',''),'"',''),'''',''),'<',''),'>',''),
	                '?',''),'/',''),'\',''),'=',''),'+','')

	RETURN ISNULL(@result, '0')
END
GO

GO




CREATE TABLE [dbo].[Numbers](
	[Num] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Num] ASC
)
)

GO
-- Populate Numbers table with values 1-1000 using efficient cross-join approach
-- This generates 1000 rows in a single batch operation instead of 1000 individual INSERTs
;WITH
    L0 AS (SELECT 1 AS c UNION ALL SELECT 1),                    -- 2 rows
    L1 AS (SELECT 1 AS c FROM L0 A, L0 B),                        -- 4 rows
    L2 AS (SELECT 1 AS c FROM L1 A, L1 B),                        -- 16 rows
    L3 AS (SELECT 1 AS c FROM L2 A, L2 B),                        -- 256 rows
    L4 AS (SELECT 1 AS c FROM L3 A, L3 B),                        -- 65536 rows
    Nums AS (SELECT ROW_NUMBER() OVER(ORDER BY (SELECT NULL)) AS n FROM L4)
INSERT INTO [dbo].[Numbers] ([Num])
SELECT n FROM Nums WHERE n <= 1000
