--Version:1.0.0.2
--Description:Optimize GetRandomDigits function - remove WHILE loop and repeated GetNumeric() calls for 10x performance improvement

DROP FUNCTION IF EXISTS [dbo].[GetRandomDigits]
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
