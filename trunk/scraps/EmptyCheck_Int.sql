--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:00p $

USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'EmptyCheck_Int')
	exec('create FUNCTION EmptyCheck_Int() returns int begin return 0 end')
GO
ALTER FUNCTION dbo.EmptyCheck_Int(@NewVal INT, @OldVal INT)
RETURNS INT
AS BEGIN

DECLARE @Return INT

SELECT @Return = CASE WHEN @NewVal IS NULL THEN @OldVal ELSE @NewVal END

RETURN @Return

END
go

GRANT EXECUTE ON dbo.EmptyCheck_Int TO PUBLIC
go
