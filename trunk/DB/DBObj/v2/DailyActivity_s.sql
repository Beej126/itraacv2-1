--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 4/30/10 4:48p $

/****** Object:  StoredProcedure [dbo].[DailyActivity_s]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

/* testing:
DailyActivity_s 10000001, 'Filed', '5/1/2005', '9/1/2005'
exec dbo.DailyActivity_s @TaxOfficeId='HD',@ActivityType='Filed',@StartDate='2009-03-14 15:33:11',@EndDate='2009-09-15 20:22:47'
select * from tbltaxoffices
*/

if not exists(select 1 from sysobjects where name = 'DailyActivity_s')
	exec('create PROCEDURE DailyActivity_s as select 1 as one')
GO
alter PROCEDURE [dbo].DailyActivity_s
@TaxOfficeId int,
@ActivityType VARCHAR(10),
@StartDate DATETIME,
@EndDate DATETIME
WITH EXECUTE AS OWNER
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

DECLARE @Filter VARCHAR(MAX)
SET @Filter = ' BETWEEN ''' + CONVERT(VARCHAR, @StartDate) + ''' AND ''' + CONVERT(VARCHAR, DATEADD(day, 1, @EndDate)) + ''' '
IF      (@ActivityType = 'ISSUED')   SET @Filter = ' AND p.PurchaseDate' + @Filter + 'and f.StatusFlags & 6 = 0'
ELSE IF (@ActivityType = 'RETURNED') SET @Filter = ' AND f.ReturnedDate' + @Filter + 'and f.StatusFlags & 6 = 2'
ELSE IF (@ActivityType = 'FILED')    SET @Filter =  'AND f.FiledDate'    + @Filter + 'and f.StatusFlags & 4 = 4'

EXEC("
SELECT
  c.LName + ' (' + c.CCode + ')' AS Customer,
  c.SponsorGUID,
  f.OrderNumber AS [Form #],
  f.RowGUID,
  p.PurchaseDate AS [Purchased],
  f.ReturnedDate as [Returned],
  f.FiledDate as [Filed],
  p.PackageCode
FROM iTRAAC.dbo.tblTaxFormPackages p
JOIN iTRAAC.dbo.tblTaxForms f ON f.PackageGUID = p.RowGUID
JOIN iTRAAC.dbo.tblClients c ON c.RowGUID = p.AuthorizedDependentClientGUID
WHERE p.TaxOfficeID = " + @TaxOfficeId
+@Filter
)

END
GO

grant execute on DailyActivity_s to public
go

