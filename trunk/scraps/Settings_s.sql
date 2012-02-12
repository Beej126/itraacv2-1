--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/****** Object:  StoredProcedure [dbo].[Settings_s]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'Settings_s')
	exec('create PROCEDURE Settings_s as select 1 as one')
GO
alter PROCEDURE [dbo].[Settings_s] 
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

WITH Settings_Pivot (OfficeCode, AdminPassword)
AS (
  select top 1 OfficeCode, AdminPassword -- we only want one big row representing all the values 
  from (SELECT OfficeCode, AdminPassword
  FROM Settings
  pivot ( max([Value]) for [Name] in (AdminPassword) ) p) t
  order by OfficeCode desc --sort office specific settings to the top of 'global' settings represented by OfficeCode = NULL
)

SELECT o.OfficeCode, p.AdminPassword
FROM iTRAAC.dbo.tblControlLocal l
join iTRAAC.dbo.tblTaxOffices o on o.TaxOfficeID = l.TaxOfficeID
join Settings_Pivot p on p.OfficeCode is null or p.OfficeCode = o.OfficeCode

END
GO

grant execute on Settings_s to public
go

