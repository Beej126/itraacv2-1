select spid, kpid, status, hostname, dbid, cmd 
from master..sysprocesses
where kpid != 0
order by kpid

DECLARE @kpid INT, @spid INT, @statement VARCHAR(8000)
select @kpid = kpid, @spid=spid from master..sysprocesses where kpid != 0

DBCC INPUTBUFFER(@spid)

EXEC KPID2Statement_s @kpid=@kpid, @statement=@statement OUT
SELECT @statement