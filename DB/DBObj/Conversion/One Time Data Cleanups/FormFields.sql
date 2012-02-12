/*
ALTER TABLE tblFormFields ALTER COLUMN MaxLength INT
ALTER TABLE tblFormFields ALTER COLUMN MaxRows INT
ALTER TABLE tblFormFields ALTER COLUMN Description VARCHAR(100)
ALTER TABLE tblFormFields add RowGUID uniqueidentifier not null default(newsequentialid()) ROWGUIDCOL 
*/


/* 
code-gen:
select
'insert tblFormFields values ('+
CONVERT(VARCHAR, FormTypeID) + ', ' +
right(' ''' + RTRIM(FieldName), 4) + ''', ' +
right(' ' + CONVERT(VARCHAR, StartRow), 4) + ', ' +
right(' ' + CONVERT(VARCHAR, StartCol), 4) + ', ' +
right('  ' + CONVERT(VARCHAR, MaxLength), 3) + ', ' +
right(' ' + CONVERT(VARCHAR, MaxRows), 2) + ', ' +
isnull('''' + description + '''', 'NULL') + ')'
FROM tblFormFields
where formtypeid in (1,3)
ORDER BY FormTypeID, CONVERT(INT, SUBSTRING(FieldName, 2,5))

select * from tblformfields

type 1 = NF1
type 2 = NF2
type 3 = Abw

*/



USE iTRAAC
go

BEGIN TRAN
TRUNCATE TABLE tblformfields

--                          ID,  Name,  Row,  Col, Len, MaxRows
insert tblFormFields values (1,  'F1', 10.0, 60.0,  20,  1, 'OrderNumber')
insert tblFormFields values (1,  'F2', 10.0,  4.0,  40,  1, 'VAT Office Name')
insert tblFormFields values (1,  'F3', 11.0,  4.0,  40,  1, 'VAT Office Addr1')
insert tblFormFields values (1,  'F4', 12.0,  4.0,  40,  1, 'VAT Office Addr2')
insert tblFormFields values (1,  'F5', 13.0,  4.0,  40,  1, 'VAT Office Addr3')
insert tblFormFields values (1,  'F6', 14.0, 60.0,  20,  1, 'From Date')
insert tblFormFields values (1,  'F7', 18.0, 60.0,  20,  1, 'Expiration Date')
insert tblFormFields values (1,  'F8', 20.0,  4.0,  40,  1, 'VAT Agent Name')
insert tblFormFields values (1,  'F9', 27.0,  4.0,   4,  2, 'NFx Checkbox')
insert tblFormFields values (1, 'F10', 36.0, 69.0,  23,  1, 'NF1 Amount')
insert tblFormFields values (1, 'F11', 52.0, 69.0,  23, -3, 'NF2 Amount (negative rowcount means wordwrap up vs down)')
insert tblFormFields values (1, 'F12', 58.0,  4.0,  25,  2, 'Sponsor Name + CCode')
insert tblFormFields values (1, 'F13', 62.0,  4.0,  25,  1, 'Auth. Dependent Name')
insert tblFormFields values (1, 'F14', 70.0,  4.0,  43,  3, 'Description')
insert tblFormFields values (1, 'F15', 59.0,  4.0,   7,  1, 'Not Used, used to be Sponsor CCode, see F12') --... now that we're stamping SponsorName + CCode onto the TaxForm when names change, it's easier to maintain this as a single field
insert tblFormFields values (1, 'F16', 70.0, 54.0,  35,  1, 'Vendor Name')
insert tblFormFields values (1, 'F17', 77.0, 30.0,  35,  1, 'Returned/Filed Check Boxes')

INSERT tblFormFields ( FormTypeID, FieldName, StartRow, StartCol, MaxLength, MaxRows, Description )
SELECT 2, FieldName, StartRow, StartCol, MaxLength, MaxRows, DESCRIPTION
FROM tblFormFields WHERE FormTypeID = 1

--this is the only one that should be different between NF1 & NF2
UPDATE tblFormFields SET StartRow = 41 WHERE FormTypeID = 2 AND FieldName = 'F9' -- NF2 Checkbox

