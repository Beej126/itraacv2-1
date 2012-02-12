--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:00p $

/* testing:
select dbo.BitToString(null)
select dbo.BitToString('blah')
*/

USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'BitToString')
	exec('create FUNCTION BitToString() returns varchar(5) begin return null end')
GO

ALTER function BitToString (@bit bit)
RETURNS VARCHAR(5) WITH SCHEMABINDING -- necessary to be used in OUTPUT clause
AS BEGIN
return(case when ISNULL(@bit, 0) > 0 THEN 'True' ELSE 'False' END)
END
GO

GRANT EXECUTE ON dbo.BitToString TO PUBLIC
go
