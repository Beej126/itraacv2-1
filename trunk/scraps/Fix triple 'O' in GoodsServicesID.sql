/*
xxx garrison servers don't have this one, only central: cp_InsertRowCounts
cp_parmdel_iTRAAC_GoodsService
cp_parmins_iTRAAC_GoodsService
cp_parmins_iTRAAC_TaxForm
cp_parmsel_GoodsServices
cp_parmsel_iTRAAC_GoodsService
cp_parmsel_iTRAAC_GoodsServices
cp_parmsel_iTRAAC_TaxForm
cp_parmsel_iTRAAC_TaxForms
cp_parmsel_iTRAAC_TaxForms
cp_parmsel_NotFiled
cp_parmsel_SessionDetails
cp_parmupd_iTRAAC_GoodsService
cp_parmupd_iTRAAC_TaxForm
ifnPurchaseOrders
vw_ABW
vw_EF1
vw_EF2
vw_NF
vw_NFx
vw_Report_1
*/


USE [iTRAAC]
GO

/****** Object:  StoredProcedure [dbo].[cp_InsertRowCounts]    Script Date: 06/10/2010 14:33:53 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

alter procedure [dbo].[cp_InsertRowCounts] AS
DECLARE @DT DATETIME
SELECT @DT = GETDATE()

INSERT INTO tblRowCounts
SELECT COUNT(TaxFormID), T.TaxOfficeID, 14, @DT FROM tblTaxOffices T
INNER JOIN tblTaxForms F ON F.TaxFormID BETWEEN T.TaxOfficeID AND T.TaxOfficeID+9999999
WHERE T.Active=1 GROUP BY T.TaxOfficeID

INSERT INTO tblRowCounts
SELECT COUNT(PackageID), T.TaxOfficeID, 13, @DT FROM tblTaxOffices T
INNER JOIN tblTaxFormPackages X ON X.PackageID BETWEEN T.TaxOfficeID AND T.TaxOfficeID+9999999
WHERE T.Active=1 GROUP BY T.TaxOfficeID

INSERT INTO tblRowCounts
SELECT COUNT(PPODataID), T.TaxOfficeID, 15, @DT FROM tblTaxOffices T
INNER JOIN tblPPOData X ON X.PPODataID BETWEEN T.TaxOfficeID AND T.TaxOfficeID+9999999
WHERE T.Active=1 GROUP BY T.TaxOfficeID

INSERT INTO tblRowCounts
SELECT COUNT(ClientID), T.TaxOfficeID, 10, @DT FROM tblTaxOffices T
INNER JOIN tblClients X ON X.ClientID BETWEEN T.TaxOfficeID AND T.TaxOfficeID+9999999
WHERE T.Active=1 GROUP BY T.TaxOfficeID

INSERT INTO tblRowCounts
SELECT COUNT(SponsorID), T.TaxOfficeID, 10, @DT FROM tblTaxOffices T
INNER JOIN tblSponsors X ON X.SponsorID BETWEEN T.TaxOfficeID AND T.TaxOfficeID+9999999
WHERE T.Active=1 GROUP BY T.TaxOfficeID

INSERT INTO tblRowCounts
SELECT COUNT(VendorID), T.TaxOfficeID, 3, @DT FROM tblTaxOffices T
INNER JOIN tblVendors X ON X.VendorID BETWEEN T.TaxOfficeID AND T.TaxOfficeID+9999999
WHERE T.Active=1 GROUP BY T.TaxOfficeID

INSERT INTO tblRowCounts
SELECT COUNT(GoodsServicesID), T.TaxOfficeID, 4, @DT FROM tblTaxOffices T
INNER JOIN tblGoodsServices X ON X.GoodsServicesID BETWEEN T.TaxOfficeID AND T.TaxOfficeID+9999999
WHERE T.Active=1 GROUP BY T.TaxOfficeID

/*

10          tblClients
4           tblGoodsServices	
15          tblPPOData
9           tblSponsors
13          tblTaxFormPackages
14          tblTaxForms
3           tblVendors

17          tblAccessControl
8           tblAttributes
20          tblBoxes
6           tblOfficeManagers
11          tblRemarks
5           tblTaxFormAgents
1           tblUsers

*/

-- TRUNCATE TABLE tblRowCounts
-- SELECT * FROM tblRowCounts

GO

