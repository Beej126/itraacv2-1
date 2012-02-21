USE iTRAAC
go


CREATE PROC Admin_ReseedIdentities
AS begin

SET NOCOUNT ON

DECLARE @NEWSEED INT, @TaxOfficeID INT, @TRO VARCHAR(50)
SELECT @TaxOfficeID = TaxOfficeID FROM tblControlLocal
SELECT @TRO = TaxOfficeName FROM tblTaxOffices WHERE TaxOfficeID = @TaxOfficeID

PRINT CHAR(13)+CHAR(13)+'ReSeeding tables for '+@TRO+', TaxOfficeID: '+CONVERT(VARCHAR, @TaxOfficeID)

print char(13)+'  tblTaxForms '
SELECT @NEWSEED = ISNULL(MAX(TaxFormID), @TaxOfficeID)+1 FROM tblTaxForms WHERE TaxFormID < @TaxOfficeID+10000000
IF @NEWSEED < @TaxOfficeID set @NEWSEED = @TaxOfficeID
dbcc CHECKIDENT (tblTaxForms, RESEED, @NEWSEED)

print char(13)+'  tblTaxFormPackages  '
SELECT @NEWSEED = ISNULL(MAX(PackageID), @TaxOfficeID)+1 FROM tblTaxFormPackages WHERE PackageID < @TaxOfficeID+10000000
IF @NEWSEED < @TaxOfficeID set @NEWSEED = @TaxOfficeID
dbcc CHECKIDENT (tblTaxFormPackages, RESEED, @NEWSEED)

print char(13)+'  tblClients  '
SELECT @NEWSEED = ISNULL(MAX(ClientID), @TaxOfficeID)+1 FROM tblClients WHERE ClientID < @TaxOfficeID+10000000
IF @NEWSEED < @TaxOfficeID set @NEWSEED = @TaxOfficeID
dbcc CHECKIDENT (tblClients, RESEED, @NEWSEED)

print char(13)+'  tblSponsors  '
SELECT @NEWSEED = ISNULL(MAX(SponsorID), @TaxOfficeID)+1 FROM tblSponsors WHERE SponsorID < @TaxOfficeID+10000000
IF @NEWSEED < @TaxOfficeID set @NEWSEED = @TaxOfficeID
dbcc CHECKIDENT (tblSponsors, RESEED, @NEWSEED)

print char(13)+'  tblDBEventData  '
SELECT @NEWSEED = ISNULL(MAX(DBEID), @TaxOfficeID)+1 FROM tblDBEventData WHERE DBEID < @TaxOfficeID+10000000
IF @NEWSEED < @TaxOfficeID set @NEWSEED = @TaxOfficeID
dbcc CHECKIDENT (tblDBEventData, RESEED, @NEWSEED)

print char(13)+'  tblOfficeManagers  '
SELECT @NEWSEED = ISNULL(MAX(ManagerID), @TaxOfficeID)+1 FROM tblOfficeManagers WHERE ManagerID < @TaxOfficeID+10000000
IF @NEWSEED < @TaxOfficeID set @NEWSEED = @TaxOfficeID
dbcc CHECKIDENT (tblOfficeManagers, RESEED, @NEWSEED)

print char(13)+'  tblTaxFormAgents  '
SELECT @NEWSEED = ISNULL(MAX(AgentID), @TaxOfficeID)+1 FROM tblTaxFormAgents WHERE AgentID < @TaxOfficeID+10000000
IF @NEWSEED < @TaxOfficeID set @NEWSEED = @TaxOfficeID
dbcc CHECKIDENT (tblTaxFormAgents, RESEED, @NEWSEED)

print char(13)+'  tblAccessControl  '
SELECT @NEWSEED = ISNULL(MAX(ACID), @TaxOfficeID)+1 FROM tblAccessControl WHERE ACID < @TaxOfficeID+10000000
IF @NEWSEED < @TaxOfficeID set @NEWSEED = @TaxOfficeID
dbcc CHECKIDENT (tblAccessControl, RESEED, @NEWSEED)

