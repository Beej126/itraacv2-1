--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:00p $

/* testing:
select dbo.Members_All_f('2/1/2100')
drop function Members_All_f
sp_procsearch 'RETURNS TABLE'
*/
USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'Members_All_f')
	exec('create FUNCTION Members_All_f() returns @dummy TABLE(dummy int) begin return end')
GO
ALTER FUNCTION dbo.Members_All_f(@SponsorGUID UNIQUEIDENTIFIER)
RETURNS @Members TABLE(TableID INT, RowGUID UNIQUEIDENTIFIER, FName VARCHAR(50))
AS BEGIN

-- gather the remarks tied to everyone under this roof...

-- first pull remarks tied directly to the sponsor record
INSERT @Members
SELECT 9, RowGUID, 'Household' FROM iTRAAC.dbo.tblSponsors WHERE RowGUID = @SponsorGUID

--then all the clients
INSERT @Members
SELECT 10, RowGUID, FName FROM iTRAAC.dbo.tblClients WHERE SponsorGUID = @SponsorGUID

-- then all the clients "previous identities" of the above clients
--INSERT @Members
--SELECT 10, c.RowGUID, c.FName
--FROM @Members i
--JOIN iTRAAC.dbo.tblClients c ON c.Replacement ClientGUID = i.RowGUID

RETURN 

END
go

GRANT select ON dbo.Members_All_f TO PUBLIC
go
