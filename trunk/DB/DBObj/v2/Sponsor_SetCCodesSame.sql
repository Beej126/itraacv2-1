--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:01p $

/****** Object:  StoredProcedure [dbo].[Sponsor_SetCCodesSame]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER On
GO

USE iTRAACv2
GO

/* testing
declare @p5 varchar(max)
exec dbo.Sponsor_SetCCodesSame @TaxOfficeId=10000001,@UserGUID='AB30A6B8-330C-49AE-B7D7-A4F7C0E34FAF',@SponsorGUID='6B8AB256-BEA7-4F3C-92AA-66E342144087',@SpouseClientGUID='56BEA131-CA1B-46ED-A74E-27C272EB53F4',@TableNames=@p5 output
select @p5

SELECT master.dbo.Concat(distinct CCode) FROM iTRAAC.dbo.tblClients WHERE SponsorGUID = 'F69F07C6-49E1-46D0-9AB1-D0B7C951B68B' AND CCode <> 'H1021'
select @@rowcount

select top 5 sponsorguid, count(distinct ccode) from tblclients 
group by sponsorguid
having count(distinct ccode) > 2

select * from tblclients where sponsorguid = 'F69F07C6-49E1-46D0-9AB1-D0B7C951B68B'

UPDATE tblClients SET StatusFlags = StatusFlags & ~POWER(2,1) WHERE RowGUID = '71284C14-1CE1-415A-8EEA-0CC1B1EE65C3'
*/

if not exists(select 1 from sysobjects where name = 'Sponsor_SetCCodesSame')
	exec('create PROCEDURE Sponsor_SetCCodesSame as select 1 as one')
GO
alter PROCEDURE [dbo].[Sponsor_SetCCodesSame]
@TaxOfficeId INT,
@UserGUID UNIQUEIDENTIFIER,
@SponsorGUID UNIQUEIDENTIFIER,
@NewCCode VARCHAR(10),
@TableNames VARCHAR(MAX) = NULL OUT
AS BEGIN

DECLARE @Remarks VARCHAR(900)
SELECT @Remarks = master.dbo.Concat(distinct CCode + ' (' + FName + ')', ',') FROM iTRAAC.dbo.tblClients WHERE SponsorGUID = @SponsorGUID AND CCode <> @NewCCode
IF (@Remarks IS null) RETURN -- nothing to do

SET @TableNames = 'Client'
UPDATE iTRAAC.dbo.tblClients SET CCode = @NewCCode
OUTPUT INSERTED.RowGUID, INSERTED.CCode
WHERE SponsorGUID = @SponsorGUID AND CCode <> @NewCCode

SET @Remarks = @Remarks + ' changed to ' + @NewCCode 
EXEC Remark_u
  @TaxOfficeId = @TaxOfficeId,
  @UserGUID = @UserGUID,
  @TableNames = @TableNames OUT,
  @Remarks = @Remarks,
  @FKRowGUID = @SponsorGUID

END
GO

grant execute on Sponsor_SetCCodesSame to public
go

