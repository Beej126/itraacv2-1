--$Author: Brent.anderson2 $
--$Date: 7/31/09 10:06a $
--$Modtime: 7/31/09 10:04a $
--$History: Admin_Duplicate_Customer_Cleanup.sql $
-- 
-- *****************  Version 3  *****************
-- User: Brent.anderson2 Date: 7/31/09    Time: 10:06a
-- Updated in $/iTRAAC/iTRAACv1/DB/DBobj/procs
-- tweaked to exclude de-active dupes because those represent the divorced
-- spouse and other scenarios where there is often a new independent
-- sponsor record with the same fname + lname + ccode
-- 
-- *****************  Version 2  *****************
-- User: Brent.anderson2 Date: 7/23/09    Time: 5:03p
-- Updated in $/iTRAAC/iTRAACv1/DB/DBobj/procs
-- 
-- *****************  Version 1  *****************
-- User: Brent.anderson2 Date: 7/23/09    Time: 5:01p
-- Created in $/iTRAAC/iTRAACv1/DB/DBobj/procs

if not exists(select 1 from sysobjects where name = 'Admin_Duplicate_Customer_Cleanup')
	exec('create PROCEDURE Admin_Duplicate_Customer_Cleanup as select 1 as one')
GO
ALTER PROC Admin_Duplicate_Customer_Cleanup
AS BEGIN

SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

------------------------------------------------------------------------------------------------------
-- elminate dupes identified as same: SponsorID, LName, FName
------------------------------------------------------------------------------------------------------
DECLARE @t TABLE (/*TaxOffice varchar(100),*/ LName varchar(50), FName varchar(50), SponsorID int, ClientID int, CCode VARCHAR(5), FormCount int)

INSERT @t (LName, FName, SponsorID, ClientID, FormCount)
SELECT
  --dbo.TaxOfficeName_f(c.ClientID) AS TaxOffice,
  c.LName,
  c.FName,
  c.SponsorID,
  c.ClientID,
  ISNULL(COUNT(DISTINCT f.OrderNumber),0) AS FormCount
FROM (
  SELECT top 100 percent 
    SponsorID, LName, FName, COUNT(1) AS [Count]
  FROM tblClients
  WHERE Active = 1
  AND CCode <> 'X0000' -- X0000 seems to be some kind of poorly implemeted "hide" bucket
  GROUP BY SponsorID, LName, FName
  HAVING COUNT(1) > 1
  ORDER BY COUNT(1) desc  
) d
JOIN tblClients c ON c.SponsorID = d.SponsorID AND c.LName = d.LName AND c.FName = d.FName
LEFT JOIN tblTaxFormPackages p ON p.ClientID = c.ClientID
LEFT JOIN tblTaxForms f ON f.PackageID = p.PackageID
GROUP BY
  --dbo.TaxOfficeName_f(c.SponsorID), 
  c.SponsorID,
  c.LName,
  c.FName,
  c.ClientID
  
IF (OBJECT_ID('tempdb..#_customer_dupes') IS NOT NULL) DROP TABLE #_customer_dupes

SELECT
  c1.SponsorID as KeepSponsorID,
  c1.ClientID as KeepClientID,
  c2.SponsorID as KillSponsorID,
  c2.ClientID as KillClientID
into #_customer_dupes
FROM @t c1
JOIN @t c2 ON c2.SponsorID = c1.SponsorID AND c2.LName = c1.LName AND c2.FName = c1.FName AND c2.ClientID <> c1.ClientID
ORDER BY c1.LName, c1.FName, c1.FormCount DESC, c2.FormCount ASC -- choose the Client record with smaller formcounts to move over to the one with larger


exec Admin_Duplicate_Customer_Loop

truncate table #_customer_dupes
DELETE @t

------------------------------------------------------------------------------------------------------
-- elminate dupes identified as same: CCode, LName, FName
------------------------------------------------------------------------------------------------------
INSERT @t (LName, FName, CCode, SponsorID, ClientID, FormCount)
SELECT
  --dbo.TaxOfficeName_f(c.ClientID) AS TaxOffice,
  c.LName, c.FName,
  c.CCode,
  c.SponsorID,
  c.ClientID,
  ISNULL(COUNT(DISTINCT f.OrderNumber),0) AS FormCount
