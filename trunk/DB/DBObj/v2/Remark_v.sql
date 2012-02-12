--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/15/09 4:27p $

/****** Object:  View [dbo].[Remark_v]    Script Date: 06/22/2009 08:23:48 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'Remark_v')
	exec('create VIEW Remark_v as select 1 as one')
GO
alter VIEW [dbo].[Remark_v]
AS

SELECT 
  r.TableID,
  r.FKRowGUID,
  r.RowGUID,
  convert(bit, r.StatusFlags & POWER(2,12)) AS Alert, --nugget: convert to bit turns anything >0 to 1, handy, eliminates the need for a "case" clause
  ISNULL(r.LastUpdate, r.CreateDate) AS LastUpdate,
  ISNULL(CASE WHEN r.RemType < 0 THEN 'UN-' ELSE '' END + t.Title, r.Title) AS Title, -- negative RemTypes are the "turning off" version (e.g. UN-suspended, UN-PCS'ed)
  r.Remarks,
  r.RemType AS RemarkTypeId,
  r.CreateDate,
  r.CreateAgentGUID,
  a.TaxOfficeID AS CreateTaxOfficeId,
  r.LastAgentGUID,
  t.CategoryId,
  r.DeleteReason,
  r.AlertResolved
FROM iTRAAC.dbo.tblRemarks r
LEFT JOIN RemarkType t ON t.RemarkTypeId = ABS(r.RemType) -- negative RemTypes are the "turning off" version (e.g. UN-suspended, UN-PCS'ed)
JOIN iTRAAC.dbo.tblTaxFormAgents a ON a.RowGUID = r.CreateAgentGUID

GO

grant select on Remark_v to public
go

