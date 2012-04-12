USE iTRAAC
go

if not exists(select 1 from master..syslogins where loginname = 'iTRAAC_User')
  exec sp_addlogin 'iTRAAC_User','iTRAAC_22_okToBer'
GO
exec sp_grantdbaccess N'iTRAAC_User', N'iTRAAC_User'
GO

/*
EXEC sp_change_users_login 'Report'
EXEC sp_change_users_login 'Auto_Fix', 'iTRAAC_User'


EXEC sp_dropuser 'iTRAAC_User'
GO
EXEC sp_dropuser 'iTRAAC_SA'
GO
go
if not exists(select 1 from master..syslogins where loginname = 'iTRAAC_User')
  exec sp_addlogin 'iTRAAC_User','iTRAAC_22_okToBer'
GO
if not exists(select 1 from master..syslogins where loginname = 'iTRAAC_SA')
exec sp_addlogin 'iTRAAC_SA','iTRAAC!09086_okToBer'
GO
exec sp_grantdbaccess N'iTRAAC_User', N'iTRAAC_User'
GO
exec sp_grantdbaccess N'iTRAAC_SA', N'iTRAAC_SA'
GO
exec sp_addrolemember N'db_owner', N'iTRAAC_SA'
GO
*/

DECLARE curs CURSOR LOCAL FAST_FORWARD FOR
SELECT name, xtype FROM sysobjects 
WHERE [name] LIKE 'cp_%' OR [name] LIKE 'vw_%' OR xtype IN ('if', 'fn')

DECLARE
  @procname VARCHAR(50), @xtype VARCHAR(50),
  @sql VARCHAR(8000)
  
OPEN curs
WHILE (1=1) BEGIN
  FETCH NEXT FROM curs INTO @procname, @xtype
  IF (@@FETCH_STATUS <> 0) BREAK

  IF @procname LIKE 'cp_%' OR @xtype = 'fn'
    SET @sql = 'grant execute on [' + @procname + '] to public'
  ELSE IF @procname LIKE 'vw_%' OR @xtype = 'if'
    SET @sql = 'grant select on [' + @procname + '] to public'

  PRINT @sql
  EXEC(@sql)
END

DEALLOCATE curs
GO

GRANT SELECT ON tblTaxFormPackages TO PUBLIC
GO

GRANT SELECT ON tblTaxForms TO PUBLIC
GO

GRANT SELECT ON tblTaxOffices TO PUBLIC
GO

GRANT SELECT ON tblTaxFormAgents TO PUBLIC
GO

--GRANT SELECT ON tblFormFields TO PUBLIC

--GRANT EXEC ON iTRAAC_old.dbo.GetFieldSpecs TO public