--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:00p $

USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'EmptyCheck_Float')
	exec('create FUNCTION EmptyCheck_Float() returns int begin return 0 end')
GO
ALTER FUNCTION dbo.EmptyCheck_Float(@NewVal float, @OldVal float)
RETURNS float
AS BEGIN

DECLARE @Return float

SELECT @Return = CASE WHEN @NewVal IS NULL THEN @OldVal ELSE @NewVal END

RETURN @Return

END
go

GRANT EXECUTE ON dbo.EmptyCheck_Float TO PUBLIC
go