insert tblFormFields values (3, 'F1',   9.5, 19.0,  4, 1, null)
insert tblFormFields values (3, 'F2',  21.0, 60.0, 50, 1, null)
insert tblFormFields values (3, 'F3',  22.0, 60.0, 50, 1, null)
insert tblFormFields values (3, 'F4',  23.0, 60.0, 50, 1, null)
insert tblFormFields values (3, 'F5',  26.5, 44.0, 16, 1, null)
insert tblFormFields values (3, 'F6',  27.0, 59.0, 15, 1, null)
insert tblFormFields values (3, 'F7',  28.0, 44.0, 50, 1, null)
insert tblFormFields values (3, 'F8',  29.0, 44.0, 50, 1, null)
insert tblFormFields values (3, 'F9',  30.0, 44.0, 50, 1, null)
insert tblFormFields values (3, 'F10', 33.0, 44.0, 50, 1, null)
insert tblFormFields values (3, 'F11', 34.0, 44.0, 50, 1, null)
insert tblFormFields values (3, 'F12', 46.0, 83.0,  4, 1, null)
insert tblFormFields values (3, 'F13', 49.0, 19.0, 50, 1, null)
insert tblFormFields values (3, 'F14', 73.5, 83.0, 10, 1, null)
insert tblFormFields values (3, 'F15', 52.0, 19.0, 50, 1, null)
insert tblFormFields values (3, 'F16', 53.0, 19.0, 50, 1, null)
insert tblFormFields values (3, 'F17', 54.0, 19.0, 50, 1, null)
insert tblFormFields values (3, 'F18', 55.0, 19.0, 50, 1, null)
insert tblFormFields values (3, 'F19', 17.0, 39.0, 22, 1, null)
insert tblFormFields values (3, 'F20', 20.5, 30.0,  4, 1, null)
insert tblFormFields values (3, 'F21', 24.0, 13.0,  4, 1, null)
insert tblFormFields values (3, 'F22', 25.5, 58.0, 20, 1, null)
insert tblFormFields values (3, 'F23', 33.5,  7.0, 20, 1, null)
insert tblFormFields values (3, 'F24', 33.5, 58.0, 75, 1, null)

/*
The Euro VAT forms should be implemented as a filled PDF
(via existing iTextSharp wrapper code, just search for it in the iTRAACv2 app source code)
Therefore the PDF will contain all of this field positioning info

insert tblFormFields values (4, 'F1', 11.5, 65.0, 15, 1, null)
insert tblFormFields values (4, 'F2', 13.4, 4.0, 40, 1, null)
insert tblFormFields values (4, 'F3', 14.4, 4.0, 41, 1, null)
insert tblFormFields values (4, 'F4', 18.0, 4.0, 42, 1, null)
insert tblFormFields values (4, 'F5', 19.0, 4.0, 41, 1, null)
insert tblFormFields values (4, 'F6', 21.7, 3.0, 4, 1, null)
insert tblFormFields values (4, 'F7', 31.5, 73.0, 10, 1, null)
insert tblFormFields values (4, 'F8', 52.0, 4.0, 30, 1, null)
insert tblFormFields values (4, 'F9', 53.0, 4.0, 30, 1, null)
insert tblFormFields values (4, 'F10', 14.0, 49.0, 32, 1, null)
insert tblFormFields values (4, 'F11', 58.0, 4.0, 30, 1, null)
insert tblFormFields values (4, 'F12', 15.0, 49.0, 16, 1, null)
insert tblFormFields values (4, 'F13', 71.0, 6.0, 60, 1, null)
insert tblFormFields values (4, 'F14', 72.0, 6.0, 60, 1, null)
insert tblFormFields values (4, 'F15', 17.0, 49.0, 15, 1, null)

insert tblFormFields values (5, 'F1', 11.5, 65.0, 15, 1, null)
insert tblFormFields values (5, 'F2', 13.4, 4.0, 40, 1, null)
insert tblFormFields values (5, 'F3', 14.4, 4.0, 41, 1, null)
insert tblFormFields values (5, 'F4', 18.0, 4.0, 42, 1, null)
insert tblFormFields values (5, 'F5', 19.0, 4.0, 41, 1, null)
insert tblFormFields values (5, 'F6', 35.5, 3.0, 4, 1, null)
insert tblFormFields values (5, 'F7', 31.5, 73.0, 10, 1, null)
insert tblFormFields values (5, 'F8', 52.0, 4.0, 30, 1, null)
insert tblFormFields values (5, 'F9', 53.0, 4.0, 30, 1, null)
insert tblFormFields values (5, 'F10', 14.0, 49.0, 32, 1, null)
insert tblFormFields values (5, 'F11', 58.0, 4.0, 30, 1, null)
insert tblFormFields values (5, 'F12', 15.0, 49.0, 16, 1, null)
insert tblFormFields values (5, 'F13', 71.0, 6.0, 60, 1, null)
insert tblFormFields values (5, 'F14', 72.0, 6.0, 60, 1, null)
insert tblFormFields values (5, 'F15', 17.0, 49.0, 15, 1, null)
*/

COMMIT

