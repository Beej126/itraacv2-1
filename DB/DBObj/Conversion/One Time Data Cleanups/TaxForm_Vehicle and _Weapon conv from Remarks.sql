
--- actually just move this logic into the TaxForm_s proc

/*
tblRemarks
remtype, title, remarks = value
3	Vehicle VIN
7	Vehicle Make
8	Vehicle Model
9	Vehicle Year

12	Weapon Serial Number
16	Weapon Model
17	Weapon Make
13	Weapon Caliber

select len(remarks), * from tblremarks where len(remarks) > 4 and RemType = 9 order by len(remarks) desc--in (3,7,8,9)
select len(remarks), * from tblremarks where len(remarks) > 50 and RemType in (12,13,16,17)
*/

use iTRAAC
go

insert iTRAACv2.dbo.TaxForm_Vehicle (TaxFormGUID, VIN, Make, Model, [Year])
SELECT TaxFormGUID, isnull([Vehicle VIN],''), isnull([Vehicle Make],''), isnull([Vehicle Model],''), isnull([Vehicle Year],'')
FROM (
  select f.RowGUID as TaxFormGUID, r.Title, r.Remarks
  FROM tblRemarks r
  join tblTaxForms f on f.TaxFormID = r.RowID
  where RemType in (3,7,8,9)
  and not exists(SELECT 1 FROM iTRAACv2.dbo.TaxForm_Vehicle v WHERE v.TaxFormGUID = f.RowGUID)
  and r.Remarks <> '!init!'
) r
pivot ( max(Remarks) for Title in ([Vehicle VIN], [Vehicle Make], [Vehicle Model], [Vehicle Year]) ) p

insert iTRAACv2.dbo.TaxForm_Weapon (TaxFormGUID, [Serial Number], Make, Model, Caliber)
SELECT TaxFormGUID, isnull([Weapon Serial Number],''), isnull([Weapon Make],''), isnull([Weapon Model],''), isnull([Weapon Caliber],'')
FROM (
  select f.RowGUID as TaxFormGUID, r.Title, r.Remarks
  FROM tblRemarks r
  join tblTaxForms f on f.TaxFormID = r.RowID
  where RemType in (12,13,16,17)
  and not exists(SELECT 1 FROM iTRAACv2.dbo.TaxForm_Weapon v WHERE v.TaxFormGUID = f.RowGUID)
  and r.Remarks <> '!init!'
) r
pivot ( max(Remarks) for Title in ([Weapon Serial Number], [Weapon Make], [Weapon Model], [Weapon Caliber]) ) p


