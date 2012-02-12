--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:00p $

/* testing:
*/
USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'TaxForm_OrderNumberBase')
	exec('create FUNCTION TaxForm_OrderNumberBase() returns int begin return 0 end')
GO
ALTER FUNCTION dbo.TaxForm_OrderNumberBase(@FormTypeID INT, @TaxOfficeCode VARCHAR(2))
RETURNS VARCHAR(16)
AS BEGIN

DECLARE @OrderNumber VARCHAR(16)
  -- e.g. NF1-BU-07-00447
SET @OrderNumber = (
  SELECT CodeName FROM iTRAAC.dbo.tblTaxFormTypes (NOLOCK) 
  WHERE FormTypeID = @FormTypeID) + '-' + 
                     @TaxOfficeCode + '-' + 
                     CONVERT(VARCHAR, dbo.FiscalYear_f(null)) + '-'
  
RETURN @OrderNumber

END
go

GRANT EXECUTE ON dbo.TaxForm_OrderNumberBase TO PUBLIC
go
