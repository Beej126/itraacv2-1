-- as a brute force patch for beta testing... simply populate a default returned/filed date for forms in returned/filed status
 -- UPDATE tblTaxForms SET ReturnedDate = 0 WHERE StatusFlags & POWER(2,1) > 0
 -- UPDATE tblTaxForms SET FiledDate = 0 WHERE StatusFlags & POWER(2,2) > 0

-- populate a worktable from raw historical Event data
-- this will have to come from pulling in all of the field databases and mounting them on the central server one by one
-- because this info was not included in the replication agents until 2012 when we upgraded to SQL Server 2008 which allowed for larger sql express data file size
--  StatusFlags represent Returned/Filed status and EventDateTime is what we need to populate on tblTaxForms.Returned/Filed-Date columns

-- drop table _StatusChanges 
select
  f.TaxFormID,
  convert(int, null) as StatusFlags,
  e.EventDateTime,
  ed.EventData,
  PATINDEX('%StatusFlags%', ed.EventData) as StartIndex,
  convert(int, null) as EndIndex
into central.dbo._StatusChanges
FROM officedb.dbo.tblDBEvents e
JOIN officedb.dbo.tblDBEventData ed ON ed.DBEID = e.DBEID
JOIN officedb.dbo.tblTaxForms f ON f.TaxFormID = e.PKID
WHERE e.TableID = 14
and PATINDEX('%StatusFlags%', ed.EventData) > 0

update _StatusChanges set EventData = substring(eventdata, startindex, 8000)
update _StatusChanges set StartIndex = charindex('N', eventdata)
update _StatusChanges set EndIndex = charindex(char(9), eventdata, startindex)
update _StatusChanges set StatusFlags = convert(int, substring(EventData, StartIndex+2, EndIndex-StartIndex-2)) 


UPDATE f SET f.ReturnedDate = s.eventDatetime
from _statuschanges s
join tbltaxforms f on f.taxformid = s.taxformid
where s.statusflags & power(2,1) >0

UPDATE f SET f.FiledDate = s.eventDatetime
from _statuschanges s
join tbltaxforms f on f.taxformid = s.taxformid
where s.statusflags & power(2,2) >0

/*
-- find some test subjects to spot check...
select f.ordernumber, c.fname, c.lname, c.ccode
from _statuschanges s
join tbltaxforms f on f.taxformid = s.taxformid
join tbltaxformpackages p on p.packageid = f.packageid
join tblclients c on c.clientid = p.clientid
where s.statusflags & power(2,2) >0
and s.eventDatetime between '2010-10-13' and '2010-10-14'
*/


-- also, assign a default for all the missing agents that have been deleted by previous iTRAAC administrators
DECLARE @BlankUserGUID UNIQUEIDENTIFIER, @BlankAgentID INT, @BlankUserID int
select TOP 1 @BlankUserGUID = RowGUID, @BlankUserID = UserID FROM tblUsers WHERE LoginName = 'deleted'
IF (@BlankUserGUID IS NULL) BEGIN
  INSERT tblUsers  ( FName, LName, LoginName, PKISerialNumber, Email,
                          DSNPhone, UserLevel, StatusFlags, RowGUID, Password,
                          CreateDate, Active )
  VALUES  ( '{deleted user}', -- FName - varchar(25)
            '', -- LName - varchar(25)
            'deleted', -- LoginName - varchar(15)
            '', -- PKISerialNumber - varchar(15)
            '', -- Email - varchar(75)
            '', -- DSNPhone - varchar(8)
            0, -- UserLevel - smallint
            0, -- StatusFlags - int
            NEWID(), -- RowGUID - uniqueidentifier
            '', -- Password - varchar(255)
            GETDATE(), -- CreateDate - datetime
            0  -- Active - bit
            )
  select TOP 1 @BlankUserGUID = RowGUID, @BlankUserID = UserID FROM tblUsers WHERE LoginName = 'deleted'              
END
SELECT TOP 1 @BlankAgentID = AgentID FROM tblTaxFormAgents WHERE SigBlock = '{deleted agent}'
IF (@BlankAgentID IS NULL) BEGIN
  INSERT tblTaxFormAgents ( TaxOfficeID, UserID, SigBlock, Active,
                                  StatusFlags, RowGUID, UserGUID )
  VALUES  ( -1, -- TaxOfficeID - int
            @BlankUserID, -- UserID - int
            '{deleted agent}', -- SigBlock - varchar(50)
            0, -- Active - bit
            0, -- StatusFlags - int
            NEWID(), -- RowGUID - uniqueidentifier
            @BlankUserGUID  -- UserGUID - uniqueidentifier
            )
  SELECT TOP 1 @BlankAgentID = AgentID FROM tblTaxFormAgents WHERE SigBlock = '{deleted agent}'
END

-- missing RetAgentID
UPDATE f SET RetAgentID = @BlankAgentID
FROM tblTaxForms f
WHERE StatusFlags & POWER(2,1) > 0 
AND (RetAgentID IS NULL OR RetAgentID NOT IN(SELECT RetAgentID FROM tblTaxFormAgents))

-- missing ReturnUserGUID
UPDATE f SET ReturnUserGUID = @BlankUserGUID
FROM tblTaxForms f
WHERE StatusFlags & POWER(2,1) > 0 
AND (ReturnUserGUID IS NULL OR ReturnUserGUID NOT IN(SELECT RowGUID FROM tblUsers))

-- missing FileAgentID
UPDATE f SET FileAgentID = @BlankAgentID
FROM tblTaxForms f
WHERE StatusFlags & POWER(2,2) > 0 
AND (FileAgentID IS NULL OR FileAgentID NOT IN(SELECT RetAgentID FROM tblTaxFormAgents) )

-- mising FileUserGUID
UPDATE f SET FileUserGUID = @BlankUserGUID
FROM tblTaxForms f
WHERE StatusFlags & POWER(2,2) > 0 
AND (FileUserGUID IS NULL OR FileUserGUID NOT IN(SELECT RowGUID FROM tblUsers))
