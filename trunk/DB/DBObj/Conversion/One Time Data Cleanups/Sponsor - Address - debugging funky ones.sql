/*
UPDATE s SET
  s.AddressLine1 = s2.AddressLine1,
  s.AddressLine2 = s2.AddressLine2
FROM iTRAAC.dbo.tblSponsors s
JOIN iTRAAC_orig.dbo.tblSponsors s2 ON s2.RowGUID = s.RowGUID
WHERE s.RowGUID = 'C20C3500-E9F2-4DC0-A958-C1A9B14013DF'

-- SELECT * FROM iTRAAC_orig.dbo.tblSponsors WHERE RowGUID = 'C20C3500-E9F2-4DC0-A958-C1A9B14013DF'

DECLARE @OfficialMailCMR varchar(3), @OfficialMailBox varchar(4), @OfficialMailCity varchar(50), @OfficialMailState varchar(2), @OfficialMailZip varchar(5)
EXEC Sponsor_Split_AddrLine2 @SponsorGUID='C20C3500-E9F2-4DC0-A958-C1A9B14013DF',
                             @OfficialMailCMR=@OfficialMailCMR OUT, @OfficialMailBox=@OfficialMailBox OUT,
                             @OfficialMailCity=@OfficialMailCity OUT, @OfficialMailState=@OfficialMailState OUT, @OfficialMailZip=@OfficialMailZip OUT

SELECT * FROM iTRAAC.dbo.tblSponsors WHERE RowGUID = '36742ABB-594B-4FEB-BB73-E303EB42F2AF'

select * from itraac.dbo.tblclients where ccode = 'm1275'
*/

DECLARE
  @OfficialMailCMR varchar(3),
  @OfficialMailBox varchar(4),
  @OfficialMailCity varchar(50),
  @OfficialMailState varchar(2),
  @OfficialMailZip varchar(5)

DECLARE @OfficialMailCMRWord VARCHAR(3), @OfficialMailBoxWord VARCHAR(3), @StartIndex int
SET @StartIndex = 1

DECLARE @AddressLine1 VARCHAR(100), @AddressLine2 VARCHAR(100)
SELECT 
  @AddressLine1 = REPLACE(AddressLine1, ', ', ','),
  @AddressLine2 = REPLACE(AddressLine2, ', ', ',')
FROM iTRAAC.dbo.tblSponsors WHERE RowGUID = '36742ABB-594B-4FEB-BB73-E303EB42F2AF'

SELECT @AddressLine1, @AddressLine2

-- play a little data cleanup games here... if Line1 has the CMR info, then concate Line1 & 2 together... and sending it into the Line2 logic below
-- then if we manage to fall out of the parsing with clean fields, slam them all back into Line2 as the new standard format and blank out Line1 (see below)
-- this works as well as you'd expect with a lot of our existing data
DECLARE @combined varchar(200)
SET @combined = ISNULL(@AddressLine1 + ',', '') + ISNULL(@AddressLine2, '')
IF (LEFT(@combined, 3) in ('CMR', 'APO')) SET @AddressLine2 = @combined

SELECT @AddressLine1 AS line1, @AddressLine2 AS line2

EXEC SubstringEx @SearchString=@AddressLine2 out, @Out=@OfficialMailCMRWord OUT
IF (@OfficialMailCMRWord = 'CMR') BEGIN
  PRINT 'cmr'
  EXEC SubstringEx @SearchString=@AddressLine2 out, @Out=@OfficialMailCMR OUT
  PRINT 'boxword'
  EXEC SubstringEx @SearchString=@AddressLine2 out, @Out=@OfficialMailBoxWord OUT
  IF (@OfficialMailBoxWord <> 'BOX') RETURN
  PRINT 'box'
  EXEC SubstringEx @SearchString=@AddressLine2 out, @Out=@OfficialMailBox OUT

  PRINT 'city'
  EXEC SubstringEx @SearchString=@AddressLine2 out, @Out=@OfficialMailCity OUT
END
ELSE IF (@OfficialMailCMRWord = 'APO') 
  SET @OfficialMailCity = @OfficialMailCMRWord

IF (@OfficialMailCity IS NOT null) BEGIN
  PRINT 'state'
  EXEC SubstringEx @SearchString=@AddressLine2 out, @Out=@OfficialMailState OUT
  PRINT 'after state'
  SET @OfficialMailZip=@AddressLine2
END

select
  @OfficialMailCMR AS cmr,
  @OfficialMailBox AS box,
  @OfficialMailCity AS city,
  @OfficialMailState AS [state],
  @OfficialMailZip AS zip
