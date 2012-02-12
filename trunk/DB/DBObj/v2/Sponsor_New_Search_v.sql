--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/15/09 4:27p $

/****** Object:  View [dbo].[Sponsor_New_Search_v]    Script Date: 06/22/2009 08:23:48 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'Sponsor_New_Search_v')
	exec('create VIEW Sponsor_New_Search_v as select 1 as one')
GO
alter VIEW [dbo].[Sponsor_New_Search_v]
AS

SELECT
  c.RowGUID AS ClientGUID,
  s.RowGUID AS SponsorGUID,
  CONVERT(bit, c.StatusFlags & POWER(2,0)) AS [Is Sponsor],
  c.LName, c.FName,
  c.SSN,
  s.DutyLocation,
  s.AddressLine1 + ', ' + s.AddressLine2 AS [Address],
  c.eMail,
  s.DutyPhone,
  ISNULL('(+'+s.HomePhoneCountry+') ', '') + s.HomePhone AS HomePhone
FROM iTRAAC.dbo.tblClients c 
LEFT JOIN iTRAAC.dbo.tblSponsors s ON s.RowGUID = c.SponsorGUID  --left join to handle bad data where clients have somehow lost their sponsor record, this allows them to be visible and thereby pulled back into good data structure

GO

grant select on Sponsor_New_Search_v to public
go

