--SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE COLUMN_NAME LIKE '%tran%'
--SELECT dbo.statusflags_f(ProhibTransFlags), OfficeCode, * FROM tblTaxOffices WHERE ProhibTransFlags > 0
SELECT * FROM tblTransactionTypes WHERE TransTypeID-1 IN (0, 4, 17, 19) --HD
SELECT * FROM tblTransactionTypes WHERE TransTypeID-1 IN (2, 6, 9, 25) --VI
SELECT * FROM tblTransactionTypes WHERE TransTypeID-1 IN (10) --MA
SELECT * FROM tblTransactionTypes WHERE TransTypeID-1 IN (17, 19, 30) --BN
SELECT * FROM tblTransactionTypes WHERE TransTypeID-1 IN (22, 28) --KL
