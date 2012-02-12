--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/15/09 4:27p $

/****** Object:  View [dbo].[Agent_v]    Script Date: 06/22/2009 08:23:48 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'Agent_v')
	exec('create VIEW Agent_v as select 1 as one')
GO
alter VIEW [dbo].[Agent_v]
AS

SELECT
  a.AgentID,
  a.RowGUID,
  u.FName + ' ' + u.LName + ' (' + o.TaxOfficeName + ')' AS ShortDescription
from iTRAAC.dbo.tblTaxFormAgents a
JOIN iTRAAC.dbo.tblUsers u ON u.UserID = a.UserID
JOIN iTRAAC.dbo.tblTaxOffices o ON a.TaxOfficeID = o.TaxOfficeID

GO

grant select on Agent_v to public
go

