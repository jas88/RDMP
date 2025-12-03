--Version:1.0.0.1
--Description:Allows you to cache files in different locations than the HIC Project directory

-- Check if table exists (LoadSchedule was later renamed to LoadMetadata)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='LoadSchedule')
BEGIN
	IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='LoadSchedule' AND COLUMN_NAME='AlternativeCacheLocation')
	BEGIN
		ALTER TABLE LoadSchedule ADD AlternativeCacheLocation varchar(3000) NULL
	END
END

-- Handle renamed table (LoadMetadata)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='LoadMetadata')
BEGIN
	IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='LoadMetadata' AND COLUMN_NAME='AlternativeCacheLocation')
	BEGIN
		ALTER TABLE LoadMetadata ADD AlternativeCacheLocation varchar(3000) NULL
	END
END