/****** Object:  StoredProcedure [dbo].[cp_parmdel_iTRAAC_GoodsService]    Script Date: 06/10/2010 14:33:53 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

alter procedure [dbo].[cp_parmdel_iTRAAC_GoodsService]

	@GoodsServicesID int

AS

DELETE

FROM 
	tblGoodsServices

WHERE 
	(tblGoodsServices.GoodsServicesID = @GoodsServicesID)

IF @@ERROR <> 0
	RETURN 1

GO

/****** Object:  StoredProcedure [dbo].[cp_parmins_iTRAAC_GoodsService]    Script Date: 06/10/2010 14:33:53 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

alter procedure [dbo].[cp_parmins_iTRAAC_GoodsService]

	@TaxOfficeID int,
	@GoodsServiceName varchar(50),
	@Description varchar(255),
	@Type smallint,
	@Active bit,
	@StatusFlags int,
	@GoodsServicesID int output

AS

INSERT INTO tblGoodsServices (
	TaxOfficeID,
	GoodsServiceName,
	Description,
	Type,
	Active,
	StatusFlags
)

VALUES (
	@TaxOfficeID,
	@GoodsServiceName,
	@Description,
	@Type,
	@Active,
	@StatusFlags
)

SET 
	@GoodsServicesID = @@IDENTITY

IF @@ERROR <> 0
	RETURN 1

GO

USE [iTRAAC]
GO
/****** Object:  StoredProcedure [dbo].[cp_parmins_iTRAAC_TaxForm]    Script Date: 06/10/2010 14:47:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[cp_parmins_iTRAAC_TaxForm]

	@OrderNumber varchar(16),
	@CtrlNumber int,
	@PackageID int,
	@FormTypeID int,
	@TransTypeID int,
	@GoodsServicesID int,
	@VendorID int,
	@StatusFlags int,
	@Description varchar(170),
	@RetAgentID int,
	@FileAgentID int,
	@TaxFormID int output

AS

INSERT INTO tblTaxForms (
	OrderNumber,
	CtrlNumber,
	PackageID,
	FormTypeID,
	TransTypeID,
	GoodsServicesID,
	VendorID,
	StatusFlags,
	[Description],
	RetAgentID,
	FileAgentID
)

VALUES (
	@OrderNumber,
	@CtrlNumber,
	@PackageID,
	@FormTypeID,
	@TransTypeID,
	@GoodsServicesID,
	@VendorID,
	@StatusFlags,
	@Description,
	@RetAgentID,
	@FileAgentID
)

SET 
	@TaxFormID = @@IDENTITY


IF @FormTypeID<>3 BEGIN
    -- Add a record to tblPPOData for this Priced Purchase Order
    INSERT INTO tblPPOData (
    	TaxFormID,
    	TotalCost,
    	CheckNumber,
    	CurrencyUsed
    )
    
    VALUES (
    	@TaxFormID,
    	0,
    	'-',
    	0
    )
END


IF @@ERROR <> 0
	RETURN 1


/****** Object:  StoredProcedure [dbo].[cp_parmsel_GoodsServices]    Script Date: 06/10/2010 14:33:53 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

alter procedure [dbo].[cp_parmsel_GoodsServices]

	@TaxOfficeID INT

AS

  SELECT 
	GoodsServicesID,
	GoodsServiceName

  FROM 
	tblGoodsServices GS 
		INNER JOIN tblGoodsServiceTypes GST 
			ON GS.Type=GST.GSTypeID
		INNER JOIN tblTaxOffices O 
			ON GS.TaxOfficeID=O.TaxOfficeID

  WHERE 
	O.TaxOfficeID = @TaxOfficeID

  ORDER BY GoodsServiceName


GO

/****** Object:  StoredProcedure [dbo].[cp_parmsel_iTRAAC_GoodsService]    Script Date: 06/10/2010 14:33:53 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

alter procedure [dbo].[cp_parmsel_iTRAAC_GoodsService]

	@GoodsServicesID int,
	@TaxOfficeID int output,
	@GoodsServiceName varchar(50) output,
	@Description varchar(255) output,
	@Type smallint output,
	@Active bit output,
	@StatusFlags int output

AS

SELECT
	@TaxOfficeID = tblGoodsServices.TaxOfficeID,
	@GoodsServiceName = tblGoodsServices.GoodsServiceName,
	@Description = tblGoodsServices.Description,
	@Type = tblGoodsServices.Type,
	@Active = tblGoodsServices.Active,
	@StatusFlags = tblGoodsServices.StatusFlags

FROM 
	tblGoodsServices

WHERE 
	(tblGoodsServices.GoodsServicesID = @GoodsServicesID)

IF @@ROWCOUNT = 0
	RETURN 1

GO

/****** Object:  StoredProcedure [dbo].[cp_parmsel_iTRAAC_GoodsServices]    Script Date: 06/10/2010 14:33:53 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

alter procedure [dbo].[cp_parmsel_iTRAAC_GoodsServices]

	@TaxOfficeID INT

AS

IF @TaxOfficeID > 10000000 BEGIN
  SELECT 
	GoodsServicesID,
	GoodsServiceName,
	GSType,
	Description,
	TaxOfficeName,
	CONVERT(CHAR(1), GS.Active)+','+
	CONVERT(VARCHAR(10), StatusFlags)+','+ 
	CONVERT(VARCHAR(10), Type) AS Tag

  FROM 
	tblGoodsServices GS 
		INNER JOIN tblGoodsServiceTypes GST 
			ON GS.Type=GST.GSTypeID
		INNER JOIN tblTaxOffices O 
			ON GS.TaxOfficeID=O.TaxOfficeID

  WHERE 
	O.TaxOfficeID = @TaxOfficeID

  ORDER BY GoodsServiceName

END ELSE BEGIN
  SELECT DISTINCT
	GoodsServicesID,
	GoodsServiceName+' ('+OfficeCode+')',
	GSType,
	Description,
	TaxOfficeName,
	CONVERT(CHAR(1), GS.Active)+','+
	CONVERT(VARCHAR(10), StatusFlags)+','+ 
	CONVERT(VARCHAR(10), Type) AS Tag


  FROM 
	tblGoodsServices GS 
		INNER JOIN tblGoodsServiceTypes GST 
			ON GS.Type=GST.GSTypeID
		INNER JOIN tblTaxOffices O 
			ON GS.TaxOfficeID=O.TaxOfficeID

  ORDER BY GoodsServiceName+' ('+OfficeCode+')'

END








GO

/****** Object:  StoredProcedure [dbo].[cp_parmsel_iTRAAC_TaxForm]    Script Date: 06/10/2010 14:33:53 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO


--drop existing PK_tblPPOData clustered
--rebuild it as simply non-clustered on PPODataID 
--drop existing IX_TaxFormID
--rebuild it is CLUSTERED on TaxFormID, TotalCost, CheckNumber, CurrencyUsed, PPODataID 

alter procedure [dbo].[cp_parmsel_iTRAAC_TaxForm]
	@TaxFormID int,
	@OrderNumber varchar(16) output,
	@CtrlNumber int output,
	@PackageID int output,
	@FormTypeID int output,
	@TransTypeID int output,
	@GoodsServicesID int output,
	@VendorID int output,
	@StatusFlags int output,
	@Description varchar(170) output,
	@RetAgentID int output,
	@FileAgentID int output,
	@InitPrt215 datetime output,
	@InitPrtAbw datetime output,
	@TotalCost decimal(8,2) output,
	@CheckNumber varchar(25) output,
	@CurrencyUsed smallint output,
	@TaxOfficeID int output

AS begin

SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

SELECT
	@OrderNumber = f.OrderNumber,
	@CtrlNumber = f.CtrlNumber,
	@PackageID = f.PackageID,
	@FormTypeID = f.FormTypeID,
	@TransTypeID = f.TransTypeID,
	@GoodsServicesID = f.GoodsServicesID,
	@VendorID = f.VendorID,
	-- force printed flag so that PO's with missing print data can be returned w/o needing to be extraneously re-printed
	@StatusFlags = CASE WHEN @RetAgentID = 0 THEN f.StatusFlags | power(2,27) | POWER(2,29) & ~power(2, 25) & ~power(2, 28) ELSE f.StatusFlags END,
	@Description = f.[Description],
	@RetAgentID = f.RetAgentID,
	@FileAgentID = f.FileAgentID,
	@InitPrt215 = f.InitPrt215,
	@InitPrtAbw = f.InitPrtAbw,
	@TaxOfficeID = A.TaxOfficeID
FROM tblTaxForms f
JOIN tblTaxFormPackages p ON p.PackageID = f.PackageID
LEFT JOIN tblTaxFormAgents a ON a.AgentID = p.AgentID --bja:unfortunately some Agent records are simply missing so the hard joins fail
WHERE f.TaxFormID = @TaxFormID

IF @@ROWCOUNT = 0
	RETURN 1

--bja:if we weren't able to join to Agent (because some are simply not out there for unknown reason) then we need to determine the @TaxOfficeID by other means
IF (@TaxOfficeID IS NULL)
  SELECT TOP 1 @TaxOfficeID=TaxOfficeID from tblTaxOffices WHERE @TaxFormID > TaxOfficeID ORDER BY TaxOfficeID desc

IF (@FormTypeID <> 3) BEGIN
  -- Select the tblPPOData record for this Priced Purchase Order
  SELECT 
    @TotalCost = tblPPOData.TotalCost,
    @CheckNumber = tblPPOData.CheckNumber,
    @CurrencyUsed = tblPPOData.CurrencyUsed
  FROM tblPPOData 
  WHERE tblPPOData.TaxFormID = @TaxFormID
END
ELSE BEGIN
  SET @TotalCost = 0
  SET @CheckNumber = '-'
  SET @CurrencyUsed = 0
END

RETURN 0

END

GO

/****** Object:  StoredProcedure [dbo].[cp_parmsel_iTRAAC_TaxForms]    Script Date: 06/10/2010 14:33:53 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO


alter procedure [dbo].[cp_parmsel_iTRAAC_TaxForms]
@TaxOfficeID varchar(200), --INT,
@PackageID varchar(200), --INT,
@ClientID varchar(200), --INT,
@TransTypeID varchar(200), --INT,
@GoodsServicesID varchar(200), --INT,
@VendorID varchar(200), --INT,
@AgentID varchar(200), --INT,
@SponsorID varchar(200), --INT,
@ExplicitStatusFlag varchar(200), --INT,

--< BEGIN FILTER FIELD "ORDERNO" DECLARATIONS >--
@ORDERNO_BEGINS_WITH varchar(200) = NULL,
@ORDER_NUMBER_CONTAINS varchar(200) = NULL,
@ORDERNO_IS_EXACTLY varchar(200) = NULL,
@ORDERNO_IS_EMPTY varchar(200) = NULL,
@ORDERNO_IS_NOT_EMPTY varchar(200) = NULL,
--< END FILTER FIELD "ORDERNO" DECLARATIONS >--

--< BEGIN FILTER FIELD "StatusFlags" DECLARATIONS >--
@PO_STATUS_EQUALS varchar(200) = NULL,
@PO_STATUS_NOT_EQUAL_TO varchar(200) = NULL,
@PO_STATUS_EXISTS varchar(200) = NULL,
@PO_STATUS_DOES_NOT_EXIST varchar(200) = NULL,
--< END FILTER FIELD "StatusFlags" DECLARATIONS >--

--< BEGIN FILTER FIELD "UserFLAGS" DECLARATIONS >--
@FLAGS_EQUALS INT = NULL,
@FLAGS_NOT_EQUAL_TO INT = NULL,
@FLAGS_EXISTS BIT = NULL,
@FLAGS_DOES_NOT_EXIST BIT = NULL,
--< END FILTER FIELD "UserFLAGS" DECLARATIONS >--

--< BEGIN FILTER FIELD "CCode" DECLARATIONS >--
@CLIENT_CODE_BEGINS_WITH varchar(200) = NULL,
@CLIENT_CODE_IS_EXACTLY varchar(200) = NULL,
@CLIENT_CODE_IS_EMPTY BIT = NULL,
@CLIENT_CODE_IS_NOT_EMPTY BIT = NULL,
--< END FILTER FIELD "CCode" DECLARATIONS >--

--< BEGIN FILTER FIELD "PODate" DECLARATIONS >--
@PO_DATE_YESTERDAY VARCHAR(200) = NULL, --bit
@PO_DATE_TODAY VARCHAR(200) = NULL, --bit
@PO_DATE_IN_THE_LAST_7_DAYS VARCHAR(200) = NULL, --bit
@PO_DATE_LAST_WEEK VARCHAR(200) = NULL, --bit
@PO_DATE_THIS_WEEK VARCHAR(200) = NULL, --bit
@PO_DATE_LAST_MONTH VARCHAR(200) = NULL, --bit
@PO_DATE_THIS_MONTH VARCHAR(200) = NULL, --bit
@PO_DATE_ON VARCHAR(200) = NULL, --datetime
@PO_DATE_ON_OR_AFTER VARCHAR(200) = NULL, --datetime
@PO_DATE_ON_OR_BEFORE VARCHAR(200) = NULL, --datetime
@PO_DATE_BETWEEN1 VARCHAR(200) = NULL, --datetime
@PO_DATE_BETWEEN2 VARCHAR(200) = NULL --datetime
--< END FILTER FIELD "PODate" DECLARATIONS >--

AS begin

IF @ExplicitStatusFlag IN (1, 2, 4) SET @PO_STATUS_EQUALS = @ExplicitStatusFlag

/*DECLARE @Mask2 INT
IF NOT @PO_STATUS_EQUALS IS NULL BEGIN
  IF @PO_STATUS_EQUALS = 1 SET @Mask2 = 2 --issued = 2^0 = 1
  ELSE IF @PO_STATUS_EQUALS = 2 SET @Mask2 = 4 --returned = 2^1 = 2
  ELSE IF @PO_STATUS_EQUALS = 4 SET @Mask2 = 0 --filed = 2^2 = 4
END*/

