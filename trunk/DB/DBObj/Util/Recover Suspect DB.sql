/* recover a suspect database...

great reference apparently from someone directly on the Microsoft SQL Server team:
http://www.sqlskills.com/blogs/paul/post/checkdb-from-every-angle-emergency-mode-repair-the-very-very-last-resort.aspx

another good reference: http://www.sqlteam.com/forums/topic.asp?TOPIC_ID=76785

. DO NOT DETACH!  it's annoying to get it back online
. if you did detach...
    . move suspect files to a backup location
    . recreate a database with EXACT SAME FILE NAMES!!!
    . stop the sql server service
    . copy suspect files back over the top of new files
    . re-start the sql service... you should have a suspect re-attached now
    . follow the steps below
*/

--ALTER DATABASE iTRAAC SET EMERGENCY --sql 2005+ only
/* not sure whether emergency mode helped or no
SP_CONFIGURE 'ALLOW UPDATES', 1 
RECONFIGURE WITH OVERRIDE
update sysdatabases set status = 32768 where name = 'iTRAAC' -- sql 2000 emergency mode
--SELECT * FROM sysdatabases WHERE name='iTRAAC';
*/
GO
ALTER DATABASE iTRAAC SET SINGLE_USER WITH ROLLBACK IMMEDIATE
go
DBCC TRACEON (3604)
DBCC REBUILD_LOG ('iTRAAC', 'C:\iTRAAC_SQLData\iTRAAC_1.ldf') <-- not necessary under sql2005+, REPAIR_ALLOW_DATA_LOSS now does all this
GO
DBCC CHECKDB (iTRAAC, REPAIR_ALLOW_DATA_LOSS) WITH NO_INFOMSGS, ALL_ERRORMSGS;
GO
DBCC CHECKDB(iTRAAC) -- this still generated issues with indexes in my scenario...
go
DBCC DBREINDEX ( MSmerge_genhistory, nc2MSmerge_genhistory) -- DBCCREINDEX worked out those index errors
go
DBCC CHECKDB(iTRAAC) -- hopefully a totally clean slate now...
go
ALTER DATABASE iTRAAC SET MULTI_USER -- put database back fully online
go
sp_configure 'allow updates', 0
go
reconfigure with OVERRIDE
go