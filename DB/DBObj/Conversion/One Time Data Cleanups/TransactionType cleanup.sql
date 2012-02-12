/* 
. this is the new list in the proper sort order
. and add a SortOrder column for displaying in the drop down box

. map all the old ones that didn't make the cut over to new ones
. make an NF1 placeholder for 2	Pre-Purchased VAT Forms and consider that deactive/unselectable in V2

new sort order
===============
Vehicle Repair/Maintenance
Vehicle Purchase
Groceries
Furniture
Electronics
Tools
Home Related Purchase
Travel/Hotels
Fuel Oil, Mineral Oil, Gas
Utilities (red flag)
Restaurants
Social Events (Wedding/Communion/Partys)
Funeral Expenses
Group Purchases
Jewelry
Legal/Tax Advisor Fees
Medical
Recreational Activities
Rentals
Services
Weapons

need conversion script mapping old to new
see: C:\Brent\Dev\iTRAAC\iTRAAC v2\DB\Code Table Cleanup\Transaction Type cleanup.xlsx

*/

USE iTRAAC
go

-- merge a few types that never needed to be this granular
UPDATE tblTaxForms SET TransTypeID = 24 WHERE TransTypeID = 1 -- 1 Advertising --> 24 Services
UPDATE tblTaxForms SET TransTypeID = 22 WHERE TransTypeID = 23 -- 23 Renovations and Replacements --> 22 Home Related Purchase (old=Repairs and Maintenance)
UPDATE tblTaxForms SET TransTypeID = 12 WHERE TransTypeID = 26 -- 26	Travel (Individual and Group)	--> 12 Travel/Hotels (old=Lodging)

-- new holes: 1,23,26 --but don't use just to be safe

update tbltransactiontypes set SortOrder = 1, Active = 0, TransactionType = 'Advertising (moved to ''Services'')' where TransTypeID = 1 
UPDATE tblTransactionTypes SET SortOrder = 0, Active = 0 WHERE TransTypeID = 2 -- 2	Pre-Purchased VAT Forms	deactivate
-- TransTypeID 3 is an upsert below
UPDATE tblTransactionTypes SET SortOrder = 2, TransactionType = 'Vehicle Repair/Maintenance' WHERE TransTypeID = 4 --4	Vehicles (Repairs)	Vehicle Repair/Maintenance
-- TransTypeID 5 is an upsert below
UPDATE tblTransactionTypes SET SortOrder = 17 WHERE TransTypeID = 6 --6	Collectibles and Valuables	same
-- TransTypeID 7 is an upsert below
UPDATE tblTransactionTypes SET SortOrder = 10, TransactionType = 'Fuel Oil, Mineral Oil, Gas' WHERE TransTypeID = 8 --8	Fuel Oil and Mineral Oil	Fuel Oil, Mineral Oil, Gas
UPDATE tblTransactionTypes SET SortOrder = 14, TransactionType = 'Funeral Expenses' WHERE TransTypeID = 9 --9	Funerals	Funeral Expenses
-- TransTypeID 10 is an upsert below
UPDATE tblTransactionTypes SET SortOrder = 15 WHERE TransTypeID = 11 --11	Group Purchases	same
UPDATE tblTransactionTypes SET SortOrder = 9, TransactionType = 'Travel/Hotels' WHERE TransTypeID = 12 --12	Lodging	Travel/Hotels
UPDATE tblTransactionTypes SET SortOrder = 1, Active = 0, TransactionType='(don''t use) Insurance' WHERE TransTypeID = 13 --13	Insurance	deactivate
UPDATE tblTransactionTypes SET SortOrder = 16 WHERE TransTypeID = 14 --14	Jewelry	same
UPDATE tblTransactionTypes SET SortOrder = 18, TransactionType = 'Legal/Tax Advisor Fees' WHERE TransTypeID = 15 --15	Legal Fees  Legal/Tax Advisor Fees
UPDATE tblTransactionTypes SET SortOrder = 12, TransactionType = 'Restaurants' WHERE TransTypeID = 16 --16	Meals/Restaurants	Restaurants
UPDATE tblTransactionTypes SET SortOrder = 19 WHERE TransTypeID = 17 --17	Medical	same
-- TransTypeID 18 stays a hole
UPDATE tblTransactionTypes SET SortOrder = 20 WHERE TransTypeID = 19 --19	Recreational Activities	same
-- TransTypeID 20 stays a hole
UPDATE tblTransactionTypes SET SortOrder = 21 WHERE TransTypeID = 21 --21	Rentals	same
UPDATE tblTransactionTypes SET SortOrder = 8, TransactionType = 'Home Related Purchase' WHERE TransTypeID = 22 --22	Repairs and Maintenance	Home Related Purchase
update tbltransactiontypes set SortOrder = 1, Active = 0, TransactionType='Renovations & Replacements (moved to ''Home'')' where TransTypeID = 23 -- -1 = hide
UPDATE tblTransactionTypes SET SortOrder = 1 WHERE TransTypeID = 24 --24	Services	same
UPDATE tblTransactionTypes SET SortOrder = 1, Active = 0, TransactionType='(don''t use) Television Service' WHERE TransTypeID = 25 --25	Television Service	deactivate
update tbltransactiontypes set SortOrder = 1, Active = 0, TransactionType = 'Travel (moved to ''Travel/Hotel'')' where TransTypeID = 26 
UPDATE tblTransactionTypes SET SortOrder = 1, Active = 0, TransactionType='(don''t use) Trade-Ins' WHERE TransTypeID = 27 --27	Trade-Ins	deactivate
UPDATE tblTransactionTypes SET SortOrder = 11, TransactionType='Utilities (red flag)', ConfirmationText = 'This is extremely rare.' WHERE TransTypeID = 28 --28	Utilities	red flag??? Popup "are you sure??"
UPDATE tblTransactionTypes SET SortOrder = 22 WHERE TransTypeID = 29 --29	Weapons	same
UPDATE tblTransactionTypes SET SortOrder = 13, TransactionType = 'Social Events (Wedding/Communion/Party)' WHERE TransTypeID = 30 --30	Weddings	Social Events (Wedding/Communion/Partys)
UPDATE tblTransactionTypes SET SortOrder = 3, TransactionType = 'Vehicle Purchase' WHERE TransTypeID = 31 --31	Vehicles (Purchases)	Vehicle Purchase

