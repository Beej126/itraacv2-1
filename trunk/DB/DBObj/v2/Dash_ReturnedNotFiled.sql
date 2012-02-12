--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/****** Object:  StoredProcedure [dbo].[Dash_ReturnedNotFiled]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'Dash_ReturnedNotFiled')
	exec('create PROCEDURE Dash_ReturnedNotFiled as select 1 as one')
GO
alter PROCEDURE [dbo].[Dash_ReturnedNotFiled] 
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

SELECT * FROM _ReturnedNotFiled ORDER BY OfficeActive, OfficeTotalFormCount, FiscalYear

RETURN

-- DROP TABLE _ReturnedNotFiled

SELECT
  o.TaxOfficeName,
  o.Active AS OfficeActive,
  CASE WHEN DATEPART(Month, p.PurchaseDate) >= 10 THEN 1 else 0 end + DATEPART(YEAR, p.PurchaseDate) AS FiscalYear,
  COUNT(f.TaxFormID) AS FormCount,
  CONVERT(INT, NULL) AS OfficeTotalFormCount
INTO _ReturnedNotFiled
FROM iTRAAC.dbo.tblTaxForms f
JOIN iTRAAC.dbo.tblTaxFormPackages p ON p.PackageID = f.PackageID
JOIN iTRAAC.dbo.tblTaxOffices o ON o.TaxOfficeID BETWEEN f.TaxFormID AND f.TaxFormID + 10000000 - 1
WHERE f.StatusFlags & (POWER(2,1) /*returned*/ | POWER(2,2) /*filed*/) = POWER(2,1) 
GROUP BY
  o.TaxOfficeName,
  o.Active,
  CASE WHEN DATEPART(Month, p.PurchaseDate) >= 10 THEN 1 else 0 end + DATEPART(YEAR, p.PurchaseDate)

-- fillout OfficeTotalFormCount
UPDATE r SET r.OfficeTotalFormCount = t.OfficeTotalFormCount
FROM [_ReturnedNotFiled] r
JOIN (
  SELECT TaxOfficeName, SUM(FormCount) AS OfficeTotalFormCount
  FROM [_ReturnedNotFiled]
  GROUP BY OfficeActive, TaxOfficeName
) t ON t.TaxOfficeName = r.TaxOfficeName


-- fill out the missing years
INSERT [_ReturnedNotFiled]
        ( TaxOfficeName ,
          OfficeActive ,
          FiscalYear ,
          FormCount ,
          OfficeTotalFormCount
        )
SELECT TaxOfficeName, OfficeActive, FiscalYear, 0, OfficeTotalFormCount FROM 
(SELECT OfficeActive, TaxOfficeName, MAX(OfficeTotalFormCount) AS OfficeTotalFormCount FROM [_ReturnedNotFiled] GROUP BY OfficeActive, TaxOfficeName) t1
CROSS JOIN (SELECT DISTINCT FiscalYear FROM [_ReturnedNotFiled]) t2
WHERE NOT EXISTS(SELECT 1 FROM [_ReturnedNotFiled] WHERE TaxOfficeName = t1.TaxOfficeName AND FiscalYear = t2.FiscalYear)


END
GO

grant execute on Dash_ReturnedNotFiled to public
go

