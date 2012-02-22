

-- for all these conversions i think it helps to think of the new fields being populated properly by v2 client (of course)...
-- so then we should code these conversions like: "WHERE {newfield} is NULL" ... so that we can continually run them in an overnight job to keep v1 & v2 in sync
-- and we should tend to use the v1 BlahID style FK's as the linkage since those would be populated in those situations where the new v2 BlahGUID FK's are not filled out yet

use itraac
go


PRINT 'Vendor cleanup - null out empty remarks'
-- the vendor haystack is an interesting nut to consider
-- my current perspective is: don't even worry about creating nice clean vendor entries
-- there's really not much benefit to all the effort required to create a clean "vendor phonebook"
-- when we go looking for a vendor based on some investigation, we do a wildcard match on the vendor name and then eyeball it from there
-- otherwise the address and stuff is basically just free form text
-- which has an interesting implication for filing forms... namely,
-- the user gets to decide what they think is faster, searching for an existing one by name...
-- thus saving themselves the trouble of re-keying...
-- or just blasting a new one in regardless of whether it's already out there,
-- thus saving themselves the time of searching
UPDATE tblVendors SET remarks = NULL --> 822 rows on 2012-02-22
WHERE remarks = 'null'

UPDATE tblVendors SET remarks = NULL --> 145 rows on 2012-02-22
WHERE rtrim(REPLACE(REPLACE(remarks, CHAR(13), ''), CHAR(10), '')) = ''

-- what remains (on 2012-02-22) is only 545 records out of 97,000 (and growing) where remarks are non-null
-- typically a phone#

PRINT 'Vendor cleanup - null out common empty placeholders (null, N/A, x, -, etc)'
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



PRINT 'Agent -> User'
UPDATE a SET a.UserGUID = u.RowGUID
FROM tblTaxFormAgents a
JOIN tblUsers u ON u.UserID = a.UserID
WHERE a.UserGUID IS null



-- !*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*
-- !*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*
-- !*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*
-- !*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*!*
-- A big part of conversion needs to be resolving where there is either:
--   no Client record flagged as Sponsor
--   or more than one Client record flagged
-- then we don't have a bunch of crazy logic strewn around trying to deal with those two invalid scenarios
-- !*!*!*!*!*!*!*

PRINT 'Clients -> Sponsors (SponsorGUID replaces SponsorID)'
UPDATE c SET c.SponsorGUID = s.RowGUID
FROM tblClients c
JOIN tblSponsors s ON s.SponsorID = c.SponsorID
WHERE c.SponsorGUID IS NULL

PRINT 'Clients -> User (CreateUserGUID replaces AgentID)'
UPDATE c SET c.CreateUserGUID = u.RowGUID
FROM tblClients c
JOIN tblTaxFormAgents a ON a.AgentID = c.AgentID
JOIN tblUsers u ON u.UserID = a.UserID
WHERE c.CreateUserGUID IS NULL

PRINT 'Sponsor -> User (CreateUserID replaces AgentID)'
UPDATE s SET s.CreateUserGUID = u.RowGUID
FROM tblSponsors s
JOIN tblTaxFormAgents a ON s.AgentID = a.AgentID
JOIN tblUsers u ON a.UserID = u.UserID
where s.CreateUserGUID is null

PRINT 'drop in a "deleted user" record if one doesn''t already exist'
IF NOT EXISTS(SELECT 1 FROM tblUsers WHERE UserID = 370000005)
BEGIN
  SET IDENTITY_INSERT tblUsers on
  INSERT INTO dbo.tblUsers ([UserID], [FName], [LName], [LoginName], [PKISerialNumber], [Email], [DSNPhone], [UserLevel], [StatusFlags], [RowGUID], [Password], [CreateDate]) 
  VALUES ('370000005', 'Deleted', 'User', 'DeletedUser', NULL, NULL, NULL, '0', '0', '00000000-0000-0000-0000-000000000000', NULL, 'Nov  2 2010  1:53PM')
  SET IDENTITY_INSERT tblUsers OFF
END

PRINT 'fill out any remaining Sponsor.CreateUserGUID holes'
UPDATE tblSponsors SET CreateUserGUID = '00000000-0000-0000-0000-000000000000' WHERE CreateUserGUID IS NULL -- "deleted user"

PRINT 'fill out any remaining Client.CreateUserGUID holes'
UPDATE tblClients SET CreateUserGUID = '00000000-0000-0000-0000-000000000000' WHERE CreateUserGUID IS NULL -- "deleted user"

