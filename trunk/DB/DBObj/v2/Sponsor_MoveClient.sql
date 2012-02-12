--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:01p $

/****** Object:  StoredProcedure [dbo].[Sponsor_MoveClient]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER On
GO

USE iTRAACv2
GO

/* testing
declare @p5 varchar(max)
exec dbo.Sponsor_MoveClient @TaxOfficeId=10000001,@UserGUID='AB30A6B8-330C-49AE-B7D7-A4F7C0E34FAF',@SponsorGUID='6B8AB256-BEA7-4F3C-92AA-66E342144087',@NewSpouseClientGUID='56BEA131-CA1B-46ED-A74E-27C272EB53F4',@TableNames=@p5 output
select @p5

UPDATE tblClients SET StatusFlags = StatusFlags & ~POWER(2,1) WHERE RowGUID = '71284C14-1CE1-415A-8EEA-0CC1B1EE65C3'
*/

if not exists(select 1 from sysobjects where name = 'Sponsor_MoveClient')
	exec('create PROCEDURE Sponsor_MoveClient as select 1 as one')
GO
alter PROCEDURE [dbo].[Sponsor_MoveClient]
@TaxOfficeId INT,
@UserGUID UNIQUEIDENTIFIER,
@SponsorGUID UNIQUEIDENTIFIER,
@MoveClientGUID UNIQUEIDENTIFIER,
@TableNames VARCHAR(MAX) = NULL OUT
AS BEGIN

DECLARE @Remarks VARCHAR(900),
        @MoveClient varchar(100), @OldSponsorGUID UNIQUEIDENTIFIER,
        @OldSponsor VARCHAR(100),
        @NewSponsor VARCHAR(100)
SELECT @MoveClient = FName + ' ' + LName + ' (' + CCode + ')', @OldSponsorGUID = SponsorGUID FROM iTRAAC.dbo.tblClients WHERE RowGUID = @MoveClientGUID
SELECT @OldSponsor = FName + ' ' + LName + ' (' + CCode + ')' FROM iTRAAC.dbo.tblClients WHERE SponsorGUID = @OldSponsorGUID AND StatusFlags & POWER(2,0) = POWER(2,0)
SELECT @NewSponsor = FName + ' ' + LName + ' (' + CCode + ')' FROM iTRAAC.dbo.tblClients WHERE SponsorGUID = @SponsorGUID AND StatusFlags & POWER(2,0) = POWER(2,0)

SET @TableNames = 'Client'
UPDATE iTRAAC.dbo.tblClients SET
  SponsorGUID = @SponsorGUID,
  StatusFlags = StatusFlags & ~POWER(2,0), --turn off sponsor flag
  Active = 1
OUTPUT
  INSERTED.RowGUID,
  INSERTED.SponsorGUID,
  CONVERT(BIT, INSERTED.StatusFlags & POWER(2,0)) AS IsSponsor,
  CONVERT(BIT, INSERTED.StatusFlags & POWER(2,1)) AS IsSpouse,
  INSERTED.Active,
  INSERTED.FName,
  INSERTED.LName,
  INSERTED.MI,
  INSERTED.SuffixName AS Suffix,
  LEFT(INSERTED.SSN, 3) AS SSN1, SUBSTRING(INSERTED.SSN, 5, 2) AS SSN2, RIGHT(INSERTED.SSN, 4) AS SSN3,
  INSERTED.DoDId,
  INSERTED.CCode,
  INSERTED.Email,
  INSERTED.BirthDate
WHERE RowGUID = @MoveClientGUID

INSERT ClientPreviousSponsor ( ClientGUID, SponsorGUID )
VALUES  ( @MoveClientGUID, @OldSponsorGUID )

SET @Remarks = 'Moved ' + @MoveClient + ISNULL(' from ' + @OldSponsor, '') + ' into this household.'
EXEC Remark_u
  @TaxOfficeId = @TaxOfficeId,
  @UserGUID = @UserGUID,
  @TableNames = @TableNames OUT,
  @Remarks = @Remarks,
  @FKRowGUID = @SponsorGUID

-- stamp a corresponding remark on the old household so we leave some breadcrumbs about what's gone down here
SET @Remarks = 'Moved ' + @MoveClient + ' from this household to ' + ISNULL(@NewSponsor, 'a new sponsor.') + '.'
EXEC Remark_u
  @TaxOfficeId = @TaxOfficeId,
  @UserGUID = @UserGUID,
  @TableNames = @TableNames OUT,
  @Remarks = @Remarks,
  @FKRowGUID = @OldSponsorGUID


END
GO

grant execute on Sponsor_MoveClient to public
go

