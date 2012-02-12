
/************************
******* Probably don't do this up front... just let incomplete come into play with v2 TaxForm saves
*************************/

USE iTRAAC
GO

-- flag all incompletes based on empty fields
UPDATE f SET f.Incomplete = 1
from tblTaxForms f
LEFT JOIN tblPPOData d ON d.TaxFormGUID = f.RowGUID
WHERE (f.StatusFlags & (POWER(2,1) | POWER(2,2) | POWER(2,5) | POWER(2,23)) = POWER(2,1)) -- only hit forms marked as returned w/o being filed, voided or already incomplete
AND (
   f.UsedDate IS null
or f.TransTypeID IS NULL
OR f.VendorGUID IS NULL
OR d.TotalCost IS NULL
OR d.CurrencyUsed IS null
--OR NOT EXISTS(SELECT 1 FROM tblPPOData d WHERE d.TaxFormGUID = f.RowGUID)
)

-- establish some pardoning cutoff date where we right everything prior off and take everything after more seriously
-- right now i'm saying the beginning of FY2009???
-- bit 6 = "pardoned"
UPDATE f SET f.StatusFlags = f.StatusFlags | POWER(2,6) 
-- SELECT COUNT(1)
FROM tblTaxForms f
JOIN tblTaxFormPackages p ON p.RowGUID = f.PackageGUID
WHERE f.StatusFlags & POWER(2,23) <> 0 AND p.PurchaseDate < '10/1/2008'

/*
SELECT TOP 50 * FROM tblTaxForms WHERE StatusFlags & POWER(2,23) <>0
SELECT COUNT(1) FROM tblTaxForms WHERE StatusFlags & POWER(2,6) <>0

SELECT *, CHARINDEX(CHAR(9), DESCRIPTION) FROM tblTaxForms WHERE RowGUID = '37AF6132-3410-4133-A3C9-B5BCEEF312DD'

SELECT TOP 10 * 
FROM FormPackageClient_v v
JOIN iTRAAC.dbo.tblClients c ON c.RowGUID = v.ClientGUID
WHERE v.FormStatusFlags & (POWER(2,23) | POWER(2,6)) = POWER(2,23)
--> m7886 had a few incompletes for demo purposes
*/

