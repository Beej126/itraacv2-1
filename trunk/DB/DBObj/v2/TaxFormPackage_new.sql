--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:01p $

/****** Object:  StoredProcedure [dbo].[TaxFormPackage_new]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

/* testing

exec TaxFormPackage_new @TaxOfficeCode='hd', @clientguid = '89BD8492-F0C2-4575-B662-4AE15AE05180', @UserGUID = '89BD8492-F0C2-4575-B662-4AE15AE05180', @servicefee = 12,
      @remarks = 'tortise', @delaysecs = 10
exec TaxFormPackage_new @TaxOfficeId='hd', @clientguid = '89BD8492-F0C2-4575-B662-4AE15AE05180', @UserGUID = '89BD8492-F0C2-4575-B662-4AE15AE05180', @servicefee = 12,
      @remarks = 'hare'

select * from itraac.dbo.tbltaxformpackages where packagecode like 'hd_10263-%'

select * from tbltaxoffices where 3008558 > taxofficeid

select * from tbltaxforms where packageguid = 'B0FBDC92-BCE0-E011-840F-00065BF0DA4B'

Checkpoint -- Write dirty pages to disk
DBCC FreeProcCache -- Clear entire proc cache
DBCC DropCleanBuffers -- Clear entire data cache


declare @p10 numeric(9,2)
declare @p11 uniqueidentifier
declare @p12 varchar(50)
declare @p13 varchar(max)
exec dbo.TaxFormPackage_New
  @TaxOfficeId=10000001,
  @TaxOfficeCode='HD',
  @UserGUID='2CEEEA9E-F2FE-428B-B966-8DB609599186',
  @FormTypeID=1,
  @FormCount=4,
  @SponsorGUID='C4714CC1-1F34-4602-B2A2-FF14CA7C431B',
  @ClientGUID='540FA79E-32DE-E011-840F-00065BF0DA4B',
  @Pending=0,
  @Remarks=default,
  @ServiceFee=@p10 output,
  @TaxFormPackageGUID=@p11 output,
  @PackageCode=@p12 output,
  @DebugText=@p13 output
select @p10, @p11, @p12, @p13

select * 
from iTRAAC.dbo.tbltaxforms f
JOIN iTRAAC.dbo.tblTaxFormPackages p ON p.RowGUID = f.PackageGUID
delete f from tbltaxforms f
where f.packageid in (
select packageid from iTRAAC.dbo.tbltaxformpackages where AuthorizedDependentClientGUID = '540FA79E-32DE-E011-840F-00065BF0DA4B'
)

--select * from tbltaxforms where ordernumber like 'NF1-HD-11-%'

*/
if not exists(select 1 from sysobjects where name = 'TaxFormPackage_new')
	exec('create PROCEDURE TaxFormPackage_new as select 1 as one')
GO 
alter PROCEDURE [dbo].[TaxFormPackage_new]
@TaxOfficeId int,
@TaxOfficeCode varchar(10),
@UserGUID UNIQUEIDENTIFIER,
@TaxFormGUIDs VARCHAR(MAX) = NULL OUT,

@FormTypeID INT,
@FormCount INT,
@SponsorGUID UNIQUEIDENTIFIER,
@ClientGUID UNIQUEIDENTIFIER,
@Pending BIT = 0,
@Remarks VARCHAR(MAX) = null,
@ServiceFee DECIMAL(9,2) = NULL OUT,
@TaxFormPackageGUID UNIQUEIDENTIFIER = NULL OUT,
@PackageCode VARCHAR(50) = NULL OUT,
@DebugText VARCHAR(MAX) = NULL OUT
AS BEGIN

SELECT @ServiceFee = TotalServiceFee FROM TaxFormPackageServiceFee WHERE FormTypeID = @FormTypeID AND FormCount = @FormCount

DECLARE @ClientID INT, @SellAgentID INT, @PackageID INT, @SponsorClientGUID UNIQUEIDENTIFIER

-- pull the sponsoring client that gets tagged with this package for evermore
SELECT @SponsorClientGUID = RowGUID FROM iTRAAC.dbo.tblClients WHERE SponsorGUID = @SponsorGUID AND StatusFlags & POWER(2,0) = POWER(2,0) AND Active = 1

--populate legacy ID columns to support transitional period with active V1 clients
SELECT @ClientID = ClientID FROM iTRAAC.dbo.tblClients WHERE RowGUID = ISNULL(@ClientGUID, @SponsorClientGUID)

