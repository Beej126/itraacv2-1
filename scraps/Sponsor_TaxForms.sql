USE [iTRAACv2]
GO
/****** Object:  StoredProcedure [dbo].[Sponsor_TaxForms]    Script Date: 06/10/2011 09:09:59 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
ALTER PROCEDURE [dbo].[Sponsor_TaxForms] 
@GUID UNIQUEIDENTIFIER, -- @SponsorRowGUID
@TableNames VARCHAR(1000) = NULL OUT
AS BEGIN

SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

SET @TableNames = 'Sponsor_TaxForm'

-- this should be interesting...
-- start fleshing out more explicit visibility on "migratory sponsors" to coin a phrase
-- for the specified household (Sponsor) GUID...
-- first gather all the forms sold to all the members of this house as a sponsor at *any*time* (including folks that are now listed as dependents)
-- then also go see if any of the current dependents' previous identities were "Form Sponsors"
-- this handles someone being a sponsor, then getting married and changing their name, since the old forms would be tied to the old Client name record

-- *** the current sponsor is not technically responsible for these forms, their dependents are
--  so the sponsor tab's list of forms is the most directly visible place to present them all

-- so first, gather the obvious list of ClientGUIDs currently under the specified SponsorGUID
DECLARE @FormSponsoringClients TABLE(ClientGUID UNIQUEIDENTIFIER PRIMARY KEY, FName VARCHAR(100))
INSERT @FormSponsoringClients (ClientGUID, FName)
SELECT RowGUID, FName
FROM iTRAAC.dbo.tblClients WHERE SponsorGUID = @GUID

-- then go get the ClientGUIDs for previous identities of the above Clients
INSERT @FormSponsoringClients (ClientGUID, FName)
SELECT RowGUID, c.FName
FROM @FormSponsoringClients i
JOIN iTRAAC.dbo.tblClients c ON c.ReplacementClientGUID = i.ClientGUID

SELECT
  f.RowGUID AS TaxFormGUID,
  @GUID AS SponsorGUID,
  f.OrderNumber AS [Form #],
  f.[Status],
  ISNULL(c.FName, s.FName) as [Purchased By],
  p.PurchaseDate AS Purchased,
  p.PackageCode AS [Package #],
  ISNULL(o.TaxOfficeName, f.LocationCode) AS Location,
  f.Printed AS PrintedID,
  f.Incomplete AS IncompleteID,
  f.UnreturnedID,
  f.StatusFlags AS StatusFlagsID,
  f.FormTypeID
FROM TaxForm_Status_v f
JOIN iTRAAC.dbo.tblTaxFormPackages p ON p.RowGUID = f.PackageGUID
JOIN @FormSponsoringClients s ON s.ClientGUID = p.SponsorClientGUID
LEFT JOIN iTRAAC.dbo.tblClients c ON c.RowGUID = p.ClientGUID
LEFT JOIN iTRAAC.dbo.tblTaxOffices o ON o.OfficeCode = f.LocationCode

END
