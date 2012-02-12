if not exists(select 1 from sysobjects where name = 'BitFlipper_f')
	exec('create FUNCTION BitFlipper_f() returns int begin return 0 end')
GO
ALTER FUNCTION IdentityCheck_f(@Id int)
returns bit
AS 
BEGIN 
  DECLARE @TaxOfficeId INT
  SELECT @TaxOfficeId=TaxOfficeId FROM tblControlLocal
  IF @Id BETWEEN @TaxOfficeId AND @TaxOfficeId+10000000 RETURN 1
  RETURN 0
END
go

GRANT EXECUTE ON dbo.BitFlipper_f TO PUBLIC
go

IF NOT EXISTS(SELECT 1 FROM sys.objects WHERE name = 'IdentityCheck_Clients' AND [type] = 'C')
  ALTER TABLE tblClients WITH nocheck
  ADD CONSTRAINT IdentityCheck_Clients CHECK (dbo.IdentityCheck_f(SponsorID) = 1 AND dbo.IdentityCheck_f(ClientID) = 1) 

IF NOT EXISTS(SELECT 1 FROM sys.objects WHERE name = 'IdentityCheck_Sponsors' AND [type] = 'C')
  ALTER TABLE tblSponsors WITH nocheck
  ADD CONSTRAINT IdentityCheck_Sponsors CHECK (dbo.IdentityCheck_f(SponsorID) = 1) 

IF NOT EXISTS(SELECT 1 FROM sys.objects WHERE name = 'IdentityCheck_Packages' AND [type] = 'C')
  ALTER TABLE tblTaxFormPackages WITH nocheck
  ADD CONSTRAINT IdentityCheck_Packages CHECK (dbo.IdentityCheck_f(ClientId) = 1) 



SELECT * FROM tblTaxFormPackages WHERE ClientID < 10000000 
AND ClientID NOT IN (SELECT ClientID FROM tblClients)
ORDER BY PurchaseDate DESC

-- DELETE tblTaxFormPackages WHERE PackageID = 230014106

SELECT * FROM tblClients WHERE SponsorID < 10000000 
AND SponsorID NOT in (SELECT SponsorID FROM tblSponsors)
