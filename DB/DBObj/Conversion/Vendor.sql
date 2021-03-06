USE iTRAAC
go

/*************** tblVendors */
ALTER TABLE tblVendors ADD Line2 varchar(100) NULL
ALTER TABLE tblVendors ADD Street varchar(100) NULL
ALTER TABLE tblVendors ADD City varchar(100) NULL
ALTER TABLE tblVendors ADD PLZ varchar(20) NULL
ALTER TABLE tblVendors ADD Phone varchar(50) NULL
ALTER TABLE tblVendors ADD CreateDate datetime NOT NULL DEFAULT (getdate())

--ALTER TABLE tblVendors ADD CONSTRAINT DF_tblVendors_RowGUID DEFAULT (NEWSEQUENTIALID()) FOR ROWGUID
--ALTER TABLE tblVendors ADD CONSTRAINT DF_tblVendors_CreateDate DEFAULT (GETDATE()) FOR CreateDate
ALTER TABLE tblVendors ADD CONSTRAINT DF_tblVendors_Active DEFAULT (1) FOR Active
ALTER TABLE tblVendors ADD CONSTRAINT DF_tblVendors_StatusFlags DEFAULT (0) FOR StatusFlags

--ALTER TABLE tblVendors ALTER COLUMN AddressLine1 varchar(100) NULL
--ALTER TABLE tblVendors ADD CONSTRAINT ReftblTaxOffices233 FOREIGN KEY (TaxOfficeID) REFERENCES tblTaxOffices (TaxOfficeID)
CREATE NONCLUSTERED INDEX idx_TaxOffice ON tblVendors (TaxOfficeID) INCLUDE (VendorID, VendorName, Active, StatusFlags)

------ _ -------------------------------- _ ---------- _ ----- _ ----------------------------------- _ -----
--    / \      ____   _       _____      / \          / /     | |      _   _  _____           __    | |  
--   / ^ \    / __ \ | |     |  __ \    / ^ \        / /      | |     | \ | ||  ___|\        / /    | |  
--  /_/ \_\  | |  | || |     | |  | |  /_/ \_\      / /     __| |__   |  \| || |__ \ \  /\  / /   __| |__
--    | |    | |  | || |     | |  | |    | |       / /      \ \ / /   |     ||  __| \ \/  \/ /    \ \ / /
--    | |    | |__| || |____ | |__| |    | |      / /        \ V /    | |\  || |____ \  /\  /      \ V / 
--    |_|     \____/ |______||_____/     |_|     /_/          \_/     |_| \_||______| \/  \/        \_/  
------------------------------------------------------------------------------------------------------------

