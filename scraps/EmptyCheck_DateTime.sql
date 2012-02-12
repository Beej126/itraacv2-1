--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:00p $

USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'EmptyCheck_DateTime')
	exec('create FUNCTION EmptyCheck_DateTime() returns int begin return 0 end')
GO
ALTER FUNCTION dbo.EmptyCheck_DateTime(@NewVal DATETIME, @OldVal DATETIME)
RETURNS DATETIME
AS BEGIN

DECLARE @Return DATETIME

SELECT @Return = CASE WHEN isnull(@NewVal, 0) = 0 THEN @OldVal ELSE @NewVal END

RETURN @Return

END
go

GRANT EXECUTE ON dbo.EmptyCheck_DateTime TO PUBLIC
go


