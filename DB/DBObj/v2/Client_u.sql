--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:01p $

/****** Object:  StoredProcedure [dbo].[Client_u]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER On
GO

USE iTRAACv2
GO


/* testing
*/

if not exists(select 1 from sysobjects where name = 'Client_u')
	exec('create PROCEDURE Client_u as select 1 as one')
GO
alter PROCEDURE [dbo].[Client_u]
@UserGUID UNIQUEIDENTIFIER,
@TaxOfficeId int,
@TableNames VARCHAR(1000) = NULL OUT,

@RowGUID UNIQUEIDENTIFIER,
@SponsorGUID UNIQUEIDENTIFIER,
@Active BIT,
@IsSponsor BIT,
@IsSpouse BIT,
@SSN1 VARCHAR(3),
@SSN2 VARCHAR(2),
@SSN3 VARCHAR(4),
@DodId VARCHAR(10),
@FName VARCHAR(25),
@MI CHAR(1),
@LName VARCHAR(25),
@CCode VARCHAR(5),
@Suffix VARCHAR(10),
@Email VARCHAR(75)
AS BEGIN


-- SET XACT_ABORT ON will cause the transaction to be uncommittable if error occurs
SET XACT_ABORT ON;

BEGIN TRY
BEGIN TRAN


DECLARE @SSN VARCHAR(11)
SET @SSN = @SSN1 + '-' + @SSN2 + '-' + @SSN3
IF (ISNUMERIC(@SSN1) = 0 OR ISNUMERIC(@SSN2) = 0 OR ISNUMERIC(@SSN3) = 0 OR LEN(ISNULL(@SSN,'')) <> 11) BEGIN
  RAISERROR('Invalid SSN',16,1)
  RETURN
END

DECLARE @OldFName varchar(25), @OldLName VARCHAR(25), @OldCCode VARCHAR(10), @NameChangeRemarkTitle varchar(100)
SELECT @OldFName = FName, @OldLName = LName, @OldCCode = CCode FROM iTRAAC.dbo.tblClients WHERE RowGUID = @RowGUID

-- if update scenario then...
IF (@@ROWCOUNT > 0) BEGIN 

  UPDATE iTRAAC.dbo.tblClients SET
    Active = @Active,
    FName = @FName,
    MI = @MI,
    LName = @LName,
    CCode = @CCode,
    SuffixName = @Suffix,
    SSN = @SSN,
    DoDId = @DodID,
    Email = @Email
  WHERE RowGUID = @RowGUID

	-- be very careful about updating the f/lname fields...
	-- since that's a *key* legal element printed on forms and it would be best from an *AUDIT* standpoint to maintain crucial consistentcy between paper and database,
	-- therefore, if the names are significantly different (via soundex), then "stamp" the previous name on all the existing form records in a new field
	-- then the TaxForm_print sproc simply either prints this name field if it's non null, otherwise it traverses the existing ClientGUID pointer to pull the corresponding name
	-- the DIFFERENCE function returns 1-4, a smaller number means LESS *similarity* between the two compared
	-- so if we get less than 4, the new name was different enough to warrant stamping the current name on the old forms
	-- actually allow the first name to vary a little more (threshold = 3) since it seems more likely to be a spelling correction than someone truly changing their first name
	SET @NameChangeRemarkTitle = 'Name Change'
	IF (DIFFERENCE(@OldFName, @FName) < 3 OR DIFFERENCE(@OldLName, @LName) < 4) BEGIN
	  SET @NameChangeRemarkTitle = @NameChangeRemarkTitle + ' (Significant)'
	  UPDATE iTRAAC.dbo.tblTaxFormPackages SET OriginalSponsorName = @OldLName + ', ' + @OldFName + ' (' + @OldCCode + ') *'
	  WHERE SponsorClientGUID = @RowGUID AND OriginalSponsorName IS NULL -- < where NULL is absolutely critical to allow multiple name changes over time... and yes, that does happen, marriage/divorce/remarriage is *AMAZINGLY* common in reality
	  
	  UPDATE iTRAAC.dbo.tblTaxFormPackages SET OriginalDependentName = @OldLName + ', ' + @OldFName + ' (' + @OldCCode + ') *'
	  WHERE AuthorizedDependentClientGUID = @RowGUID AND OriginalDependentName IS NULL
	END
	
	IF (@OldLName <> @LName OR @OldFName <> @FName OR @OldCCode <> @CCode) BEGIN
	  -- log the change
	  DECLARE @Remarks VARCHAR(942)
	  SET @Remarks = @OldLName + ', ' + @OldFName + ' (' + @OldCCode + ') => ' + @LName + ', ' + @FName + ' (' + @CCode + ')'
	  EXEC Remark_u
		  @TaxOfficeId = @TaxOfficeId,
		  @UserGUID = @UserGUID,
		  @Title = @NameChangeRemarkTitle,
		  @Remarks = @Remarks,
		  @FKRowGUID = @SponsorGUID,
		  @TableNames = @TableNames OUT  
	END
END

-- otherwise insert logic...
ELSE BEGIN
  DECLARE @AgentID INT, @OfficeID INT, @SponsorID INT
  EXEC UserGUID_To_AgentID @UserGUID = @UserGUID, @TaxOfficeId = @TaxOfficeId, @AgentID = @AgentID OUT
  
  SELECT @SponsorID = SponsorID FROM iTRAAC.dbo.tblSponsors WHERE RowGUID = @SponsorGUID
  
  SELECT @CCode = LEFT(@LName,1) + RIGHT(@SSN, 4)
  IF (@IsSponsor = 0) -- if this isn't the sponsor, pull the CCode from the Sponsor to keep new dependents consistent
    SELECT @CCode = CCode FROM iTRAAC.dbo.tblClients 
    WHERE SponsorGUID = @SponsorGUID
    AND StatusFlags & POWER(2,0) = POWER(2,0)
  IF (@CCode IS NULL) SET @CCode = LEFT(@LName, 1) + @SSN3
  
  INSERT iTRAAC.dbo.tblClients (
    StatusFlags,
    SSN, DoDId, FName, MI, LName, SuffixName, Email, CCode, 
    Active, AgentID, CreateDate, CreateUserGUID, PrimeTaxOfficeID, SponsorGUID, SponsorID
  ) VALUES (
    @IsSponsor * POWER(2,0) | @IsSpouse * POWER(2,1),
    @SSN, @DodId, @FName, @MI, @LName, @Suffix, @Email, @CCode,
    1, @AgentID, GETDATE(), @UserGUID, @TaxOfficeId, @SponsorGUID, @SponsorID
  )
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

grant execute on Client_u to public
go
