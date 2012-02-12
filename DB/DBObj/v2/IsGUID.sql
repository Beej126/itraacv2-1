--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:00p $

/* stolen from here: http://weblogs.sqlteam.com/mladenp/archive/2005/08/16/7545.aspx#3094

SELECT replicate('[0-9A-Fa-f]',8) + '-' +
replicate('[0-9A-Fa-f]',4) + '-' +
replicate('[0-9A-Fa-f]',4) + '-' +
replicate('[0-9A-Fa-f]',4) + '-' +
replicate('[0-9A-Fa-f]',12) 
*/  

/* testing:
select dbo.IsGUID(newid())
select dbo.IsGUID('blah')
*/

USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'IsGUID')
	exec('create FUNCTION IsGUID() returns int begin return 0 end')
GO

ALTER function IsGuid (@string varchar(38))
returns BIT AS
BEGIN
return(case when @string LIKE '[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]-[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]-[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]-[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]-[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'
  THEN 1 ELSE 0 END)
END
GO

GRANT EXECUTE ON dbo.IsGUID TO PUBLIC
go
