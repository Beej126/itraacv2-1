--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:00p $

/* testing:
select dbo.BitFlipper_f('2/1/2100')
sp_procsearch 'bitflipper'
*/
USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'BitFlipper_f')
	exec('create FUNCTION BitFlipper_f() returns int begin return 0 end')
GO
ALTER FUNCTION dbo.BitFlipper_f(@CurrentVal INT, @BitNum INT, @On bit)
RETURNS int
AS BEGIN

IF (@On = 1) SET @CurrentVal = @CurrentVal | POWER(2,@BitNum)
ELSE IF (@On = 0) SET @CurrentVal = @CurrentVal & ~POWER(2,@BitNum)
-- @On == null leaves current value alone

RETURN @CurrentVal

END
go

GRANT EXECUTE ON dbo.BitFlipper_f TO PUBLIC
go
