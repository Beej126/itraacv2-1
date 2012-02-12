--$Author: Brent.anderson2 $
--$Date: 5/05/10 4:33p $
--$Modtime: 5/05/10 4:25p $

/* testing:
exec GoodsAndServices_Search @SearchType='either', @SearchName = 'hotel'
select top 5 * from tblGoodsServices
select top 5 * from tblGoodsServiceTypes
*/
/****** Object:  StoredProcedure [dbo].[GoodsAndServices_Search]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'GoodsAndServices_Search')
	exec('create PROCEDURE GoodsAndServices_Search as select 1 as one')
GO
alter PROCEDURE [dbo].[GoodsAndServices_Search] 
@SearchType VARCHAR(50),
@SearchName VARCHAR(100)
AS BEGIN

SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

select @SearchType = GSTypeID from iTRAAC.dbo.tblGoodsServiceTypes where GSType = @SearchType
IF @@ROWCOUNT = 0 SET @SearchType = null
SET @SearchType = iTRAAC.dbo.fn_DynParm('AND Type = ''?''', @SearchType)
SET @SearchName = iTRAAC.dbo.fn_DynParm('AND (GoodsServiceName like ''%?%'' or Description like ''%?%'')', @SearchName)

--debug: PRINT '@VendorName: '+ISNULL(@VendorName, 'NULL')
--debug: PRINT '@VendorCity: '+ISNULL(@VendorCity, 'NULL')


EXEC ("
SELECT GoodsServicesID, GoodsServiceName, Description
FROM iTRAAC.dbo.tblGoodsServices
where Active = 1
"
+@SearchType
+@SearchName
)

END
GO

grant execute on GoodsAndServices_Search to public
go

