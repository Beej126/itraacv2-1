--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 4/30/10 4:48p $

/****** Object:  StoredProcedure [dbo].[Remark_d]    Script Date: 06/19/2009 15:58:30 ******/

/* testing:
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'Remark_d')
	exec('create PROCEDURE Remark_d as select 1 as one')
GO
alter PROCEDURE [dbo].Remark_d
@TaxOfficeId INT,
@UserGUID UNIQUEIDENTIFIER,
@RemarkGUID UNIQUEIDENTIFIER,
@Reason VARCHAR(100)
AS BEGIN

DECLARE @AgentId int, @AgentGUID UNIQUEIDENTIFIER
EXEC UserGUID_To_AgentID @UserGUID = @UserGUID, @TaxOfficeId = @TaxOfficeId, @AgentId = @AgentId OUT, @AgentGUID = @AgentGUID OUT

UPDATE iTRAAC.dbo.tblRemarks SET
  DeleteReason = @Reason,
  LastUpdate = GETDATE(),
  LastAgentGUID = @AgentGUID
WHERE RowGUID = @RemarkGUID

END
GO

grant execute on Remark_d to public
go
