--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 4/30/10 4:48p $

/****** Object:  StoredProcedure [dbo].[Remark_u]    Script Date: 06/19/2009 15:58:30 ******/

/* testing:
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

-- [Brent:15 June 2011] considering Violations, for example: "Over NF1 Limit"...
-- one could readily envision the TaxForm_u sproc containing logic to check the 2500 limit and fire off the Violation Remark record
-- however, after MUCH internal debate, i've decided to go with leaving that responsibility to the v2 Tax Form Business Object layer
-- the leaning factor is that for the other non automatic violations (e.g. split forms), the user and hence the Tax Form object will be the *driver*
-- there is no way to trigger those Violations from the sproc layer, because the database just doesn't know enough about the situation
-- therefore, to keep things simple with a SINGLE approach, i'm deciding to initiate all violations the same way

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'Remark_u')
	exec('create PROCEDURE Remark_u as select 1 as one')
GO
alter PROCEDURE [dbo].Remark_u
@TaxOfficeId INT,
@UserGUID UNIQUEIDENTIFIER,
@TableNames VARCHAR(1000) = NULL OUT,

-- these are the only parms that will matter for an existing remark...
@RowGUID UNIQUEIDENTIFIER = NULL OUT,
@Alert BIT = 0, -- optional, since new form violation type remarks will automatically flip this bit in the sproc logic below
@Title VARCHAR(50) = null, --optional, since we use the @RemarkTypeId -> RemarkType.Title for Form Violations
@Remarks VARCHAR(942),
@DeleteReason varchar(200) = NULL,
@AlertResolved VARCHAR(200) = NULL,

-- for new remarks...
@FKRowGUID UNIQUEIDENTIFIER, -- this is the RowGUID of either Sponsor or TaxForm that we're assigning this Remark to
@RemarkTypeId INT = NULL -- optional, generic remarks have no type
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

SET XACT_ABORT ON;

BEGIN TRY
BEGIN TRAN


SET @RemarkTypeId = ISNULL(@RemarkTypeId, 0) --v1 depends on this

DECLARE @AgentId int, @AgentGUID UNIQUEIDENTIFIER
EXEC UserGUID_To_AgentID @UserGUID = @UserGUID, @TaxOfficeId = @TaxOfficeId, @AgentId = @AgentId OUT, @AgentGUID = @AgentGUID OUT

-- new v2 Remarks will only come from Sponsor or TaxForm...
DECLARE @TableId INT, @RowId INT, @OrderNumber varchar(20)
-- take advantage of the fact that since we're using GUIDs, only one of these queries will get a hit and thereby set the appropriate TableId
SELECT @RowId = SponsorID, @TableId = 9 /*sponsors*/ FROM iTRAAC.dbo.tblSponsors WHERE RowGUID = @FKRowGUID
SELECT @RowId = TaxFormId, @TableId = 14 /*tax forms*/, @OrderNumber = OrderNumber FROM iTRAAC.dbo.tblTaxForms WHERE RowGUID = @FKRowGUID
IF (@RowId IS NULL) BEGIN DECLARE @char_guid VARCHAR(50) SET @char_guid = CONVERT(VARCHAR(50), @FKRowGUID) RAISERROR('@FKRowGUID = ''%s'' is not a SponsorGUID or TaxFormGUID', 16, 1, @char_guid) END

-- update existing remark...
IF (@RowGUID IS NOT NULL) BEGIN

  IF @TableId = 9 /*Sponsor*/ SET @TableNames = ISNULL(@TableNames + ',', '') + 'Sponsor_Remark'
  ELSE IF @TableId = 14 /*TaxForm*/ SET @TableNames = ISNULL(@TableNames + ',', '') + 'TaxForm_Remark'
  UPDATE iTRAAC.dbo.tblRemarks SET
    LastUpdate = GETDATE(),
    LastAgentGUID = @AgentGUID,
    StatusFlags = dbo.BitFlipper_f(StatusFlags, 12, @Alert),
    Title = CASE WHEN RemType = 0 then @Title ELSE NULL END,
    Remarks = @Remarks,
    DeleteReason = @DeleteReason,
    AlertResolved = @AlertResolved
  OUTPUT
    INSERTED.RowGUID,
    INSERTED.LastUpdate,
    INSERTED.LastAgentGUID,
    CONVERT(BIT, INSERTED.StatusFlags & POWER(2,12)) AS Alert
  WHERE RowGUID = @RowGUID
  
  COMMIT TRANSACTION;
  RETURN
  
