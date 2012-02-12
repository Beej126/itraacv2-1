--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 4/30/10 4:48p $

/****** Object:  StoredProcedure [dbo.[Sponsor_New    Script Date: 06/19/2009 15:58:30 ******/

/* testing:
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'Sponsor_New')
	exec('create PROCEDURE Sponsor_New as select 1 as one')
GO
alter PROCEDURE Sponsor_New
@NewSponsorGUID UNIQUEIDENTIFIER = NULL OUT,
@TableNames varchar(MAX) = NULL OUT
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

SET @TableNames = ISNULL(@TableNames + ',', '') + 'Sponsor'

SET @NewSponsorGUID = NEWID()

SELECT 
  @NewSponsorGUID AS RowGUID,
  CONVERT(bit, 1) AS Active,
  'APO' AS OfficialMailCity,
  'AE' AS OfficialMailState,
  0 AS IsUTAPActive
  /*,'-' AS [Rank], --if we're doing a moveclient, need non-null/blank defaults so that this record passes field validation and will save to the DB, so that it's present for the client to be connected to
  '-' AS DutyLocation,
  '-' AS DutyPhoneDSN1,
  '-' AS DutyPhoneDSN2*/

/*
SET @TableNames = ISNULL(@TableNames + ',', '') + 'Client'

SELECT 
  NEWID() AS RowGUID,
  @NewSponsorGUID AS SponsorGUID,
  '' AS LName, --just to feed the databinding a value so we see the initial LName + CCode in the tab header
  CONVERT(bit, 1) AS Active,
  CONVERT(bit, 1) AS IsSponsor,
  CONVERT(bit, 0) AS IsSpouse,
  'New Sponsor' AS CCode
*/
--UNION ALL
--SELECT 
--  NEWID() AS RowGUID,
--  @NewSponsorGUID AS SponsorGUID,
--  null AS LName,
--  CONVERT(bit, 1) AS Active,
--  CONVERT(bit, 0) AS IsSponsor,
--  CONVERT(bit, 1) AS IsSpouse,
--  NULL AS CCode

END
GO

grant execute on Sponsor_New to public
go
