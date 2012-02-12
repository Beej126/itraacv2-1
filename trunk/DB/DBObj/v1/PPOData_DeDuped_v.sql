--$Author: Brent.anderson2 $
--$Date: 2/17/10 2:23p $
--$Modtime: 1/04/10 11:08a $

-- for some crazy reason there is often two duplicate rows in the tblPPOData for the same TaxFormID
-- so i created this view which groups those into a single row,
-- and then declared a clustered index over this view so that it could be joined to and perform as well as possible
-- an indexed view is essentially another physical copy of the data in the established sort order that the database automatically keeps in sync with the source data for us

/****** Object:  View [dbo].[PPOData_DeDuped_v]    Script Date: 06/22/2009 08:23:48 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER On
GO

/****** Object:  Index [ix_main]    Script Date: 12/18/2009 10:46:38 ******/
/*
drop index PPOData_DeDuped_v.ix_main

CREATE UNIQUE CLUSTERED INDEX [ix_main] ON [dbo].[PPOData_DeDuped_v] 
(
	TaxFormID,
	CurrencyUsed,
	TotalCost --,
	--CheckNumber 
)
GO
*/

if not exists(select 1 from sysobjects where name = 'PPOData_DeDuped_v')
	exec('create VIEW PPOData_DeDuped_v as select 1 as one')
GO
alter VIEW [dbo].[PPOData_DeDuped_v]
WITH SCHEMABINDING -- required for creating indexed view
AS

SELECT
  COUNT_BIG(*) AS [count], -- required for creating indexed view
  TaxFormID,
  TotalCost,
  CurrencyUsed--,
  --CheckNumber
FROM dbo.tblPPOData (NOLOCK)
GROUP BY 
  TaxFormID,
  TotalCost,
  CurrencyUsed--,
  --CheckNumber
GO

grant select on PPOData_DeDuped_v to public
go

