-- drop the replicated tables
DROP TABLE tblAccessControl
DROP TABLE tblAttributes
DROP TABLE tblBoxes
DROP TABLE tblClients
DROP TABLE tblControlCentral
DROP TABLE tblGoodsServices
DROP TABLE tblOfficeManagers
DROP VIEW PPOData_DeDuped_v
DROP TABLE tblPPOData
DROP TABLE tblRemarks
DROP TABLE tblSponsors
DROP TABLE tblTaxFormAgents
DROP TABLE tblTaxFormPackages
DROP TABLE tblTaxForms
DROP TABLE tblUsers
DROP TABLE tblVendors

DROP TABLE tblTaxFormsO -- drop forever!!!
DROP TABLE tblRowCounts -- interesting! still up to date
DROP TABLE tblTaxFormExt
DROP TABLE tblFormFields

-- keep these code tables data
  -- tblTables
  -- tblControlLocal
  -- tblConversionRates -- already blank
  -- tblTaxFormTypes
  -- tblGoodsServiceTypes
  -- tblTransactionTypes
  -- tblCommunities
  -- tblInstallationCodes
  -- tblMACOMs
  -- tblUnits

-- blank out non-codetables
truncate TABLE tblControlNumbers
truncate TABLE tblDBEventData 
DELETE tblDBEvents 
truncate TABLE tblFormCounts 
truncate TABLE tblSessionEvents
truncate TABLE tblSessions


-- **** now do a shrink file!!!