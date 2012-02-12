--$Author: Brent.anderson2 $
--$Date: 11/24/09 5:03p $
--$Modtime: 11/23/09 5:04p $

/* testing:
exec Sponsor_TaxForms @GUID = 'eda2ef78-4bdc-4bd5-bc48-8e4c9f919a90'
*/

/****** Object:  StoredProcedure [dbo].[Sponsor_TaxForms]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'Sponsor_TaxForms')
	exec('create PROCEDURE Sponsor_TaxForms as select 1 as one')
GO
alter PROCEDURE [dbo].[Sponsor_TaxForms] 
@GUID UNIQUEIDENTIFIER, -- @SponsorRowGUID
@TableNames VARCHAR(1000) = NULL OUT
AS BEGIN

SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

SET @TableNames = 'Sponsor_TaxForm'

-- this should be interesting...
-- start fleshing out more explicit visibility on "migratory sponsors" to coin a phrase
-- for the specified household (Sponsor) GUID gather the following:
-- 1) all the forms sold to all the members of this house as a sponsor at *any*time* (including folks that are now listed as dependents)
-- 2) then any of forms sold to the current dependents' previous identities that were "Form Sponsors"
-- this handles someone being a sponsor, then getting married and changing their name, since the old forms would be tied to the old Client name record

-- *** the current sponsor is not technically responsible for these forms, their dependents are
--  so the sponsor tab's list of forms is the most directly visible place to present them all

-- tblTaxForm.StatusFlags: bit = flag
--  0 = issued
--  1 = returned
--  2 = filed
--  5 = void

SELECT
  f.RowGUID AS TaxFormGUID, --PrimaryKeyGUID *MUST* be the first column for the TableCache logic to work on the client
  @GUID AS SponsorGUID,
  f.OrderNumber AS [Form #],
  ISNULL(p.OriginalSponsorName, cs.FName) AS Sponsor,
  ISNULL(p.OriginalDependentName, c.FName) AS [AuthDep],
  p.PurchaseDate AS Purchased,
  p.PackageCode AS [Package #],
  o.TaxOfficeName AS Location,

  s.[Status], 
  s.IsUnReturned AS IsUnreturnedId,
  s.IsPrinted AS IsPrintedId,
  f.Incomplete AS IsIncompleteId,
  f.StatusFlags AS StatusFlagsId,
  f.FormTypeID
FROM iTRAAC.dbo.tblTaxForms f
CROSS APPLY dbo.TaxForm_Status_f(f.StatusFlags, f.LocationCode, f.Incomplete, f.InitPrt215, f.InitPrtAbw) s
JOIN iTRAAC.dbo.tblTaxFormPackages p ON p.RowGUID = f.PackageGUID
JOIN iTRAAC.dbo.tblClients cs ON cs.RowGUID = p.SponsorClientGUID
LEFT JOIN iTRAAC.dbo.tblClients c ON c.RowGUID = p.AuthorizedDependentClientGUID --p.ClientGUID could be null, it's just a pointer to Authorized Dependent *where one was chosen* (optional)
LEFT JOIN iTRAAC.dbo.tblTaxOffices o ON o.OfficeCode = f.LocationCode
where cs.SponsorGUID = @GUID

END
GO

grant execute on Sponsor_TaxForms to public
go
