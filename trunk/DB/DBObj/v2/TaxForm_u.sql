--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:01p $

/****** Object:  StoredProcedure [dbo].[TaxForm_u]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER On
GO

USE iTRAACv2
GO


/* testing

declare @p18 uniqueidentifier
exec dbo.TaxForm_u @TaxOfficeId=10000001,@RowGUID='AE9DB1C3-3468-484B-8C64-D61DF53E6FC1',@Used='2011-06-09 00:00:00',@TransactionTypeID=22,@GoodServiceGUID=NULL,@Description='sadfgdsfg',@VendorGUID='869FC996-C70C-4D25-86F7-68455E11B15D',@Returned=NULL,@ReturnUserGUID=NULL,@Filed='2011-06-09 17:42:32',@FileUserGUID='AB30A6B8-330C-49AE-B7D7-A4F7C0E34FAF',@TotalCost=$2600.0000,@CurrencyID=2,@CheckNumber=NULL,@LocationCode=NULL,@Incomplete=0,@StatusFlagsID=671088645,@ViolationRemarkGUID=@p18 output
select @p18

*/

if not exists(select 1 from sysobjects where name = 'TaxForm_u')
	exec('create PROCEDURE TaxForm_u as select 1 as one')
GO
alter PROCEDURE [dbo].[TaxForm_u]
@TaxOfficeId INT,
@TaxOfficeCode VARCHAR(10),
@UserGUID UNIQUEIDENTIFIER,
@TableNames VARCHAR(1000) = NULL OUT,

@RowGUID UNIQUEIDENTIFIER,
@IsFiling BIT = 0,

@FormTypeId INT,
@UsedDate DATETIME,
@TransTypeID int,
@Description VARCHAR(MAX),
@VendorGUID UNIQUEIDENTIFIER,
@TotalCost money,
@CurrencyUsed int,
@CheckNumber VARCHAR(25),
@LocationCode varchar(4),
@Incomplete BIT,
@StatusFlags INT,

-- these only for a new NF2
@SponsorGUID UNIQUEIDENTIFIER = NULL,
@AuthorizedDependentClientGUID UNIQUEIDENTIFIER = NULL
AS BEGIN

BEGIN TRY
BEGIN TRAN

UPDATE iTRAAC.dbo.tblTaxForms SET
  UsedDate        = @UsedDate,
  TransTypeID     = @TransTypeID,
  [Description]   = @Description,
  VendorGUID      = @VendorGUID,
  LocationCode    = @LocationCode,
  Incomplete      = @Incomplete,
  StatusFlags     = @StatusFlags
WHERE RowGUID = @RowGUID

IF (@@RowCount = 0) BEGIN
  -- NF1 TaxForm records get generated in batches via TaxFormPackage_new
  -- NF2's should be the only ones coming in this way
  IF (@FormTypeId <> 2) BEGIN RAISERROR('Invalid attempt to save new form of type other than NF2', 16,1) RETURN -1 END
  
  EXEC TaxFormPackage_new
    @TaxOfficeId = @TaxOfficeId,
    @TaxOfficeCode = @TaxOfficeCode,
    @FormTypeID = @FormTypeId,
    @FormCount = 1, -- int
    @SponsorGUID = NULL, -- uniqueidentifier
    @ClientGUID = NULL, -- uniqueidentifier
    @UserGUID = NULL, -- uniqueidentifier
    @Pending = NULL, -- bit
    @Remarks = '', -- varchar(max)
    @ServiceFee = NULL, -- decimal
    @TaxFormPackageGUID = NULL, -- uniqueidentifier
    @PackageCode = '', -- varchar(50)
    @DebugText = '' -- varchar(max)
  
  
  
  DECLARE @OrderNumber VARCHAR(16)
  SET @OrderNumber = dbo.TaxForm_OrderNumberBase(@FormTypeID, @TaxOfficeCode)
  --(SELECT ISNULL(MAX(CtrlNumber),0) FROM iTRAAC.dbo.tblTaxForms WHERE OrderNumber LIKE @OrderNumber + '%') + number,

END

IF (@TotalCost IS NOT NULL) BEGIN

  -- [Brent:15 June 2011] considering Violations, for example: "Over NF1 Limit"...
  -- one could readily envision this TaxForm_u sproc containing logic to check the 2500 limit and fire off the Violation Remark record
  -- however, after MUCH internal debate, i've decided to go with leaving that responsibility to the v2 Tax Form Business Object layer
  -- the leaning factor is that for the other non automatic violations (e.g. split forms), the user and hence the Tax Form object will be the *driver*
  -- there is no way to trigger those Violations from the sproc layer, because the database just doesn't know enough about the situation
  -- therefore, to keep things simple with a SINGLE approach, i'm deciding to initiate all violations the same way

  UPDATE iTRAAC.dbo.tblPPOData SET
    TotalCost = @TotalCost,
    CurrencyUsed = @CurrencyUsed,
    CheckNumber = @CheckNumber
  WHERE TaxFormGUID = @RowGUID
  
  IF (@@ROWCOUNT = 0) BEGIN
    DECLARE @TaxFormID INT
    SELECT @TaxFormID = TaxFormID FROM iTRAAC.dbo.tblTaxForms WHERE RowGUID = @RowGUID
    INSERT iTRAAC.dbo.tblPPOData (TaxFormGUID, TaxFormID, TotalCost, CheckNumber, CurrencyUsed) 
    VALUES (@RowGUID, @TaxFormID, @TotalCost, @CheckNumber, @CurrencyUsed)
  END
  
END

-- this logic has to be last so that the TaxForm SELECT it sends back to the client will reflect any StatusFlags changes in the main TaxForm UPDATE at the top of this proc
IF (@IsFiling = 1) EXEC TaxForm_ReturnFile 
  @TaxFormGUID = @RowGUID,
  @UserGUID = @UserGUID,
  @TaxOfficeCode = @TaxOfficeCode,
  @TaxOfficeId = @TaxOfficeId,
  @File = 1,
  @TableNames = @TableNames OUT
  


COMMIT TRAN
END TRY
BEGIN CATCH
  IF @@trancount > 0 ROLLBACK TRANSACTION
  DECLARE @error VARCHAR(1000)
  SET @error = ERROR_MESSAGE()
  RAISERROR(@error, 16,1)
END CATCH 


END
GO

grant execute on TaxForm_u to public
go