END
  
-- otherwise new remark...
DECLARE @CategoryId VARCHAR(20)
IF (@RemarkTypeId <> 0)
  SELECT
    @CategoryId = CategoryId,
    @Title = Title, -- Violations Remarks use the RemarkType.Title rather than their own Title (see Remark_v)
    -- some RemarkTypes drive the Alert on
    @Alert = ISNULL(@Alert, 0) | CASE WHEN @RemarkTypeId < 0 THEN 0 ELSE Alert END -- take the incoming alert flag or if defaulted true via RemarkTypes (negative @RemarkTypeId means we're turning off, so no alert)
  FROM RemarkType 
  WHERE RemarkTypeId = ABS(@RemarkTypeId)

-- passing NEGATIVE @RemarkTypeId turns off the Alert flag on any *other* Remarks of this *same*RemarkType* (tied to this same @FKRowGUID of course)
IF (@RemarkTypeId < 0) BEGIN
  IF @TableId = 9 /*Sponsor*/ SET @TableNames = ISNULL(@TableNames + ',', '') + 'Sponsor_Remark'
  ELSE IF @TableId = 14 /*TaxForm*/ SET @TableNames = ISNULL(@TableNames + ',', '') + 'TaxForm_Remark'
  UPDATE iTRAAC.dbo.tblRemarks SET StatusFlags = StatusFlags & ~POWER(2,12) 
  OUTPUT INSERTED.RowGUID, CONVERT(BIT, INSERTED.StatusFlags & POWER(2,12)) AS Alert
  WHERE FKRowGUID = @FKRowGUID AND RemType = ABS(@RemarkTypeId) AND StatusFlags & POWER(2,12) = POWER(2,12)
END

DECLARE @StatusFlags int
SET @StatusFlags = dbo.BitFlipper_f(0, 12, @Alert)

DECLARE @RemarkGUID_tbl TABLE(RemarkGUID UNIQUEIDENTIFIER)
INSERT iTRAAC.dbo.tblRemarks (
  RowID, TableID, RemType, Title, Remarks, RoleID, StatusFlags, FKRowGUID,
  CreateDate, CreateAgentGUID)
OUTPUT inserted.RowGUID INTO @RemarkGUID_tbl(RemarkGUID)
VALUES (
  @RowId, 
  @TableId, 
  @RemarkTypeId, 
  @Title, 
  @Remarks, 
  @AgentId * -1, -- RoleID - iTRAAC v1 does this thing where positive RoleId's mean Managers and negative mean Agents, but there are basically no Manager entries out there after 6 years
  @StatusFlags, 
  @FKRowGUID, 
  GETDATE(), -- CreateDate 
  @AgentGUID -- CreateAgentGUID
)
-- can't use OUTPUT INSERTED... directly and have to fire sprocs below since we're using Remark_v 
SELECT TOP 1 @RowGUID = RemarkGUID FROM @RemarkGUID_tbl
IF @TableId = 9 /*Sponsor*/ BEGIN SET @TableNames = ISNULL(@TableNames + ',', '') + 'Sponsor_Remark' EXEC Sponsor_Remarks @SponsorGUID = @FKRowGUID, @RemarkGUID = @RowGUID END
ELSE IF @TableId = 14 /*TaxForm*/ BEGIN SET @TableNames = ISNULL(@TableNames + ',', '') + 'TaxForm_Remark' EXEC TaxForm_Remarks @RemarkGUID = @RowGUID END


-- form violation specific logic...
IF (@CategoryId = 'FORM_VIOLATION') BEGIN
  DECLARE @SponsorGUID UNIQUEIDENTIFIER
  SELECT @SponsorGUID = c.SponsorGUID
  FROM iTRAAC.dbo.tblTaxForms f
  JOIN iTRAAC.dbo.tblTaxFormPackages p ON p.RowGUID = f.PackageGUID
  JOIN iTRAAC.dbo.tblClients c ON c.RowGUID = p.SponsorClientGUID
  WHERE f.RowGUID = @FKRowGUID
  
  SET @Remarks = 'Form Violation (' + @OrderNumber + ')'
  EXEC Sponsor_Suspend
    @TableNames = @TableNames OUT,
    @SponsorGUID = @SponsorGUID,
    @TaxOfficeId = @TaxOfficeId,
    @UserGUID = @UserGUID,
    @Remarks = @Remarks
END 

COMMIT TRANSACTION;


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
  

END
GO

grant execute on Remark_u to public
go