EXEC UserGUID_To_AgentID @UserGUID = @UserGUID, @TaxOfficeId = @TaxOfficeId, @AgentID = @SellAgentID out
--SET @TaxFormPackageGUID = NEWSEQUENTIALID()

declare @NewTaxFormPackageRowGUID TABLE(RowGUID UNIQUEIDENTIFIER, PackageID int)

DECLARE @NewFormCount INT -- logical formcount to differentiate between previously printed and new forms, i.e. adding more forms to a package that's already been printed during this customer session
SET @NewFormCount = ABS(@FormCount) -- for $2 reprint scenario, we pass in -1 for @FormCount to hit the appropriate row in TaxFormPackageServiceFee

-- SET XACT_ABORT ON will cause the transaction to be uncommittable if error occurs
SET XACT_ABORT ON;

BEGIN TRY
  BEGIN TRAN
  
  -- if @PackageCode is passed in, we're ADDING more forms to a package that has already had some forms printed during this customer "session", so skip the package record insert
  IF (@PackageCode IS NULL) BEGIN
  
    SELECT @PackageCode = @TaxOfficeCode + '_' + CONVERT(VARCHAR, dbo.FiscalYear_f(null)) + right('00' + CONVERT(VARCHAR, DATEPART(DAYofyear, GETDATE())), 3) + '-'

    INSERT INTO iTRAAC.dbo.tblTaxFormPackages (
      TaxOfficeID,
      --nixing this:SponsorGUID, -- stamp the package with the original address it was sold to... so that reprints will always display the original "Duty Station"
      SponsorClientGUID, --this is the Sponsor who is *permanently* responsible for this package no matter how their sponsorship status evolves
      AuthorizedDependentClientGUID, --this is who it was physically sold to in the VAT office, could be a dependent or could just be the same as SponsorClientGUID
      ClientID, -- legacy
      SellUserGUID,
      AgentID,
      PurchaseDate,
      ServiceFee,
      Remarks,
      StatusFlags,
      PackageCode,
      --RowGUID ,  --wanting to use NEWSEQUENTIALID() in order to minimize index fragmentation, especially on a biggish table like this, but it can only be implemented as a default constraint so have to fire the insert and then gather the value via "clunky" OUTPUT clause below
      ExpirationDate)
    OUTPUT inserted.RowGUID, INSERTED.PackageID INTO @NewTaxFormPackageRowGUID(RowGUID, PackageID)
    VALUES (
      @TaxOfficeId,
      @SponsorClientGUID,
      @ClientGUID,
      @ClientID,
      @UserGUID,
      @SellAgentID,
      GETDATE(),
      @ServiceFee,
      @Remarks,
      0, -- StatusFlags - we never use this column
      @PackageCode+'0000', --initially insert with a placeholder packagecode
      --@TaxFormPackageGUID,
      DATEADD(year, 2, GETDATE()) --@ExpirationDate
    )


    SELECT
      @TaxFormPackageGUID = RowGUID,
      @PackageID = PackageID
    FROM @NewTaxFormPackageRowGUID
    
    --debug:PRINT 'New TaxFormPackageGUID: ' + CONVERT(VARCHAR(50), @TaxFormPackageGUID)

    -- create the "smart number" packagecode...
    -- the last portion is basically an identity
    -- i don't know how important it is to maintain this number going forward, we don't really use it anywhere in the system that i can tell
    -- but it was a fun little challenge that i decided to go through just to be safe
    -- same goes for OrderNumber below
    -- went with update with a nested select to create and implicit transaction context
    
    -- but the performance was insanely slow
    -- switched to a tran with ISOLATION LEVEL REPEATABLE READ to lock iTRAAC.dbo.tblTaxFormPackages from any other new inserts done in parallel

    --debug: PRINT '1: ' + CONVERT(VARCHAR, GETDATE(), 14)

    UPDATE iTRAAC.dbo.tblTaxFormPackages SET 
      PackageCode = @PackageCode + RIGHT('000' + 
        CONVERT(VARCHAR, 
          (SELECT ISNULL(MAX(RIGHT(PackageCode, 4)),0) + 1 
          FROM iTRAAC.dbo.tblTaxFormPackages
          WHERE PurchaseDate > dbo.FiscalStartDate_f(null)
          AND PackageCode LIKE @PackageCode + '%')
        )
      , 4)
    WHERE RowGUID = @TaxFormPackageGUID
    
  
  END -- IF @PackageCode is null
  
  ELSE BEGIN
  
    SELECT
      @TaxFormPackageGUID = RowGUID,
      @PackageID = PackageID
    FROM iTRAAC.dbo.tblTaxFormPackages WHERE PackageCode = @PackageCode
  
    SELECT @NewFormCount = @FormCount - COUNT(1) FROM iTRAAC.dbo.tblTaxForms WHERE PackageGUID = @TaxFormPackageGUID
    
  END

  --debug: PRINT '2: ' + CONVERT(VARCHAR, GETDATE(), 14)

  DECLARE @OrderNumber VARCHAR(16)
  SET @OrderNumber = dbo.TaxForm_OrderNumberBase(@FormTypeID, @TaxOfficeCode)
  -- select dbo.TaxForm_OrderNumberBase(1, 'HD')
                      
  DECLARE @NewTaxFormRowGUIDs TABLE(RowGUID UNIQUEIDENTIFIER)
  DECLARE @PrintDate DATETIME, @StatusFlags INT
  SET @StatusFlags = 0
  IF (@Pending = 0) SELECT @PrintDate = GETDATE(), @StatusFlags = 1 --issued

  INSERT iTRAAC.dbo.tblTaxForms (
    OrderNumber,
    CtrlNumber,
    PackageGUID,
    PackageID,
    FormTypeID,
    TransTypeID,
    StatusFlags,
    InitPrt215,
    InitPrtAbw,
    LocationCode
  )
  OUTPUT inserted.RowGUID INTO @NewTaxFormRowGUIDs(RowGUID)
  SELECT
    @OrderNumber,
    (
      SELECT ISNULL(MAX(CtrlNumber),0)
      FROM iTRAAC.dbo.tblTaxForms
      WHERE OrderNumber LIKE @OrderNumber + '%'
    ) + Number,
    @TaxFormPackageGUID,
    @PackageID,
    @FormTypeID,
    2, --the default NF1 TransactionType
    @StatusFlags,
    @PrintDate,
    @PrintDate,
    'CUST'
  FROM Integers
  WHERE number BETWEEN 1 AND @NewFormCount
  ORDER BY number ASC
  
  --debug: PRINT '3: ' + CONVERT(VARCHAR, GETDATE(), 14)
  --debug: select * from iTRAAC.dbo.tblTaxForms where packageguid = @TaxFormPackageGUID

  UPDATE iTRAAC.dbo.tblTaxForms SET
    -- for 4 digits or less, tack on left padded zeros
    OrderNumber = OrderNumber + CASE WHEN CtrlNumber > 9999 THEN CONVERT(VARCHAR, CtrlNumber) ELSE RIGHT('0000' + CONVERT(VARCHAR, CtrlNumber), 5) end
  WHERE PackageGUID = @TaxFormPackageGUID

  --debug: PRINT '4: ' + CONVERT(VARCHAR, GETDATE(), 14)
  COMMIT TRANSACTION;
  
  SELECT @TaxFormGUIDs = master.dbo.CONCAT(RowGUID) FROM @NewTaxFormRowGUIDs
  
END TRY
BEGIN CATCH
  DECLARE @ErrorMessage VARCHAR(MAX)
  SET @ErrorMessage = ISNULL(ERROR_MESSAGE(),'') + ', Proc: ' + ISNULL(ERROR_PROCEDURE(),'-') + ', Line: ' + ISNULL(CONVERT(VARCHAR, ERROR_LINE()), '-')
  --ERROR_NUMBER() AS ErrorNumber, ERROR_SEVERITY() AS ErrorSeverity, ERROR_STATE() AS ErrorState

  -- XACT_STATE:
    --  1, transaction is committable.
    -- -1, transaction is uncommittable and should be rolled back
    --  0, no transaction and a commit or rollback operation would generate an error
  IF (XACT_STATE() <> 0) ROLLBACK TRANSACTION;
  
  RAISERROR(@ErrorMessage, 16, 1)
END CATCH;

--debug: SELECT @DebugText = '@TaxFormPackageGUID: ' + CONVERT(VARCHAR(36), @TaxFormPackageGUID)

END
GO

grant execute on TaxFormPackage_new to public
go

