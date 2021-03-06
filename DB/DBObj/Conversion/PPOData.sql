use iTRAAC
go

/*************** tblPPOData */

ALTER TABLE [dbo].[tblPPOData] ADD [TaxFormGUID] [uniqueidentifier] NULL -- replaces TaxFormID

ALTER TABLE [dbo].[tblPPOData] ALTER COLUMN [CheckNumber] [varchar] (25) NULL -- allow null obviously not all VAT purchases are by check... finally stop forcing everybody to enter bogus data in this field and get rid of thousands of junk records

drop index tblppodata.idx_clustered
CREATE CLUSTERED INDEX [ix_TaxFormGUID] ON [dbo].[tblPPOData] ([TaxFormGUID]) -- moving towards multiple payments for vehicle NF2's (cash + loan + tradein), so can't make these PPOData -> TaxForm indexes UNIQUE
CREATE INDEX [ix_TaxFormID] ON [dbo].[tblPPOData] ([TaxFormID]) INCLUDE ([TaxFormGUID])

CREATE UNIQUE INDEX [ix_PPODataID_PK] ON [dbo].[tblPPOData] ([PPODataID])

ALTER TABLE tblPPOData ADD CONSTRAINT DF_tblPPOData_RowGUID DEFAULT (NEWSEQUENTIALID()) FOR RowGUID

--ALTER TABLE [dbo].[tblTaxFormTypes] ADD CONSTRAINT [PK18] PRIMARY KEY CLUSTERED  ([FormTypeID])

--revoke SELECT ON  [dbo].[tblClients] TO [ReportsUser]
--revoke SELECT ON  [dbo].[tblPPOData] TO [ReportsUser]
--revoke SELECT ON  [dbo].[tblTaxFormPackages] TO [ReportsUser]
--revoke SELECT ON  [dbo].[tblTransactionTypes] TO [ReportsUser]
--GRANT SELECT ON  [dbo].[tblVendors] TO [ReportsUser]

