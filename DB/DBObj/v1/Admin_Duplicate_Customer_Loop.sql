--$Author: Brent.anderson2 $
--$Date: 8/05/09 12:08p $
--$Modtime: 8/05/09 12:07p $
--$History: Admin_Duplicate_Customer_Loop.sql $
-- 
-- *****************  Version 4  *****************
-- User: Brent.anderson2 Date: 8/05/09    Time: 12:08p
-- Updated in $/iTRAAC/iTRAACv1/DB/DBobj/procs
-- 
-- *****************  Version 3  *****************
-- User: Brent.anderson2 Date: 7/23/09    Time: 5:03p
-- Updated in $/iTRAAC/iTRAACv1/DB/DBobj/procs
-- 
-- *****************  Version 2  *****************
-- User: Brent.anderson2 Date: 7/23/09    Time: 5:02p
-- Updated in $/iTRAAC/iTRAACv1/DB/DBobj/procs
-- 
-- *****************  Version 1  *****************
-- User: Brent.anderson2 Date: 7/23/09    Time: 5:01p
-- Created in $/iTRAAC/iTRAACv1/DB/DBobj/procs

if not exists(select 1 from sysobjects where name = 'Admin_Duplicate_Customer_Loop')
	exec('create PROCEDURE Admin_Duplicate_Customer_Loop as select 1 as one')
GO
ALTER PROC Admin_Duplicate_Customer_Loop
AS begin

DECLARE
  @KillSponsorID INT, @KillClientID INT, 
  @KeepClientID INT, @KeepSponsorID INT
  
DECLARE curs CURSOR LOCAL FAST_FORWARD FOR  
SELECT 
  KeepSponsorID,
  KeepClientID,
  KillSponsorID,
  KillClientID
FROM #_customer_dupes

OPEN curs
WHILE (1=1) BEGIN

  FETCH NEXT FROM curs INTO @KeepSponsorID, @KeepClientID, @KillSponsorID, @KillClientID
  IF (@@FETCH_STATUS <> 0) BREAK
  
  -- move Packages over to the keeper ClientID
  UPDATE tblTaxFormPackages SET ClientID = @KeepClientID WHERE ClientID = @KillClientID
  
  -- delete client that has been dupe eliminated
    -- archive the record just to be extra careful
  INSERT [_deleted_dupe_clients] (ClientID, SponsorID, FName, LName, MI, SSN, Active, StatusFlags, AgentID, CCode, Email, PrimeTaxOfficeID, SuspensionExpiry, SuspensionRoleID, RowGUID)
  SELECT ClientID, SponsorID, FName, LName, MI, SSN, Active, StatusFlags, AgentID, CCode, Email, PrimeTaxOfficeID, SuspensionExpiry, SuspensionRoleID, RowGUID
  FROM tblClients WHERE ClientID = @KillClientID

  DELETE tblClients WHERE ClientID = @KillClientID
  DELETE #_customer_dupes WHERE KeepClientID = @KillClientID --should've caught this originally, 
                                                             --joining two dupe records will commonly return two mirrored rows where the keep and kill are flipped
                                                             --so this delete just makes sure that we only processes the first one and then skip the second
  
  IF (@KeepSponsorID <> @KillSponsorID) begin
    -- now merge any additional clients that were under the @KillSponsorID over to the @KeepSponsorID
    -- it's arbitrary which one you choose to keep... i'm just using the form count to decide... we keep the one with more forms
    UPDATE tblClients SET SponsorID = @KeepSponsorID WHERE SponsorID = @KillSponsorID
    
    -- now we should be able to whack this completely abandonded Sponsor record (making sure it's not just dupes under the same sponsor)
      -- archive the record just to be extra careful
    INSERT [_deleted_dupe_sponsors] (SponsorID, [Rank], DutyLocation, DutyPhone, HomePhone, AddressLine1, AddressLine2, Active, StatusFlags, AgentID, RowGUID)
    SELECT SponsorID, [Rank], DutyLocation, DutyPhone, HomePhone, AddressLine1, AddressLine2, Active, StatusFlags, AgentID, RowGUID
    FROM tblSponsors WHERE SponsorID = @KillSponsorID
    
    DELETE tblSponsors WHERE SponsorID = @KillSponsorID

  end
  ELSE set @KillSponsorID = null

  --debug: print 'Consolidated under SponsorID: '+CONVERT(VARCHAR, @KeepSponsorID)+', Deleted SponsorID: '+isnull(CONVERT(VARCHAR, @KillSponsorID), '{N/A}')+', ClientID: '+CONVERT(VARCHAR, @KillClientID)  --SELECT * FROM [_deleted_dupe_sponsors]
  --SELECT * FROM [_deleted_dupe_clients]

END --while

DEALLOCATE curs

return

END
GO
