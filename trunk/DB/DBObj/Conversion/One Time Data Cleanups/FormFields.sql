
/*
ALTER TABLE tblFormFields ALTER COLUMN MaxLength INT
ALTER TABLE tblFormFields ALTER COLUMN MaxRows INT
ALTER TABLE tblFormFields ALTER COLUMN Description VARCHAR(100)
ALTER TABLE tblFormFields add RowGUID uniqueidentifier not null default(newsequentialid()) ROWGUIDCOL 
ALTER TABLE tblFormFields add [Page] int not null default(0)

ALTER TABLE tblFormFields drop column [Page] 
ALTER TABLE tblFormFields drop constraint DF__tblFormFie__Page__4E791BE0

drop table tblformfields

CREATE TABLE [tblFormFields](
	[FormTypeID] [int] NOT NULL,
	[FieldName] [varchar](10) NOT NULL,
	[Page] INT NOT NULL DEFAULT(0),
	[StartRow] [decimal](4, 1) NOT NULL,
	[StartCol] [decimal](4, 1) NOT NULL,
	[MaxLength] [int] NULL,
	[MaxRows] [int] NULL,
	[Description] [varchar](100) NULL,
	[RowGUID] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
  CONSTRAINT [PK] PRIMARY KEY CLUSTERED (FormTypeID, FieldName, Page)
)
GO

ALTER TABLE [tblFormFields] ADD  DEFAULT (newsequentialid()) FOR [RowGUID]
GO
*/


/* 
code-gen:
select
'insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values ('+
CONVERT(VARCHAR, FormTypeID) + ', ' +
right(' ''' + RTRIM(FieldName), 4) + ''', ' +
CONVERT(VARCHAR, page) + ', ' +
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

SELECT * FROM vw_ABW WHERE TaxFormID= 50528628-- 50466038
select top 5 * from tbltaxforms where TaxFormID>50466038 and formtypeid = 2
*/

USE iTRAAC
go

BEGIN TRAN
DELETE tblformfields

-- NF1 fields
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (1,  'F1', 0, 10.0, 60.0,  20,  1, 'OrderNumber')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (1,  'F2', 0, 10.0,  4.0,  40,  1, 'VAT Office Name')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (1,  'F3', 0, 11.0,  4.0,  40,  1, 'VAT Office Addr1')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (1,  'F4', 0, 12.0,  4.0,  40,  1, 'VAT Office Addr2')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (1,  'F5', 0, 13.0,  4.0,  40,  1, 'VAT Office Addr3')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (1,  'F6', 0, 14.0, 60.0,  20,  1, 'From Date')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (1,  'F7', 0, 18.0, 60.0,  20,  1, 'Expiration Date')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (1,  'F8', 0, 20.0,  4.0,  40,  1, 'VAT Agent Name')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (1,  'F9', 0, 27.0,  4.0,   4,  2, 'NFx Checkbox')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (1, 'F10', 0, 36.0, 69.0,  23,  1, 'NF1 Amount')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (1, 'F11', 0, 52.0, 69.0,  23, -3, 'NF2 Amount (negative rowcount means wordwrap up vs down)')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (1, 'F12', 0, 59.0,  4.0,  27,  2, 'Sponsor Name')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (1, 'F13', 0, 62.0,  4.0,  25,  1, 'Auth. Dependent Name')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (1, 'F14', 0, 70.0,  4.0,  43,  3, 'Description')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (1, 'F15', 0, 58.0,  4.0,   7,  1, 'Sponsor CCode')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (1, 'F16', 0, 70.0, 54.0,  35,  1, 'Vendor Name')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (1, 'F17', 0, 77.0, 30.0,  35,  1, 'Returned/Filed Check Boxes')

-- NF2 fields, basically the same as NF1
INSERT tblFormFields ( FormTypeID, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description )
SELECT 2, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, DESCRIPTION
FROM tblFormFields WHERE FormTypeID = 1

--this is the only one that should be different between NF1 & NF2
UPDATE tblFormFields SET StartRow = 41 WHERE FormTypeID = 2 AND FieldName = 'F9' -- NF2 Checkbox

-- Abw fields
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3,  'F1', 0,  9.5, 19.0,   4,  1, 'XXXX for NATO box')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3,  'F2', 0, 21.0, 60.0,  50,  1, 'Vendor Name')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3,  'F3', 0, 22.0, 60.0,  50,  2, 'Vendor Address')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3,  'F4', 0, 23.0, 60.0,  50,  1, '(not used, previously address line2, now combining into F2, and wordwrapping two lines)')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3,  'F5', 0, 26.5, 44.0,  16,  1, 'Order Number')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3,  'F6', 0, 27.0, 59.0,  15,  1, 'Date, but blanked out in vw_ABW, maybe to avoid audit compliance issues??')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3,  'F7', 0, 28.0, 44.0,  50,  1, 'VAT Office Address Line 1')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3,  'F8', 0, 29.0, 44.0,  50,  1, 'VAT Office Address Line 2')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3,  'F9', 0, 30.0, 44.0,  50,  1, 'VAT Office Address Line 3')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3, 'F10', 0, 33.0, 44.0,  50,  1, 'Customer Name')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3, 'F11', 0, 34.0, 44.0,  50,  1, 'Duty Location')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3, 'F12', 0, 46.0, 83.0,   4,  1, 'NF2 Currency')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3, 'F13', 0, 49.0, 19.0,  50,  1, 'NF2 Transaction Type')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3, 'F14', 0, 73.5, 83.0,  10,  1, 'NF2 Total Cost')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3, 'F15', 0, 52.0, 19.0,  50,  4, 'NF2 Transaction Type Special Fields (vehicle, weapons)')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3, 'F16', 0, 53.0, 19.0,  50,  1, 'v2 - not used, v1 - special field 2')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3, 'F17', 0, 54.0, 19.0,  50,  1, 'v2 - not used, v1 - special field 3')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3, 'F18', 0, 55.0, 19.0,  50,  1, 'v2 - not used, v1 - special field 4')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3, 'F19', 1, 17.0, 39.0,  22,  1, 'NF2 Currency TotalCost')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3, 'F20', 1, 20.5, 30.0,   4,  1, 'NF2 Currency')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3, 'F21', 1, 24.0, 13.0,   4,  1, 'NF2 X in cheque box')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3, 'F22', 1, 25.5, 58.0,  20,  1, 'NF2 Purchase Date')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3, 'F23', 1, 33.5,  7.0,  20,  1, 'NF2 Purchase Date')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3, 'F24', 1, 33.5, 58.0,  75,  1, 'VAT Agent Sigblock')
insert tblFormFields (FormTypeId, FieldName, Page, StartRow, StartCol, MaxLength, MaxRows, Description) values (3, 'F25', 0, 31.0, 44.0,  50,  1, 'VAT Office Address Line 4 (new)')
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