DECLARE @Mask2 INT
IF NOT @PO_STATUS_EQUALS IS NULL BEGIN
  IF      @PO_STATUS_EQUALS = POWER(2,0) SET @Mask2 = POWER(2,2) ^ POWER(2,1) --issued   = 0 bit 
  ELSE IF @PO_STATUS_EQUALS = POWER(2,1) SET @Mask2 = POWER(2,2)              --returned = 1 bit
  ELSE IF @PO_STATUS_EQUALS = POWER(2,2) SET @Mask2 = 0                       --filed    = 2 bit
END

--bja: if specifying OrderNumber criteria, remove the restriction on TaxOfficeID since
--this just hides forms somebody is specifically looking for that happen to be issued in another office
--and winds up confusing folks unecessarily
IF COALESCE(@ORDERNO_BEGINS_WITH, 
            @ORDER_NUMBER_CONTAINS,
            @ORDERNO_IS_EXACTLY,
            @ORDERNO_IS_EMPTY,
            @ORDERNO_IS_NOT_EMPTY) IS NOT NULL
  SET @TaxOfficeID = 1

IF @TaxOfficeID = 1 SET @TaxOfficeID=NULL

SET @TaxOfficeID = dbo.fn_DynParm('and t.TaxOfficeID = ?', @TaxOfficeID)
SET @PackageID   = dbo.fn_DynParm('and f.PackageID = ?', @PackageID)

SET @ClientID         = dbo.fn_DynParm('and c.ClientID = ?', @ClientID)
SET @TransTypeID      = dbo.fn_DynParm('and f.TransTypeID = ?', @TransTypeID)
SET @GoodsServicesID = dbo.fn_DynParm('and f.GoodsServicesID = ?', @GoodsServicesID)
SET @VendorID         = dbo.fn_DynParm('and f.VendorID = ?', @VendorID)
SET @AgentID          = dbo.fn_DynParm('and p.AgentID = ?', @AgentID)
SET @SponsorID        = dbo.fn_DynParm('and c.SponsorID = ?', @SponsorID)

SET @ORDERNO_BEGINS_WITH   = dbo.fn_DynParm('and f.OrderNumber LIKE ''?''', @ORDERNO_BEGINS_WITH)
SET @ORDER_NUMBER_CONTAINS = dbo.fn_DynParm('and f.OrderNumber LIKE ''?''', @ORDER_NUMBER_CONTAINS)
SET @ORDERNO_IS_EXACTLY    = dbo.fn_DynParm('and f.OrderNumber = ''?''', @ORDERNO_IS_EXACTLY)
SET @ORDERNO_IS_EMPTY      = dbo.fn_DynParm('and f.OrderNumber IS NULL', @ORDERNO_IS_EMPTY)
SET @ORDERNO_IS_NOT_EMPTY  = dbo.fn_DynParm('and f.OrderNumber IS not NULL', @ORDERNO_IS_NOT_EMPTY)


SET @PO_STATUS_EQUALS  = dbo.fn_DynParm('and f.StatusFlags & (? ^ '+CONVERT(VARCHAR, @mask2)+') = ?', @PO_STATUS_EQUALS)
--brent:tbd2:--AND (@PO_STATUS_NOT_EQUAL_TO IS NULL OR f.StatusFlags&@PO_STATUS_NOT_EQUAL_TO <> @PO_STATUS_NOT_EQUAL_TO)
--ditto: AND (@PO_STATUS_EXISTS IS NULL OR NOT f.StatusFlags IS NULL)
--ditto: AND (@PO_STATUS_DOES_NOT_EXIST IS NULL OR f.StatusFlags IS NULL)

DECLARE @DateRange VARCHAR(200), @BeginDate VARCHAR(20), @EndDate VARCHAR(20), @Today DATETIME
SET @Today = CONVERT(VARCHAR, GETDATE(), 101)
SET @DateRange = ''

