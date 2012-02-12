--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:07p $

/* testing:
exec Delete_All_Objects @GUID = '286423b9-7f5b-4d0b-a170-047ee7020935'

*/
/****** Object:  StoredProcedure [dbo].[Delete_All_Objects]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'Delete_All_Objects')
	exec('create PROCEDURE Delete_All_Objects as select 1 as one')
GO
alter PROCEDURE [dbo].[Delete_All_Objects] 
AS BEGIN

set nocount on

declare @types table(systype varchar(2) collate Latin1_General_CI_AS_KS_WS primary key, friendly varchar(20))
insert @types values ('FN', 'function')
insert @types values ('TF', 'function')
insert @types values ('IF', 'function')
insert @types values ('P',  'proc')
insert @types values ('V',  'view')

DECLARE curs CURSOR LOCAL FAST_FORWARD FOR
  select o.name, t.friendly
  from sys.objects o
  join @types t on t.systype = o.type
  where name <> 'Delete_All_Objects' --type in ('FN', 'TF', 'IF', 'P', 'V') and 
  
DECLARE
  @objname VARCHAR(50), @type VARCHAR(50), @sql VARCHAR(max)

set @sql = ''

OPEN curs
WHILE (1=1) BEGIN
  FETCH NEXT FROM curs INTO @objname, @type
  IF (@@FETCH_STATUS <> 0) BREAK

  select @sql = @sql + 'drop ' + @type + ' [' + @objname + ']' + char(13) + char(10)
END

PRINT @sql
EXEC(@sql)

deallocate curs

set nocount off

END
GO

grant execute on Delete_All_Objects to public
go
