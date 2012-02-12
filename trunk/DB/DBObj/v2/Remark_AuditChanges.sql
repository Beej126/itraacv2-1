--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:01p $

/****** Object:  StoredProcedure [dbo].[Remark_AuditChanges]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER On
GO

USE iTRAACv2
GO

/* testing
*/

if not exists(select 1 from sys.types where name = 'File_UDT')
  create TYPE File_UDT AS TABLE
  (
    FullPath varchar(900) PRIMARY KEY, 
    ModifiedDate datetime, 
    [Size] bigint
  )
go

if not exists(select 1 from sysobjects where name = 'Remark_AuditChanges')
	exec('CREATE PROCEDURE Remark_AuditChanges as select 1 as one')
GO
ALTER PROCEDURE [dbo].[Remark_AuditChanges]
AS BEGIN

SELECT 1

END
GO

grant execute on Remark_AuditChanges to public
go

