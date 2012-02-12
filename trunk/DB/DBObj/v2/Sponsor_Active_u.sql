
--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/****** Object:  StoredProcedure [dbo].[Sponsor_Active_u]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER On
GO

/* testing:
drop proc Sponsor_Active_u
*/

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'Sponsor_Active_u')
	exec('create PROCEDURE Sponsor_Active_u as select 1 as one')
GO

alter PROCEDURE [dbo].[Sponsor_Active_u]
@TaxOfficeId INT,
@UserGUID UNIQUEIDENTIFIER,
@TableNames VARCHAR(MAX) = NULL OUT,

@SponsorGUID UNIQUEIDENTIFIER,
@RemarkTypeId INT, --expecting (+/-) 22 or 23 here
@Reason VARCHAR(942) = null
AS BEGIN

-- if we're setting PCS'ed = true, indicate the household active flag hit as well
IF (@RemarkTypeId = 22) SET @Reason = 'Household Deactivated'

SET @TableNames = ISNULL(@TableNames + ',', '') + 'Sponsor,Client'
UPDATE iTRAAC.dbo.tblSponsors SET
  ACTIVE = CASE WHEN @RemarkTypeId < 0 THEN 1 ELSE 0 END 
OUTPUT INSERTED.RowGUID, INSERTED.Active
WHERE RowGUID = @SponsorGUID

UPDATE iTRAAC.dbo.tblClients SET
  Active = CASE WHEN @RemarkTypeId < 0 THEN 1 ELSE 0 END
OUTPUT INSERTED.RowGUID, INSERTED.Active
WHERE SponsorGUID = @SponsorGUID AND StatusFlags & POWER(2,0) = POWER(2,0)

EXEC Remark_u
  @TaxOfficeId = @TaxOfficeId,
  @UserGUID = @UserGUID,
  @RemarkTypeId = @RemarkTypeId,
  @Remarks = @Reason,
  @FKRowGUID = @SponsorGUID,
  @TableNames = @TableNames OUT

END
GO

grant execute on Sponsor_Active_u to public
go

