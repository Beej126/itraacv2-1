--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:01p $

/****** Object:  StoredProcedure [dbo].[TaxForm_s]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO


/* testing

select top 5 * from tbltaxforms where rowguid = '3d2676e1-7cee-4edc-ae0b-f0155c1e7653'
select * from tblRemarks where rowid = 270003525 --need to write conversion for tblRemarks to extended tax form tables and that will have to run in an ongoing loop during rollout

select * from tbltaxforms where ordernumber = 'NF1-RA-07-29410'

declare @TableNames varchar(100)
exec TaxForm_s @GUID='96A8BC5C-063D-4163-9BCD-8D57201F2458', @TableNames=@TableNames out
select @TableNames as TableNames

select itraac.dbo.statusflags_f(671088675)

*/


if not exists(select 1 from sysobjects where name = 'TaxForm_s')
	exec('create PROCEDURE TaxForm_s as select 1 as one')
GO
alter PROCEDURE [dbo].[TaxForm_s] 
@GUID varchar(50) OUT,
@UserGUID UNIQUEIDENTIFIER = NULL,
@TaxOfficeCode varchar(2) = NULL,
@TableNames varchar(1000) = null OUT
WITH EXECUTE AS OWNER 
AS BEGIN

-- little switcheroo to support firing up TaxForms based on OrderNumber as well as GUID
-- the initial driving scenario is Remark comments... iTRAAC v2 UI automatically hyperlinks anything matching the OrderNumber format in the Remarks text... it's handy
IF (dbo.IsGUID(@GUID) = 0)
  SELECT @GUID = RowGUID FROM iTRAAC.dbo.tblTaxForms WHERE OrderNumber = @GUID

SET @TableNames = 'TaxForm'
-- PRINT '@TableNames: ' + ISNULL(@TableNames, 'NULL')

SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

-- *NOTE* the column names returned here must necessarily line up with the columns returned from TaxForm_Return

-- TaxForm table
SELECT 
  f.RowGUID,

  f.OrderNumber,
  --p.PackageCode,
  p.ExpirationDate,
  p.TaxOfficeID,

  sc.SponsorGUID,
  sc.LName + ', ' + sc.FName AS SponsorName,
  sc.CCode AS SponsorCCode,
  c.LName + ', ' + c.FName + ' (' + c.CCode + ')' AS AuthorizedDependent,

  f.StatusFlags,
  s.[Status],
  f.Incomplete,
  f.LocationCode,
  f.FormTypeID,
  f.InitPrt215,
  f.InitPrtAbw,

  p.PurchaseDate,
  iu.FName + ' ' + iu.LName AS IssuedBy,
  f.ReturnedDate,
  f.ReturnUserGUID,
  ru.FName + ' ' + ru.LName AS ReturnedBy,
  f.FiledDate,
  f.FileUserGUID,
  fu.FName + ' ' + fu.LName AS FiledBy,

  f.TransTypeID,

  f.VendorGUID,
  v.ShortDescription AS Vendor,

  f.UsedDate,
  d.TotalCost,
  CASE d.CurrencyUsed WHEN 1 THEN 'USD' WHEN 2 THEN 'Euro' ELSE 'Unknown' END AS [Currency],
  d.CurrencyUsed,
  d.CheckNumber,
  f.[Description]

FROM iTRAAC.dbo.tblTaxForms f 
JOIN iTRAAC.dbo.tblTaxFormPackages p ON p.RowGUID = f.PackageGUID

CROSS APPLY dbo.TaxForm_Status_f(f.StatusFlags, f.LocationCode, f.Incomplete, f.InitPrt215, f.InitPrtAbw) s

JOIN iTRAAC.dbo.tblClients sc ON sc.RowGUID = p.SponsorClientGUID
LEFT JOIN iTRAAC.dbo.tblClients c ON c.RowGUID = p.AuthorizedDependentClientGUID

LEFT JOIN iTRAAC.dbo.tblPPOData d on d.TaxFormGUID = f.RowGUID
LEFT JOIN Vendor_v v ON v.RowGUID = f.VendorGUID
JOIN iTRAAC.dbo.tblUsers iu ON iu.RowGUID = p.SellUserGUID
LEFT JOIN iTRAAC.dbo.tblUsers ru ON ru.RowGUID = f.ReturnUserGUID
LEFT JOIN iTRAAC.dbo.tblUsers fu ON fu.RowGUID = f.FileUserGUID

WHERE f.RowGUID = @GUID


IF (@GUID IS NULL) RETURN --gets called this way from TransactionTypes_s

DECLARE @TransactionType VARCHAR(50), @ViolationRemarkGUID UNIQUEIDENTIFIER
SELECT 
  @TransactionType = 'TaxForm_' + CASE TransTypeID
    WHEN 29 THEN 'Weapon' 
    WHEN 31 THEN 'Vehicle'
    ELSE NULL end
FROM iTRAAC.dbo.tblTaxForms WHERE RowGUID = @GUID

-- TaxForm_Weapon or TaxForm_Vehicle table 
IF (@TransactionType IS NOT NULL)
BEGIN
  SET @TableNames = @TableNames + ',' + @TransactionType
  --exec("select * into #t from " + @TransactionType + " where TaxFormGUID = '" + @GUID + "' if @@ROWCOUNT = 0 insert #t values (newid(), '" + @GUID + "', '','','','') select * from #t ")
  exec("select * from " + @TransactionType + " where TaxFormGUID = '" + @GUID + "'")
END

-- TaxForm_Remark table
SET @TableNames = @TableNames + ',TaxForm_Remark' 
EXEC TaxForm_Remarks @TaxFormGUID = @GUID

IF (@GUID = dbo.EmptyGUID()) BEGIN
  SET @TableNames = @TableNames + ',TaxForm'
  SET @GUID = NEWID()
  
  -- new nf2 defaults
  SELECT
    @GUID AS RowGUID,
    dbo.TaxForm_OrderNumberBase(2, @TaxOfficeCode) + '{pending}' AS OrderNumber,
    2 AS FormTypeId,
    GETDATE() AS PurchaseDate,
    DATEADD(YEAR, 2, GETDATE()) AS ExpirationDate,
    (SELECT FName + ' ' + LName FROM iTRAAC.dbo.tblUsers WHERE RowGUID = @UserGUID) AS IssuedBy
END


END
GO

grant execute on TaxForm_s to public
go