PRINT 'PPOData -> TaxForm (TaxFormGUID replaces TaxFormID)'
UPDATE d SET d.TaxFormGUID = f.RowGUID
FROM tblPPOData d
JOIN tblTaxForms f ON f.TaxFormID = d.TaxFormID
WHERE d.TaxFormGUID IS null


PRINT 'Client -> Remarks'
UPDATE r SET r.FKRowGUID = c.RowGUID
FROM tblRemarks r
JOIN tblClients c ON c.ClientID = r.RowID AND r.TableID = 10
WHERE r.FKRowGUID IS null

PRINT 'Sponsor -> Remarks'
UPDATE r SET r.FKRowGUID = s.RowGUID
FROM tblRemarks r
JOIN tblSponsors s ON s.SponsorID = r.RowID AND r.TableID = 9
WHERE r.FKRowGUID IS NULL

PRINT 'TaxForm -> Remarks'
UPDATE r SET r.FKRowGUID = f.RowGUID
FROM tblRemarks r
JOIN tblTaxForms f ON f.TaxFormID = r.RowID AND r.TableID = 14
WHERE r.FKRowGUID IS NULL

/*
these are the tblRemarks.StatusFlags that have been set
would be nice to know what 1,3,4 & 16 actually indicate
i know that 12 = alert and 2 seems to correspond to normal

none
12
2, 12
2
1, 16
1, 3, 4

SELECT COUNT(1) FROM tblRemarks --> 226210
SELECT COUNT(1) FROM tblRemarks WHERE StatusFlags & POWER(2,1) >0 --> 143628
SELECT COUNT(1) FROM tblRemarks WHERE StatusFlags & POWER(2,16) >0 --> 122401
SELECT COUNT(1) FROM tblRemarks WHERE StatusFlags & POWER(2,3) >0 --> 21227
SELECT COUNT(1) FROM tblRemarks WHERE StatusFlags & POWER(2,4) >0 --> 21227
*/

/* tblRemark.RemType:
3	Vehicle VIN
7	Vehicle Make
8	Vehicle Model
9	Vehicle Year

12	Weapon Serial Number
16	Weapon Model
17	Weapon Make
13	Weapon Caliber

4 - reprint
6 - suspension
10 - over NF1 limit
11 - bar
14 - void
15 - service fee change

inventing #20 for v2 - deactivating entire sponsor due to *PCS*
*/




PRINT 'some TaxForm records have more than one tab separator for some reason'
UPDATE tblTaxForms SET Description = REPLACE(Description, CHAR(9)+CHAR(9), CHAR(9)) 
PRINT 'needed to run this more than once because there must be some with 3 or 4 tabs present'
UPDATE tblTaxForms SET Description = REPLACE(Description, CHAR(9)+CHAR(9), CHAR(9))

PRINT 'convert TaxForm description+tab+date to propery UsedDate'
UPDATE f SET
  f.UsedDate = T.UsedDate,
  f.DESCRIPTION = CASE WHEN len(T.DESCRIPTION) = 0 THEN NULL ELSE T.description END
--SELECT TOP 50 t.*
FROM tblTaxForms f
JOIN (
  SELECT
    f.taxformid,
    DATEADD(DAY, 
      CONVERT(INT, SUBSTRING(f.[description], CHARINDEX(CHAR(9), f.[description])+1, 500)), '2000-01-01') AS UsedDate,
    SUBSTRING(f.[description], 1, CHARINDEX(CHAR(9), f.Description)-1) AS Description
  FROM tblTaxForms f 
  WHERE CHARINDEX(CHAR(9), f.[description]) >0
) t ON t.TaxFormID = f.TaxFormID
where f.UsedDate is null


PRINT 'Bring TaxForm.ReturnedDate and FiledDate out of EventData'
-- run this on every *garrison* DB... this data is not currently replicated to central!!
-- **AFTER** new TaxForm.ReturnedDate & TaxForm.FiledDate fields have been deployed
-- it will be so good to finally bring this key info into the centrally available replicated data pool
UPDATE f SET
  f.ReturnedDate = CASE T.StatusFlags & POWER(2,1) when POWER(2,1) THEN T.EventDateTime else f.ReturnedDate end,
  f.FiledDate = CASE T.StatusFlags & POWER(2,2) when POWER(2,2) THEN T.EventDateTime else f.FiledDate end
