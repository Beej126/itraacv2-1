--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:00p $

/* testing:
select dbo.Sponsor_ViolationInfo_f('2/1/2100')

SELECT COUNT(1), c.SponsorGUID
FROM tblClients c
JOIN tblRemarks r ON r.FKRowGUID = c.RowGUID
WHERE r.RemType IN (6, 10, 20, 21)
GROUP BY c.SponsorGUID
HAVING COUNT(1) > 1
ORDER BY COUNT(1) desc
*/
USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'Sponsor_ViolationInfo_f')
	exec('create FUNCTION Sponsor_ViolationInfo_f() returns @dummy TABLE(dummy int) begin return end')
GO
ALTER FUNCTION dbo.Sponsor_ViolationInfo_f(@SponsorGUID UNIQUEIDENTIFIER)
RETURNS @ViolationInfo TABLE(ViolationCountTotal INT, ViolationCountInLast3Years int, ViolationCountUnresolved int)
AS BEGIN

INSERT @ViolationInfo
SELECT
  SUM(1),
  SUM(CASE WHEN ISNULL(CreateDate, LastUpdate) > DATEADD(YEAR, -3, GETDATE()) THEN 1 ELSE 0 END),
  SUM(CASE WHEN StatusFlags & POWER(2,12) > 0 THEN 1 ELSE 0 END)
FROM iTRAAC.dbo.tblRemarks
WHERE RemType IN (SELECT RemarkTypeId FROM RemarkType WHERE CategoryId = 'FORM_VIOLATION'
                  UNION ALL SELECT 6) -- we're making suspends synonymous with violations
AND FKRowGUID IN (SELECT RowGUID FROM dbo.Members_All_f(@SponsorGUID))
AND DeleteReason IS NULL

RETURN 

END
go

GRANT select ON dbo.Sponsor_ViolationInfo_f TO PUBLIC
go
