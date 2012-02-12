--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 4/30/10 4:48p $

/****** Object:  StoredProcedure [dbo].[TaxForm_ReturnFile]    Script Date: 06/19/2009 15:58:30 ******/

/* testing:

declare
  @Success bit,
  @Message varchar(500),
  @CustomerName varchar(50),
  @StatusFlags int
exec TaxForm_ReturnFile @OrderNumber = 'blah', @Success=@Success out, @Message=@Message out, @CustomerName=@CustomerName out
select @Success as Success, @Message as Message, @CustomerName as Customer

UPDATE tblTaxForms SET RetAgentID = NULL, ReturnedDate = NULL, ReturnUserGUID = NULL, StatusFlags = StatusFlags & ~POWER(2,1) & ~POWER(2,2), LocationCode = null WHERE OrderNumber = 'NF1-MA-05-09381'
sp_procsearch 'TaxForm_CheckStatus'

*/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'TaxForm_ReturnFile')
	exec('create PROCEDURE TaxForm_ReturnFile as select 1 as one')
GO
alter PROCEDURE [dbo].TaxForm_ReturnFile
@TaxFormGUID UNIQUEIDENTIFIER,
@UserGUID UNIQUEIDENTIFIER,
@TaxOfficeCode VARCHAR(4),
@TaxOfficeId INT,
@File BIT = 0, --0 = Return only, 1 = Return & File, NULL assumes 0,
@TableNames varchar(MAX) = NULL OUT
AS BEGIN

SET @File = ISNULL(@File, 0) -- just defensive coding

DECLARE @Error VARCHAR(MAX)

SELECT @Error = 'Already ' + CASE
  WHEN StatusFlags & POWER(2,5) > 0 THEN 'VOIDED'
  WHEN @File = 0 AND StatusFlags & POWER(2,1) > 0 THEN 'RETURNED' + isnull('  on ' + convert(varchar, ReturnedDate, 106), '') 
  WHEN @File = 1 AND StatusFlags & POWER(2,2) > 0 THEN 'FILED' + isnull(' on ' + convert(varchar, FiledDate, 106), '')
END
FROM iTRAAC.dbo.tblTaxForms
WHERE RowGUID = @TaxFormGUID

IF (@Error IS NOT NULL) BEGIN RAISERROR(@Error, 16,1) RETURN(-1) END

DECLARE @AgentId int
EXEC UserGUID_To_AgentID @UserGUID = @UserGUID, @TaxOfficeId = @TaxOfficeId, @AgentId = @AgentId OUT

update iTRAAC.dbo.tblTaxForms set 
  StatusFlags = StatusFlags | POWER(2,1) | @FILE * POWER(2,2),
  ReturnedDate    = ISNULL(ReturnedDate, GETDATE()),
  ReturnUserGUID  = ISNULL(ReturnUserGUID, @UserGUID),
  RetAgentID      = ISNULL(RetAgentID, @AgentId),
  FiledDate       = CASE WHEN @File = 1 THEN GETDATE() ELSE FiledDate END,
  FileUserGUID    = CASE WHEN @File = 1 THEN @UserGUID ELSE FileUserGUID END,
  FileAgentId     = CASE WHEN @File = 1 THEN @AgentId ELSE FileAgentId END,
  LocationCode = @TaxOfficeCode
where RowGUID = @TaxFormGUID

-- return _only_ the updated column values to be merged into the existing TaxForm record cached on the client
-- *NOTE* the column names returned here *must* line up with the columns returned from TaxForm_s
SET @TableNames = 'TaxForm'	
SELECT
  f.RowGUID,
  f.StatusFlags,
  f.ReturnedDate,
  f.ReturnUserGUID,
  ru.FName + ' ' + ru.LName AS ReturnedBy,
  f.FiledDate,
  f.FileUserGUID,
  fu.FName + ' ' + fu.LName AS FiledBy,
  f.LocationCode
FROM iTRAAC.dbo.tblTaxForms f
JOIN iTRAAC.dbo.tblUsers ru ON ru.RowGUID = f.ReturnUserGUID
left JOIN iTRAAC.dbo.tblUsers fu ON fu.RowGUID = f.FileUserGUID
where f.RowGUID = @TaxFormGUID

      
END
GO

grant execute on TaxForm_ReturnFile to public
go