print char(13)+'  tblAttributes  '
SELECT @NEWSEED = ISNULL(MAX(AttribID), @TaxOfficeID)+1 FROM tblAttributes WHERE AttribID < @TaxOfficeID+10000000
IF @NEWSEED < @TaxOfficeID set @NEWSEED = @TaxOfficeID
dbcc CHECKIDENT (tblAttributes, RESEED, @NEWSEED)

print char(13)+'  tblDBEvents  '
SELECT @NEWSEED = ISNULL(MAX(DBEID), @TaxOfficeID)+1 FROM tblDBEvents WHERE DBEID < @TaxOfficeID+10000000
IF @NEWSEED < @TaxOfficeID set @NEWSEED = @TaxOfficeID
dbcc CHECKIDENT (tblDBEvents, RESEED, @NEWSEED)

print char(13)+'  tblGoodsServices  '
SELECT @NEWSEED = ISNULL(MAX(GooodsServicesID), @TaxOfficeID)+1 FROM tblGoodsServices WHERE GooodsServicesID < @TaxOfficeID+10000000
IF @NEWSEED < @TaxOfficeID set @NEWSEED = @TaxOfficeID
dbcc CHECKIDENT (tblGoodsServices, RESEED, @NEWSEED)

print char(13)+'  tblSessionEvents '
SELECT @NEWSEED = ISNULL(MAX(SEID), @TaxOfficeID)+1 FROM tblSessionEvents WHERE SEID < @TaxOfficeID+10000000
IF @NEWSEED < @TaxOfficeID set @NEWSEED = @TaxOfficeID
dbcc CHECKIDENT (tblSessionEvents, RESEED, @NEWSEED)

print char(13)+'  tblVendors  '
SELECT @NEWSEED = ISNULL(MAX(VendorID), @TaxOfficeID)+1 FROM tblVendors WHERE VendorID < @TaxOfficeID+10000000
IF @NEWSEED < @TaxOfficeID set @NEWSEED = @TaxOfficeID
dbcc CHECKIDENT (tblVendors, RESEED, @NEWSEED)

print char(13)+'  tblPPOData  '
SELECT @NEWSEED = ISNULL(MAX(PPODataID), @TaxOfficeID)+1 FROM tblPPOData WHERE PPODataID < @TaxOfficeID+10000000
IF @NEWSEED < @TaxOfficeID set @NEWSEED = @TaxOfficeID
dbcc CHECKIDENT (tblPPOData, RESEED, @NEWSEED)

print char(13)+'  tblControlNumbers  '
SELECT @NEWSEED = ISNULL(MAX(CNID), @TaxOfficeID)+1 FROM tblControlNumbers WHERE CNID < @TaxOfficeID+10000000
IF @NEWSEED < @TaxOfficeID set @NEWSEED = @TaxOfficeID
dbcc CHECKIDENT (tblControlNumbers, RESEED, @NEWSEED)

print char(13)+'  tblSessions  '
SELECT @NEWSEED = ISNULL(MAX(SessionID), @TaxOfficeID)+1 FROM tblSessions WHERE SessionID < @TaxOfficeID+10000000
IF @NEWSEED < @TaxOfficeID set @NEWSEED = @TaxOfficeID
dbcc CHECKIDENT (tblSessions, RESEED, @NEWSEED)

print char(13)+'  tblUsers  '
SELECT @NEWSEED = ISNULL(MAX(UserID), @TaxOfficeID)+1 FROM tblUsers WHERE UserID < @TaxOfficeID+10000000
IF @NEWSEED < @TaxOfficeID set @NEWSEED = @TaxOfficeID
dbcc CHECKIDENT (tblUsers, RESEED, @NEWSEED)

print char(13)+'  tblRemarks  '
SELECT @NEWSEED = ISNULL(MAX(RemarksID), @TaxOfficeID)+1 FROM tblRemarks WHERE RemarksID < @TaxOfficeID+10000000
IF @NEWSEED < @TaxOfficeID set @NEWSEED = @TaxOfficeID
dbcc CHECKIDENT (tblRemarks, RESEED, @NEWSEED)


END
GO

GRANT EXEC ON Admin_ReseedIdentities TO PUBLIC
GO

EXEC Admin_ReseedIdentities
