--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:01p $

/*
           ___ __          __              _     _                     _             _      
     /\   |  _ \ \        / /     /\      | |   | |                   | |           (_)     
    /  \  | |_) \ \  /\  / /     /  \   __| | __| |_ __ ___ ___ ___   | | ___   __ _ _  ___ 
   / /\ \ |  _ < \ \/  \/ /     / /\ \ / _` |/ _` | '__/ _ | __/ __|  | |/ _ \ / _` | |/ __|
  / ____ \| |_) | \  /\  /     / ____ \ (_| | (_| | | |  __|__ \__ \  | | (_) | (_| | | (__ 
 /_/    \_\____/   \/  \/     /_/    \_\__,_|\__,_|_|  \___|___/___/  |_|\___/ \__, |_|\___|
                                                                               __/ |       
                                                                              |___/        

sp_procsearch 'InitPrt215'
*/






/****** Object:  StoredProcedure [dbo].[TaxForm_print]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

/* testing
SELECT * INTO #TT FROM vw_NFx WHERE TaxFormID=@TaxFormID AND FormTypeID=@FormTypeID
SELECT * FROM iTRAAC.dbo.tblFormFields WHERE FormTypeID = 1
select * from itraac.dbo.tbltaxforms where ordernumber = 'NF1-HD-08-08382'
TaxForm_print 'FD1C164A-24EF-4BC9-A209-E31ABF8A8382', 1
TaxForm_print '00000000-0000-0000-0000-000000000000', 1 
*/

if not exists(select 1 from sysobjects where name = 'TaxForm_print')
	exec('create PROCEDURE TaxForm_print as select 1 as one')
GO
alter PROCEDURE [dbo].[TaxForm_print]
@TaxFormGUID UNIQUEIDENTIFIER,
@PrintComponent INT, -- PO=0x1, Abw=0x2
@TableNames varchar(1000) = null OUT
AS BEGIN

SET NOCOUNT ON

SET @TableNames = 'PO,Abw,TaxForm'

DECLARE @POFlag int, @AbwFlag int
SELECT @POFlag = 0x1, @AbwFlag = 0x2

DECLARE
  @FormTypeID INT,
  @OrderNumber VARCHAR(50),
  @Description VARCHAR(500),
  @TransactionTypeID INT,
  @AgencyLine1 VARCHAR(100),
  @AgencyLine2 VARCHAR(100),
  @AgencyLine3 VARCHAR(100),
  @AgencyLine4 VARCHAR(100),
  @SellUserGUID UNIQUEIDENTIFIER,
  @SigBlock VARCHAR(50),
  @PurchaseDate VARCHAR(15),
  @ExpirationDate varchar(15),
  @AuthorizedDependentClientGUID UNIQUEIDENTIFIER,
  @SponsorClientGUID UNIQUEIDENTIFIER,
  @SponsorGUID UNIQUEIDENTIFIER,
  @SponsorName varchar(100),
  @SponsorCCode varchar(10),
  @DutyLocation VARCHAR(100),
  @AuthorizedDependentName varchar(100),
  @VendorGUID UNIQUEIDENTIFIER,
  @VendorName VARCHAR(100),
  
  @PackageGUID UNIQUEIDENTIFIER,
  @TaxOfficeID INT,
  
  --NF2 only fields
  @VendorAddress VARCHAR(100),
  @NF2Amount varchar(100),
  @Currency VARCHAR(20),
  @TransactionTypeID INT,
  @TransactionType VARCHAR(100)
  
SELECT 
  @FormTypeID = f.FormTypeID,
  @OrderNumber = f.OrderNumber,
  @Description = f.[Description],
  @TransactionTypeID = f.TransTypeID,
  @VendorGUID = f.VendorGUID,
  @PackageGUID = f.PackageGUID,
  @TransactionTypeID = f.TransTypeID
FROM iTRAAC.dbo.tblTaxForms f
WHERE f.RowGUID = @TaxFormGUID

