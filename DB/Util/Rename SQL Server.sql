SELECT @@servername
EXEC sp_linkedservers
sp_dropserver 'IMCMEUROA4VDB03'
GO
sp_addserver 'iTRAAC_Central', local
GO

EXEC sp_dropdistributor @no_checks = 1, @ignore_distributor = 1

sp_dropserver 'iTRAAC_Central', 'droplogins'

sp_dropserver 'repl_distributor', 'droplogins'
GO
sp_addserver 'IMCMEUROA4VDB03', local
GO

-- restart the sql service now and check that it worked
-- related commands
SELECT @@servername
EXEC sp_linkedservers
SELECT SERVERPROPERTY('servername') 
SELECT * FROM sys.servers
sp_get_distributor