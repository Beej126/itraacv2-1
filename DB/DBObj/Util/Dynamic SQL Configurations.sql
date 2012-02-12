/*
discovered all this secondary user stuff isn't necessary if you use "WITH EXECUTE AS OWNER" on the procs as explained in Baseline_DB_Settings.sql
decided to keep this code around just in case it comes in handy for something else
the difference with this approach is that instead of "WITH EXECUTE AS OWNER" at the top of the proc definition,
you structure the dynsql execs like this: exec('...dynsql...') with execute as user='DynSQL_Selects'
*/

-- Create the DynSQL_Selects LOGIN and USER that will be granted db_datareader permission and provide the execution context for DynSQL stored procs via "EXECUTE AS"
USE master
go
IF  EXISTS (SELECT * FROM sys.server_principals WHERE name = 'DynSQL_Selects')
DROP LOGIN [DynSQL_Selects]
GO
CREATE LOGIN [DynSQL_Selects] WITH PASSWORD='$(SQLCMDPASSWORD)',  CHECK_EXPIRATION=OFF, CHECK_POLICY=OFF
GO

USE iTRAAC
go
IF  EXISTS (SELECT * FROM sys.database_principals WHERE name = 'DynSQL_Selects')
  EXEC sp_dropuser DynSQL_Selects
go
CREATE USER DynSQL_Selects FOR LOGIN DynSQL_Selects WITH DEFAULT_SCHEMA = dbo
GO

--*************************************************************************
-- this is what gives DynSQL_Selects select access in iTRAAC
exec sp_addrolemember db_datareader, DynSQL_Selects
go


USE iTRAACv2
go
IF  EXISTS (SELECT * FROM sys.database_principals WHERE name = 'DynSQL_Selects')
  EXEC sp_dropuser DynSQL_Selects
go
CREATE USER DynSQL_Selects FOR LOGIN DynSQL_Selects WITH DEFAULT_SCHEMA = dbo
GO

--*************************************************************************
-- this is what gives the iTRAACv2.iTRAAC_User USER the ability to impersonate the DynSQL_Selects USER via "EXECUTE AS" in stored proc exec('...') style calls
GRANT IMPERSONATE ON user::DynSQL_Selects TO iTRAAC_User
go
