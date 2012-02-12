--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 4/30/10 4:48p $

/****** Object:  StoredProcedure [dbo].[Users_s]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

/* testing:
Users_s 1
*/

if not exists(select 1 from sysobjects where name = 'Users_s')
	exec('create PROCEDURE Users_s as select 1 as one')
GO
alter PROCEDURE [dbo].Users_s
@UserGUID uniqueidentifier = null
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

SELECT
  u.RowGUID,
  a.RowGUID AS AgentGUID,
  a.TaxOfficeID,
  o.TaxOfficeName,
  u.FName AS FirstName,
  u.LName AS LastName,
  u.DSNPhone,
  u.Email,
  a.SigBlock,
  u.ACTIVE & a.ACTIVE AS Active
FROM iTRAAC.dbo.tblTaxFormAgents a
JOIN iTRAAC.dbo.tblUsers u ON u.RowGUID = a.UserGUID
JOIN iTRAAC.dbo.tblTaxOffices o ON o.TaxOfficeID = a.TaxOfficeID
WHERE (@UserGUID IS NULL OR u.RowGUID = @UserGUID)

END
GO

grant execute on Users_s to public
go

