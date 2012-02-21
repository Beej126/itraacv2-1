-- todo before deploying to every office
-- fix the description to be consistent with the delete 
-- update sysmaintplan_subplans SET subplan_description = 'OvernightCleanup.Subplan_1' WHERE subplan_name  = 'overnightcleanup_subplan_1'

-- this script creates the scheduled job for specified maintenance plan
-- meant to be called from SQLCMD.EXE command line with the following variable inputs:
--    InstanceName = the \instance to install the plan to... only if neededa and *including* the initial backslash
--    PlanName = the name of the maintenance plan pulled from the DTSX file, to be displayed under SSMS > Object Explorer > Management > 'Maintenance Plans'
--    PlanId = GUID pulled from the DTSX file
--    SubPlanId = GUID pulled from the DTSX file
-- e.g.
--    sqlcmd -S server\instance -v InstanceName=\mssql2008 PlanName=OverNightCleanup -i "schedule maint.plan job.sql"

USE [msdb]
GO

DECLARE @SubPlanName sysname
SET @SubPlanName = 'Subplan_1' --hard coding this to the structure produced by the current Maint.Plan wizard (SQL Server 2008 R2 SP1 = v10.50.2500.0)
DECLARE @JobName sysname
SET @JobName = '$(PlanName).' + @SubPlanName


BEGIN TRANSACTION
DECLARE @ReturnCode INT
SELECT @ReturnCode = 0

-- the maint.plan log stuff sort of works to the "side" of the general job stuff, so we have to manage those inserts and deletes separately
-- blow away the maint plan log records so there are no foreign keys preventing deletion of the associated job
DECLARE @old_subplan_id UNIQUEIDENTIFIER
SELECT @old_subplan_id = subplan_id FROM sysmaintplan_subplans WHERE subplan_description = @JobName --*** note, this is a convention i'm going with, that the description is what we use to target the previously established records
DELETE sysmaintplan_log WHERE subplan_id = @old_subplan_id
IF (@@ERROR <> 0) GOTO QuitWithRollback
DELETE sysmaintplan_subplans WHERE subplan_id = @old_subplan_id
IF (@@ERROR <> 0) GOTO QuitWithRollback

-- clear out the old job
DECLARE @job_id UNIQUEIDENTIFIER
SELECT @job_id = job_id FROM msdb.dbo.sysjobs_view WHERE name = @JobName
IF (@job_id IS NOT NULL) 
BEGIN
  EXEC @ReturnCode = msdb.dbo.sp_delete_job @job_id=@job_id, @delete_unused_schedule=1
  IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
END


/******   Create JobCategory [Database Maintenance]    ******/
IF NOT EXISTS (SELECT name FROM msdb.dbo.syscategories WHERE name=N'Database Maintenance' AND category_class=1)
BEGIN
  EXEC @ReturnCode = msdb.dbo.sp_add_category @class=N'JOB', @type=N'LOCAL', @name=N'Database Maintenance'
  IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
END

SET @job_id = NULL -- *critical* have to reset this to NULL or sp_add_job will fail with a misleading error about MSX

EXEC @ReturnCode = msdb.dbo.sp_add_job
  @job_name = @JobName,
	@enabled=1, 
	@notify_level_eventlog=2, 
	@notify_level_email=0, 
	@notify_level_netsend=0, 
	@notify_level_page=0, 
	@delete_level=0, 
	@description = N'', 
	@category_name = N'Database Maintenance', 
	@owner_login_name=N'sa',
	@job_id = @job_id OUTPUT
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback

/******   Create Job Step [Subplan_1]    ******/
DECLARE @command VARCHAR(max)
SET @command = '/Server "(local)\mssql2008" /SQL "Maintenance Plans\OvernightCleanup" /set "\Package\'+@SubPlanName+'.Disable;false"'
EXEC @ReturnCode = msdb.dbo.sp_add_jobstep
  @job_id=@job_id,
  @step_name=@SubPlanName,
  @step_id=1, 
  @cmdexec_success_code=0, 
  @on_success_action=1, 
  @on_success_step_id=0, 
  @on_fail_action=2, 
  @on_fail_step_id=0, 
  @retry_attempts=0, 
  @retry_interval=0, 
  @os_run_priority=0, @subsystem=N'SSIS', 
  @command=@command, 
  @flags=0
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
EXEC @ReturnCode = msdb.dbo.sp_update_job @job_id = @job_id, @start_step_id = 1
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback

/******   Create Job Schedule     ******/
DECLARE @schedule_uid uniqueidentifier, @schedule_id int
EXEC @ReturnCode = msdb.dbo.sp_add_jobschedule 
  @job_id = @job_id, 
  @name = @JobName, 
  @enabled=1, 
  @freq_type=4, 
  @freq_interval=1, 
  @freq_subday_type=1, 
  @freq_subday_interval=0, 
  @freq_relative_interval=0, 
  @freq_recurrence_factor=0, 
  @active_start_date=20120131, 
  @active_end_date=99991231, 
  @active_start_time=0, 
  @active_end_time=235959,
  @schedule_uid=@schedule_uid OUT
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
SELECT @schedule_id = schedule_id FROM msdb.dbo.sysschedules WHERE schedule_uid = @schedule_uid

EXEC @ReturnCode = msdb.dbo.sp_add_jobserver @job_id = @job_id --defaults to local:, @server_name = N'(local)'
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback

-- a sysmaintplan_subplans record is required by the logging called upon by the maintenance plan
-- otherwise you'll get a foreign key error upon the corresponding sysmaintplan_log insert
INSERT sysmaintplan_subplans (
  subplan_id, subplan_name, subplan_description, plan_id, job_id, msx_job_id, schedule_id, msx_plan
) VALUES  (
  '$(SubPlanId)',
  @SubPlanName, 
  @JobName, --description
  '$(PlanId)',
  @job_id,
  NULL,
  @schedule_id,
  0  -- msx_plan - bit
)
IF (@@ERROR <> 0) GOTO QuitWithRollback

COMMIT TRANSACTION

GOTO EndSave
QuitWithRollback:
    IF (@@TRANCOUNT > 0) ROLLBACK TRANSACTION
EndSave:


/*
DECLARE @old_subplan_id UNIQUEIDENTIFIER
SELECT @old_subplan_id = subplan_id FROM sysmaintplan_subplans WHERE subplan_description = 'OvernightCleanup.Subplan_1'
DELETE sysmaintplan_log WHERE subplan_id = @old_subplan_id
DELETE sysmaintplan_subplans WHERE subplan_id = @old_subplan_id
*/

-- DELETE sysjobschedules WHERE job_id = '17A911A9-408C-49F1-88EC-A2851CDDFC0C'
-- DELETE sysjobs WHERE job_id = '17A911A9-408C-49F1-88EC-A2851CDDFC0C'
-- SELECT * FROM sysjobservers

