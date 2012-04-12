USE iTRAAC_Reports
go

DECLARE curs CURSOR LOCAL FAST_FORWARD FOR
SELECT name, xtype FROM sysobjects WHERE xtype = 'p' AND name LIKE 'GL_REPORT%'

DECLARE
  @procname VARCHAR(50), @xtype VARCHAR(50),
  @sql VARCHAR(8000)
  
OPEN curs
WHILE (1=1) BEGIN
  FETCH NEXT FROM curs INTO @procname, @xtype
  IF (@@FETCH_STATUS <> 0) BREAK

  IF @xtype = 'p' OR @xtype = 'fn'
    SET @sql = 'grant execute on [' + @procname + '] to public'
  ELSE IF @procname LIKE 'vw_%' OR @xtype = 'if'
    SET @sql = 'grant select on [' + @procname + '] to public'

  PRINT @sql
  EXEC(@sql)
END

DEALLOCATE curs
GO

USE master
go

GRANT EXECUTE ON dbo.CONCAT TO PUBLIC
GO
