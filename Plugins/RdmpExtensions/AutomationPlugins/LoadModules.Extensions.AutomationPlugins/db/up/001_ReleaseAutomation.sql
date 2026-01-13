--Version: 1.1.0.0
--Description: Adds support for 

if not exists (select 1 from sys.columns where name = 'RefreshCohort')
begin
	alter table AutomateExtraction add RefreshCohort bit null
	alter table AutomateExtraction add Release bit null
	alter table AutomateExtractionSchedule add ExecutionTimeOfDay varchar(50) null
	alter table AutomateExtractionSchedule add ReleasePipeline_ID int null
end
GO

update AutomateExtraction set  RefreshCohort = 0 where RefreshCohort is null
update AutomateExtraction set  Release = 0 where Release is null
update  AutomateExtractionSchedule set ExecutionTimeOfDay = '12:00:00' where ExecutionTimeOfDay is null


if exists (select 1 from sys.columns where name = 'RefreshCohort' and is_nullable = 1)
begin
	alter table AutomateExtraction alter column RefreshCohort bit not null
	alter table AutomateExtraction  alter column Release bit not null
	alter table AutomateExtractionSchedule  alter column ExecutionTimeOfDay varchar(50) not null
	
end