select
  @PurchaseDate = iTRAAC.dbo.ClearDate(p.PurchaseDate),
  @ExpirationDate = iTRAAC.dbo.ClearDate(p.ExpirationDate),
  @SellUserGUID = p.SellUserGUID,
  @AuthorizedDependentClientGUID = p.AuthorizedDependentClientGUID,
  @SponsorClientGUID = p.SponsorClientGUID,
  @SponsorName = p.OriginalSponsorName, --this will only be populated if the SponsorClient record has had it's name changed after this form was created (it will include the old CCode for reference)
  @AuthorizedDependentName = p.OriginalDependentName
  @TaxOfficeID = p.TaxOfficeID
FROM iTRAAC.dbo.tblTaxFormPackages p 
WHERE p.RowGUID = @PackageGUID

select
  @AgencyLine1 = o.AgencyLine1,
  @AgencyLine2 = o.AgencyLine2,
  @AgencyLine3 = o.AgencyLine3,
  @AgencyLine4 = o.FormFootNoteLine1
FROM iTRAAC.dbo.tblTaxOffices o 
WHERE o.TaxOfficeID = @TaxOfficeID



SELECT @SigBlock = a.SigBlock
FROM iTRAAC.dbo.tblTaxFormAgents a
JOIN iTRAAC.dbo.tblUsers u ON u.RowGUID = a.UserGUID
WHERE u.RowGUID = @SellUserGUID

-- pull sponsor and dependent that form was specifically sold to
IF (@SponsorName IS NULL) SELECT @SponsorName = UPPER(LName + ', ' + FName), @SponsorCCode = '(' + UPPER(CCode) + ')' FROM iTRAAC.dbo.tblClients WHERE RowGUID = @SponsorClientGUID
-- if the Sold-To Client is not the sponsor, pull the AuthorizedDependent name
IF (@AuthorizedDependentName IS NULL) SELECT @AuthorizedDependentName = UPPER(LName + ', ' + FName) FROM iTRAAC.dbo.tblClients WHERE RowGUID = @AuthorizedDependentClientGUID -- @AuthorizedDependentClientGUID only populated if diff from Sponsor

IF (@FormTypeID = 2)
BEGIN
  SELECT @VendorName = VendorName FROM iTRAAC.dbo.tblVendors WHERE RowGUID = @VendorGUID
  SELECT @VendorAddress = [Address] FROM Vendor_Address_v WHERE RowGUID = @VendorGUID
  SELECT @Currency = CASE CurrencyUsed WHEN 1 THEN 'U.S. DOLLARS' ELSE 'EURO' END FROM iTRAAC.dbo.tblPPOData WHERE TaxFormGUID = @TaxFormGUID
  SELECT @TransactionType + ISNULL(' - ' + @Description, '') FROM iTRAAC.dbo.tblTransactionTypes WHERE TransTypeID = @TransactionTypeID
  
  IF (@TransactionTypeID = 29) THEN (SELECT Title +': '+Remarks FROM tblRemarks WHERE RowID=TF.TaxFormID AND TableID=14 AND RemType=17)
  WHEN TF.TransTypeID = 31 THEN (SELECT Title +': '+Remarks FROM tblRemarks WHERE RowID=TF.TaxFormID AND TableID=14 AND RemType=3)
  ELSE '' END AS F15,

  WHEN TF.TransTypeID = 29 THEN (SELECT Title +': '+Remarks FROM tblRemarks WHERE RowID=TF.TaxFormID AND TableID=14 AND RemType=16)
  WHEN TF.TransTypeID = 31 THEN (SELECT Title +': '+Remarks FROM tblRemarks WHERE RowID=TF.TaxFormID AND TableID=14 AND RemType=7)
  ELSE '' END AS F16,

  WHEN TF.TransTypeID = 29 THEN (SELECT Title +': '+Remarks FROM tblRemarks WHERE RowID=TF.TaxFormID AND TableID=14 AND RemType=13)
  WHEN TF.TransTypeID = 31 THEN (SELECT Title +': '+Remarks FROM tblRemarks WHERE RowID=TF.TaxFormID AND TableID=14 AND RemType=8)
  ELSE '' END AS F17,

  WHEN TF.TransTypeID = 29 THEN (SELECT Title +': '+Remarks FROM tblRemarks WHERE RowID=TF.TaxFormID AND TableID=14 AND RemType=12)
  WHEN TF.TransTypeID = 31 THEN (SELECT Title +': '+Remarks FROM tblRemarks WHERE RowID=TF.TaxFormID AND TableID=14 AND RemType=9)
  ELSE '' END AS F18,

