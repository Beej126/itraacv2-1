--$Author: Brent.anderson2 $
--$Date: 5/05/10 4:33p $
--$Modtime: 5/05/10 4:32p $

/****** Object:  View [dbo].[TaxForm_Full_v]    Script Date: 06/22/2009 08:23:48 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

/*testing
select top 5 * from TaxForm_Full_v where used is not null and description is not null
description is not null
[form #] = 'NF1-RA-09-118124' 
*/

USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'TaxForm_Full_v')
	exec('create VIEW TaxForm_Full_v as select 1 as one')
GO
alter VIEW [dbo].[TaxForm_Full_v]
AS

SELECT 
  s.SponsorGUID,
  --p.ClientGUID,
  --p.SpoonsorClientGUID,
  s.LName + ', ' + s.FName + ' (' + s.CCode + ')' AS SponsorName,
  c.LName + ', ' + c.FName + ' (' + c.CCode + ')' AS AuthorizedDependent,
  --c.FName,
  --c.CCode,
  
  f.RowGUID,
  f.OrderNumber AS [Form #],
  p.PackageCode AS [Package #],
  p.ExpirationDate AS [Expires],

  f.StatusFlags AS StatusFlagsID,
  CASE
    WHEN f.LocationCode = 'Lost' THEN 'Lost'
    WHEN f.Incomplete = 1 THEN 'Incomplete'
    WHEN f.StatusFlags & POWER(2, 5) <> 0 THEN 'Voided'
    WHEN f.StatusFlags & POWER(2, 2) <> 0 THEN 'Filed'
    WHEN f.StatusFlags & POWER(2, 1) <> 0 THEN 'Returned'
    ELSE 'Unreturned' END AS [Status],
  f.Incomplete,
  f.LocationCode,

  p.PurchaseDate AS [Purchased],
  iu.FName + ' ' + iu.LName AS [Issued By],
  f.ReturnedDate AS Returned,
  f.ReturnUserGUID,
  ru.FName + ' ' + ru.LName AS [Returned By],
  f.FiledDate AS Filed,
  f.FileUserGUID,
  fu.FName + ' ' + fu.LName AS [Filed By],

  f.TransTypeID AS TransactionTypeID,
  tx.TransactionType AS [Transaction Type],

  f.GoodServiceGUID,
  g.GoodsServiceName AS [Purchase Type],

  f.VendorGUID,
  v.ShortDescription AS Vendor,

  f.UsedDate AS [Used],
  d.TotalCost AS [Total Cost],
  CASE d.CurrencyUsed WHEN 1 THEN 'USD' WHEN 2 THEN 'Euro' ELSE 'Unknown' END AS [Currency],
  d.CurrencyUsed AS CurrencyID,
  d.CheckNumber,
  f.[Description],
  f.ViolationRemarkGUID

FROM iTRAAC.dbo.tblTaxForms f 
JOIN iTRAAC.dbo.tblTaxFormPackages p ON p.RowGUID = f.PackageGUID

JOIN iTRAAC.dbo.tblClients s ON s.RowGUID = p.SponsorClientGUID
LEFT JOIN iTRAAC.dbo.tblClients c ON c.RowGUID = p.ClientGUID

LEFT JOIN iTRAAC.dbo.tblTransactionTypes tx ON tx.TransTypeID = f.TransTypeID
LEFT JOIN iTRAAC.dbo.tblGoodsServices g ON g.RowGUID = f.GoodServiceGUID
LEFT JOIN iTRAAC.dbo.tblPPOData d on d.TaxFormGUID = f.RowGUID
LEFT JOIN Vendor_v v ON v.VendorGUID = f.VendorGUID
JOIN iTRAAC.dbo.tblUsers iu ON iu.RowGUID = p.SellUserGUID
LEFT JOIN iTRAAC.dbo.tblUsers ru ON ru.RowGUID = f.ReturnUserGUID
LEFT JOIN iTRAAC.dbo.tblUsers fu ON fu.RowGUID = f.FileUserGUID

LEFT JOIN iTRAAC.dbo.tblRemarks r ON r.RowGUID = ViolationRemarkGUID

GO

grant select on TaxForm_Full_v to public
go

