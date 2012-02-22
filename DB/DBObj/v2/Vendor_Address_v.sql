USE iTRAACv2
go

/*
SELECT * FROM sys.objects WHERE [object_id] = OBJECT_ID('Vendor_Address_v', 'view') 
SELECT OBJECT_ID('Vendor_Address_v', 'view') 
select top 50 TaxOfficeId, VendorName, [Address], VendorId, RowGUID from Vendor_Address_v where vendorname like '%heidel%'
*/

-- indexed view wasn't providing any benefit yet, so keeping this offline for now

--Set the options to support indexed views.
--SET NUMERIC_ROUNDABORT OFF;
--SET ANSI_PADDING, ANSI_WARNINGS, CONCAT_NULL_YIELDS_NULL, ARITHABORT, QUOTED_IDENTIFIER, ANSI_NULLS ON;
-- also required: "WITH SCHEMABINDING" on the view and "UNIQUE CLUSTERED" on the index
-- and you must create the indexed view in the same database as the table
GO

--Create view with schemabinding.
--IF OBJECT_ID('Vendor_Address_v', 'view') IS NOT NULL BEGIN
--  IF OBJECT_ID('idx_Vendor_Address_v', 'index') IS NOT NULL DROP INDEX Vendor_Address_v.IDX_Vendor_Address_v
--  DROP VIEW Vendor_Address_v
--END
IF OBJECT_ID('Vendor_Address_v', 'view') IS NULL
  EXEC('create view Vendor_Address_v as select 1 as one')
GO
ALTER VIEW Vendor_Address_v
--WITH SCHEMABINDING
AS
SELECT
  VendorId, RowGUID, TaxOfficeID, VendorName, Active,
  ISNULL(Street + ', ' + City + ', ' + PLZ, --select the new address if we actually have a full clean one, otherwise concat the legacy address
    isnull(AddressLine1, '') + isnull(', ' + AddressLine2, '') + isnull(', ' + AddressLine3, '')) as [Address],
  VendorName + ISNULL(' (' + ISNULL(Street, AddressLine1) + ')', '') AS ShortDescription
  --Line2, -- not sure if we need this in the primary search vector where address is most important
  --Phone
FROM iTRAAC.dbo.tblVendors
go

/*
IF OBJECT_ID('idx_Vendor_Address_v', 'index') IS NOT NULL
  DROP INDEX Vendor_Address_v.IDX_Vendor_Address_v
go
CREATE UNIQUE CLUSTERED INDEX IDX_Vendor_Address_v ON Vendor_Address_v 
(TaxOfficeId, VendorName, [Address], VendorId, RowGUID)
go
*/