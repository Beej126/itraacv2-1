/* basic sql server requirements:
. backup / restore privileges
	. shared folder with write access so I can upload database .BAK files to be restored
. ability to create some scheduled jobs ("SQL Agent" facilities)
. two iTRAAC databases enabled for replication publishing
*/


-- the identities running on the central server where we typically pull a backup 
-- are pretty jacked since in the scheme of things records never get created there
USE iTRAAC; exec itraac.dbo.Admin_ReseedIdentities;



EXEC sp_configure 'show advanced options', 1; RECONFIGURE; 
EXEC sp_configure 'xp_cmdshell', 1; RECONFIGURE; 
EXEC sp_configure 'clr enabled', 1; RECONFIGURE;
-- enable cross database ownership chaining so that iTRAACv2 procs can select/update iTRAAC(v1) tables
EXEC sys.sp_configure 'cross db ownership chaining', 1; RECONFIGURE;

--make sure SA is dbo on both databases so ownership chaining is happy
ALTER AUTHORIZATION ON DATABASE::iTRAAC TO sa
go
ALTER AUTHORIZATION ON DATABASE::iTRAAC_old TO iTRAAC_user
go


USE iTRAAC
go
GRANT SELECT ON tblTaxFormPackages TO PUBLIC
GO

GRANT SELECT ON tblTaxForms TO PUBLIC
GO

GRANT SELECT ON tblTaxOffices TO PUBLIC
GO

GRANT SELECT ON tblTaxFormAgents TO PUBLIC
GO


/*************************************************************************
. the basic thing we want is for iTRAACv2 stored procs to be able to access iTRAAC(v1) tables
. that's simple with 'normal' stored procs via "cross db ownership chaining" setting above

. unfortunately dynamic sql doesn't inherit the nifty security context provided by stored procs
. easiest way to work that is to put "WITH EXECUTE AS OWNER" in the proc definition
  (right under the parms, before "AS BEGIN", see [Customer_Search] as example)
. setting the calling database (iTRAACv2 in this case) to be TRUSTWORTHY is also a requirement
*************************************************************************/

-- REQUIRED for DynSQL procs!!
-- resolves error: The server principal "sa" is not able to access the database "iTRAAC" under the current security context.
ALTER DATABASE iTRAAC SET TRUSTWORTHY On
go


--create the iTRAAC_User login to SQL Server
USE master
go
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'iTRAAC_User')
  CREATE LOGIN [iTRAAC_User] WITH PASSWORD=, DEFAULT_DATABASE=[master]
go

-- Create iTRAAC_User USER objects (tied to global iTRAAC_User LOGIN object) in both iTRAAC and iTRAACv2 databases
USE iTRAAC
go
IF not EXISTS (SELECT * FROM sys.database_principals WHERE name = 'iTRAAC_User')
  CREATE USER iTRAAC_User FOR LOGIN iTRAAC_User WITH DEFAULT_SCHEMA = dbo
GO
--exec sp_addrolemember db_datareader, iTRAAC_User -- not necessary if we use "WITH EXECUTE AS OWNER" in proc definitions

USE iTRAACv2
go
IF not EXISTS (SELECT * FROM sys.database_principals WHERE name = 'iTRAAC_User')
  CREATE USER iTRAAC_User FOR LOGIN iTRAAC_User WITH DEFAULT_SCHEMA = dbo
GO

-- fix orphaned users when you restore a backup, remount an MDF, etc.
-- exec sp_change_users_login 'report' -- this shows the list of orphaned users in the current database
-- EXEC sp_change_users_login 'update_one', 'iTRAAC_User', 'iTRAAC_User' -- this is how you remap one
-- EXEC sp_change_users_login 'Auto_Fix', 'iTRAAC_User'

-- exec sp_dropuser 'iTRAAC_User'

-- this error happened after i dropped and remounted MDF after doing a major hard drive switcheroo
-- error: The database owner SID recorded in the master database differs from  the database owner SID recorded in database 'iTRAACv2'.
--        You should correct this situation by resetting the owner of database 'iTRAACv2' using the ALTER AUTHORIZATION statement.
--EXEC sp_changedbowner @loginame = N'sa', @map = false


