--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 4/30/10 4:48p $

/****** Object:  StoredProcedure [dbo].[TaxOffice_Init]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

/* testing:
TaxOffice_Init 1
*/

if not exists(select 1 from sysobjects where name = 'TaxOffice_Init')
	exec('create PROCEDURE TaxOffice_Init as select 1 as one')
GO
alter PROCEDURE [dbo].TaxOffice_Init
@TableNames VARCHAR(1000) = NULL OUT
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

SET @TableNames = 'TaxOffice,User'

EXEC TaxOffices_s @IncludeLocations=1
EXEC Users_s


END
GO

grant execute on TaxOffice_Init to public
go

