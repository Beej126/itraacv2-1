USE iTRAAC
go

-- save off a copy so we can keep improving the conversion script from a known baseline
-- SELECT * INTO _VendorsRawBackup FROM tblVendors 
-- select * from tblVendors WHERE AddressLine2 IS NULL AND AddressLine3 IS NOT NULL

UPDATE v set
v.AddressLine1  = v2.AddressLine1,
v.AddressLine3  = v2.AddressLine3,
v.AddressLine2  = v2.AddressLine2,
v.Line2         = NULL,
v.Street        = NULL,
v.City          = NULL,
v.PLZ           = NULL
FROM tblVendors v
JOIN _VendorsRawBackup v2 ON v2.vendorid = v.VendorID

UPDATE tblVendors SET addressline1=NULL WHERE addressline1 = 'null'
UPDATE tblVendors SET addressline2=NULL WHERE addressline2 = 'null'
UPDATE tblVendors SET addressline3=NULL WHERE addressline3 = 'null'

UPDATE tblVendors SET AddressLine1 = NULL WHERE AddressLine1 IN ('n/a', 'NA')
UPDATE tblVendors SET AddressLine2 = NULL WHERE AddressLine2 IN ('n/a', 'NA')
UPDATE tblVendors SET AddressLine3 = NULL WHERE AddressLine3 IN ('n/a', 'NA')

UPDATE tblVendors SET AddressLine1 = NULL WHERE LTRIM(REPLACE(AddressLine1, '.', '')) = ''
UPDATE tblVendors SET AddressLine1 = NULL WHERE LTRIM(REPLACE(AddressLine1, 'x', '')) = ''
UPDATE tblVendors SET AddressLine1 = NULL WHERE LTRIM(REPLACE(AddressLine1, '-', '')) = ''
UPDATE tblVendors SET AddressLine1 = NULL WHERE LTRIM(REPLACE(AddressLine1, '*', '')) = ''

UPDATE tblVendors SET AddressLine2 = NULL WHERE LTRIM(REPLACE(AddressLine2, '.', '')) = ''
UPDATE tblVendors SET AddressLine2 = NULL WHERE LTRIM(REPLACE(AddressLine2, 'x', '')) = ''
UPDATE tblVendors SET AddressLine2 = NULL WHERE LTRIM(REPLACE(AddressLine2, '-', '')) = ''
UPDATE tblVendors SET AddressLine2 = NULL WHERE LTRIM(REPLACE(AddressLine2, '*', '')) = ''

UPDATE tblVendors SET AddressLine3 = NULL WHERE LTRIM(REPLACE(AddressLine3, '.', '')) = ''
UPDATE tblVendors SET AddressLine3 = NULL WHERE LTRIM(REPLACE(AddressLine3, 'x', '')) = ''
UPDATE tblVendors SET AddressLine3 = NULL WHERE LTRIM(REPLACE(AddressLine3, '-', '')) = ''
UPDATE tblVendors SET AddressLine3 = NULL WHERE LTRIM(REPLACE(AddressLine3, '*', '')) = ''


UPDATE tblVendors SET plz = addressline1, AddressLine1=null WHERE addressline1 LIKE '[0-9][0-9][0-9][0-9][0-9]'
UPDATE tblVendors SET plz = addressline2, AddressLine2=null WHERE addressline2 LIKE '[0-9][0-9][0-9][0-9][0-9]'
UPDATE tblVendors SET plz = addressline3, AddressLine3=null WHERE addressline3 LIKE '[0-9][0-9][0-9][0-9][0-9]'

-- "PLZ City"
UPDATE tblVendors SET PLZ = LEFT(AddressLine1, 5), City = Right(AddressLine1, LEN(AddressLine1)-6), AddressLine1 = null
WHERE AddressLine1 LIKE '[0-9][0-9][0-9][0-9][0-9] [a-z]%'

UPDATE tblVendors SET PLZ = LEFT(AddressLine2, 5), City = Right(AddressLine2, LEN(AddressLine2)-6), AddressLine2 = null
WHERE AddressLine2 LIKE '[0-9][0-9][0-9][0-9][0-9] [a-z]%'