FROM (
  SELECT top 100 percent
    CCode, LName, FName, COUNT(1) AS [Count]
  FROM tblClients
  WHERE Active = 1
  AND CCode <> 'X0000'
  GROUP BY CCode, LName, FName
  HAVING COUNT(1) > 1
  ORDER BY COUNT(1) desc, CCode
) d
JOIN tblClients c ON c.CCode = d.CCode AND c.LName = d.LName AND c.FName = d.FName
LEFT JOIN tblTaxFormPackages p ON p.ClientID = c.ClientID
LEFT JOIN tblTaxForms f ON f.PackageID = p.PackageID
GROUP BY
  --dbo.TaxOfficeName_f(c.ClientID),
  c.CCode,
  c.LName, 
  c.FName,
  c.SponsorID,
  c.ClientID

insert #_customer_dupes
SELECT
  c1.SponsorID,
  c1.ClientID,
  c2.SponsorID,
  c2.ClientID
FROM @t c1
JOIN @t c2 ON c2.CCode = c1.CCode AND c2.LName = c1.LName AND c2.FName = c1.FName AND c2.ClientID <> c1.ClientID
ORDER BY c1.CCode, c1.LName, c1.FName, c1.FormCount DESC, c2.FormCount asc



exec Admin_Duplicate_Customer_Loop

truncate table #_customer_dupes

end
go


/*
-- *** run a secondary cleanup that pulled duplicates with same first + last under consolidated sponsor with different CCodes -->383, cleaned up to 0!!
-- ** so there could very well be dupes with different CCodes... no real way to tell those other than case by case basis
select top 5 * from tblclients where sponsorid in (
select top 100 percent sponsorid, lname, fname, count(1)
from tblclients
group by sponsorid, lname, fname
having count(1)>1
order by count(1) desc
)

-- ** run report on any taxpackages that aren't connected to a client  --> 153
select dbo.TaxOfficeName_f(p.ClientID), count(1) 
from tblTaxFormPackages p 
WHERE NOT EXISTS(SELECT 1 FROM tblclients c WHERE c.clientid = p.clientid) 
group by dbo.TaxOfficeName_f(p.ClientID)
order by count(1) desc

--Hohenfels	77
--Grafenwoehr	14
--Hanau	11
--Darmstadt	9
--Heidelberg	8
--Spangdahlem	7
--Bamberg	7
--Stuttgart	6
--Mannheim	4
--Friedberg	4
--*Central*	3
--Ramstein	2
--Baumholder	1

-- **** run a report on tblSponsors that don't have any tblClient records --> -->4206!!!! cleaned up to 0
INSERT [_deleted_dupe_sponsors] 
select * /*delete s*/ from tblSponsors s where not exists(select 1 from tblClients c where c.SponsorID = s.SponsorID) 
select * from tblclients where sponsorid = 10003530

-- **** clean up tblClient records with same SponsorID and >1 claiming the Sponsor status flag... awesome, only 5 rows that way
select SponsorID, count(1)
from tblClients
where ccode not like '_0000' and sponsorid <> 0
and StatusFlags & 1 = 1
group by SponsorID
having count(1) >1
order by count(1) desc


select * from #_customer_dupes where count >2 order by count desc 

SELECT TOP 20 * 
FROM #counts c1
JOIN #counts c2 ON c2.SponsorID = c1.SponsorID AND c2.FullName = c1.FullName and c2.ClientID <> c1.ClientID AND c2.Processed = 0 --AND c2.Active = 1
WHERE c1.Processed = 0 --and c1.ccode = 'V2958'
ORDER BY c1.FullName, c1.FormCount DESC, c2.FormCount ASC

SELECT c2.*, c1.*
FROM tblClients c1
JOIN tblClients c2 ON c2.SponsorID = c1.SponsorID AND c2.ClientID <> c1.ClientID
WHERE c1.CCode = 'x0000' 

SELECT
  dbo.TaxOfficeName_f(c.ClientID) AS TaxOffice,
  c.FName+' '+c.LName AS FullName,
  c.SponsorID,
  c.ClientID,
  ISNULL(COUNT(DISTINCT p.PackageID),0) AS PackageCount,
  ISNULL(COUNT(DISTINCT f.OrderNumber),0) AS FormCount,
  convert(bit, 0) as Processed
FROM #_customer_dupes d
JOIN tblClients c ON c.SponsorID = d.SponsorID AND c.LName = d.LName AND c.FName = d.FName
LEFT JOIN tblTaxFormPackages p ON p.ClientID = c.ClientID
LEFT JOIN tblTaxForms f ON f.PackageID = p.PackageID
GROUP BY
  dbo.TaxOfficeName_f(c.SponsorID),
  c.FName+' '+c.LName,
  c.SponsorID,
  c.ClientID


*/