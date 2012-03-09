use itraacv2
GO
/****** Object:  StoredProcedure [dbo].[Vendor_New]    Script Date: 10/17/2010 22:31:09 ******/

-- EXEC [Vendor_New] @TaxOfficeID = 10000001, @FirstName = 'Brenda', @LastName = 'Anderson', @Email = 'B@A.com', @DSNPhone = '1'
-- DELETE tblusers WHERE fname = 'brenda' AND lname = 'anderson'
-- delete tbltaxformagents where sigblock = 'anderson, brenda'

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
if not exists(select 1 from sysobjects where name = 'Vendor_New')
	exec('create PROCEDURE Vendor_New as select 1 as one')
GO
ALTER PROCEDURE [dbo].[Vendor_New]
@TaxOfficeId INT,
@VendorGUID UNIQUEIDENTIFIER = NULL OUT,
@VendorName VARCHAR(100),
@Line2 VARCHAR(100),
@Street VARCHAR(100),
@City VARCHAR(100),
@PLZ VARCHAR(100),
@Phone VARCHAR(50)
AS BEGIN

INSERT iTRAAC.dbo.tblvendors (TaxOfficeID, VendorName, Line2, Street, City, PLZ, Phone)
VALUES (@TaxOfficeId, @VendorName, @Line2, @Street, @City, @PLZ, @Phone)
SELECT @VendorGUID FROM iTRAAC.dbo.tblVendors WHERE VendorID = SCOPE_IDENTITY()

END
go

grant execute on [Vendor_New] to public
go
