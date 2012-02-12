--SELECT * FROM FormPackageClientSponsor_v 
/*
SELECT f.*
FROM tblTaxFormPackages p 
--JOIN tblTaxForms f ON f.PackageID = p.PackageID
JOIN 
WHERE PurchaseDate > '1/23/2012' 
AND ClientID IN
(
)
ORDER BY PurchaseDate desc
*/

SELECT o.OfficeCode, c.PrimeTaxOfficeID, c.SponsorID, c.ClientID, c.CCode, c.FName, c.LName, MIN(p.PurchaseDate) AS Min_PurchaseDate
--SELECT o.OfficeCode, * 
FROM tblclients c 
JOIN tblTaxOffices o ON o.TaxOfficeId = c.PrimeTaxOfficeID --BETWEEN o.TaxOfficeId AND o.TaxOfficeId + 10000000
JOIN tblTaxFormPackages p ON p.ClientID = c.ClientID
WHERE c.SponsorID < 10000001
--AND p.PurchaseDate > '1/23/2012'
GROUP BY o.OfficeCode, c.PrimeTaxOfficeID, c.SponsorID, c.ClientID, c.CCode, c.FName, c.LName
ORDER BY MIN(p.PurchaseDate) DESC

--select * from information_schema.COLUMNS WHERE COLUMN_NAME LIKE '%office%' AND TABLE_NAME LIKE 'tbl%'
