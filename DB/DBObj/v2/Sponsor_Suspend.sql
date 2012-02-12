
--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/****** Object:  StoredProcedure [dbo].[Sponsor_Suspend]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER On
GO

/* testing:
drop proc Sponsor_Suspend
*/

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'Sponsor_Suspend')
	exec('create PROCEDURE Sponsor_Suspend as select 1 as one')
GO

alter PROCEDURE [dbo].[Sponsor_Suspend]
@SponsorGUID UNIQUEIDENTIFIER,
@TaxOfficeId INT,
@UserGUID UNIQUEIDENTIFIER,
@SuspensionExpiry datetime = '12/31/9999',-- default to infinite end date, to play into manual resolution procedures; pass in NULL to lift suspension
@Remarks VARCHAR(942) = null,
@TableNames VARCHAR(1000) = NULL OUT
AS BEGIN

-- set the suspension... set it on all client records so that even if they migrate to another household they carry status this with them
-- tagging the suspension "responsibility" to the current Office,
-- which should hang together pretty good because we'd be here due to filing, and filing only occurs at the office originating the form which initiated the violation
DECLARE @SuspensionExpiry_tbl TABLE(SuspensionExpiry DATETIME)
UPDATE iTRAAC.dbo.tblClients SET
  SuspensionRoleID = CASE WHEN @SuspensionExpiry IS NULL THEN NULL ELSE @TaxOfficeId END,
  SuspensionExpiry = @SuspensionExpiry
WHERE SponsorGUID = @SponsorGUID

DECLARE @RemarkTypeId INT, @Title VARCHAR(50) 
SET @RemarkTypeId = 6
-- if we're turning off the suspension, flip the RemarkType to negative to indicate "off" 
IF @SuspensionExpiry IS NULL SET @RemarkTypeId = -1 * @RemarkTypeId 
EXEC Remark_u
  @TaxOfficeId = @TaxOfficeId,
  @UserGUID = @UserGUID,
  @RemarkTypeId = @RemarkTypeId,
  @Remarks = @Remarks,
  @FKRowGUID = @SponsorGUID,
  @TableNames = @TableNames OUT
  
-- return the updated fields at the sponsor level
SET @TableNames = ISNULL(@TableNames + ',', '') + 'Sponsor'
SELECT @SponsorGUID AS RowGUID, *, 
  @SponsorGUID AS RowGUID, @TaxOfficeId AS SuspensionTaxOfficeId,
  @SuspensionExpiry AS SuspensionExpiry
FROM dbo.Sponsor_ViolationInfo_f(@SponsorGUID)


END
GO

grant execute on Sponsor_Suspend to public
go