END    


-- Order Form data & fields positions...

-- if PO was not requested, send back dummy placeholder table
IF (@PrintComponent & @POFlag = 0) SELECT 1 WHERE 1=0 -- dummy table so that the client can consistently look for Abw in Table[1]
--otherwise if both or PO are requested, send PO data
ELSE BEGIN
  -- request test print by passing @TaxFormGUID = '00000000-0000-0000-0000-000000000000'
  IF (@TaxFormGUID = '00000000-0000-0000-0000-000000000000')
    EXEC TaxForm_Print_FieldDefs @FormTypeID = 1
    
  ELSE BEGIN
  
    -- load the list of fields 
    DECLARE @t TABLE(Data VARCHAR(500), FieldName VARCHAR(10), Row int, Col INT, MaxLength INT, MaxRows INT, UNIQUE CLUSTERED (FieldName))
    INSERT @t SELECT NULL, RTRIM(FieldName), FLOOR(StartRow), StartCol, MaxLength, MaxRows FROM iTRAAC.dbo.tblFormFields WHERE FormTypeID = 1

    -- set all the values
    UPDATE @t SET DATA = @OrderNumber WHERE FieldName = 'F1'
    UPDATE @t SET DATA = @AgencyLine1 WHERE FieldName = 'F2'
    UPDATE @t SET DATA = @AgencyLine2 WHERE FieldName = 'F3'
    UPDATE @t SET DATA = @AgencyLine3 WHERE FieldName = 'F4'
    UPDATE @t SET DATA = @AgencyLine4 WHERE FieldName = 'F5'
    UPDATE @t SET DATA = @PurchaseDate WHERE FieldName = 'F6'
    UPDATE @t SET DATA = @ExpirationDate WHERE FieldName = 'F7'
    UPDATE @t SET DATA = @SigBlock WHERE FieldName = 'F8'
    UPDATE @t SET DATA = 'XXXX XXXX' WHERE FieldName = 'F9' --NF1 form type checkbox

    -- F10 = NF1 amount field, blank for NF1 and XXX'd out for NF2
    UPDATE @t SET DATA = CASE WHEN (@FormTypeID = 2) then 'XXXXXXXXXXXXXXXXXXXX' END WHERE FieldName = 'F10'
    
    UPDATE @t SET DATA = 
      -- for NF2: simply print the total amount
      CASE WHEN (@FormTypeID = 2) then (SELECT CONVERT(VARCHAR, TotalCost) FROM iTRAAC.dbo.tblPPOData WHERE TaxFormGUID = @TaxFormGUID)
      -- for NF1: print "not valid for purchases of € 2,500.00 net or more!" to more prominently block out the NF2 amount field
      ELSE 'ungültig für Einkäufe von Netto 2500,00 € oder höher!' END --CHAR(14) is a custom indicator to the client logic to send the ESC codes to go into German character set, char(15) turns it off
    WHERE FieldName = 'F11' -- NF2 amount field, crossed out for NF1's
    
    UPDATE @t SET DATA = @SponsorName WHERE FieldName = 'F12'
    UPDATE @t SET DATA = @AuthorizedDependentName WHERE FieldName = 'F13'
    UPDATE @t SET DATA = @TransactionType WHERE FieldName = 'F14'
    UPDATE @t SET DATA = @SponsorCCode WHERE FieldName = 'F15' 
    UPDATE @t SET DATA = (SELECT UPPER(VendorName) FROM iTRAAC.dbo.tblVendors WHERE RowGUID = @VendorGUID) WHERE FieldName = 'F16' -- F16 = Vendor info
    UPDATE @t SET DATA = '[ ] Returned      [ ] Filed' WHERE FieldName = 'F17'

    SELECT * FROM @t ORDER BY CONVERT(INT, RIGHT(FieldName, LEN(FieldName)-1))

    UPDATE iTRAAC.dbo.tblTaxForms SET InitPrt215 = GETDATE() WHERE RowGUID = @TaxFormGUID
  END
