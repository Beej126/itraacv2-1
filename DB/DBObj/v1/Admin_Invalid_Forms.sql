--$Author: Brent.anderson2 $
--$Date: 2/17/10 2:23p $
--$Modtime: 12/18/09 10:59a $

/****** Object:  StoredProcedure [dbo].[Admin_Invalid_Forms]    Script Date: 06/19/2009 15:58:30 ******/
USE iTRAAC
go

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

if not exists(select 1 from sysobjects where name = 'Admin_Invalid_Forms')
	exec('create PROCEDURE Admin_Invalid_Forms as select 1 as one')
GO

-- http://www.oanda.com/currency/historical-rates

ALTER PROC Admin_Invalid_Forms

AS BEGIN

SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

-- initial basic obvious concept of "invalid" is NF1's over 2499.99... perhaps we can get a little more clever over time
SELECT 
  ISNULL(dbo.TaxOfficeName_f(f.TaxFormID), '{unknown}') AS TaxOfficeName,
  f.OrderNumber, 
  c.LName, c.FName, c.CCode,
  isnull(eur.totalcost,0) AS [Euros], isnull(usd.totalcost,0) AS [USD],
  e.DateUsed,
  r.Rate AS [Exchange Rate on Date Used]
FROM tblTaxForms f
JOIN tblTaxFormPackages p ON p.PackageID = f.PackageID
JOIN tblClients c ON c.ClientID = p.ClientID
LEFT JOIN PPOData_DeDuped_v usd ON usd.TaxFormID = f.TaxFormID AND usd.CurrencyUsed = 1
LEFT JOIN PPOData_DeDuped_v eur ON eur.TaxFormID = f.TaxFormID AND eur.CurrencyUsed = 2
LEFT JOIN tblTaxFormExt e ON e.RowGUID = f.RowGUID
LEFT JOIN ExchangeRate r ON r.[Date] = e.DateUsed
WHERE f.FormTypeID = 1 
and (isnull(eur.totalcost,0) > 2499.99 or isnull(usd.totalcost,0) * ISNULL(r.Rate, 0.66) > 2499.99)
ORDER BY isnull(eur.totalcost, 0) desc, isnull(usd.totalcost, 0) DESC

END
go

grant execute on Admin_Invalid_Forms to public
go

