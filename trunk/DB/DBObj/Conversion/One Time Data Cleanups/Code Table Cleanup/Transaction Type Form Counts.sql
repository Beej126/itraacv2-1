--SELECT COUNT(1) FROM tblTaxForms WHERE FormTypeID = 2 AND TransTypeID <> 2

SELECT ft.CodeName, tt.TransactionType, COUNT(1) AS [Count of this type]
FROM tblTaxForms f
JOIN tblTaxFormTypes ft ON ft.FormTypeID = f.FormTypeID
JOIN tblTransactionTypes tt ON tt.TransTypeID = f.TransTypeID
GROUP BY ft.CodeName, tt.TransactionType 
ORDER BY ft.CodeName, tt.TransactionType

--SELECT * FROM tblTransactionTypes