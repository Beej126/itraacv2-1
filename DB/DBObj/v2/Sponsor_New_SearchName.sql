--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 4/30/10 4:48p $

/****** Object:  StoredProcedure [dbo].[Sponsor_New_SearchName]    Script Date: 06/19/2009 15:58:30 ******/

/* testing:
exec Sponsor_New_SearchName 'Sponsor SSN', '347', '50', '7543'

SELECT TOP 50 * FROM tblsponsors WHERE homephone <> '' AND homephone NOT LIKE '0%' AND LEN(homephone) > 11

addressline2 NOT LIKE 'apo%' AND addressline1 NOT LIKE 'apo%' AND addressline2 <> ''
AND addressline2 NOT LIKE 'cmr%' AND addressline1 NOT LIKE 'cmr%'


sponsorid = 10009987
SELECT * FROM tblclients WHERE lname = 'anderson' AND fname = 'anne'

*/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'Sponsor_New_SearchName')
	exec('create PROCEDURE Sponsor_New_SearchName as select 1 as one')
GO
alter PROCEDURE [dbo].Sponsor_New_SearchName
@MatchType VARCHAR(50),
@FirstName VARCHAR(5), --just search on the first x characters to be a little more "fuzzy"
@LastName VARCHAR(5)
AS BEGIN

SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

SELECT @MatchType AS MatchType, * FROM Sponsor_New_Search_v
WHERE FName LIKE @FirstNAme + '%'
AND LName LIKE @LastName + '%'

END
GO

grant execute on Sponsor_New_SearchName to public
go
