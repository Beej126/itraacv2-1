--$Author: Brent.anderson2 $
--$Date: 11/24/09 5:03p $
--$Modtime: 11/23/09 5:04p $

/* testing:
exec Vendor_s @VendorName = '', @VendorCity = null 
*/
/****** Object:  StoredProcedure [dbo].[Vendor_s]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'Vendor_s')
	exec('create PROCEDURE Vendor_s as select 1 as one')
GO
alter PROCEDURE [dbo].[Vendor_s] 
@VendorID int
AS BEGIN

SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

SELECT
  VendorID,
  VendorName,
  Line2,
  Street,
  City,
  PLZ,
  Phone,
  isnull(AddressLine1 + ', ', '') + isnull(AddressLine2 + ', ', '') + isnull(AddressLine3 + ', ', '') as Legacy 
FROM iTRAAC.dbo.tblVendors
where VendorID = @VendorID

END
GO

grant execute on Vendor_s to public
go

