SELECT 
GooodsServicesID,
--TaxOfficeID
o.TaxOfficeName,
GoodsServiceName,
s.Description,
--Type,
t.GSType AS Type,
s.Active
--StatusFlags,
--RowGUID
FROM tblGoodsServices s 
JOIN tblTaxOffices o ON s.TaxOfficeID = o.TaxOfficeID
JOIN tblGoodsServiceTypes t ON t.GSTypeID = s.Type