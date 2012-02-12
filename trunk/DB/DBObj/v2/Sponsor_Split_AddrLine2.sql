--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 4/30/10 4:48p $

/****** Object:  StoredProcedure [dbo].[Sponsor_Split_AddrLine2]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

/* testing:
select * from tblclients where email = '***test***'

declare
  @OfficialMailCMR varchar(3),
  @OfficialMailBox varchar(4),
  @OfficialMailCity varchar(50),
  @OfficialMailState varchar(2),
  @OfficialMailZip varchar(5)
  
exec Sponsor_Split_AddrLine2 @SponsorGUID = 'E80ECD15-98EC-4A6B-B86D-28369F164DE3',
                             @OfficialMailCMR=@OfficialMailCMR OUT, @OfficialMailBox=@OfficialMailBox OUT,
                             @OfficialMailCity=@OfficialMailCity OUT, @OfficialMailState=@OfficialMailState OUT,
                             @OfficialMailZip=@OfficialMailZip OUT

SELECT
  @OfficialMailCMR AS OfficialMailCMR,
  @OfficialMailBox AS OfficialMailBox,
  @OfficialMailCity AS OfficialMailCity,
  @OfficialMailState AS OfficialMailState,
  @OfficialMailZip AS OfficialMailZip
  
select * from itraac.dbo.tblsponsors where rowguid = 'E80ECD15-98EC-4A6B-B86D-28369F164DE3'
--update itraac.dbo.tblsponsors set addressline2 = 'apo ae 09102' where rowguid = '3298087b-36a4-4538-b672-be519d0f49a9'

*/

if not exists(select 1 from sysobjects where name = 'SubstringEx')
	exec('create PROCEDURE SubstringEx as select 1 as one')
GO
alter PROCEDURE [dbo].SubstringEx
@SearchString VARCHAR(MAX) OUT,
@Out VARCHAR(MAX) = NULL OUT
AS BEGIN

DECLARE @CharIndex INT

SET @CharIndex = PATINDEX('%[, ]%', @SearchString)

IF @CharIndex = 0
BEGIN
  SET @Out = ""
  RETURN
END

-- PRINT 'before left'
SET @Out = left(@SearchString, @CharIndex-1)
-- PRINT 'after left'

-- PRINT 'before right'
SET @SearchString = RIGHT(@SearchString, datalength(@SearchString) - datalength(@out) - 1)
-- PRINT 'after right'

-- PRINT '@CharIndex: ' + CONVERT(VARCHAR, @CharIndex)
-- PRINT '@Out: ' + CONVERT(VARCHAR, @Out) + ', datalength(@Out): ' + CONVERT(VARCHAR, datalength(@out))
-- PRINT ''
	
END
GO

grant execute on SubstringEx to public
go



if not exists(select 1 from sysobjects where name = 'Sponsor_Split_AddrLine2')
	exec('create PROCEDURE Sponsor_Split_AddrLine2 as select 1 as one')
GO
alter PROCEDURE [dbo].Sponsor_Split_AddrLine2
@SponsorGUID UNIQUEIDENTIFIER,
@OfficialMailCMR varchar(3) = NULL OUT,
@OfficialMailBox varchar(4) = NULL OUT,
@OfficialMailCity varchar(50) = NULL OUT,
@OfficialMailState varchar(2) = NULL OUT,
@OfficialMailZip varchar(5) = NULL OUT
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

DECLARE @OfficialMailCMRWord VARCHAR(3), @OfficialMailBoxWord VARCHAR(3), @StartIndex int
SET @StartIndex = 1

DECLARE @AddressLine1 VARCHAR(100), @AddressLine2 VARCHAR(100)
SELECT 
  @AddressLine1 = REPLACE(AddressLine1, ', ', ','),
  @AddressLine2 = REPLACE(AddressLine2, ', ', ',')
FROM iTRAAC.dbo.tblSponsors WHERE RowGUID = @SponsorGUID

-- play a little data cleanup games here... if Line1 has the CMR info, then concat Line1 & 2 together... and sending it into the Line2 logic below
-- then if we manage to fall out of the parsing with clean fields, slam them all back into Line2 as the new standard format and blank out Line1 (see below)
-- this works as well as you'd expect with a lot of our existing data
DECLARE @combined varchar(200)
SET @combined = ISNULL(@AddressLine1 + ',', '') + ISNULL(@AddressLine2, '')
IF (LEFT(@combined, 3) in ('CMR', 'APO')) SET @AddressLine2 = @combined

EXEC SubstringEx @SearchString=@AddressLine2 out, @Out=@OfficialMailCMRWord OUT
IF (@OfficialMailCMRWord = 'CMR') BEGIN
  EXEC SubstringEx @SearchString=@AddressLine2 out, @Out=@OfficialMailCMR OUT
  EXEC SubstringEx @SearchString=@AddressLine2 out, @Out=@OfficialMailBoxWord OUT
  IF (@OfficialMailBoxWord <> 'BOX') RETURN
  EXEC SubstringEx @SearchString=@AddressLine2 out, @Out=@OfficialMailBox OUT

  EXEC SubstringEx @SearchString=@AddressLine2 out, @Out=@OfficialMailCity OUT
END
ELSE IF (@OfficialMailCMRWord = 'APO') 
  SET @OfficialMailCity = @OfficialMailCMRWord

IF (@OfficialMailCity IS NOT null) BEGIN
  EXEC SubstringEx @SearchString=@AddressLine2 out, @Out=@OfficialMailState OUT
  SET @OfficialMailZip=@AddressLine2 
END

IF (LEFT(@combined, 3) in ('CMR', 'APO'))
  UPDATE iTRAAC.dbo.tblSponsors SET
    AddressLine1 = NULL,
    AddressLine2 = ISNULL('CMR ' + @OfficialMailCMR, '') + ISNULL(' BOX ' + @OfficialMailBox + ', ', '') + 
                   ISNULL(@OfficialMailCity, '') + ', ' + ISNULL(@OfficialMailState, '') + ', ' + ISNULL(@OfficialMailZip, '')
  WHERE RowGUID = @SponsorGUID
  
END
GO

grant execute on Sponsor_Split_AddrLine2 to public
go