END

-- if Abw was not requested, send back dummy placeholder table
IF (@PrintComponent & @AbwFlag = 0) BEGIN SELECT 1 WHERE 1=0 END --dummy table so that the client is completely driven by the proc and doesn't have to code around presence of OrderForm or Abw resultset

-- otherwise either Both or Abw were requested...
ELSE BEGIN
  -- Abw data & fields positions
  IF (@TaxFormGUID = '00000000-0000-0000-0000-000000000000')
    EXEC TaxForm_Print_FieldDefs @FormTypeID = 3

  ELSE BEGIN
  
    SELECT @DutyLocation = s.DutyLocation
    FROM tblClients c
    JOIN tblSponsors s ON s.RowGUID = c.SponsorGUID
    WHERE c.RowGUID = @SponsorClientGUID
  
    -- load the list of fields 
    DECLARE @t TABLE(Data VARCHAR(500), FieldName VARCHAR(10), Row int, Col INT, MaxLength INT, MaxRows INT, UNIQUE CLUSTERED (FieldName))
    INSERT @t SELECT NULL, RTRIM(FieldName), FLOOR(StartRow), StartCol, MaxLength, MaxRows FROM iTRAAC.dbo.tblFormFields WHERE FormTypeID = 3

    -- set all the values
    UPDATE @t SET DATA = 'XXXX' WHERE FieldName = 'F1'

    UPDATE @t SET DATA = @VendorName WHERE FieldName = 'F2'
    UPDATE @t SET DATA = @VendorAddress WHERE FieldName = 'F3'
    -- F4, not used anymore, used to be second line of vendor address, now covered by word wrapping 2 lines of F3

    UPDATE @t SET DATA = @OrderNumber WHERE FieldName = 'F5'
    -- F6 is date but not printed... maybe to avoid trouble 

    UPDATE @t SET DATA = @AgencyLine1 WHERE FieldName = 'F7'
    UPDATE @t SET DATA = @AgencyLine2 WHERE FieldName = 'F8'
    UPDATE @t SET DATA = @AgencyLine3 WHERE FieldName = 'F9'
    UPDATE @t SET DATA = @AgencyLine4 WHERE FieldName = 'F30'
    
    UPDATE @t SET DATA = ISNULL(@AuthorizedDependentName + ' / ', '') + @SponsorName WHERE FieldName = 'F10'
    UPDATE @t SET DATA = @DutyLocation WHERE FieldName = 'F11'

    UPDATE @t SET DATA = @Currency WHERE FieldName = 'F12'
    UPDATE @t SET DATA = @TransactionType WHERE FieldName = 'F13'
    
  END

  UPDATE iTRAAC.dbo.tblTaxForms SET InitPrtAbw = GETDATE() WHERE RowGUID = @TaxFormGUID
END

-- lastly, return updated print date fields to the client so "printed" status changes visually
SELECT RowGUID, InitPrt215, InitPrtAbw FROM iTRAAC.dbo.tblTaxForms WHERE RowGUID = @TaxFormGUID
  

END
GO

grant execute on TaxForm_print to public
go

