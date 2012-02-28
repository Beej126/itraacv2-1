USE [iTRAAC_old]
GO

/****** Object:  StoredProcedure [Report_Discovery]    Script Date: 02/27/2012 11:23:36 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [Report_Discovery]
AS BEGIN

SELECT 
  REPLACE(REPLACE(specific_name, '_rpt', ''), '_',' ') AS ReportName,
  specific_name AS ProcName
FROM information_schema.routines WHERE specific_name LIKE '%_rpt'

END 

GO


