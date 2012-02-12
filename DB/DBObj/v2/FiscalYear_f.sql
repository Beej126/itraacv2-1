--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:00p $

/* testing:
select dbo.FiscalYear_f('2/1/2100')
sp_procsearch 'FiscalYear'
drop function fiscalyear
*/
USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'FiscalYear_f')
	exec('create FUNCTION FiscalYear_f() returns int begin return 0 end')
GO
ALTER FUNCTION dbo.FiscalYear_f(@Date DATETIME)
RETURNS VARCHAR(2)
AS BEGIN

DECLARE @FiscalYear_f VARCHAR(4), @FiscalBoundaryDate DATETIME
SET @Date = ISNULL(@Date, GETDATE())
SET @FiscalYear_f = RIGHT(CONVERT(VARCHAR, @date, 101), 4)
SET @FiscalBoundaryDate = '10/1/' + @FiscalYear_f

IF (@Date >= @FiscalBoundaryDate) SET @FiscalYear_f = @FiscalYear_f + 1

SET @FiscalYear_f = RIGHT(@FiscalYear_f, 2) --maintaining 4 digits up to this point was necessary because once you hit 2050, then the default two digit year logic assumes 1950
RETURN @FiscalYear_f

END
go

GRANT EXECUTE ON dbo.FiscalYear_f TO PUBLIC
go
