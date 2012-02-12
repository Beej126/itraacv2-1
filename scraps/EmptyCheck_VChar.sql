--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:00p $

USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'EmptyCheck_VChar')
	exec('create FUNCTION EmptyCheck_VChar() returns int begin return 0 end')
GO
ALTER FUNCTION dbo.EmptyCheck_VChar(@NewVal VARCHAR(MAX), @OldVal VARCHAR(MAX))
RETURNS VARCHAR(MAX)
AS BEGIN

DECLARE @Return VARCHAR(MAX)

SELECT @Return = CASE WHEN ISNULL(RTRIM(@NewVal), '') = '' THEN @OldVal ELSE @NewVal END

RETURN @Return

END
go

GRANT EXECUTE ON dbo.EmptyCheck_VChar TO PUBLIC
go