UPDATE tblVendors SET PLZ = LEFT(AddressLine3, 5), City = Right(AddressLine3, LEN(AddressLine3)-6), AddressLine3 = null
WHERE AddressLine3 LIKE '[0-9][0-9][0-9][0-9][0-9] [a-z]%'

-- "D_PLZ City"
UPDATE tblVendors SET PLZ = substring(AddressLine1, 3, 5), City = Right(AddressLine1, LEN(AddressLine1)-8), AddressLine1 = null
WHERE AddressLine1 LIKE 'D_[0-9][0-9][0-9][0-9][0-9] [a-z]%'

UPDATE tblVendors SET PLZ = substring(AddressLine2, 3, 5), City = Right(AddressLine2, LEN(AddressLine2)-8), AddressLine2 = null
WHERE AddressLine2 LIKE 'D_[0-9][0-9][0-9][0-9][0-9] [a-z]%'

UPDATE tblVendors SET PLZ = substring(AddressLine3, 3, 5), City = Right(AddressLine3, LEN(AddressLine3)-8), AddressLine3 = null
WHERE AddressLine3 LIKE 'D_[0-9][0-9][0-9][0-9][0-9] [a-z]%'

-- "City PLZ"
UPDATE tblVendors SET PLZ = Right(AddressLine1, 5), City = Left(AddressLine1, LEN(AddressLine1)-6), AddressLine1 = null
WHERE AddressLine1 LIKE '%[a-z] [0-9][0-9][0-9][0-9][0-9]'

UPDATE tblVendors SET PLZ = Right(AddressLine2, 5), City = Left(AddressLine2, LEN(AddressLine2)-6), AddressLine2 = null
WHERE AddressLine2 LIKE '%[a-z] [0-9][0-9][0-9][0-9][0-9]'

UPDATE tblVendors SET PLZ = Right(AddressLine3, 5), City = Left(AddressLine3, LEN(AddressLine3)-6), AddressLine3 = null
WHERE AddressLine3 LIKE '%[a-z] [0-9][0-9][0-9][0-9][0-9]'

UPDATE tblVendors SET PLZ = Right(AddressLine1, 5), City = Left(AddressLine1, LEN(AddressLine1)-6), AddressLine1 = null
WHERE AddressLine1 LIKE '%[a-z] [0-9][0-9][0-9][0-9][0-9]'

-- Assume Line1 is the Street when Line2 & 3 are blank and we already have the City pulled from one of the other field conversions above
UPDATE tblVendors SET Street = AddressLine1, AddressLine1 = null
WHERE AddressLine1 IS NOT NULL AND AddressLine2 IS NULL AND AddressLine3 IS NULL AND city IS NOT NULL

-- convert obviously labled phone#'s
UPDATE tblVendors SET 
  Phone = LTRIM(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(AddressLine1, 'telephone', ''), 'telefon', ''), 'tel', ''), 'phone', ''), 'ph', ''), '.', ''), ':', '')),
  AddressLine1 = null
WHERE AddressLine1 LIKE 'tel%' OR AddressLine1 LIKE 'ph[.:]%' OR AddressLine1 LIKE 'phone%' OR AddressLine1 LIKE 'telephone%' OR AddressLine1 LIKE 'telefon%' 

UPDATE tblVendors SET 
  Phone = LTRIM(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(AddressLine2, 'telephone', ''), 'telefon', ''), 'tel', ''), 'phone', ''), 'ph', ''), '.', ''), ':', '')),
  AddressLine2 = null
WHERE AddressLine2 LIKE 'tel%' OR AddressLine2 LIKE 'ph[.:]%' OR AddressLine2 LIKE 'phone%' OR AddressLine2 LIKE 'telephone%' OR AddressLine2 LIKE 'telefon%' 

UPDATE tblVendors SET 
  Phone = LTRIM(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(AddressLine3, 'telephone', ''), 'telefon', ''), 'tel', ''), 'phone', ''), 'ph', ''), '.', ''), ':', '')),
  AddressLine3 = null