FROM tblTaxForms f
JOIN (
  SELECT 
    f.TaxFormID,
    e.EventDateTime,
    --bja:nice tough example string :) looking for "N:671088647" chunk in this case
    --each chunk is tab separated, i.e. char(9)
    --GooodsServicesID O:0 N:290000299 VendorID O:0 N:290002903 StatusFlags O:671088641 N:671088647 Description O: N: 2967 RetAgentID O:0 N:290000019 FileAgentID O:0 N:290000019 TotalCost O:0 N:856.13 CurrencyUsed O:0 N:2 
    CONVERT(INT,
      SUBSTRING(ed.EventData, 
        -- find the starting index of our chunk by looking for first "N" past "StatusFlags"
        CHARINDEX('N', ed.EventData, PATINDEX('%StatusFlags%', ed.EventData))+2, -- start+2 (skip "N:")
        -- length = end - (start+2)
        CHARINDEX(CHAR(9), ed.EventData, -- find end by looking for the next tab after our established starting point
          CHARINDEX('N', ed.EventData, PATINDEX('%StatusFlags%', ed.EventData)) 
        )
        - ( CHARINDEX('N', ed.EventData, PATINDEX('%StatusFlags%', ed.EventData))+2 )
      )
    ) 
    AS StatusFlags
  FROM tblDBEvents e
  JOIN tblDBEventData ed ON ed.DBEID = e.DBEID
  JOIN tblTaxForms f ON f.TaxFormID = e.PKID
  WHERE e.TableID = 14
  AND ed.EventData LIKE '%StatusFlags%'

  AND -- and flags are for returned or filed
    CONVERT(INT,
      SUBSTRING(ed.EventData, 
        -- find the starting index of our chunk by looking for first "N" past "StatusFlags"
        CHARINDEX('N', ed.EventData, PATINDEX('%StatusFlags%', ed.EventData))+2, -- start+2 (skip "N:")
        -- length = end - (start+2)
        CHARINDEX(CHAR(9), ed.EventData, -- find end by looking for the next tab after our established starting point
          CHARINDEX('N', ed.EventData, PATINDEX('%StatusFlags%', ed.EventData)) 
        )
        - (CHARINDEX('N', ed.EventData, PATINDEX('%StatusFlags%', ed.EventData))+2)
      )
    ) 
    & (POWER(2,1) | POWER(2,2)) > 0 -- 1 bit = returned; 2 bit = filed

  AND --and substring length > 0
    CHARINDEX(CHAR(9), ed.EventData, -- find end by looking for the next tab after our established starting point
      CHARINDEX('N', ed.EventData, PATINDEX('%StatusFlags%', ed.EventData)) 
    )
    - (CHARINDEX('N', ed.EventData, PATINDEX('%StatusFlags%', ed.EventData))+2)
  >0) t ON t.TaxFormID = f.TaxFormID
WHERE f.ReturnedDate IS NULL OR f.FiledDate IS NULL




/*************** TaxForm ****************/

PRINT 'TaxForm.PackageGUID replaces PackageID'
UPDATE f SET f.PackageGUID = p.RowGUID
FROM tblTaxForms f
JOIN tblTaxFormPackages p ON p.PackageID = f.PackageID
WHERE f.PackageGUID IS NULL

PRINT 'TaxForm.GoodServiceGUID replaces GooodsServicesID'
UPDATE f SET f.GoodServiceGUID = g.RowGUID
FROM tblTaxForms f
JOIN tblGoodsServices g ON g.GooodsServicesID = f.GooodsServicesID
WHERE f.GoodServiceGUID IS NULL

PRINT 'TaxForm.VendorGUID replaces VendorID'
UPDATE f SET f.VendorGUID = v.RowGUID
FROM tblTaxForms f
JOIN tblVendors v ON v.VendorID = f.VendorID
WHERE f.VendorGUID IS NULL

PRINT 'TaxForm.ReturnUserGUID replaces RetAgentID'
UPDATE f SET f.ReturnUserGUID = u.RowGUID
FROM tblTaxForms f
JOIN tblTaxFormAgents a ON a.AgentID = f.RetAgentID
JOIN tblUsers u ON u.UserID = a.UserID
WHERE f.ReturnUserGUID IS null

