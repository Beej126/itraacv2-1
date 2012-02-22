--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 4/30/10 2:00p $

/* testing:
exec Vendor_Search @VendorName = 'heidel', @VendorName_SearchType='contains', @VendorCity = null, @VendorCity_SearchType='begins with'
select top 5 * from tblvendors where plz is not null
*/
/****** Object:  StoredProcedure [dbo].[Vendor_Search]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'Vendor_Search')
	exec('create PROCEDURE Vendor_Search as select 1 as one')
GO
alter PROCEDURE [dbo].[Vendor_Search] 
@VendorName VARCHAR(100),
@VendorName_SearchType VARCHAR(50),
@VendorCity VARCHAR(100),
@VendorCity_SearchType varchar(50)
with execute as owner
AS BEGIN

SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

SET @VendorName = @VendorName + '%'
IF (@VendorName_SearchType = 'contains') SET @VendorName = '%'+@VendorName
--SET @VendorName = iTRAAC.dbo.fn_DynParm('AND VendorName like ''?%''', @VendorName)

IF (@VendorCity_SearchType = 'contains') SET @VendorCity = '%'+@VendorCity
SET @VendorCity = iTRAAC.dbo.fn_DynParm('AND City like ''?%''', @VendorCity)

--debug: PRINT '@VendorName: '+ISNULL(@VendorName, 'NULL')
--debug: PRINT '@VendorCity: '+ISNULL(@VendorCity, 'NULL')


--EXEC ("
--SELECT
--  VendorName,
--  [Address],
--  ShortDescription,
--  RowGUID
--FROM Vendor_Address_v
--where Active = 1
--"
--+@VendorName
--+@VendorCity
--)

SELECT
  VendorName,
  [Address],
  ShortDescription,
  RowGUID
FROM Vendor_Address_v
where Active = 1
AND VendorName LIKE @VendorName

END
GO

grant execute on Vendor_Search to public
go

