SELECT * FROM tblSponsors WHERE AddressLine1 LIKE '%[0-9]box%'
UPDATE tblSponsors SET AddressLine1 = REPLACE(AddressLine1, '  ', ' ') -- **like 4 times at least***
UPDATE tblSponsors SET AddressLine1 = REPLACE(AddressLine1, 'bx#', ' BOX ') 
UPDATE tblSponsors SET AddressLine1 = REPLACE(AddressLine1, 'bx #', ' BOX ') WHERE AddressLine1 LIKE '%bx #%'
UPDATE tblSponsors SET AddressLine1 = REPLACE(AddressLine1, 'bx', ' BOX ') 
UPDATE tblSponsors SET AddressLine1 = REPLACE(AddressLine1, '  ', ' ') -- **like 4 times at least***

UPDATE tblSponsors SET AddressLine2 = REPLACE(AddressLine2, '  ', ' ') -- **like 4 times at least***
UPDATE tblSponsors SET AddressLine2 = REPLACE(AddressLine2, 'bx#', ' BOX ') 
UPDATE tblSponsors SET AddressLine2 = REPLACE(AddressLine2, 'bx #', ' BOX ') WHERE AddressLine2 LIKE '%bx #%'
UPDATE tblSponsors SET AddressLine2 = REPLACE(AddressLine2, 'bx', ' BOX ') 
UPDATE tblSponsors SET AddressLine2 = REPLACE(AddressLine2, '  ', ' ') -- **like 4 times at least***