WHERE AddressLine3 LIKE 'tel%' OR AddressLine3 LIKE 'ph[.:]%' OR AddressLine3 LIKE 'phone%' OR AddressLine3 LIKE 'telephone%' OR AddressLine3 LIKE 'telefon%' 

-- convert address fields that start numerically and contain no alphas as obviously phone#'s
UPDATE tblVendors SET Phone = AddressLine1, AddressLine1 = NULL WHERE AddressLine1 LIKE '[0-9]%' AND AddressLine1 NOT LIKE '%[a-z]%'
UPDATE tblVendors SET Phone = AddressLine2, AddressLine2 = NULL WHERE AddressLine2 LIKE '[0-9]%' AND AddressLine2 NOT LIKE '%[a-z]%'
UPDATE tblVendors SET Phone = AddressLine3, AddressLine3 = NULL WHERE AddressLine3 LIKE '[0-9]%' AND AddressLine3 NOT LIKE '%[a-z]%'




-- now that we have some clean city names in the city column, bang those up against the address fields to pull out easy direct matches
UPDATE v1 SET City = AddressLine1, AddressLine1 = NULL
FROM tblVendors v1
WHERE EXISTS (SELECT 1 FROM tblVendors v2 WHERE v1.AddressLine1 = v2.City)

UPDATE v1 SET City = AddressLine2, AddressLine2 = null
FROM tblVendors v1
WHERE EXISTS (SELECT 1 FROM tblVendors v2 WHERE v1.AddressLine2 = v2.City)

UPDATE v1 SET City = AddressLine3, AddressLine3 = null
FROM tblVendors v1
WHERE EXISTS (SELECT 1 FROM tblVendors v2 WHERE v1.AddressLine3 = v2.City)

RETURN 

SELECT VendorID, vendorname, AddressLine1, AddressLine2, AddressLine3, line2, street, city, plz, phone FROM tblVendors WHERE AddressLine1 IS NOT NULL AND AddressLine2 IS NOT NULL AND AddressLine3 IS NOT NULL 

SELECT * FROM tblVendors WHERE city is null and (AddressLine1 IS NOT NULL OR addressline2 IS NOT NULL OR AddressLine3 IS NOT NULL)

--Hauptstr. 21-23, 67691 Hochspeyer
SELECT 
  LEFT(AddressLine1, CHARINDEX(',', AddressLine1)-1) AS Street,
  LTRIM(SUBSTRING(AddressLine1, CHARINDEX(',', AddressLine1)+1, 100)) AS 'PLZ City',
  VendorID, vendorname, AddressLine1, AddressLine2, AddressLine3, line2, street, city, plz, phone
FROM tblVendors
WHERE LEN(REPLACE(AddressLine1, ',', '')) = LEN(AddressLine1)-1
AND SUBSTRING(AddressLine1, CHARINDEX(',', AddressLine1)+1, 5) LIKE '[ 0-9][0-9][0-9][0-9][0-9]'

SELECT 
  LEFT(AddressLine2, CHARINDEX(',', AddressLine2)-1) AS Street,
  LTRIM(SUBSTRING(AddressLine2, CHARINDEX(',', AddressLine2)+1, 100)) AS 'PLZ City',
  VendorID, vendorname, AddressLine1, AddressLine2, AddressLine3, line2, street, city, plz, phone
FROM tblVendors
WHERE LEN(REPLACE(AddressLine2, ',', '')) = LEN(AddressLine2)-1
AND SUBSTRING(AddressLine2, CHARINDEX(',', AddressLine2)+1, 5) LIKE '[ 0-9][0-9][0-9][0-9][0-9]'



SELECT
  AddressLine3,
  REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(addressline3, '(', ''), ')', ''), '/', ''), '-', ''), ' ', ''),
  ISNUMERIC(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(addressline3, '(', ''), ')', ''), '/', ''), '-', ''), ' ', '')),
  vendorid, vendorname, AddressLine1, AddressLine2, AddressLine3, line2, street, city, plz, phone
