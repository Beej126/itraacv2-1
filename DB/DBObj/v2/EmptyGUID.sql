--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:00p $

/* testing:
select dbo.EmptyGUID('2/1/2100')
sp_procsearch 'bitflipper'
*/
USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'EmptyGUID')
	exec('create FUNCTION EmptyGUID() returns int begin return 0 end')
GO
ALTER FUNCTION dbo.EmptyGUID()
RETURNS UNIQUEIDENTIFIER
AS BEGIN RETURN ('00000000-0000-0000-0000-000000000000') END

go

GRANT EXECUTE ON dbo.EmptyGUID TO PUBLIC
go