IF (@PO_DATE_YESTERDAY IS NOT NULL) begin
  SET @BeginDate = DATEADD(DAY, -1, @Today)
  SET @EndDate = @Today
  SET @DateRange = 'and p.PurchaseDate between '''+@BeginDate+''' and '''+@EndDate+''''+CHAR(13)
END
ELSE IF (@PO_DATE_TODAY IS NOT NULL) begin
  SET @BeginDate = @Today
  SET @DateRange = 'and p.PurchaseDate > '''+@BeginDate+''''+CHAR(13)
END
ELSE IF (@PO_DATE_IN_THE_LAST_7_DAYS IS NOT NULL) begin
  SET @BeginDate = DATEADD(DAY, -6, @Today)
  SET @DateRange = 'and p.PurchaseDate > '''+@BeginDate+''''+CHAR(13)
END
ELSE IF (@PO_DATE_LAST_WEEK IS NOT NULL) begin
  SET @BeginDate = DATEADD(DAY, -DATEPART(WEEKDAY, @Today-1) -7, @Today) 
  SET @EndDate = DATEADD(DAY, 7, @BeginDate)
  SET @DateRange = 'and p.PurchaseDate between '''+@BeginDate+''' and '''+@EndDate+''''+CHAR(13)
END
ELSE IF (@PO_DATE_THIS_WEEK IS NOT NULL) begin
  SET @BeginDate = DATEADD(DAY, -DATEPART(WEEKDAY, @Today-1), @Today) 
  SET @DateRange = 'and p.PurchaseDate > '''+@BeginDate+''''+CHAR(13)
END
ELSE IF (@PO_DATE_LAST_MONTH IS NOT NULL) begin
  SET @BeginDate = CONVERT(VARCHAR, DATEPART(MONTH, DATEADD(MONTH, -1, @Today))) + '/1/' + CONVERT(VARCHAR, DATEPART(YEAR, DATEADD(MONTH, -1, @Today)))
  SET @EndDate = CONVERT(VARCHAR, DATEPART(MONTH, @Today)) + '/1/' + CONVERT(VARCHAR, DATEPART(YEAR, @Today))
  SET @DateRange = 'and p.PurchaseDate between '''+@BeginDate+''' and '''+@EndDate+''''+CHAR(13)
END
ELSE IF (@PO_DATE_THIS_MONTH IS NOT NULL) begin
  SET @BeginDate = CONVERT(VARCHAR, DATEPART(MONTH, @Today)) + '/1/' + CONVERT(VARCHAR, DATEPART(YEAR, @Today))
  SET @DateRange = 'and p.PurchaseDate > '''+@BeginDate+''''+CHAR(13)
END
ELSE IF (@PO_DATE_ON IS NOT NULL) begin
  SET @BeginDate = @PO_DATE_ON
  SET @EndDate = DATEADD(DAY, 1, @BeginDate)
  SET @DateRange = 'and p.PurchaseDate between '''+@BeginDate+''' and '''+@EndDate+''''+CHAR(13)
END
ELSE IF (@PO_DATE_ON_OR_AFTER IS NOT NULL) begin
  SET @BeginDate = @PO_DATE_ON_OR_AFTER
  SET @DateRange = 'and p.PurchaseDate > '''+@BeginDate+''''+CHAR(13)
END
ELSE IF (@PO_DATE_ON_OR_BEFORE IS NOT NULL) BEGIN --bja:this would basically always be too killer of a query so hard coding it to just "today's" forms so the GUI doesn't go off into lala land waiting for response
  SET @BeginDate = @PO_DATE_ON_OR_BEFORE
  SET @EndDate = DATEADD(DAY, 1, @BeginDate)
  SET @DateRange = 'and p.PurchaseDate between '''+@BeginDate+''' and '''+@EndDate+''''+CHAR(13)
END
/*PO_DATE_BETWEEN doesn't appear to have been implemented in the existing GUI
ELSE IF (@PO_DATE_BETWEEN1 IS NOT NULL) BEGIN
  ELSE begin
    SET @BeginDate = @PO_DATE_BETWEEN1
    SET @EndDate = DATEADD(DAY, 1, @PO_DATE_BETWEEN2)
    SET @DateRange = 'and p.PurchaseDate between '''+@BeginDate+''' and '''+@EndDate+''''+CHAR(13)
  end
END
*/

EXEC('
set transaction isolation level read uncommitted
SELECT 
  --f.PackageID,
  f.TaxFormID, 
  f.OrderNumber AS OrderNo, 
  f.StatusFlags, 
  f.StatusFlags AS UserFlags,
  --dbo.fn_StatusFlags(f.StatusFlags) as Flags,
  c.Client+''  (''+c.CCode+'')'' As Client, 
  t.TaxOfficeName,
  t.TaxOfficeID
FROM tblTaxForms f
JOIN tblTaxFormPackages p ON f.PackageID = p.PackageID
JOIN vw_Clients c ON p.ClientID = c.ClientID
LEFT JOIN tblTaxFormAgents a ON p.AgentID = a.AgentID
LEFT JOIN tblTaxOffices t ON a.TaxOfficeID = t.TaxOfficeID

WHERE 1=1
'
+@TaxOfficeID
+@PackageID

+@ClientID
+@TransTypeID
+@GoodsServicesID
+@VendorID
+@AgentID
+@SponsorID

+@ORDERNO_BEGINS_WITH  
+@ORDER_NUMBER_CONTAINS
+@ORDERNO_IS_EXACTLY   
+@ORDERNO_IS_EMPTY     
+@ORDERNO_IS_NOT_EMPTY 

+@PO_STATUS_EQUALS

+@DateRange

+'ORDER BY p.PurchaseDate')

--< BEGIN FILTER FIELD "USERFLAGS" WHERE CLAUSE >--
--AND (@FLAGS_EQUALS IS NULL OR UserFLAGS&@FLAGS_EQUALS=@FLAGS_EQUALS)
--AND (@FLAGS_NOT_EQUAL_TO IS NULL OR UserFLAGS&@FLAGS_NOT_EQUAL_TO<>@FLAGS_NOT_EQUAL_TO)
--AND (@FLAGS_EXISTS IS NULL OR NOT UserFLAGS IS NULL)
--AND (@FLAGS_DOES_NOT_EXIST IS NULL OR UserFLAGS IS NULL)
--< END FILTER FIELD "USERFLAGS" WHERE CLAUSE >--

--< BEGIN FILTER FIELD "CCode" WHERE CLAUSE >--
--AND (@CLIENT_CODE_BEGINS_WITH IS NULL OR CCode LIKE @CLIENT_CODE_BEGINS_WITH)
--AND (@CLIENT_CODE_IS_EXACTLY IS NULL OR CCode = @CLIENT_CODE_IS_EXACTLY)
--AND (@CLIENT_CODE_IS_EMPTY IS NULL OR CCode IS NULL)
--AND (@CLIENT_CODE_IS_NOT_EMPTY IS NULL OR NOT CCode IS NULL)
----< END FILTER FIELD "CCode" WHERE CLAUSE >--

----< BEGIN FILTER FIELD "PODate" WHERE CLAUSE >--
--AND (@PO_DATE_YESTERDAY IS NULL OR p.PurchaseDate  > DATEADD(DAY, -1, @BASEDATE) AND  p.PurchaseDate  < @BASEDATE)
--done: AND (@PO_DATE_TODAY IS NULL OR p.PurchaseDate  >= @BASEDATE AND  p.PurchaseDate  < DATEADD(DAY, 1, @BASEDATE))
--AND (@PO_DATE_IN_THE_LAST_7_DAYS IS NULL OR p.PurchaseDate  > DATEADD(DAY, -6, @BASEDATE))
--AND (@PO_DATE_LAST_WEEK IS NULL OR p.PurchaseDate  BETWEEN DATEADD(DAY, (-DATEPART(DW, @BASEDATE-1) -7), @BASEDATE) AND DATEADD(DAY, 7, DATEADD(DAY, (-DATEPART(DW, @BASEDATE)-7), @BASEDATE)))
--AND (@PO_DATE_THIS_WEEK IS NULL OR p.PurchaseDate  BETWEEN DATEADD(DAY, (-DATEPART(DW, @BASEDATE-1)), @BASEDATE) AND DATEADD(DAY, 1, @BASEDATE))
--AND (@PO_DATE_LAST_MONTH IS NULL OR p.PurchaseDate  > CONVERT(VARCHAR(2), DATEPART(MONTH, DATEADD(MONTH, -1, @BASEDATE)))+'/1/'+CONVERT(VARCHAR(4), DATEPART(YEAR, DATEADD(MONTH, -1, @BASEDATE))) AND p.PurchaseDate  < CONVERT(VARCHAR(2), DATEPART(MONTH, @BASEDATE))
--    +'/1/'+CONVERT(VARCHAR(4), DATEPART(YEAR, @BASEDATE)))
--AND (@PO_DATE_THIS_MONTH IS NULL OR p.PurchaseDate  BETWEEN CONVERT(VARCHAR(2), DATEPART(MONTH ,@BASEDATE))+'/1/'+CONVERT(VARCHAR(4), DATEPART(YEAR ,@BASEDATE)) AND @BASEDATE)
--AND (@PO_DATE_ON IS NULL OR p.PurchaseDate  > @PO_DATE_ON AND  p.PurchaseDate  < DATEADD(D, 1, @PO_DATE_ON))
--AND (@PO_DATE_ON_OR_AFTER IS NULL OR p.PurchaseDate  > @PO_DATE_ON_OR_AFTER)
--AND (@PO_DATE_ON_OR_BEFORE IS NULL OR (P.PurchaseDate > @PO_DATE_ON_OR_BEFORE AND p.PurchaseDate  < DATEADD(D, 1, @PO_DATE_ON_OR_BEFORE) OR p.PurchaseDate  < @PO_DATE_ON_OR_BEFORE))
--AND ((@PO_DATE_BETWEEN1 IS NULL AND @PO_DATE_BETWEEN2 IS NULL) OR p.PurchaseDate  BETWEEN @PO_DATE_BETWEEN1 AND @PO_DATE_BETWEEN2)
--< END FILTER FIELD "PODate" WHERE CLAUSE >--

end

GO

/****** Object:  StoredProcedure [dbo].[cp_parmsel_NotFiled]    Script Date: 06/10/2010 14:33:53 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

alter procedure [dbo].[cp_parmsel_NotFiled]

    @TaxOfficeID INT, @CtrlNumber INT, @ReturnedOnly BIT

AS
/*
DECLARE @TaxOfficeID INT, @CtrlNumber INT
SELECT @TaxOfficeID =10000001, @CtrlNumber = 14

-- cp_parmsel_NotFiled 10000001, 4
-- cp_parmsel_NotFiled 10000001, 9
-- cp_parmsel_NotFiled 10000001, 61

*/


IF @ReturnedOnly = 0
    SELECT 
        F.TaxFormID, 
        F.TransTypeID, F.VendorID, F.GoodsServicesID,
        D.TotalCost,D.CurrencyUsed,
        F.OrderNumber + CHAR(10) + 
            C.LName + ', ' + C.FName + ' (' + C.CCode + ')' + CHAR(10) + 
            D.CheckNumber + CHAR(10) + STR(F.StatusFlags) + CHAR(10) + 
            STR(F.FormTypeID) + CHAR(10) + ISNULL(F.[Description], '') Tag
    
    FROM dbo.tblTaxFormPackages P 
        INNER JOIN dbo.tblTaxForms F ON P.PackageID = F.PackageID 
        INNER JOIN dbo.tblPPOData D ON D.TaxFormID = F.TaxFormID
        INNER JOIN dbo.tblTaxFormAgents A ON P.AgentID = A.AgentID 
        INNER JOIN dbo.tblClients C ON P.ClientID = C.ClientID 
    
    WHERE (A.TaxOfficeID = @TaxOfficeID) AND (F.CtrlNumber = @CtrlNumber) 
        AND (F.StatusFlags & 1 = 1) AND (F.StatusFlags & 4 = 0) 
        AND (F.StatusFlags & 32 = 0)
    
    ORDER BY F.FormTypeID, F.TaxFormID

ELSE
    SELECT 
        F.TaxFormID, 
        F.TransTypeID, F.VendorID, F.GoodsServicesID,
        D.TotalCost,D.CurrencyUsed,
        F.OrderNumber + CHAR(10) + 
            C.LName + ', ' + C.FName + ' (' + C.CCode + ')' + CHAR(10) + 
            D.CheckNumber + CHAR(10) + STR(F.StatusFlags) + CHAR(10) + 
            STR(F.FormTypeID) + CHAR(10) + ISNULL(F.[Description], '') Tag
    
    FROM dbo.tblTaxFormPackages P 
        INNER JOIN dbo.tblTaxForms F ON P.PackageID = F.PackageID 
        INNER JOIN dbo.tblPPOData D ON D.TaxFormID = F.TaxFormID
        INNER JOIN dbo.tblTaxFormAgents A ON P.AgentID = A.AgentID 
        INNER JOIN dbo.tblClients C ON P.ClientID = C.ClientID 
    
    WHERE (A.TaxOfficeID = @TaxOfficeID) AND (F.CtrlNumber = @CtrlNumber) 
        AND (F.StatusFlags & 1 = 1) AND (F.StatusFlags & 2 = 2) 
         AND (F.StatusFlags & 4 = 0)AND (F.StatusFlags & 32 = 0)
    
    ORDER BY F.FormTypeID, F.TaxFormID

GO

/****** Object:  StoredProcedure [dbo].[cp_parmsel_SessionDetails]    Script Date: 06/10/2010 14:33:54 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

alter procedure [dbo].[cp_parmsel_SessionDetails]

    @SessionID INT = NULL

AS

    SET NOCOUNT ON

	SELECT 
	    E.DBEID AS SRC_ID, 
        2 AS SRC,
		DBAction AS Type,
		T.AttributeName AS Category,
        CASE 
        	WHEN T.TableID=1 THEN -- tblUsers
        		(SELECT FName+' '+LName FROM tblUsers WHERE USERID=PKID)+ISNULL('; '+ED.EventData, '')
        	WHEN T.TableID=2 THEN -- tblTaxOffices
        		(SELECT TaxOfficeName FROM tblTaxOffices WHERE TaxOfficeID=PKID)+ISNULL('; '+ED.EventData, '')
        	WHEN T.TableID=3 THEN -- tblVendors
        		(SELECT VendorName FROM tblVendors WHERE VendorID=PKID)+ISNULL('; '+ED.EventData, '')
        	WHEN T.TableID=4 THEN -- tblGoodsServices
        		(SELECT GoodsServiceName FROM tblGoodsServices WHERE GoodsServicesID=PKID)+ISNULL('; '+ED.EventData, '')
        	WHEN T.TableID=5 THEN -- tblTaxFormAgents
        		(SELECT Agent FROM vw_Agents WHERE AgentID=PKID)+ISNULL('; '+ED.EventData, '')
        	WHEN T.TableID=6 THEN -- tblOfficeManagers
        		(SELECT U.FName+' '+U.LName FROM dbo.tblUsers U INNER JOIN tblOfficeManagers R ON U.UserID = R.UserID WHERE ManagerID=PKID)+ISNULL('; '+ED.EventData, '')
        --	WHEN T.TableID=8 THEN -- tblAttributes
          --      (SELECT T.AttributeName+'/'+A.AttributeName FROM tblAttributes A INNER JOIN tblTables T ON A.TableID=T.TableID WHERE AttribID=PKID)+ISNULL('; '+ED.EventData, '')
        	WHEN T.TableID=9 THEN -- tblSponsors
        		(SELECT Sponsor FROM dbo.vw_Sponsors WHERE SponsorID=PKID)+ISNULL('; '+ED.EventData, '')
        	WHEN T.TableID=10 THEN -- tblClients
                (SELECT Client FROM dbo.vw_Clients WHERE ClientID=PKID)+ISNULL('; '+ED.EventData, '')
        	WHEN T.TableID=13 THEN -- tblTaxFormPackages
                (SELECT PackageCode FROM tblTaxFormPackages WHERE PackageID=PKID)+ISNULL('; '+ED.EventData, '')
        	WHEN T.TableID=14 THEN -- tblTaxForms
                (SELECT OrderNumber FROM tblTaxForms WHERE TaxFormID=PKID)+ISNULL('; '+ED.EventData, '')
        	WHEN T.TableID=17 THEN -- tblAccessControl
                (SELECT CASE WHEN RoleType=2 THEN (SELECT Agent FROM vw_Agents WHERE AgentID=RoleID)
                 ELSE (SELECT U.FName+' '+U.LName FROM dbo.tblUsers U INNER JOIN tblOfficeManagers R ON U.UserID = R.UserID WHERE ManagerID=RoleID)
                 END FROM tblAccessControl WHERE ACID=PKID)+ISNULL('; '+ED.EventData, '')
        END AS [Description],
        EventDateTime,
   		S.SessionID
	
	FROM 
		dbo.tblSessions S
			INNER JOIN tblDBEvents E ON S.SessionID=E.SessionID
			LEFT OUTER JOIN tblDBEventData ED ON E.DBEID=ED.DBEID
			INNER JOIN tblTables T ON E.TableID=T.TableID
	
	WHERE 
		(@SessionID IS NULL OR S.SessionID = @SessionID)

UNION ALL

	SELECT 
		E.SEID,
        1, 
		TypeID,
		Category,
		[Description],
		E.EventDateTime,
   		S.SessionID

	FROM 
		dbo.tblSessions S
			INNER JOIN tblSessionEvents E ON S.SessionID=E.SessionID
			INNER JOIN tblUsers U ON U.UserID=S.UserID

	WHERE 
		(@SessionID IS NULL OR S.SessionID = @SessionID)

    ORDER BY
		S.SessionID DESC, EventDateTime

GO

/****** Object:  StoredProcedure [dbo].[cp_parmupd_iTRAAC_GoodsService]    Script Date: 06/10/2010 14:33:54 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

alter procedure [dbo].[cp_parmupd_iTRAAC_GoodsService]

	@GoodsServicesID int,
	@TaxOfficeID int,
	@GoodsServiceName varchar(50),
	@Description varchar(255),
	@Type smallint,
	@Active bit,
	@StatusFlags int

AS

UPDATE tblGoodsServices

SET
	tblGoodsServices.TaxOfficeID = @TaxOfficeID,
	tblGoodsServices.GoodsServiceName = @GoodsServiceName,
	tblGoodsServices.Description = @Description,
	tblGoodsServices.Type = @Type,
	tblGoodsServices.Active = @Active,
	tblGoodsServices.StatusFlags = @StatusFlags

WHERE 
	(tblGoodsServices.GoodsServicesID = @GoodsServicesID)

IF @@ERROR <> 0
	RETURN 1

GO

/****** Object:  StoredProcedure [dbo].[cp_parmupd_iTRAAC_TaxForm]    Script Date: 06/10/2010 14:33:54 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

alter procedure [dbo].[cp_parmupd_iTRAAC_TaxForm]

	@TaxFormID int,
	@OrderNumber varchar(16),
	@CtrlNumber int,
	@PackageID int,
	@FormTypeID int,
	@TransTypeID int,
	@GoodsServicesID int,
	@VendorID int,
	@StatusFlags int,
	@Description varchar(170),
	@RetAgentID int,
	@FileAgentID int,
	@InitPrt215 datetime,
	@InitPrtAbw datetime,

	@TotalCost decimal(8,2),
	@CheckNumber varchar(25),
	@CurrencyUsed smallint

AS

UPDATE tblTaxForms

SET
	tblTaxForms.OrderNumber = @OrderNumber,
	tblTaxForms.CtrlNumber = @CtrlNumber,
	tblTaxForms.PackageID = @PackageID,
	tblTaxForms.FormTypeID = @FormTypeID,
	tblTaxForms.TransTypeID = @TransTypeID,
	tblTaxForms.GoodsServicesID = @GoodsServicesID,
	tblTaxForms.VendorID = @VendorID,
	tblTaxForms.StatusFlags = @StatusFlags,
	tblTaxForms.[Description] = @Description,
	tblTaxForms.RetAgentID = @RetAgentID,
	tblTaxForms.FileAgentID = @FileAgentID,
	tblTaxForms.InitPrt215 = @InitPrt215,
	tblTaxForms.InitPrtAbw = @InitPrtAbw

WHERE 
	(tblTaxForms.TaxFormID = @TaxFormID)

IF @FormTypeID<>3 BEGIN
	IF NOT EXISTS(SELECT * FROM tblPPOData WHERE TAXFORMID=@TaxFormID) BEGIN
	    INSERT INTO tblPPOData (
	    	TaxFormID,
	    	TotalCost,
	    	CheckNumber,
	    	CurrencyUsed
	    )
	    VALUES (
	    	@TaxFormID,
	    	@TotalCost,
	    	'N/A',
	    	@CurrencyUsed
	    )
	END ELSE BEGIN
	    UPDATE tblPPOData 
	    SET tblPPOData.TotalCost = @TotalCost,
	    	tblPPOData.CheckNumber = @CheckNumber,
	    	tblPPOData.CurrencyUsed = @CurrencyUsed
	    WHERE (tblPPOData.TaxFormID = @TaxFormID)
	END
END

IF @@ERROR <> 0
	RETURN 1

GO


USE [iTRAAC]
GO

/****** Object:  View [dbo].[vw_ABW]    Script Date: 06/10/2010 14:36:39 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

alter view [dbo].[vw_ABW] AS
SELECT
	TF.TaxFormID, 
	'XXXX' AS F1,
	ISNULL(V.VendorName, '') AS F2,
	ISNULL(V.AddressLine1, '') AS F3,
	ISNULL(V.AddressLine2, '') AS F4,
	--CONVERT(VARCHAR(25), P.PurchaseDate, 7) AS F5,
	TF.OrderNumber AS F5, 
	' ' AS F6,
	--TF.OrderNumber AS F6, 
	TOF.AgencyLine1 AS F7,
	TOF.AgencyLine2 AS F8,
	C.Client AS F9, 
	S.DutyLocation AS F10, 
	'EURO' AS F11,
	'EURO' AS F12,
	A.SigBlock AS F13, 
	C.Client AS F14

FROM dbo.tblTaxOffices T
	INNER JOIN dbo.vw_Agents A ON T.TaxOfficeID = A.TaxOfficeID 
	INNER JOIN dbo.tblTaxOffices TOF ON TOF.TaxOfficeID = A.TaxOfficeID 
	INNER JOIN dbo.tblTaxFormPackages P ON A.AgentID = P.AgentID 
	INNER JOIN dbo.tblTaxForms TF ON P.PackageID = TF.PackageID 
	INNER JOIN dbo.vw_Clients C ON P.ClientID = C.ClientID 
	INNER JOIN dbo.vw_Sponsors S ON C.SponsorID = S.SponsorID
	LEFT OUTER JOIN dbo.tblVendors V ON TF.VendorID = V.VendorID
	LEFT OUTER JOIN dbo.tblGoodsServices G ON TF.GoodsServicesID = G.GoodsServicesID
	LEFT OUTER JOIN dbo.tblTransactionTypes TT ON TF.TransTypeID = TT.TransTypeID

GO

/****** Object:  View [dbo].[vw_EF1]    Script Date: 06/10/2010 14:36:39 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

alter view [dbo].[vw_EF1] AS
SELECT
	TF.TaxFormID, 
	TF.OrderNumber AS F1, 
	T.TaxOfficeName+' VAT Office' AS F2,
	T.HomePageURL AS F3,
	A.SigBlock AS F4, 
	dbo.ClearDate(P.PurchaseDate) AS F5, 
	'XXXX' AS F6,
	' ' AS F7,
	S.Sponsor AS F8, 
	S.AddressLine1 AS F9, 
	'EXPIRATION DATE/VERFALLDATUM' AS F10, 
    CASE WHEN EXISTS(SELECT TOP 1 C.ClientID FROM dbo.vw_Clients C 
                        WHERE C.StatusFlags & 1 = 0 AND C.SponsorID = S.SponsorID 
                        ORDER BY C.ClientID ASC)
    THEN 
        CASE WHEN P.ClientID=
             (SELECT TOP 1 C.ClientID FROM dbo.vw_Clients C 
                WHERE C.StatusFlags & 1 = 1 AND C.SponsorID = S.SponsorID 
                ORDER BY C.ClientID ASC)
        THEN -- Use the first family member name found
             (SELECT TOP 1 C.Client FROM dbo.vw_Clients C 
                 WHERE C.StatusFlags & 1 = 0 AND C.SponsorID = S.SponsorID 
                 ORDER BY C.ClientID ASC)
        ELSE -- Use the specific family member name
            (SELECT C.Client FROM dbo.vw_Clients C WHERE C.ClientID = P.ClientID)
        END
    ELSE -- No family members
    'XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX'
    END AS F11,
	dbo.ClearDate(DATEADD(yy, 2, P.PurchaseDate)) AS F12, 
	T.FormFootNoteLine1 AS F13, 
	T.FormFootNoteLine2 AS F14, 
    'C-CODE: '+C.CCode AS F15,
	TF.FormTypeID	

FROM dbo.tblTaxOffices T
	INNER JOIN dbo.vw_Agents A ON T.TaxOfficeID = A.TaxOfficeID 
	INNER JOIN dbo.tblTaxFormPackages P ON A.AgentID = P.AgentID 
	INNER JOIN dbo.tblTaxForms TF ON P.PackageID = TF.PackageID 
	INNER JOIN dbo.vw_Clients C ON P.ClientID = C.ClientID 
	INNER JOIN dbo.vw_Sponsors S ON C.SponsorID = S.SponsorID
	LEFT OUTER JOIN dbo.tblVendors V ON TF.VendorID = V.VendorID
	LEFT OUTER JOIN dbo.tblGoodsServices G ON TF.GoodsServicesID = G.GoodsServicesID
	LEFT OUTER JOIN dbo.tblTransactionTypes TT ON TF.TransTypeID = TT.TransTypeID

WHERE
 	(TF.FormTypeID = 4) 

GO

/****** Object:  View [dbo].[vw_EF2]    Script Date: 06/10/2010 14:36:39 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

alter view [dbo].[vw_EF2] AS
SELECT
	TF.TaxFormID, 
	TF.OrderNumber AS F1, 
	T.TaxOfficeName+' VAT Office' AS F2,
	T.HomePageURL AS F3,
	A.SigBlock AS F4, 
	dbo.ClearDate(P.PurchaseDate) AS F5, 
	'XXXX' AS F6,
	' ' AS F7,
	S.Sponsor AS F8, 
	S.AddressLine1 AS F9, 
	'EXPIRATION DATE/VERFALLDATUM' AS F10, 
    CASE WHEN EXISTS(SELECT TOP 1 C.ClientID FROM dbo.vw_Clients C 
                        WHERE C.StatusFlags & 1 = 0 AND C.SponsorID = S.SponsorID 
                        ORDER BY C.ClientID ASC)
    THEN 
        CASE WHEN P.ClientID=
             (SELECT TOP 1 C.ClientID FROM dbo.vw_Clients C 
                WHERE C.StatusFlags & 1 = 1 AND C.SponsorID = S.SponsorID 
                ORDER BY C.ClientID ASC)
        THEN -- Use the first family member name found
             (SELECT TOP 1 C.Client FROM dbo.vw_Clients C 
                 WHERE C.StatusFlags & 1 = 0 AND C.SponsorID = S.SponsorID 
                 ORDER BY C.ClientID ASC)
        ELSE -- Use the specific family member name
            (SELECT C.Client FROM dbo.vw_Clients C WHERE C.ClientID = P.ClientID)
        END
    ELSE -- No family members
    'XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX'
    END AS F11,
	dbo.ClearDate(DATEADD(dd, 90, P.PurchaseDate)) AS F12, 
	T.FormFootNoteLine1 AS F13, 
	T.FormFootNoteLine2 AS F14, 
    'C-CODE: '+C.CCode AS F15,
	TF.FormTypeID	

FROM dbo.tblTaxOffices T
	INNER JOIN dbo.vw_Agents A ON T.TaxOfficeID = A.TaxOfficeID 
	INNER JOIN dbo.tblTaxFormPackages P ON A.AgentID = P.AgentID 
	INNER JOIN dbo.tblTaxForms TF ON P.PackageID = TF.PackageID 
	INNER JOIN dbo.vw_Clients C ON P.ClientID = C.ClientID 
	INNER JOIN dbo.vw_Sponsors S ON C.SponsorID = S.SponsorID
	LEFT OUTER JOIN dbo.tblVendors V ON TF.VendorID = V.VendorID
	LEFT OUTER JOIN dbo.tblGoodsServices G ON TF.GoodsServicesID = G.GoodsServicesID
	LEFT OUTER JOIN dbo.tblTransactionTypes TT ON TF.TransTypeID = TT.TransTypeID

WHERE
 	(TF.FormTypeID = 5) 

GO

/****** Object:  View [dbo].[vw_NF]    Script Date: 06/10/2010 14:36:39 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

alter view [dbo].[vw_NF]
AS
SELECT
	TF.TaxFormID, 
	TF.OrderNumber AS F1, 
	C.Client AS F2, 
	C.CCode AS F3, 

	CASE WHEN TF.FormTypeID=1 THEN
		dbo.ClearDate(DATEADD(yy, 2, P.PurchaseDate))
	WHEN TF.FormTypeID=2 THEN
		dbo.ClearDate(DATEADD(dd, 90, P.PurchaseDate))
	END AS F4,
	
	CASE WHEN C.IsSponsor=1 THEN 
		ISNULL((SELECT TOP 1 Client FROM vw_Clients WHERE SponsorID=C.SponsorID AND ClientID<>C.ClientID), 'XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX')
	ELSE
		S.Sponsor
	END F5,
	CASE WHEN C.IsSponsor=1 THEN 
		ISNULL((SELECT TOP 1 CCode FROM vw_Clients WHERE SponsorID=C.SponsorID AND ClientID<>C.ClientID), 'XXXXXXXX')
	ELSE
		S.CCode
	END F6,
	S.DutyLocation AS F7, 
	S.AddressLine1 AS F8, 
	A.SigBlock AS F9, 
	T.FormFootNoteLine1 AS F10, 
	T.FormFootNoteLine2 AS F11, 

	CASE WHEN TF.FormTypeID=1 THEN
		'(UNTER EURO 2500)'
	WHEN TF.FormTypeID=2 THEN
		(SELECT CONVERT(VARCHAR(12), TotalCost) FROM tblPPOData WHERE TaxFormID=TF.TaxFormID)
	END AS F12,

	CASE WHEN TF.FormTypeID=1 THEN
		'NETTO____________'
	WHEN TF.FormTypeID=2 THEN
		(SELECT CASE WHEN CurrencyUsed=1 THEN 'U.S. DOLLARS' ELSE 'EURO' END FROM tblPPOData WHERE TaxFormID=TF.TaxFormID)
	END AS F13,

	CASE WHEN TF.FormTypeID=1 THEN
		''
	WHEN TF.FormTypeID=2 THEN
		(SELECT CASE WHEN CheckNumber='-' THEN 'CK NO:' ELSE 'CK NO: '+CheckNumber END FROM tblPPOData WHERE TaxFormID=TF.TaxFormID)
	END AS F14,

	ISNULL(V.VendorName, '') AS F15,
	ISNULL(V.AddressLine1, '') AS F16,
	ISNULL(V.AddressLine2, '') AS F17,
	ISNULL(V.AddressLine3, '') AS F18,
	CASE 
		WHEN TF.GoodsServicesID=0 THEN 
			''
		ELSE  
			ISNULL(TF.Description, TT.TransactionType+'; '+G.GoodsServiceName)
	END AS F19,
--    ISNULL(TF.Description,'') AS F20,
	TF.FormTypeID, 
	P.PurchaseDate

FROM dbo.tblTaxOffices T
	INNER JOIN dbo.vw_Agents A ON T.TaxOfficeID = A.TaxOfficeID 
	INNER JOIN dbo.tblTaxFormPackages P ON A.AgentID = P.AgentID 
	INNER JOIN dbo.tblTaxForms TF ON P.PackageID = TF.PackageID 
	INNER JOIN dbo.vw_Clients C ON P.ClientID = C.ClientID 
	INNER JOIN dbo.vw_Sponsors S ON C.SponsorID = S.SponsorID
	LEFT OUTER JOIN dbo.tblVendors V ON TF.VendorID = V.VendorID
	LEFT OUTER JOIN dbo.tblGoodsServices G ON TF.GoodsServicesID = G.GoodsServicesID
	LEFT OUTER JOIN dbo.tblTransactionTypes TT ON TF.TransTypeID = TT.TransTypeID

GO

/****** Object:  View [dbo].[vw_NFx]    Script Date: 06/10/2010 14:36:39 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO



alter view [dbo].[vw_NFx]
AS
SELECT
  tf.TaxFormID,
  tf.FormTypeID,
  
  -- 1. Tax office name/address
  t.AgencyLine1 AS F2,
  t.AgencyLine2 AS F3,
  t.AgencyLine3 AS F4,
  ' ' AS F5,

  -- 2. VAT officer's sig block
  a.SigBlock AS F8,
  
  -- 3. Order number
  tf.OrderNumber AS F1,
  
  -- 3. From date
  dbo.ClearDate(p.PurchaseDate) AS F6,

  -- 3. Until date
  CASE 
    WHEN tf.FormTypeID = 1 THEN dbo.ClearDate(DATEADD(dd, -1, DATEADD(yy, 2, p.PurchaseDate)))
    WHEN tf.FormTypeID = 2 THEN dbo.ClearDate(DATEADD(dd, 90, p.PurchaseDate))
  END AS F7,
  
  -- 4. NF1 check box
  'XX' AS F9,

  -- 4. NF1 amount
  CASE 
    WHEN tf.FormTypeID = 1 THEN '' -- NF1
    ELSE 'XXXXXXXXXXXXXXXX' -- NF2
  END AS F10,

  -- 5. NF2 amount
  CASE
    WHEN tf.FormTypeID = 1 THEN 'XXXXXXXXXXXXXXXX' -- NF1
    ELSE -- NF2
      CASE 
        WHEN tf.TransTypeID = 2 THEN ' '
        ELSE (SELECT CONVERT(varchar, TotalCost) FROM tblPPOData WHERE TaxFormID = tf.TaxFormID)
      END
  END F11,
  
  -- 6. Designated agent (the Customer)
  C.CCode AS F15,

  -- 6. Designated Agent (the Customer)
  ISNULL(c2.Client, '') AS F12,

  -- 9. Authorized family member
  CASE 
    -- when client is the sponsor...
    WHEN c2.ClientID = C.ClientID THEN ISNULL(
        -- then try to pull the spouse 
        (SELECT TOP 1
          Client
        FROM vw_Clients
        WHERE IsSponsor = 0
        AND SponsorID = C.SponsorID
        AND Active = 1  -- don't show spouse's name if they've been deactivated (typically divorced)
        ORDER BY
          CASE
            WHEN StatusFlags & 2 = 2 THEN 0 --sort IsSpouse to the top
            ELSE 1
          END,
          ClientID),
     'XXXXXXXXXXXXXXXX')
     ELSE C.Client
  END AS F13,

  -- 12. Goods/Service
  CASE
    -- pull the form description if there is one
    WHEN tf.FormTypeID = 2 THEN ISNULL(
      -- truncate the "Used On" date tacked on at the end of the form Description field
      CASE WHEN CHARINDEX(CHAR(9), tf.[description]) >0 THEN 
        SUBSTRING(tf.Description, 1, CHARINDEX(CHAR(9), tf.Description))
      ELSE tf.Description end,
    -- otherwise ...
    tt.TransactionType + '; ' + g.GoodsServiceName)
    ELSE ' '
  END AS F14,
  
  -- 13. Vendor  
  ISNULL(v.VendorName, ' ') AS F16,
  
  -- same as F11??? Stuttgart had it in their cp_parmsetl_TaxFormData_NF1 & _NF2 definitions
  ' ' AS F17
  
FROM tblTaxForms tf 
JOIN tblTaxFormPackages p ON p.PackageID = tf.PackageID
LEFT JOIN tblTaxFormAgents a ON a.AgentID = p.AgentID
JOIN tblTaxOffices t ON t.TaxOfficeID = a.TaxOfficeID
LEFT JOIN tblPPOData ppo ON ppo.TaxFormID = tf.TaxFormID
JOIN vw_Clients c ON c.ClientID = p.ClientID
LEFT JOIN vw_Clients c2 ON c2.SponsorID = c.SponsorID AND c2.IsSponsor = 1 AND c2.Active = 1
LEFT JOIN tblVendors v ON v.VendorID = tf.VendorID
LEFT JOIN tblGoodsServices g ON g.GoodsServicesID = tf.GoodsServicesID
LEFT JOIN tblTransactionTypes tt ON tt.TransTypeID = tf.TransTypeID



GO

/****** Object:  View [dbo].[vw_Report_1]    Script Date: 06/10/2010 14:36:39 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

alter view [dbo].[vw_Report_1]
AS
SELECT
        TF.TaxFormID, 
        TF.OrderNumber AS PONumber, 
        C.Client Customer, 
        C.CCode [C-Code], 

        CASE WHEN TF.FormTypeID=1 THEN
                dbo.ClearDate(DATEADD(yy, 2, P.PurchaseDate))
        WHEN TF.FormTypeID=2 THEN
                dbo.ClearDate(DATEADD(dd, 90, P.PurchaseDate))
        END AS [Expiration Date],
        
        CASE WHEN C.IsSponsor=1 THEN 
                ISNULL((SELECT TOP 1 Client FROM vw_Clients WHERE SponsorID=C.SponsorID AND ClientID<>C.ClientID), 'XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX')
        ELSE
                S.Sponsor
        END F5,
        CASE WHEN C.IsSponsor=1 THEN 
                ISNULL((SELECT TOP 1 CCode FROM vw_Clients WHERE SponsorID=C.SponsorID AND ClientID<>C.ClientID), 'XXXXXXXX')
        ELSE
                S.CCode
        END F6,

        S.DutyLocation, 
        S.AddressLine1, 
/*        A.SigBlock AS F9, 
        T.FormFootNoteLine1 AS F10, 
        T.FormFootNoteLine2 AS F11, 

        CASE WHEN TF.FormTypeID=1 THEN
                '(UNTER EURO 2500)'
        WHEN TF.FormTypeID=2 THEN
                (SELECT CONVERT(VARCHAR(12), TotalCost) FROM tblPPOData WHERE TaxFormID=TF.TaxFormID)
        END AS F12,

        CASE WHEN TF.FormTypeID=1 THEN
                'NETTO____________'
        WHEN TF.FormTypeID=2 THEN
                (SELECT CASE WHEN CurrencyUsed=1 THEN 'U.S. DOLLARS' ELSE 'EURO' END FROM tblPPOData WHERE TaxFormID=TF.TaxFormID)
        END AS F13,

        CASE WHEN TF.FormTypeID=1 THEN
                ''
        WHEN TF.FormTypeID=2 THEN
                (SELECT CASE WHEN CheckNumber='-' THEN 'CK NO:' ELSE 'CK NO: '+CheckNumber END FROM tblPPOData WHERE TaxFormID=TF.TaxFormID)
        END AS F14,
*/
        ISNULL(V.VendorName, 'TBD') AS Vendor,
/*        ISNULL(V.AddressLine1, '') AS F16,
        ISNULL(V.AddressLine2, '') AS F17,
        ISNULL(V.AddressLine3, '') AS F18,
*/
        CASE 
                WHEN TF.GoodsServicesID=0 THEN 
                        'TBD'
                ELSE  
                        ISNULL(TF.Description, TT.TransactionType+'; '+G.GoodsServiceName)
        END AS [Goods/Service],
--    ISNULL(TF.Description,'') AS F20,
        TF.FormTypeID, 
        P.PurchaseDate,
    T.TaxOfficeID,
    T.TaxOfficeName AS [Tax Office Name],
    IC.InstallationCode AS [Installation Code], 
    IC.InstallationTitle AS [Installation Title]

FROM dbo.tblTaxOffices T
        INNER JOIN dbo.vw_Agents A ON T.TaxOfficeID = A.TaxOfficeID 
        INNER JOIN dbo.tblTaxFormPackages P ON A.AgentID = P.AgentID 
        INNER JOIN dbo.tblTaxForms TF ON P.PackageID = TF.PackageID 
        INNER JOIN dbo.vw_Clients C ON P.ClientID = C.ClientID 
        INNER JOIN dbo.vw_Sponsors S ON C.SponsorID = S.SponsorID
        LEFT OUTER JOIN dbo.tblVendors V ON TF.VendorID = V.VendorID
        LEFT OUTER JOIN dbo.tblGoodsServices G ON TF.GoodsServicesID = G.GoodsServicesID
        LEFT OUTER JOIN dbo.tblTransactionTypes TT ON TF.TransTypeID = TT.TransTypeID
    INNER JOIN dbo.tblInstallationCodes IC ON T.InstallationCodeID = IC.InstallationCodeID


GO


/****** Object:  UserDefinedFunction [dbo].[ifnPurchaseOrders]    Script Date: 06/10/2010 14:42:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER FUNCTION [dbo].[ifnPurchaseOrders]
 
(
	@TaxOfficeID INT,
	@PackageID INT
)

RETURNS TABLE 

AS

RETURN

	SELECT 
		F.TaxFormID,
		F.OrderNumber As OrderNo,
		F.StatusFlags,
		F.StatusFlags AS UserFlags,
		C.Client+'  ('+CCode+')' As Client,
		T.TaxOfficeName,
		C.CCode,
		P.PurchaseDate As PODate,
		P.PackageID,
		P.ClientID,
		F.TransTypeID,
		F.GoodsServicesID,
		F.VendorID,
		P.AgentID,
		T.TaxOfficeID,
        C.SponsorID

	FROM tblTaxForms F
		INNER JOIN tblTaxFormPackages P ON F.PackageID=P.PackageID
		INNER JOIN tblTaxFormTypes FT ON F.FormTypeID=FT.FormTypeID
		INNER JOIN vw_Clients C ON P.ClientID=C.ClientID
		INNER JOIN tblTaxFormAgents A ON P.AgentID=A.AgentID
		INNER JOIN tblTaxOffices T ON A.TaxOfficeID=T.TaxOfficeID

	WHERE 
		(@TaxOfficeID IS NULL OR T.TaxOfficeID = @TaxOfficeID)
		AND (@PackageID IS NULL OR P.PackageID = @PackageID)