FROM tblVendors 
WHERE (city IS NOT NULL OR plz IS NOT NULL) AND AddressLine3 IS NOT NULL
AND ISNUMERIC(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(addressline3, '(', ''), ')', ''), '/', ''), '-', ''), ' ', '')) = 0
AND AddressLine3 LIKE '[0-9]%'



SELECT LEN(AddressLine3),* FROM tblVendors WHERE VendorID = 70001167

-- first clean up the few where AddressLine3 has PLZ City and Addressline2 is empty
UPDATE tblVendors SET
  AddressLine2 = AddressLine3, AddressLine3 = NULL
  --select top 50 * from tblVendors
WHERE AddressLine2 IS NULL AND AddressLine3 IS NOT NULL

-- if Addressline2 is null (then Addressline3 will be null as well) and Addressline1 is then "PLZ City" (or Street)
UPDATE tblVendors SET
  City = case when SUBSTRING(AddressLine1, 1, 1) LIKE '[0-9]' THEN AddressLine1 END,
  Street = case when SUBSTRING(AddressLine1, 1, 1) NOT LIKE '[0-9]' THEN AddressLine1 END,
  AddressLine1 = null
WHERE AddressLine1 IS NOT null
AND AddressLine2 IS NULL
AND AddressLine3 IS null

-- if line3 is null and line1&2 are not, then line1 = street and line2 = "PLZ City"
UPDATE tblVendors SET
  Street = AddressLine1, AddressLine1 = NULL,
  City = AddressLine2, Addressline2 = null
WHERE AddressLine1 IS NOT NULL 
  AND AddressLine2 IS NOT NULL
  AND AddressLine3 IS NULL

-- if all 3 lines are populated then line1 = "Attention To", line2 = street, line3 = PLZ City
UPDATE tblVendors SET
  Line2 = AddressLine1, AddressLine1 = NULL,
  Street = AddressLine2, Addressline2 = NULL,
  City = AddressLine3, Addressline3 = null
WHERE AddressLine1 IS NOT NULL 
  AND AddressLine2 IS NOT NULL
  AND AddressLine3 IS NOT NULL


 
UPDATE tblVendors SET city = NULL WHERE City = '...' OR City = 'NA'
UPDATE tblVendors SET Street = NULL WHERE Street = 'NA'

-- deal with these manually where there are multiple commas, it's a small subset
SELECT * FROM tblVendors WHERE LEN(city) - LEN(REPLACE(City, ',' ,'')) > 1

UPDATE tblVendors SET
  Street = 
  SUBSTRING(City, 1, CHARINDEX(',', City)-1),
  City = 
  LTRIM(SUBSTRING(City, CHARINDEX(',', City)+1, 100))
WHERE LEN(city) - LEN(REPLACE(City, ',' ,'')) = 1 --single comma

-- split "PLZ City"
-- select charindex(' ', '79098 FREIBURG IM BRIESGAU')
--                        123456 
update tblVendors SET
  --select top 50 
  PLZ = 
  SUBSTRING(City, 1, CHARINDEX(' ', City)-1),
  City = 
  SUBSTRING(City, CHARINDEX(' ', City)+1, 100)
  --from tblVendors
WHERE SUBSTRING(City, 1,1) LIKE '[0-9]'
AND CHARINDEX(' ', City) > 0

/*
SELECT TOP 50 v1.VendorName, v1.line2, v1.street, v1.city, v1.plz, '--', v3.addressline1, v3.addressline2, v3.addressline3
FROM tblVendors v1
JOIN _VendorsRawBackup v3 ON v3.vendorid = v1.VendorID
WHERE EXISTS (SELECT 1 FROM tblVendors v2 WHERE v2.City = v1.Street)
SELECT * FROM _VendorsRawBackup WHERE addressline1 = 'AM WARMFREIBAD 3'
*/

-- if a value sitting in the street slot is now in the list of converted cities, and the city slot is null, then consider it a city
UPDATE v1 SET v1.City = v1.Street, v1.Street = null
FROM tblVendors v1
WHERE v1.City IS null
AND EXISTS (SELECT 1 FROM tblVendors v2 WHERE v2.City = v1.Street)

