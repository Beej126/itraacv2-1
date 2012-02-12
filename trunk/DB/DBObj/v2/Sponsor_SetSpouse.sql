--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:01p $

/****** Object:  StoredProcedure [dbo].[Sponsor_SetSpouse]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER On
GO

USE iTRAACv2
GO

/* testing
declare @p5 varchar(max)
exec dbo.Sponsor_SetSpouse @TaxOfficeId=10000001,@UserGUID='AB30A6B8-330C-49AE-B7D7-A4F7C0E34FAF',@SponsorGUID='6B8AB256-BEA7-4F3C-92AA-66E342144087',@NewSpouseClientGUID='56BEA131-CA1B-46ED-A74E-27C272EB53F4',@TableNames=@p5 output
select @p5

UPDATE tblClients SET StatusFlags = StatusFlags & ~POWER(2,1) WHERE RowGUID = '71284C14-1CE1-415A-8EEA-0CC1B1EE65C3'
*/

if not exists(select 1 from sysobjects where name = 'Sponsor_SetSpouse')
	exec('create PROCEDURE Sponsor_SetSpouse as select 1 as one')
GO
alter PROCEDURE [dbo].[Sponsor_SetSpouse]
@TaxOfficeId INT,
@UserGUID UNIQUEIDENTIFIER,
@SponsorGUID UNIQUEIDENTIFIER,
@NewSpouseClientGUID UNIQUEIDENTIFIER,
@IsDivorce bit = 0,
@TableNames VARCHAR(MAX) = NULL OUT
AS BEGIN

DECLARE @Remarks VARCHAR(900),
        @OldSpouseName VARCHAR(100), @OldSpouseClientGUID UNIQUEIDENTIFIER, 
        @NewSpouseName VARCHAR(100)
SELECT @OldSpouseName = FName + ' ' + LName, @OldSpouseClientGUID = RowGUID FROM iTRAAC.dbo.tblClients WHERE SponsorGUID = @SponsorGUID AND StatusFlags & POWER(2,1) = POWER(2,1)
SELECT @NewSpouseName = FName + ' ' + LName FROM iTRAAC.dbo.tblClients WHERE RowGUID = @NewSpouseClientGUID
IF (@IsDivorce = 1) SET @Remarks = 'Divorced Spouse ' + ISNULL(@OldSpouseName,'{}') + ' deactivated.'
ELSE SET @Remarks = 'Spouse'+ ISNULL(' changed from ' + @OldSpouseName, ' set') + ' to ' + ISNULL(@NewSpouseName, '{}')

SET @TableNames = 'Client'
UPDATE iTRAAC.dbo.tblClients SET
  StatusFlags = CASE WHEN RowGUID = @NewSpouseClientGUID THEN StatusFlags | POWER(2,1) 
                ELSE StatusFlags & ~POWER(2,1) END,
  Active = CASE WHEN @IsDivorce = 1 and RowGUID = @OldSpouseClientGUID THEN 0 
           WHEN RowGUID = @NewSpouseClientGUID THEN 1 
           ELSE Active END 
OUTPUT INSERTED.RowGUID, CONVERT(BIT, INSERTED.StatusFlags & POWER(2,1)) AS IsSpouse, INSERTED.Active
WHERE SponsorGUID = @SponsorGUID
AND RowGUID IN (@OldSpouseClientGUID, @NewSpouseClientGUID)

EXEC Remark_u
  @TaxOfficeId = @TaxOfficeId,
  @UserGUID = @UserGUID,
  @TableNames = @TableNames OUT,
  @Remarks = @Remarks,
  @FKRowGUID = @SponsorGUID


END
GO

grant execute on Sponsor_SetSpouse to public
go

