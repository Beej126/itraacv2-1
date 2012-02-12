--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 4/30/10 4:48p $

/****** Object:  StoredProcedure [dbo].[Sponsor_New_SearchSSN]    Script Date: 06/19/2009 15:58:30 ******/

/* testing:
exec Sponsor_New_SearchSSN 'Sponsor SSN', '347', '50', '7543'

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

if not exists(select 1 from sysobjects where name = 'Sponsor_New_SearchSSN')
	exec('create PROCEDURE Sponsor_New_SearchSSN as select 1 as one')
GO
alter PROCEDURE [dbo].Sponsor_New_SearchSSN
@MatchType VARCHAR(50),
@SSN1 VARCHAR(3),
@SSN2 VARCHAR(2),
@SSN3 VARCHAR(4)
AS BEGIN

SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

SELECT @MatchType AS MatchType, * FROM Sponsor_New_Search_v
WHERE ssn =    @SSN1 + '-' + @SSN2 + '-' + @SSN3 --direct match
OR    ssn LIKE @SSN1 + '-' + '__'  + '-' + @SSN3 --match on wildcarding the middle block (since users commonly mangled that to get past the SSN validation check)
OR    ssn LIKE @SSN1 + '-' + @SSN2 + '%'         --match on wildcarding the last block (same reasoning)

END
GO

grant execute on Sponsor_New_SearchSSN to public
go
