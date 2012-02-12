--$Author: Brent.anderson2 $
--$Date: 5/05/10 4:33p $
--$Modtime: 5/05/10 4:32p $

/****** Object:  View [dbo].[FormPackageClient_v]    Script Date: 06/22/2009 08:23:48 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

/* testing

select top 5 * from FormPackageClient_v where used is not null and description is not null
description is not null
[form #] = 'NF1-RA-09-118124' 
*/

USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'FormPackageClient_v')
	exec('create VIEW FormPackageClient_v as select 1 as one')
GO
alter VIEW [dbo].[FormPackageClient_v]
AS

SELECT
  '(' + c.CCode + ') ' + c.LName + ', ' + c.FName as SponsorName,
  f.OrderNumber,
  f.RowGUID AS TaxFormGUID,
  f.StatusFlags AS FormStatusFlags,
  f.ReturnedDate,
  f.FiledDate,
  p.SponsorClientGUID,
  c.SponsorGUID
FROM iTRAAC.dbo.tblTaxForms f 
JOIN iTRAAC.dbo.tblTaxFormPackages p ON p.RowGUID = f.PackageGUID
JOIN iTRAAC.dbo.tblClients c ON c.RowGUID = p.SponsorClientGUID

GO

grant select on FormPackageClient_v to public
go

