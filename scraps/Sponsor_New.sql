--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 4/30/10 4:48p $

/****** Object:  StoredProcedure [dbo.[Sponsor_New    Script Date: 06/19/2009 15:58:30 ******/

/* testing:
declare @p1 uniqueidentifier
exec dbo.Sponsor_New @SponsorGUID=@p1 output,
  @UserGUID='2CEEEA9E-F2FE-428B-B966-8DB609599186',
  @TaxOfficeId='HD',
  @SponsorSSN='347-347-347',
  @SponsorLastName='Baker',
  @SponsorFirstName='Jim',
  @SponsorMI=NULL,
  @SponsorSuffix=NULL,
  @SponsorEmail='jb@um.com',
  @SpouseSSN='121-121-121',
  @SpouseLastName='Baker',
  @SpouseFirstName='Tammy',
  @SpouseMI='F',
  @SpouseSuffix=NULL,
  @SpouseEmail=NULL,
  @DutyPhone='370-1111',
  @DutyLocation='EUCOM J63',
  @RankCode='CIV',
  @DEROS='2011-08-31 00:00:00',
  @PersonalPhoneCountry='49',
  @PersonalPhoneNumber='62213388573',
  @OfficialMailLine1=NULL,
  @OfficialMailCMR='419',
  @OfficialMailBox='1472',
  @OfficialMailCity='APO',
  @OfficialMailState='AE',
  @OfficialMailZip='09102',
  @HostAddrStreet='Max-Joseph-Str.',
  @HostAddrNumber='48',
  @HostAddrCity='Heidelberg',
  @HostAddrPostal='69126'
select @p1
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'Sponsor_New')
	exec('create PROCEDURE Sponsor_New as select 1 as one')
GO
alter PROCEDURE Sponsor_New
@SponsorGUID UNIQUEIDENTIFIER = NULL OUT,

@UserGUID UNIQUEIDENTIFIER,
@TaxOfficeId int,

@SponsorSSN varchar(50),
@SponsorLastName varchar(50),
@SponsorFirstName varchar(50),
@SponsorMI varchar(1),
@SponsorSuffix varchar(50),
@SponsorEmail varchar(50),

@SpouseSSN varchar(50),
@SpouseLastName varchar(50),
@SpouseFirstName varchar(50),
@SpouseMI varchar(1),
@SpouseSuffix varchar(50),
@SpouseEmail varchar(50),

@DutyPhone varchar(50),
@DutyLocation varchar(100),
@RankCode VARCHAR(10),
@DEROS datetime,

@PersonalPhoneCountry varchar(10),
@PersonalPhoneNumber varchar(50),

@OfficialMailLine1  varchar(50),
@OfficialMailCMR varchar(10),
@OfficialMailBox varchar(10),
@OfficialMailCity varchar(50),
@OfficialMailState varchar(10),
@OfficialMailZip varchar(10),

@HomeStreet varchar(50),
@HomeNumber varchar(10),
@HomeCity varchar(50),
@HomePostal varchar(10)

AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

DECLARE @OfficialMailLine2 VARCHAR(100)
SET @OfficialMailLine2 = 'CMR ' + ISNULL(@OfficialMailCMR, '-') + ' BOX ' + ISNULL(@OfficialMailBox, '-') + ', ' + 
    ISNULL(@OfficialMailCity, '-') + ', ' + ISNULL(@OfficialMailState, '-') + ', ' + ISNULL(@OfficialMailZip, '-')

DECLARE @AgentID INT
EXEC UserGUID_To_AgentID @UserGUID = @UserGUID, @TaxOfficeId = @TaxOfficeId, @AgentID = @AgentID out

declare @NewSponsorRowGUID TABLE(RowGUID UNIQUEIDENTIFIER, SponsorID int)

-- SET XACT_ABORT ON will cause the transaction to be uncommittable if error occurs
SET XACT_ABORT ON;

BEGIN TRY
  BEGIN TRAN

  INSERT iTRAAC.dbo.tblSponsors (
    [Rank], DutyLocation, DutyPhone,
    HomePhoneCountry, HomePhone,
    AddressLine1, AddressLine2,
    Active, StatusFlags,
    AgentID, CreateUserID,
    DEROS, CreateDate,
    HomeStreet, HomeStreetNumber, HomeCity, HomePostal
  )
  OUTPUT INSERTED.RowGUID, INSERTED.SponsorID INTO @NewSponsorRowGUID(RowGUID, SponsorID)
  VALUES(
    @RankCode, @DutyLocation, @DutyPhone,
    @PersonalPhoneCountry, @PersonalPhoneNumber,
    @OfficialMailLine1, @OfficialMailLine2,
    1, -- Active - bit
    0 , -- StatusFlags - int
    @AgentID, @UserGUID,
    @DEROS, GETDATE(),
    @HomeStreet, @HomeNumber, @HomeCity, @HomePostal
  )

  DECLARE @SponsorID int
  SELECT @SponsorGUID = RowGUID, @SponsorID = SponsorID FROM @NewSponsorRowGUID

  INSERT iTRAAC.dbo.tblClients ( 
    SponsorID, SponsorGUID,
    FName, LName, MI, SuffixName,
    CCode,
    Email, SSN,
    Active, StatusFlags,
    AgentID, CreateUserGUID, PrimeTaxOfficeID, CreateDate
  ) VALUES  (
    @SponsorID, @SponsorGUID,
    @SponsorFirstName, @SponsorLastName, @SponsorMI, @SponsorSuffix,
    LEFT(@SponsorLastName,1) + RIGHT(@SponsorSSN, 4),
    @SponsorEmail, @SponsorSSN,
    1, 1, -- sponsor
    @AgentID, @UserGUID, @TaxOfficeId, GETDATE()
  )

  IF (@SpouseFirstName IS NOT NULL)
  BEGIN
    INSERT iTRAAC.dbo.tblClients ( 
      SponsorID, SponsorGUID,
      FName, LName, MI, SuffixName,
      CCode,
      Email, SSN,
      Active, StatusFlags,
      AgentID, CreateUserGUID, PrimeTaxOfficeID, CreateDate
    ) VALUES  (
      @SponsorID, @SponsorGUID,
      @SpouseFirstName, @SpouseLastName, @SpouseMI, @SpouseSuffix,
      LEFT(@SponsorLastName,1) + RIGHT(@SponsorSSN, 4), --make it a standard that everyone in the household gets the same code (to begin with anyway, marriage/divorce will still muddy this up... wouldn't be bad to change folk's codes to the sponsors when they join a new household)
      @SpouseEmail, @SpouseSSN,
      1, 2, -- spouse
      @AgentID, @UserGUID, @TaxOfficeId, GETDATE()
    )
  END

  COMMIT TRANSACTION;
  
END TRY
BEGIN CATCH
  DECLARE @ErrorMessage VARCHAR(MAX)
SET @ErrorMessage = ISNULL(ERROR_MESSAGE(),'') + ', Proc: ' + ISNULL(ERROR_PROCEDURE(),'-') + ', Line: ' + ISNULL(CONVERT(VARCHAR,ERROR_LINE()), '-')
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

grant execute on Sponsor_New to public
go
