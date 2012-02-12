SELECT
v.VendorID,
--TaxOfficeID
o.TaxOfficeName,
VendorName,
AddressLine1,
AddressLine2,
AddressLine3,
v.Remarks,
v.Active,
v.StatusFlags
FROM tblVendors v JOIN tblTaxOffices o ON v.TaxOfficeID = o.TaxOfficeID