--SELECT LName, 
--  SUBSTRING(Lname, 1, PATINDEX('%, i%', LName)-1),
--  SUBSTRING(Lname, PATINDEX('%, i%', LName)+2, 100),
--  PATINDEX('%, i%', LName)
UPDATE c SET 
  lname = SUBSTRING(Lname, 1, PATINDEX('%[, ]i%', LName)-1),
  SuffixName = SUBSTRING(Lname, PATINDEX('%[, ]i%', LName)+1, 100) + ISNULL(', ' + SuffixName,'')
FROM tblClients c
WHERE lname LIKE '%[, ]i%'
AND SSN <> '000-00-0000'

UPDATE c SET 
  lname = SUBSTRING(Lname, 1, PATINDEX('%[ ,][js]r%', LName)-1),
  SuffixName = SUBSTRING(Lname, PATINDEX('%[ ,][js]r%', LName)+1, 100) + ISNULL(', ' + SuffixName,'')
FROM tblClients c
WHERE lname LIKE '%[ ,][js]r%'


--UPDATE c SET 
--  lname = RTRIM(lname)
--FROM tblClients c
--WHERE LEN(LName) <> LEN(RTRIM(LName))

UPDATE c SET 
  lname = LEFT(LName, LEN(LName)-1)
FROM tblClients c
WHERE RIGHT(LName,1) = ','

UPDATE c SET 
  suffixname = replace(suffixname, '.', '')
FROM tblClients c
WHERE suffixname is not null

UPDATE c SET 
  suffixname = LEFT(suffixname, LEN(suffixname)-1)
FROM tblClients c
WHERE RIGHT(suffixname ,1) = ','


-- that does pretty good... that gets over 4700 and leaves only 18 stragglers

/*
SELECT LName, SuffixName, *
  --SUBSTRING(Lname, 1, PATINDEX('% [js]r%', LName)-1),
  --SUBSTRING(Lname, PATINDEX('% [js]r%', LName)+1, 100) + ISNULL(', ' + SuffixName,'')
FROM tblClients c
WHERE lname LIKE '%,%'
AND SSN <> '000-00-0000'
*/


