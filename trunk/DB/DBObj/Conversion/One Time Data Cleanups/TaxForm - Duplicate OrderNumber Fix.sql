


-- i already double checked that this DOES NOT blow up in the primary screens of iTRAACv1


SELECT COUNT(1), OrderNumber 
FROM tblTaxForms
GROUP BY OrderNumber
HAVING COUNT(1)>1
ORDER BY COUNT(1) DESC


SELECT
  TaxFormID,
  OrderNumber,
  OrderNumber + CHAR(96+ROW_NUMBER() OVER (PARTITION BY OrderNumber ORDER BY OrderNumber)) AS NewOrdNum
  INTO _deleteme_2011_09_14_TaxForm_OrderNumber_Dupe_Fix_BAK
FROM tblTaxForms WHERE OrderNumber IN (
  SELECT --COUNT(1), 
  OrderNumber FROM tblTaxForms
  GROUP BY OrderNumber
  HAVING COUNT(1)>1
  --ORDER BY COUNT(1) DESC
)

/*
UPDATE f SET f.OrderNumber = t.NewOrdNum
from tblTaxForms f
join (
  SELECT
    TaxFormID,
    OrderNumber,
    OrderNumber + CHAR(96+ROW_NUMBER() OVER (PARTITION BY OrderNumber ORDER BY OrderNumber)) as NewOrdNum
  FROM tblTaxForms WHERE OrderNumber IN (
    SELECT --COUNT(1), 
    OrderNumber FROM tblTaxForms
    GROUP BY OrderNumber
    HAVING COUNT(1)>1
    --ORDER BY COUNT(1) DESC
  )
) t on t.TaxFormID = f.TaxFormID
*/