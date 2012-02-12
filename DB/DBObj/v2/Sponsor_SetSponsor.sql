--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:01p $

/****** Object:  StoredProcedure [dbo].[Sponsor_SetSponsor]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER On
GO

USE iTRAACv2
GO

/* testing
declare @p6 varchar(max)
exec dbo.Sponsor_SetSponsor @TaxOfficeId=10000001,@UserGUID='AB30A6B8-330C-49AE-B7D7-A4F7C0E34FAF',@SponsorGUID='6B8AB256-BEA7-4F3C-92AA-66E342144087',@NewSponsorClientGUID='56BEA131-CA1B-46ED-A74E-27C272EB53F4',@FixExistingPackageLinks=default,@TableNames=@p6 output
select @p6

UPDATE tblClients SET StatusFlags = StatusFlags & ~POWER(2,1) WHERE RowGUID = '71284C14-1CE1-415A-8EEA-0CC1B1EE65C3'

select * from tbltaxformpackages where OriginalSponsorName is not null
update tbltaxformpackages set OriginalSponsorName = null where OriginalSponsorName is not null
*/

if not exists(select 1 from sysobjects where name = 'Sponsor_SetSponsor')
	exec('create PROCEDURE Sponsor_SetSponsor as select 1 as one')
GO
alter PROCEDURE [dbo].[Sponsor_SetSponsor]
@TaxOfficeId INT,
@UserGUID UNIQUEIDENTIFIER,
@SponsorGUID UNIQUEIDENTIFIER,
@NewSponsorClientGUID UNIQUEIDENTIFIER,
@FixExistingPackageLinks BIT = 0,
@TableNames VARCHAR(MAX) = NULL OUT
AS BEGIN

DECLARE @OldSponsorClientGUID UNIQUEIDENTIFIER, @OldClientId int, @OldName VARCHAR(100),
        @NewClientId INT, @NewName VARCHAR(100), @NewSponsorWasSpouse BIT,
        @Remarks VARCHAR(942)
SELECT TOP 1 
  @OldSponsorClientGUID = RowGUID, 
  @OldClientId = ClientID,
  @OldName = FName + ' ' + LName + ' (' + CCode + ')'
FROM iTRAAC.dbo.tblClients
WHERE SponsorGUID = @SponsorGUID and StatusFlags & POWER(2,0) = POWER(2,0) ORDER BY ACTIVE DESC

SELECT
  @NewClientId = ClientId,
  @NewName = FName + ' ' + LName + ' (' + CCode + ')',
  @NewSponsorWasSpouse = CONVERT(BIT, StatusFlags & POWER(2,1))
FROM iTRAAC.dbo.tblClients WHERE RowGUID = @NewSponsorClientGUID

SET @Remarks = 'Sponsor'+ ISNULL(' changed from ' + @OldName,' set') + ' to ' + ISNULL(@NewName, '{} ')

-- if we're considering this Sponsor change as a *CORRECTION* versus a new status, then go back and reassign all the existing packages to this new Sponsor
IF (@FixExistingPackageLinks = 1) BEGIN
  UPDATE iTRAAC.dbo.tblTaxFormPackages SET 
    SponsorClientGUID = @NewSponsorClientGUID,
    ClientId = CASE WHEN ClientId = @OldClientId THEN @NewClientId ELSE ClientID END --only twiddle ClientId when it was pointing directly at the Sponsor, otherwise leave it pointing at the existing Dependent
  WHERE SponsorClientGUID = @OldSponsorClientGUID
  
  SET @Remarks = @Remarks + CHAR(13) + '[*** All existing Tax Forms have been re-assigned to this new Sponsor ***]'
END

-- otherwise, leave the old Sponsor name as-is by "stamping" it on the existing Packages
-- *** this was bad logic, the Name on the package WILL still be ok because the package's SponsorClientGUID is still pointing at the same Client record and the Client name hasn't been changed
--ELSE BEGIN
--  UPDATE iTRAAC.dbo.tblTaxFormPackages SET OriginalSponsorName = @OldName + ' *'
--  WHERE SponsorClientGUID = @OldSponsorClientGUID AND OriginalSponsorName IS NULL
--END

SET @TableNames = 'Client'
UPDATE iTRAAC.dbo.tblClients
SET StatusFlags = CASE WHEN RowGUID = @NewSponsorClientGUID THEN (StatusFlags | POWER(2,0)) -- set sponsor flag on new sponsor
                    & ~POWER(2,1) -- remove spouse flag from new sponsor
                  ELSE (StatusFlags & ~POWER(2,0)) -- remove sponsor flag from old sponsor
                    | CASE @NewSponsorWasSpouse WHEN 1 THEN POWER(2,1) ELSE 0 END -- put spouse flag on old sponsor, *if* new sponsor used to be the spouse
                  END
OUTPUT INSERTED.RowGUID,
       CONVERT(BIT, INSERTED.StatusFlags & POWER(2,0)) AS IsSponsor,
       CONVERT(BIT, INSERTED.StatusFlags & POWER(2,1)) AS IsSpouse
WHERE SponsorGUID = @SponsorGUID
AND RowGUID IN (@OldSponsorClientGUID, @NewSponsorClientGUID)

EXEC Remark_u
  @TaxOfficeId = @TaxOfficeId,
  @UserGUID = @UserGUID,
  @TableNames = @TableNames OUT,
  @Remarks = @Remarks,
  @FKRowGUID = @SponsorGUID


END
GO

grant execute on Sponsor_SetSponsor to public
go

