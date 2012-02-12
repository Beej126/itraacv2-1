--sp_msforeachtable 'select * from information_schema.columns where table''?'''
select * from information_schema.columns where COLUMN_NAME LIKE 'rowguid%'
ALTER TABLE tblTaxForms ALTER COLUMN RowGUID UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL

sp_help tblformfields

sp_msforeachtable 'select ''?'' as tablename, columnproperty(object_id(''?''), c.[name], ''IsRowGUIDCol'') as IsRowGUIDCol, c.[name] from syscolumns c where c.id = object_id(''?'') and c.name = ''rowguid'' '

ALTER TABLE tblClients ADD CONSTRAINT DF_tblClients_RowGUID DEFAULT (NEWSEQUENTIALID()) FOR ROWGUID
ALTER TABLE tblPPOData ADD CONSTRAINT DF_tblPPOData_RowGUID DEFAULT (NEWSEQUENTIALID()) FOR RowGUID
ALTER TABLE tblRemarks  ADD CONSTRAINT DF_tblRemarks_RowGUID DEFAULT (NEWSEQUENTIALID()) FOR ROWGUID
ALTER TABLE tblSponsors ADD CONSTRAINT DF_tblSponsors_RowGUID DEFAULT (NEWSEQUENTIALID()) FOR ROWGUID
ALTER TABLE tblTaxForms ADD CONSTRAINT DF_tblTaxForms_RowGUID DEFAULT (NEWSEQUENTIALID()) FOR RowGUID
ALTER TABLE tblTaxFormPackages ADD CONSTRAINT DF_tblTaxFormPackages_RowGUID DEFAULT (NEWSEQUENTIALID()) FOR ROWGUID
ALTER TABLE tblVendors ADD CONSTRAINT DF_tblVendors_RowGUID DEFAULT (NEWSEQUENTIALID()) FOR ROWGUID