--$Author: Brent.anderson2 $
--$Date: 5/05/10 4:33p $
--$Modtime: 5/05/10 4:32p $

/****** Object:  View [dbo].[TaxForm_FriendlyStatus_v]    Script Date: 06/22/2009 08:23:48 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

/*testing
select top 5 * from TaxForm_FriendlyStatus_v where used is not null and description is not null
description is not null
[form #] = 'NF1-RA-09-118124' 
*/

USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'TaxForm_FriendlyStatus_v')
	exec('create VIEW TaxForm_FriendlyStatus_v as select 1 as one')
GO
alter VIEW [dbo].[TaxForm_FriendlyStatus_v]
AS

SELECT 
  OrderNumber,
  RowGUID,
  PackageGUID,
  CASE
    WHEN f.StatusFlags & POWER(2,23) <> 0 THEN 'Incomplete'
    WHEN f.StatusFlags & POWER(2, 5) <> 0 THEN 'Voided'
    WHEN f.StatusFlags & POWER(2, 2) <> 0 THEN 'Filed'
    WHEN f.StatusFlags & POWER(2, 1) <> 0 THEN 'Returned'
    ELSE 'Unreturned' END AS [Status]
FROM iTRAAC.dbo.tblTaxForms f 

GO

grant select on TaxForm_FriendlyStatus_v to public
go

