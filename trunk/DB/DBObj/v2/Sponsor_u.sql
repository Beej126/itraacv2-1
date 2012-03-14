--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:01p $

/****** Object:  StoredProcedure [dbo].[Sponsor_u]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER On
GO

USE iTRAACv2
GO


/* testing
*/

if not exists(select 1 from sysobjects where name = 'Sponsor_u')
	exec('create PROCEDURE Sponsor_u as select 1 as one')
GO
alter PROCEDURE [dbo].[Sponsor_u]
@TableNames VARCHAR(1000) = NULL OUT,
@UserGUID UNIQUEIDENTIFIER,
@TaxOfficeId int,
@TaxOfficeCode VARCHAR(4),

@RowGUID UNIQUEIDENTIFIER,
@Rank VARCHAR(10),
@DEROS DATETIME,
@DutyLocation VARCHAR(100),
@DutyPhoneDSN1 VARCHAR(3),
@DutyPhoneDSN2 VARCHAR(4),
@AddressLine1 VARCHAR(100),
@OfficialMailCMR VARCHAR(10),
@OfficialMailBox VARCHAR(10),
@OfficialMailCity VARCHAR(50),
@OfficialMailState VARCHAR(2),
@OfficialMailZip varchar(5),
@HomePhoneCountry varchar(4),
@HomePhone varchar(12),
@HomeStreet varchar(50),
@HomeStreetNumber varchar(10),
@HomeCity varchar(50),
@HomePostal varchar(10),
@SuspensionExpiry DATETIME,
@SuspensionTaxOfficeId INT,
@IsUTAPActive bit
AS BEGIN

DECLARE @OfficialMailLine2 VARCHAR(100)
SET @OfficialMailLine2 = ISNULL('CMR '  + @OfficialMailCMR, '') + ISNULL(' BOX ' + @OfficialMailBox + ', ', '') + 
    ISNULL(@OfficialMailCity + ', ' , '') + ISNULL(@OfficialMailState + ', ' , '') + ISNULL(@OfficialMailZip, '')
    
DECLARE @AgentID INT, @OfficeID int
EXEC UserGUID_To_AgentID @UserGUID = @UserGUID, @TaxOfficeId = @TaxOfficeId, @AgentID = @AgentID OUT

DECLARE @Remarks VARCHAR(942)
DECLARE @ChangesTbl TABLE([Changes] VARCHAR(942))

UPDATE iTRAAC.dbo.tblSponsors SET
  [Rank] = @Rank,
  DEROS = @DEROS,
  DutyLocation = @DutyLocation,
  DutyPhone = @DutyPhoneDSN1 + '-' + @DutyPhoneDSN2,
  AddressLine1 = @AddressLine1,
  AddressLine2 = @OfficialMailLine2,
  HomePhoneCountry = @HomePhoneCountry,
  HomePhone = @HomePhone,
  HomeStreet = @HomeStreet,
  HomeStreetNumber = @HomeStreetNumber,
  HomeCity = @HomeCity,
  HomePostal = @HomePostal,
  IsUTAPActive = @IsUTAPActive
OUTPUT CASE WHEN ISNULL(DELETED.IsUTAPActive,0) <> ISNULL(INSERTED.IsUTAPActive,0) THEN 'UTAP Active: ' + dbo.BitToString(DELETED.IsUTAPActive) + ' -> ' + dbo.BitToString(INSERTED.IsUTAPActive) END INTO @ChangesTbl
WHERE RowGUID = @RowGUID

IF (@@ROWCOUNT = 1) BEGIN
  SELECT @Remarks = master.dbo.Concat([Changes],',') FROM @ChangesTbl WHERE [Changes] IS NOT NULL
  IF (@Remarks IS NOT null) EXEC Remark_u
    @TaxOfficeId = @TaxOfficeId,
    @UserGUID = @UserGUID,
    @RemarkTypeId = 24, -- 24 = customer status change
    @Remarks = @Remarks,
    @FKRowGUID = @RowGUID,
    @TableNames = @TableNames OUT
END

ELSE BEGIN
  declare @NewSponsorRowGUIDtbl TABLE(RowGUID UNIQUEIDENTIFIER)

  INSERT iTRAAC.dbo.tblSponsors (
    Active,
    StatusFlags,
    AgentID,
    RowGUID,
    CreateDate,
    CreateUserGUID,

    [Rank],
    DEROS,
    DutyLocation,
    DutyPhone,
    AddressLine1,
    AddressLine2,
    HomePhoneCountry,
    HomePhone,
    HomeStreet,
    HomeStreetNumber,
    HomeCity,
    HomePostal)
  OUTPUT INSERTED.RowGUID INTO @NewSponsorRowGUIDtbl
  VALUES (
    1, -- Active - bit
    0, -- StatusFlags - int
    @AgentID, -- AgentID - int
    @RowGUID, -- RowGUID - uniqueidentifier
    GETDATE(), -- CreateDate - datetime
    @UserGUID, -- CreateUserGUID - uniqueidentifier
    
    @Rank, -- Rank - varchar(10)
    @DEROS, -- DEROS - datetime
    @DutyLocation, -- DutyLocation - varchar(100)
    @DutyPhoneDSN1 + '-' + @DutyPhoneDSN2, -- DutyPhone - varchar(8)
    @AddressLine1, -- AddressLine1 - varchar(100)
    @OfficialMailLine2, -- AddressLine2 - varchar(100)
    @HomePhoneCountry, -- HomePhoneCountry - varchar(4)
    @HomePhone, -- HomePhone - varchar(12)
    @HomeStreet, -- Home Street - varchar(50)
    @HomeStreetNumber, -- Home Street Number - varchar(10)
    @HomeCity, -- Home City - varchar(50)
    @HomePostal  -- Home Postal - varchar(5)
  )
  
  SELECT @RowGUID = ISNULL(@RowGUID /*iTRAACv2 client actually passes in the @RowGUID on new customers*/, RowGUID) FROM @NewSponsorRowGUIDtbl 
  
  SET @Remarks = 'New Account (created in ' + @TaxOfficeCode + ')'
  EXEC Remark_u
    @TaxOfficeId = @TaxOfficeId,
    @UserGUID = @UserGUID,
    @RemarkTypeId = 24, -- 24 = customer status change
    @Remarks = @Remarks,
    @FKRowGUID = @RowGUID,
    @TableNames = @TableNames OUT
  
END

END
GO

grant execute on Sponsor_u to public
go

