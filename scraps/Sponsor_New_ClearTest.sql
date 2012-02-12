--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 4/30/10 4:48p $

/****** Object:  StoredProcedure [dbo].[Sponsor_New_ClearTest]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

/* testing:
Sponsor_New_ClearTest 1
*/

if not exists(select 1 from sysobjects where name = 'Sponsor_New_ClearTest')
	exec('create PROCEDURE Sponsor_New_ClearTest as select 1 as one')
GO
alter PROCEDURE [dbo].Sponsor_New_ClearTest
@IncludeLocations BIT = 0
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

DECLARE @SponsorGUID UNIQUEIDENTIFIER
SELECT @SponsorGUID = SponsorGUID from iTRAAC.dbo.tblClients WHERE Email = '***test***'
DELETE iTRAAC.dbo.tblSponsors WHERE RowGUID = @SponsorGUID
DELETE iTRAAC.dbo.tblClients WHERE Email = '***test***'

END
GO

grant execute on Sponsor_New_ClearTest to public
go

