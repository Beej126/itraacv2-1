USE [iTRAAC]
GO

/* testing:
select * from vw_NFX where f1 = 'NF1-BR-12-06486'
EXEC cp_parmsel_TaxFormData_NF1 @TaxFormID = 110076430
F12|58.0|4.0|25|2|
Ramirez Montollo, Robinson L. (R3567)
insert tblFormFields values (1, 'F12', 58.0,  4.0,  25,  2, 'Sponsor Name + CCode')
*/
UPDATE tblFormFields SET MaxLength = 28 WHERE FormTypeID = 1 AND FieldName = 'f12'


/****** Object:  View [vw_NFx]    Script Date: 02/29/2012 16:46:17 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

if OBJECT_ID('vw_NFx') IS null
	exec('create view vw_NFx as select 1 as one')
go

alter VIEW [vw_NFx]
AS
SELECT
  tf.TaxFormID,
  tf.FormTypeID,
  
  -- 1. Tax office name/address
  t.AgencyLine1 AS F2,
  t.AgencyLine2 AS F3,
  t.AgencyLine3 AS F4,
  ISNULL(t.FormFootNoteLine1,'') AS F5, -- this field is now getting hijacked to effectively become AgencyLine4 since we're started having office addresses which require all 4 lines (e.g. Mannheim circa 2011)

  -- 2. VAT officer's sig block
  a.SigBlock AS F8,
  
  -- 3. Order number
  tf.OrderNumber AS F1,
  
  -- 3. From date
  dbo.ClearDate(p.PurchaseDate) AS F6,

  -- 3. Until date
  dbo.ClearDate(
  CASE 
    WHEN tf.FormTypeID = 1 THEN DATEADD(dd, -1, DATEADD(yy, 2, p.PurchaseDate))
    WHEN tf.FormTypeID = 2 THEN case tf.OrderNumber WHEN 'NF2-MA-11-00034' THEN '7 June 2011' else DATEADD(dd, 125, p.PurchaseDate) end
  END) AS F7,
  
  -- 4. NF1 check box
  'XX' AS F9,

  -- 4. NF1 amount
  CASE 
    WHEN tf.FormTypeID = 1 THEN '' -- NF1
    ELSE 'XXXXXXXXXXXXXXXX' -- NF2
  END AS F10,

  -- 5. NF2 amount
  CASE
    WHEN tf.FormTypeID = 1 THEN 'XXXXXXXXXXXXXXXX' -- NF1
    ELSE -- NF2
      --this was a pretty bad logic choice in the original implementation... i.e. if the used chooses "Pre-Purchased VAT Forms" then the price isn't printed on the NF2 form (that's a bad thing)
      --CASE 
        --WHEN tf.TransTypeID = 2 THEN ' ' ELSE 
        (SELECT CONVERT(varchar, TotalCost) FROM tblPPOData WHERE TaxFormID = tf.TaxFormID)
      --END
  END F11,
  
  -- 6. Designated agent (the Customer)
  ISNULL('('+C.CCode+')','') AS F15,

  -- 6. Designated Agent (the Customer)
  ISNULL(c2.Client, '') AS F12,

  -- 9. Authorized family member
  CASE 
    -- when client is the sponsor...
    WHEN c2.ClientID = C.ClientID THEN ISNULL(
        -- then try to pull the spouse 
        (SELECT TOP 1
          Client
        FROM vw_Clients
        WHERE IsSponsor = 0
        AND SponsorID = C.SponsorID
        AND Active = 1  -- don't show spouse's name if they've been deactivated (divorce is the typical scenario here)
        ORDER BY
          CASE
            WHEN StatusFlags & 2 = 2 THEN 0 --sort IsSpouse to the top
            ELSE 1
          END,
          ClientID),
     'XXXXXXXXXXXXXXXX')
     ELSE C.Client
  END AS F13,

  -- 12. Goods/Service
  CASE
    -- pull the form description if there is one
    WHEN tf.FormTypeID = 2 THEN
      ISNULL(dbo.TaxFormDescription_f(tf.DESCRIPTION), ISNULL(tt.TransactionType, '') + ISNULL('; ' + g.GoodsServiceName, ''))
    ELSE ''
  END AS F14,
  
  -- 13. Vendor  
  ISNULL(v.VendorName, ' ') AS F16,
  
  -- same as F11??? Stuttgart had it in their cp_parmsetl_TaxFormData_NF1 & _NF2 definitions
  '[ ] Returned      [ ] Filed' AS F17
  
FROM tblTaxForms tf
JOIN tblTaxFormPackages p ON p.PackageID = tf.PackageID
LEFT JOIN tblTaxFormAgents a ON a.AgentID = p.AgentID
JOIN tblTaxOffices t ON t.TaxOfficeID = a.TaxOfficeID
--not needed, done via a correlated subquery: LEFT JOIN tblPPOData ppo ON ppo.TaxFormID = tf.TaxFormID
JOIN vw_Clients c ON c.ClientID = p.ClientID
LEFT JOIN vw_Clients c2 ON c2.SponsorID = c.SponsorID AND c2.IsSponsor = 1 AND c2.Active = 1
LEFT JOIN tblVendors v ON v.VendorID = tf.VendorID
LEFT JOIN tblGoodsServices g ON g.GooodsServicesID = tf.GooodsServicesID
LEFT JOIN tblTransactionTypes tt ON tt.TransTypeID = tf.TransTypeID



GO

GRANT SELECT ON vw_NFx TO PUBLIC
go