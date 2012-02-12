--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:00p $

/* testing:
select f.* from itraac.dbo.tbltaxforms 
cross apply itraacv2.dbo.TaxForm_Status_f(statusflags, locationcode, incomplete, initprt215, initprtabw) f
where ordernumber = 'NF1-RA-09-50351'

select convert(bit, convert(bit, 1) | 0)
select power(2,5)
*/

USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'TaxForm_Status_f')
	exec('create FUNCTION TaxForm_Status_f() returns TABLE return select 1 as one')
GO
ALTER FUNCTION dbo.TaxForm_Status_f(
  @StatusFlags INT,
  @LocationCode VARCHAR(4),
  @Incomplete BIT,
  @InitPrt215 DATETIME,
  @InitPrtAbw DATETIME
) RETURNS TABLE --(RowGUID UNIQUEIDENTIFIER, [Status] VARCHAR(50), IsPrinted bit, IsUnreturned bit)
AS 

RETURN SELECT 
  -- choosing to hide voided forms in the "not printed" view since they're probably not what one is looking for
  -- CONVERT(BIT, CONVERT(BIT, COALESCE(@InitPrt215, @InitPrtAbw, 0)) | (@StatusFlags & POWER(2,5))) AS IsPrinted,
  CONVERT(BIT, COALESCE(@InitPrt215, @InitPrtAbw, 0)) AS IsPrinted,
  
  CONVERT(bit, CASE -- establishing a logical "Returned" flag, to consolidate the similar "not outstanding" kinds of status, when form is...
    WHEN @Incomplete = 1 -- Incomplete
    OR @LocationCode = 'LOST' -- OR LOST
    OR @StatusFlags & 38 > 0 -- OR returned, filed or voided
    THEN 0 ELSE 1 END) AS IsUnReturned, --flip from Returned to UN-returned here at the end
    
  CASE
    WHEN @StatusFlags & POWER(2, 5) = POWER(2, 5) THEN 'Voided'
    WHEN isnull(@InitPrt215, @InitPrtAbw) IS NULL THEN 'Not Printed'
    WHEN @LocationCode = 'LOST' THEN 'LOST'
    WHEN @StatusFlags & POWER(2, 2) = POWER(2, 2) THEN 'Filed'
    WHEN @Incomplete = 1 THEN 'Incomplete'
    WHEN LEN(ISNULL(@LocationCode,'')) = 2 THEN 'Returned' --if location == an office code -> then status = returned --this handles new v2 client data
    WHEN @StatusFlags & POWER(2, 1) = POWER(2, 1) THEN 'Returned' -- and this catches old v1 client data, had to compromise that there will be two ways to represent returned while we're running in parallel
    ELSE 'Unreturned' END AS [Status]

go

GRANT SELECT ON dbo.TaxForm_Status_f TO PUBLIC
go

