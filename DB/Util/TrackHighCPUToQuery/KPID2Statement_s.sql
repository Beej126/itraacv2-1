ALTER PROC KPID2Statement_s
@KPID INT,
@SPID INT = NULL OUT ,
@Statement VARCHAR(8000) = NULL out
AS BEGIN

SET NOCOUNT ON

-- make sure args are cleared from a previous call since this will be rapid fire called in a loop by the client
SELECT @spid = NULL, @statement = null

DECLARE @Handle binary(20)

select @SPID = spid, @Handle = [sql_handle] --status, hostname, dbid, cmd 
from master..sysprocesses
where kpid = @KPID

IF (@@ROWCOUNT = 0) RETURN

CREATE TABLE #inputbuffer 
(
  EventType nvarchar(30), 
  Parameters int, 
  EventInfo nvarchar(255)
)

DECLARE @ExecStr varchar(50)
SET @ExecStr = 'DBCC INPUTBUFFER(' + STR(@SPID) + ')'
INSERT INTO #inputbuffer 
EXEC (@ExecStr)

SET @Statement = 'DBCC INPUTBUFFER => '
SELECT @Statement = @Statement + EventInfo FROM #inputbuffer
SET @Statement = @Statement + '; fn_get_sql => '

select @Statement = @Statement + CONVERT(VARCHAR, [text]) FROM ::fn_get_sql(@Handle) 

END
