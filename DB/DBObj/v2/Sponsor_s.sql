--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:07p $

/* testing:
exec Sponsor_s @GUID = '87BC09AB-A9B4-487F-A1C5-7911FAAFB6B7'
exec Sponsor_s @GUID = '00000000-0000-0000-0000-000000000000'
UPDATE iTRAAC.dbo.tblClients SET StatusFlags = 0 WHERE SponsorGUID = '87BC09AB-A9B4-487F-A1C5-7911FAAFB6B7'
EXEC Customer_Search @CCode = 'A0999'
EXEC Sponsor_s @GUID = '9302B079-3307-4097-A5B8-7FC6BBC4D3AB'
*/

/****** Object:  StoredProcedure [dbo].[Sponsor_s]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'Sponsor_s')
	exec('create PROCEDURE Sponsor_s as select 1 as one')
GO
alter PROCEDURE [dbo].[Sponsor_s] 
@GUID UNIQUEIDENTIFIER OUT, -- must be a Sponsor.ROWGUID
@TableNames VARCHAR(max) = NULL OUT
AS BEGIN

SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED 
SET NOCOUNT ON

SET @TableNames = 'Sponsor, Client, Sponsor_TaxForm, Sponsor_Remark, Sponsor_UTAP' 

IF (@GUID <> dbo.EmptyGUID()) BEGIN
  DECLARE @SponsorCount INT
  SELECT @SponsorCount = COUNT(1) FROM iTRAAC.dbo.tblClients WHERE SponsorGUID = @GUID and StatusFlags & POWER(2,0) = POWER(2,0) 
  -- conversion needs to be responsible for these chronic invalid data issues rather than trying to gloss them over here
  IF (@SponsorCount <> 1) BEGIN
    DECLARE @CentralAdmin VARCHAR(100)
    SELECT @CentralAdmin = CentralAdmin FROM iTRAAC.dbo.tblControlCentral
  END
  
  IF (@SponsorCount = 0) 
    RAISERROR('No Member as been flagged as the Sponsor. Call the iTRAAC Admin on this - %s', 10,0, @CentralAdmin)
  ELSE IF (@SponsorCount > 1) 
    RAISERROR('More than one Member is flagged as Sponsor.  Call the iTRAAC Admin on this - %s', 10,0, @CentralAdmin) 
END

DECLARE @OfficialMailCMR varchar(3), @OfficialMailBox varchar(4), @OfficialMailCity varchar(50), @OfficialMailState varchar(2), @OfficialMailZip varchar(5)
EXEC Sponsor_Split_AddrLine2 @SponsorGUID=@GUID, @OfficialMailCMR=@OfficialMailCMR OUT, @OfficialMailBox=@OfficialMailBox OUT,
                             @OfficialMailCity=@OfficialMailCity OUT, @OfficialMailState=@OfficialMailState OUT, @OfficialMailZip=@OfficialMailZip OUT
DECLARE @SuspensionExpiry DATETIME, @SuspensionTaxOfficeId INT
-- to reduce ambiguity, v2 consolidates suspends at the sponsor level just like bars
-- so pull the list of clients under this sponsor ... TODO:2:once v1 is flushed out of the field we could move this field up to tblSponsor
-- ABS is because v1 puts a negative agentid in this field
SELECT @SuspensionExpiry = MAX(SuspensionExpiry), @SuspensionTaxOfficeId = MAX(ABS(SuspensionRoleID)) FROM iTRAAC.dbo.tblClients WHERE SponsorGUID = @GUID AND SuspensionExpiry is NOT NULL
-- if suspension has already expired, then turn it off
IF (@SuspensionExpiry < GETDATE()) SELECT @SuspensionExpiry = NULL, @SuspensionTaxOfficeId = NULL
ELSE IF @SuspensionTaxOfficeId = 0 SET @SuspensionTaxOfficeId = NULL --v1 drops 0 into this field for NULL
-- v1 also puts an agentid/managerid in this field rather than an office field... but we're moving to office since particular individuals tend to come and go
ELSE SELECT @SuspensionTaxOfficeId = TaxOfficeId FROM iTRAAC.dbo.tblTaxOffices WHERE @SuspensionTaxOfficeId BETWEEN TaxOfficeID AND TaxOfficeID + 9999999

-- since Active is such a critical flag (i.e. selling forms or not)
-- we need to code defensively around v1 data... i.e. take either Sponsor.Active && Sponsoring-Client.Active
-- v2 will then save the Active flag consistently to both the Sponsor and Sponsoring-Client records
-- the v2 client will be responsible for enforcing the other "no-sell" flags... currently suspended is the only other one
DECLARE @HouseholdActive bit
SELECT @HouseholdActive = Active FROM iTRAAC.dbo.tblSponsors WHERE RowGUID = @GUID
SELECT @HouseholdActive = @HouseholdActive & -- Sponsor record active
  CONVERT(BIT, isnull( (SELECT Active FROM iTRAAC.dbo.tblClients -- Sponsoring Client Record active
                WHERE SponsorGUID = @GUID
                AND StatusFlags & POWER(2,0) = POWER(2,0)), 0))


-- Sponsor table (Household info like phone, address, stats)
SELECT
  s.RowGUID,
--  @SponsorName AS SponsorNameID, --this is what shows up on the Tab header
  @HouseholdActive AS Active,
  s.[Rank],
  s.DEROS,
  s.DutyLocation,
  LEFT(s.DutyPhone, 3) AS DutyPhoneDSN1,
  RIGHT(s.DutyPhone, 4) AS DutyPhoneDSN2,
  s.AddressLine1,
  CASE WHEN @OfficialMailCity IS NULL AND @OfficialMailCity IS NULL then s.AddressLine2 ELSE NULL END AS AddressLine2,
  @OfficialMailCMR AS OfficialMailCMR,
  @OfficialMailBox AS OfficialMailBox,
  @OfficialMailCity AS OfficialMailCity,
  @OfficialMailState AS OfficialMailState,
  @OfficialMailZip AS OfficialMailZip,
  s.HomePhoneCountry,
  s.HomePhone,
  s.HomeStreet,
  s.HomeStreetNumber,
  s.HomeCity,
  s.HomePostal,
  @SuspensionExpiry AS SuspensionExpiry,
  @SuspensionTaxOfficeId AS SuspensionTaxOfficeId,
  s.IsUTAPActive,
  v.*
FROM iTRAAC.dbo.tblSponsors s
CROSS APPLY dbo.Sponsor_ViolationInfo_f(@GUID) v
WHERE RowGUID = @GUID

-- Household "Members" - i.e. Client table records
SELECT
  RowGUID,
  @GUID AS SponsorGUID, 
  CONVERT(BIT, StatusFlags & POWER(2,0)) AS IsSponsor,
  CONVERT(BIT, StatusFlags & POWER(2,1)) AS IsSpouse,
  CASE WHEN StatusFlags & POWER(2,0) = POWER(2,0) THEN @HouseholdActive else Active END AS Active,
  FName,
  LName,
  MI,
  SuffixName AS Suffix,
  LEFT(SSN, 3) AS SSN1, SUBSTRING(SSN, 5, 2) AS SSN2, RIGHT(SSN, 4) AS SSN3,
  DoDId,
  CCode,
  Email,
  BirthDate
FROM iTRAAC.dbo.tblClients
WHERE SponsorGUID = @GUID

-- "Sponsor_TaxForm" - 'Light' TaxForm list table (not a full blown deep list of all Tax Form fields)
EXEC Sponsor_TaxForms @GUID = @GUID

-- "Sponsor_Remark" (aka Diary)
EXEC Sponsor_Remarks @SponsorGUID=@GUID

-- Sponsor_UTAP
SELECT
  @GUID AS RowGUID,
  NULL AS [Start Date], 
  NULL AS [Signup Fee], 
  NULL AS [Electricity Vendor],
  NULL AS [Electricity Account#],
  NULL AS [Electricity Contract],
  NULL AS [Electric Meter 1],
  NULL AS [Electric Meter 2],
  NULL AS [Gas Vendor],
  NULL AS [Gas Account#],
  NULL AS [Gas Contract],
  NULL AS [Gas Meter],
  NULL AS [Water Vendor],
  NULL AS [Water Account#],
  NULL AS [Water Contract],
  NULL AS [Water Meter]


-- running all the "extra" queries above ensures that the cache is populated with all fields of the Sponsor table
-- before returning this simple minimal result set just for this row
IF (@GUID = dbo.EmptyGUID())
  EXEC Sponsor_New @NewSponsorGUID = @GUID OUT, @TableNames = @TableNames OUT



END
GO

grant execute on Sponsor_s to public
go

