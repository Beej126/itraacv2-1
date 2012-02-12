-- wound up going with SponsorGUID rather than SponsorClientGUID on everything
-- just saving this logic in case it comes back up
/*
UPDATE c SET
  c.SponsorClientGUID = t.RowGUID,
  c.StatusFlags = (c.StatusFlags | 1) & ~POWER(2,8)
FROM tblClients c
OUTER APPLY (
  SELECT TOP 1 s.RowGUID
  FROM tblClients s
  WHERE s.SponsorGUID = c.SponsorGUID
  ORDER BY
    s.ACTIVE DESC,
    CASE WHEN s.StatusFlags & 1 = 1 THEN 0 ELSE 1 END
) t
WHERE c.SponsorClientGUID IS NULL




UPDATE p SET p.SponsorClientGUID = c.SponsorClientGUID
FROM tblTaxFormPackages p
JOIN tblClients c ON c.RowGUID = p.ClientGUID
*/