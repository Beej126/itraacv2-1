
--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/****** Object:  StoredProcedure [dbo].[Settings_u]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER On
GO

/* testing:
DECLARE @xml XML
SET @xml = '<Settings><s n="name1" v="val1"/><s n="name1" v="val1"/></Settings>'
SELECT
  t.setting.value('@n', 'varchar(100)'),
  t.setting.value('@v', 'varchar(max)')
FROM @xml.nodes('Settings/s') AS t(setting)

select * from Settings
truncate table Settings

declare @p1 xml
set @p1=convert(xml,N'<Settings><s n="MaxClass1FormsCount" v="10"/><s n="NFPrinter" v="Microsoft XPS Document Writer"/><s n="AbwPrinter" v="Fax"/></Settings>')
exec dbo.Settings_u @TaxOfficeId='GG', @xml=@p1

*/

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'Settings_u')
	exec('create PROCEDURE Settings_u as select 1 as one')
GO
alter PROCEDURE [dbo].[Settings_u]
@TaxOfficeId int,
@Settings NameValue READONLY
AS BEGIN

IF @TaxOfficeId IS NULL OR @TaxOfficeId NOT IN (SELECT TaxOfficeId FROM iTRAAC.dbo.tblTaxOffices WHERE TaxOfficeId <> 1) BEGIN
  RAISERROR('Settings_u: Invalid @TaxOfficeId supplied ("%s")', 16, 1, @TaxOfficeId)
  RETURN
END

UPDATE l SET l.[Value] = s.[Value]
FROM Settings l
JOIN @Settings s ON s.[Name] = l.[Name]
WHERE l.TaxOfficeId = @TaxOfficeId

INSERT Settings (TaxOfficeId, Name, Value)
SELECT @TaxOfficeId, [Name], [Value]
FROM @Settings s
WHERE NOT EXISTS(SELECT 1 FROM Settings l WHERE l.[Name] = s.[Name] AND l.TaxOfficeId = @TaxOfficeId)

SELECT * FROM @Settings

END
GO

grant execute on Settings_u to public
go

