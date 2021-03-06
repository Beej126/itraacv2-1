USE [iTRAAC]
GO

/*
exec Agent_Sales_rpt @TaxOfficeID = 280000001, @BeginDate = '2/14/2012', @EndDate = '2/14/2012'
select * from tbltaxoffices
*/

/****** Object:  StoredProcedure [Agent_Sales_rpt]    Script Date: 02/27/2012 12:05:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
IF OBJECT_ID('Agent_Sales_rpt') IS NULL EXEC('create proc Agent_Sales_rpt as select 1')
go

ALTER PROCEDURE [Agent_Sales_rpt]
@TaxOfficeID INT,
@BeginDate DATETIME = NULL OUT,
@EndDate DATETIME = NULL OUT
AS BEGIN

IF (@BeginDate IS null) SET @BeginDate = CONVERT(VARCHAR, GETDATE(), 1) 
IF (@EndDate IS null) SET @EndDate = CONVERT(VARCHAR, GETDATE(), 1) 

--SELECT CONVERT(VARCHAR, CONVERT(DATETIME, CONVERT(VARCHAR, GETDATE(), 1)), 8) 
--SELECT CONVERT(DATETIME, CONVERT(VARCHAR, GETDATE(), 1)) + '23:59'
IF (CONVERT(VARCHAR, @EndDate, 8) = '00:00:00')
  SET @EndDate = @EndDate + '23:59'

--PRINT '@BeginDate: '+ CONVERT(VARCHAR, @BeginDate) + ', @EndDate: ' + CONVERT(VARCHAR, @EndDate)

SELECT
  CONVERT(VARCHAR, ttfp.PurchaseDate, 106) AS [Date], 
  tu.FName + ' ' + tu.LName AS Agent, 
  CONVERT(DECIMAL(9,2), SUM(ttfp.ServiceFee)) AS [Total Fees]
FROM tblTaxFormAgents ttfa 
JOIN tblUsers tu ON ttfa.UserID = tu.UserID
JOIN tblTaxFormPackages ttfp ON ttfa.AgentID = ttfp.AgentID
WHERE ttfp.PurchaseDate BETWEEN @BeginDate AND @EndDate
AND ttfa.TaxOfficeID = @TaxOfficeID
GROUP BY
  CONVERT(VARCHAR, ttfp.PurchaseDate, 106),
  tu.FName + ' ' + tu.LName
ORDER BY 
  CONVERT(VARCHAR, ttfp.PurchaseDate, 106),
  tu.FName + ' ' + tu.LName

END 
