--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:00p $

/* testing:
*/
USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'UserGUID_To_AgentID')
	exec('create PROCEDURE UserGUID_To_AgentID as select 1 as one')
GO 
alter PROCEDURE [dbo].[UserGUID_To_AgentID]
@UserGUID UNIQUEIDENTIFIER,
@TaxOfficeId INT,
@AgentID INT = NULL OUT,
@AgentGUID UNIQUEIDENTIFIER = NULL OUT
AS BEGIN

DECLARE @UserID INT, @UserName VARCHAR(50)

SELECT @UserID = UserID, @UserName = FName + ' ' + LName FROM iTRAAC.dbo.tblUsers WHERE RowGUID = @UserGUID

SELECT @AgentID = a.AgentID, @AgentGUID = a.RowGUID
FROM iTRAAC.dbo.tblTaxFormAgents a
JOIN iTRAAC.dbo.tblTaxOffices o ON o.TaxOfficeID = a.TaxOfficeID AND o.TaxOfficeID = @TaxOfficeId
WHERE a.UserID = @UserID

IF (@AgentID IS NULL)
BEGIN
  INSERT iTRAAC.dbo.tblTaxFormAgents (TaxOfficeID, UserID, SigBlock, Active, StatusFlags, RowGUID)
  VALUES (@TaxOfficeId, @UserID, @UserName, 1, 0, NEWID())
  SET @AgentID = SCOPE_IDENTITY()
  SELECT @AgentGUID = RowGUID FROM iTRAAC.dbo.tblTaxFormAgents WHERE AgentID = @AgentID
END

END
go

GRANT EXECUTE ON dbo.UserGUID_To_AgentID TO PUBLIC
go
