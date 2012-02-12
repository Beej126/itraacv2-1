--$Author: Brent.anderson2 $
--$Date: 5/05/10 4:33p $
--$Modtime: 5/05/10 4:32p $

/****** Object:  View [dbo].[TaxForm_Status_v]    Script Date: 06/22/2009 08:23:48 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

/*testing
*/

USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'TaxForm_Status_v')
	exec('create VIEW TaxForm_Status_v as select 1 as one')
GO
alter VIEW [dbo].[TaxForm_Status_v]
AS

SELECT
  OrderNumber,
  RowGUID,
  PackageGUID,
  FormTypeID,
  StatusFlags,
  LocationCode,
  Incomplete,
  Printed,
  CASE WHEN Printed = 0 THEN 'Not Printed' ELSE [Status] END AS [Status], --for display
  CONVERT(BIT, CASE WHEN [Status] in ('Unreturned', 'Incomplete', 'Not Printed') THEN 1 ELSE 0 END) AS UnreturnedID
FROM (
  SELECT 
    f.OrderNumber,
    f.RowGUID,
    f.PackageGUID,
    f.FormTypeID,
    f.StatusFlags,
    f.LocationCode,
    f.Incomplete,
    CONVERT(BIT, CASE WHEN isnull(InitPrt215, InitPrtAbw) IS NOT NULL THEN 1 ELSE 0 END) as Printed,
    CASE
      WHEN f.StatusFlags & POWER(2, 5) = POWER(2, 5) THEN 'Voided'
      WHEN f.LocationCode = 'LOST' THEN 'LOST'
      WHEN f.StatusFlags & POWER(2, 2) = POWER(2, 2) THEN 'Filed'
      WHEN f.Incomplete = 1 THEN 'Incomplete'
      WHEN LEN(ISNULL(f.LocationCode,'')) = 2 THEN 'Returned' --if location == an office code -> then status = returned --this handles new v2 client data
      WHEN f.StatusFlags & POWER(2, 1) = POWER(2, 1) THEN 'Returned' -- and this catches old v1 client data, had to compromise that there will be two ways to represent returned while we're running in parallel
      ELSE 'Unreturned' END AS [Status]
  FROM iTRAAC.dbo.tblTaxForms f
) t

GO

grant select on TaxForm_Status_v to public
go

