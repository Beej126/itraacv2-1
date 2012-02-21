-- dbcc CHECKIDENT (tblTaxForms, NORESEED)
-- SET COMPATIBILITY_LEVEL option of ALTER DATABASE
-- ALTER DATABASE itraac SET COMPATIBILITY_LEVEL = 100
-- sp_dbcmptlevel itraac

SELECT Tbl, Ident
FROM (
SELECT 
IDENT_CURRENT('tblAccessControl') AS tblAccessControl,
IDENT_CURRENT('tblAttributes') AS tblAttributes,
IDENT_CURRENT('tblBoxes') AS tblBoxes,
IDENT_CURRENT('tblClients') AS tblClients,
IDENT_CURRENT('tblControlCentral') AS tblControlCentral,
IDENT_CURRENT('tblFormFields') AS tblFormFields,
IDENT_CURRENT('tblGoodsServices') AS tblGoodsServices,
IDENT_CURRENT('tblOfficeManagers') AS tblOfficeManagers,
IDENT_CURRENT('tblPPOData') AS tblPPOData,
IDENT_CURRENT('tblRemarks') AS tblRemarks,
IDENT_CURRENT('tblSponsors') AS tblSponsors,
IDENT_CURRENT('tblTaxFormAgents') AS tblTaxFormAgents,
IDENT_CURRENT('tblTaxFormPackages') AS tblTaxFormPackages,
IDENT_CURRENT('tblTaxForms') AS tblTaxForms,
IDENT_CURRENT('tblTaxOffices') AS tblTaxOffices,
IDENT_CURRENT('tblUsers') AS tblUsers,
IDENT_CURRENT('tblVendors') AS tblVendors
) t
UNPIVOT (Ident FOR Tbl IN (tblAccessControl, tblAttributes, tblBoxes, tblClients, tblControlCentral, tblFormFields, tblGoodsServices,
tblOfficeManagers, tblPPOData, tblRemarks, tblSponsors, tblTaxFormAgents, tblTaxFormPackages, tblTaxForms, tblTaxOffices, tblUsers, tblVendors)) AS t2