PRINT 'TaxForm.FileUserGUID replaces FileAgentID'
UPDATE f SET f.FileUserGUID = u.RowGUID
FROM tblTaxForms f
JOIN tblTaxFormAgents a ON a.AgentID = f.FileAgentID
JOIN tblUsers u ON u.UserID = a.UserID
WHERE f.FileUserGUID IS NULL

PRINT 'set TaxForm.LocationCode = CUST where not returned filed or voided'
UPDATE f SET f.LocationCode = 'CUST'
FROM tblTaxForms f
WHERE f.LocationCode is null 
and StatusFlags & (POWER(2,1) | POWER(2,2) | POWER(2,5)) = 0
--SELECT TOP 50 dbo.StatusFlagsBits_f(statusflags), *

PRINT 'set TaxForm.LocationCode = LOST by pulling from existing remarks'
UPDATE f SET f.LocationCode = 'LOST'
--SELECT TOP 50 f.OrderNumber, r.* 
FROM tblRemarks r 
JOIN tblTaxForms f ON f.TaxFormID = r.RowID AND r.TableID = 14
WHERE f.LocationCode is null
and r.Remarks LIKE '%lost%'

PRINT 'set TaxForm.LocationCode = TaxOfficeCode for everything else'
UPDATE f SET f.LocationCode = o.OfficeCode
FROM tblTaxForms f 
JOIN tblTaxOffices o ON f.TaxFormID BETWEEN o.TaxOfficeID AND o.TaxOfficeID + 1000000 - 1
WHERE f.LocationCode IS null
AND f.StatusFlags & (POWER(2,1) | POWER(2,2)) > 0



/***** Client & Sponsor table conversion must be run before TaxFormPackage and TaxForm due to dependencies on the new RowGUID's!!!!! */

-- TaxFormPackage.AuthorizedDependentClientGUID replaces ClientID... 
-- well not exactly... AuthorizedDependentClientGUID is now only populated if an authorized dependent was selected to be printed on the form
-- TaxFormPackage.SponsorClientGUID will always be populated with the sponsor
PRINT 'TaxFormPackage.SponsorClientGUID and .AuthorizedDependentClientGUID'
UPDATE p SET 
  p.SponsorClientGUID = sc.RowGUID,
  p.AuthorizedDependentClientGUID = CASE WHEN c.RowGUID <> sc.RowGUID then c.RowGUID ELSE NULL END --only fill out TaxFormPackage.AuthorizedDependentClientGUID when it's NOT the same as the SponsorClientGUID
FROM tblTaxFormPackages p
JOIN tblClients c ON c.ClientID = p.ClientID
JOIN tblClients sc ON sc.SponsorID = c.SponsorID AND sc.StatusFlags & POWER(2,0) = POWER(2,0)
WHERE p.SponsorClientGUID IS NULL

PRINT 'TaxFormPackage.SellUserGUID replaces AgentID'
UPDATE p SET 
  p.SellUserGUID = u.RowGUID
FROM tblTaxFormPackages p
JOIN tblTaxFormAgents a ON a.AgentID = p.AgentID
JOIN tblUsers u ON u.UserID = a.UserID
WHERE p.SellUserGUID IS NULL

PRINT 'TaxFormPackage.SellUserGUID = ''deleted user'' where NULL'
UPDATE tblTaxFormPackages SET SellUserGUID = '00000000-0000-0000-0000-000000000000' WHERE SellUserGUID IS null

-- 
PRINT 'Explicit TaxFormPackage.TaxOfficeID replaces dependency on AgentID... makes many queries and reports easier'
UPDATE p2 SET p2.TaxOfficeID = t.TaxOfficeID
FROM tblTaxFormPackages p2
JOIN (
SELECT p.PackageID, max(o.TaxOfficeID) AS TaxOfficeID
FROM tblTaxFormPackages p 
JOIN tblTaxOffices o ON p.PackageID > o.TaxOfficeID
WHERE p.TaxOfficeID IS NULL 
-- why did i do this??AND o.TaxOfficeID <> 1
GROUP BY p.PackageID
) t ON t.PackageID = p2.PackageID

PRINT 'Explicit TaxFormPackage.ExpirationDate just in case the 2yrs rule ever changes to something else'
UPDATE p SET 
  p.ExpirationDate = DATEADD(day, case f.FormTypeID when 2 then 90 ELSE 365 * 2 end, p.PurchaseDate)
FROM tblTaxFormPackages p
JOIN tblTaxForms f ON f.PackageGUID = p.RowGUID
WHERE p.ExpirationDate IS NULL

