USE [iTRAAC]
GO

/****** Object:  View [dbo].[FormsPackageClientSponsor_v]    Script Date: 09/28/2011 12:11:52 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


if not exists(select 1 from sysobjects where name = 'FormPackageClientSponsor_v')
	exec('create view FormPackageClientSponsor_v as select 1 as one')
GO
alter VIEW [dbo].FormPackageClientSponsor_v
AS

SELECT
  p.PackageID, p.PurchaseDate,
  f.OrderNumber, f.CtrlNumber,
  f.Description,
  f.TaxFormID,
  f.RowGUID AS TaxFormGUID,
  CASE 
  WHEN f.StatusFlags & POWER(2,5) >0 THEN 'voided'
  WHEN f.StatusFlags & POWER(2,2) >0 THEN 'filed'
  WHEN f.StatusFlags & POWER(2,1) >0 THEN 'returned'
  ELSE 'issued' END AS [Status],
  f.StatusFlags AS Form_StatusFlags,
  p.ClientID, c.SponsorID, c.StatusFlags AS Client_StatusFlags, c.Active,
  c.FName, c.LName, c.CCode, c.SSN,
  s.AddressLine1, s.DutyLocation, s.DutyPhone
FROM tblTaxFormPackages p
JOIN tblTaxForms f ON f.PackageID = p.PackageID
JOIN tblClients c ON c.ClientID = p.ClientID
JOIN tblSponsors s ON s.SponsorID = c.SponsorID




GO


