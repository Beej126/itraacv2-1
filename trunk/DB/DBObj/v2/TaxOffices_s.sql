--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 4/30/10 4:48p $

/****** Object:  StoredProcedure [dbo].[TaxOffices_s]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

/* testing:
TaxOffices_s 1
*/

if not exists(select 1 from sysobjects where name = 'TaxOffices_s')
	exec('create PROCEDURE TaxOffices_s as select 1 as one')
GO
alter PROCEDURE [dbo].TaxOffices_s
@IncludeLocations BIT = 0
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

SELECT
  TaxOfficeId,
  o.TaxOfficeName + CASE WHEN o.LocationOnly = 0 THEN ' (' + o.OfficeCode + ')' ELSE '' END AS Office,
  o.Phone,
  o.OfficeHours,
  --poc.FName + ' ' + poc.LName + ISNULL(' (' + poc.Email + ')', '') AS POC,
  o.AgencyLine1,
  o.AgencyLine2,
  o.AgencyLine3,
  o.FormFootNoteLine1 AS AgencyLine4,
  o.OfficeCode,
  o.Active,
  o.POC_UserGUID --ISNULL(o.POC_UserGUID, '00000000-0000-0000-0000-000000000000') AS POC_UserGUID
FROM iTRAAC.dbo.tblTaxOffices o
LEFT JOIN iTRAAC.dbo.tblUsers poc ON poc.RowGUID = POC_UserGUID
WHERE o.OfficeCode NOT IN ('xx', 't1', 't2')
AND (@IncludeLocations = 1 OR o.LocationOnly = 0)
ORDER BY o.LocationOnly DESC, o.Active DESC, o.TaxOfficeName

END
GO

grant execute on TaxOffices_s to public
go

