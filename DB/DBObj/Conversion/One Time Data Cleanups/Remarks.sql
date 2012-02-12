-- honestly can't remember what i was going for here anymore
/*
UPDATE tblRemarks SET Remarks = REPLACE(remarks, 'due to', '') WHERE Remarks LIKE 'due to form violation%'
UPDATE tblRemarks SET Remarks = REPLACE(remarks, 'form #:', '') WHERE Remarks LIKE 'form violation%' --<-- confirm this line for proper string format
*/