--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:00p $

/* testing:
select dbo.FiscalStartDate_f(null)
sp_procsearch 'FiscalYear'
drop function fiscalyear
*/
USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'FiscalStartDate_f')
	exec('create FUNCTION FiscalStartDate_f() returns int begin return 0 end')
GO
ALTER FUNCTION dbo.FiscalStartDate_f(@Date DATETIME)
RETURNS DATETIME
AS BEGIN

DECLARE @FiscalStartDate DATETIME
SET @Date = ISNULL(@Date, GETDATE())
SET @FiscalStartDate = '10/1/' + CONVERT(VARCHAR, DATEPART(YEAR, @Date))
IF (@Date < @FiscalStartDate) SET @FiscalStartDate = DATEADD(YEAR, -1, @FiscalStartDate)

RETURN @FiscalStartDate

END
go

GRANT EXECUTE ON dbo.FiscalStartDate_f TO PUBLIC
go