-- existing holes: 3,5,7,10,18,20
-- new types: Furniture-3, Groceries-5, Electronics-7, Tools-10 
-- remaining holes: 18,20 (+ 1,23,26 --but don't use until way after rollout just to be safe)

--SELECT * FROM tblTransactionTypes ORDER BY TransTypeID
--SELECT * FROM tblTaxForms WHERE TransTypeID IN (3,5,7,10,18,20)

UPDATE tblTransactionTypes SET SortOrder = 4, TransactionType = 'Furniture' WHERE TransTypeID = 3
IF (@@RowCount = 0) BEGIN
  SET IDENTITY_INSERT tblTransactionTypes on
  INSERT tblTransactionTypes ( TransTypeID, TransactionType, Active, SortOrder ) VALUES  (3, 'Furniture', 1, 4)
  SET IDENTITY_INSERT tblTransactionTypes OFF
END

UPDATE tblTransactionTypes SET SortOrder = 5, TransactionType = 'Groceries' WHERE TransTypeID = 5
IF (@@RowCount = 0) BEGIN
  SET IDENTITY_INSERT tblTransactionTypes on
  INSERT tblTransactionTypes ( TransTypeID, TransactionType, Active, SortOrder ) VALUES  (5, 'Groceries', 1, 5)
  SET IDENTITY_INSERT tblTransactionTypes OFF
END

UPDATE tblTransactionTypes SET SortOrder = 6, TransactionType = 'Electronics' WHERE TransTypeID = 7
IF (@@RowCount = 0) BEGIN
  SET IDENTITY_INSERT tblTransactionTypes on
  INSERT tblTransactionTypes ( TransTypeID, TransactionType, Active, SortOrder ) VALUES  (7, 'Electronics', 1, 6)
  SET IDENTITY_INSERT tblTransactionTypes OFF
END

UPDATE tblTransactionTypes SET SortOrder = 7, TransactionType = 'Tools' WHERE TransTypeID = 10
IF (@@RowCount = 0) BEGIN
  SET IDENTITY_INSERT tblTransactionTypes on
  INSERT tblTransactionTypes ( TransTypeID, TransactionType, Active, SortOrder ) VALUES  (10, 'Tools', 1, 7)
  SET IDENTITY_INSERT tblTransactionTypes OFF
END

