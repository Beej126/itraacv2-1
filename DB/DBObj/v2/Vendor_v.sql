--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 4/29/10 4:40p $

/****** Object:  View [dbo].[Vendor_v]    Script Date: 06/22/2009 08:23:48 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'Vendor_v')
	exec('create VIEW Vendor_v as select 1 as one')
GO
alter VIEW [dbo].[Vendor_v]
AS

SELECT
  RowGUID,
  VendorName + ISNULL(' ('+City+isnull(', '+Street, '')+' )', '') AS ShortDescription
from iTRAAC.dbo.tblVendors

GO

grant select on Vendor_v to public
go

