/*
SELECT
  CONVERT(INT, UserID) AS UserID, --dummy conversion eliminates the identity when mirroring the table schema
  FName, LName, LoginName, PKISerialNumber, Email, DSNPhone,
  UserLevel, StatusFlags, RowGUID, [Password], CreateDate, ACTIVE, CONVERT(DATETIME, null) as DeleteDate
INTO _Deleted_Duplicate_Users
FROM tblUsers
WHERE 1=0

ALTER TABLE _Deleted_Duplicate_Users ADD DEFAULT (getdate()) FOR DeleteDate

select * from _Deleted_Duplicate_Users

SELECT MIN(UserID), MAX(UserID), FName, LName, COUNT(1) FROM tblUsers GROUP BY FName, LName HAVING COUNT(1) >1

truncate table _Deleted_Duplicate_Users 

SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE COLUMN_NAME = 'userid' AND TABLE_NAME NOT LIKE '_onflict%'
SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE COLUMN_NAME = 'userguid' AND TABLE_NAME NOT LIKE '_onflict%'
SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE COLUMN_NAME = 'agentid' AND TABLE_NAME NOT LIKE '_onflict%'

select * from tblusers where fname = 'brent'

update tblusers set active = 1
update tbltaxformagents set active = 1
*/

DECLARE curs CURSOR LOCAL FAST_FORWARD FOR
SELECT MIN(UserID), MAX(UserID), FName, LName FROM tblUsers GROUP BY FName, LName HAVING COUNT(1) >1

DECLARE
  @MinUserID INT, @MaxUserID INT, @MinUserGUID UNIQUEIDENTIFIER, @MaxUserGUID UNIQUEIDENTIFIER,
  @FName VARCHAR(50), @LName VARCHAR(50),
  @AgentID INT, @NewAgentID int
  
OPEN curs
WHILE (1=1) BEGIN

  FETCH NEXT FROM curs INTO @MinUserID, @MaxUserID, @FName, @LName 
  IF @@FETCH_STATUS <> 0 BREAK
  
  --SELECT @MinUserGUID = RowGUID FROM tblUsers WHERE UserID = @MinUserID
  --SELECT @MaxUserGUID = RowGUID FROM tblUsers WHERE UserID = @MaxUserID
  
  PRINT '-------------------------------------------------'
  PRINT @FName + ' ' + @LName
  
  PRINT 'updating tblOfficeManagers'
  UPDATE tblOfficeManagers SET UserID = @MaxUserID WHERE UserID = @MinUserID
  PRINT 'updating/deleting tblTaxFormAgents'
  UPDATE tblTaxFormAgents SET UserID = @MaxUserID WHERE UserID = @MinUserID
  UPDATE tblTaxFormAgents SET UserGUID = @MaxUserGUID WHERE UserGUID = @MinUserGUID
  
  SELECT @AgentID = AgentID FROM tblTaxFormAgents WHERE UserID = @MinUserID
  IF (@@ROWCOUNT > 0)
  BEGIN
    SELECT @NewAgentID = AgentID FROM tblTaxFormAgents WHERE UserID = @MaxUserID
    UPDATE tblSponsors SET AgentID = @NewAgentID WHERE AgentID = @AgentID
    UPDATE tblTaxFormPackages SET AgentID = @NewAgentID WHERE AgentID = @AgentID
    UPDATE tblClients SET AgentID = @NewAgentID WHERE AgentID = @AgentID
  END
  
  DELETE tblTaxFormAgents WHERE UserID = @MinUserID
  DELETE tblTaxFormAgents WHERE UserGUID = @MinUserGUID
  
  PRINT 'updating tblSessions'
  UPDATE tblSessions SET UserID = @MaxUserID WHERE UserID = @MinUserID
  
  PRINT 'inserting _Deleted_Duplicate_Users'
  INSERT [_Deleted_Duplicate_Users]
  SELECT *, GETDATE() FROM tblUsers WHERE UserID = @MinUserID

  PRINT 'deleting tblUsers'
  DELETE tblUsers WHERE UserID = @MinUserID
    
END

DEALLOCATE curs
