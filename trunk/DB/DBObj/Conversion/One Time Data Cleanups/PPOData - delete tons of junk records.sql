USE iTRAAC
go

/*******************
** TRY ** putting a unique index on tblPPOData.TaxFormID 
*/


/* this is the magic sauce we should run in a loop on central to keep things clean */
DELETE tblppodata WHERE TotalCost = 0 AND CurrencyUsed = 0

UPDATE tblPPOData SET CheckNumber = NULL WHERE CheckNumber IN ('n/a', '-', '', 
  '0',
  '00',
  '000',
  '0000',
  '00000',
  '000000',
  '0000000',
  '00000000',
  '000000000',
  '0000000000',
  '00000000000',
  '000000000000',
  '0000000000000',
  '00000000000000',
  '000000000000000'
)

                --find the zeros - save for usefulness
                --SELECT DISTINCT r.MATCH, LEN(r.MATCH)
                --FROM tblPPOData d
                --CROSS APPLY master.dbo.regexmatches(d.CheckNumber, '0+') r
                --WHERE d.CheckNumber IS NOT NULL
                --ORDER BY LEN(r.MATCH)

                --sp_procsearch 'ppodataid' --> cp_parmsel_iTRAAC_TaxForm, cp_InsertRowCounts
                
--SELECT * INTO _deleted_ppodata FROM tblPPOData WHERE TaxFormID NOT IN (SELECT TaxFormID FROM tblTaxForms)
-- double check this one: 
DELETE tblPPOData WHERE TaxFormID NOT IN (SELECT TaxFormID FROM tblTaxForms)


DELETE tblPPOData WHERE PPODataID IN (
  SELECT min(PPODataID)
  FROM tblPPOData
  GROUP BY TaxFormID, TotalCost, CheckNumber, CurrencyUsed
  HAVING COUNT(1) >1
)

/* eyeball this one after the above run with the commented select * and if it looks good fire it and run the DELETE above one last time
UPDATE d SET d.TotalCost = t.TotalCost, d.CheckNumber = t.CheckNumber, d.CurrencyUsed = t.CurrencyUsed
FROM tblPPOData d
JOIN (
  select TaxformID, max(TotalCost) AS TotalCost, max(CheckNumber) AS CheckNumber, max(CurrencyUsed) AS CurrencyUsed
  --SELECT *
  FROM tblPPOData WHERE TaxFormID IN (
    SELECT TaxformID FROM tblPPOData
    GROUP BY TaxFormID
    HAVING COUNT(1) >1
  )
  group by TaxformID
) t ON t.TaxformID = d.TaxformID                
*/


