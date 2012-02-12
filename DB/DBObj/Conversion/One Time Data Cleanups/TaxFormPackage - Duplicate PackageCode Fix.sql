ALTER TABLE tblTaxFormPackages ALTER COLUMN PackageCode VARCHAR(14)

UPDATE p SET p.packagecode = T.NewPackageCode
from tblTaxFormpackages p
join (
  SELECT
    PackageID,
    PackageCode,
    PackageCode + CHAR(96+ROW_NUMBER() OVER (PARTITION BY PackageCode ORDER BY PackageCode)) as NewPackageCode
  FROM tblTaxFormpackages WHERE PackageCode IN (
    SELECT PackageCode --, COUNT(1)
    FROM tblTaxFormPackages
    GROUP BY PackageCode
    HAVING COUNT(1) > 1
  )
) t ON T.packageid = p.PackageID
