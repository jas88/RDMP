--Version:1.11.0.0
--Description:Adds IsDisabled Column to LoadSchedule

-- Check if table exists (LoadSchedule was later renamed to LoadMetadata)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='LoadSchedule')
BEGIN
	IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='LoadSchedule' AND COLUMN_NAME='IsDisabled')
	BEGIN
		ALTER TABLE LoadSchedule ADD IsDisabled bit NOT NULL
		CONSTRAINT [DF_LoadSchedule_IsDisabled]  DEFAULT ((0))
	END
END

-- Handle renamed table (LoadMetadata)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='LoadMetadata')
BEGIN
	IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='LoadMetadata' AND COLUMN_NAME='IsDisabled')
	BEGIN
		ALTER TABLE LoadMetadata ADD IsDisabled bit NOT NULL
		CONSTRAINT [DF_LoadMetadata_IsDisabled]  DEFAULT ((0))
	END
END