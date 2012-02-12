--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 4/30/10 4:48p $

/****** Object:  StoredProcedure [dbo].[Ranks_s]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

/* testing:
Ranks_s 1
*/

if not exists(select 1 from sysobjects where name = 'Ranks_s')
	exec('create PROCEDURE Ranks_s as select 1 as one')
GO
alter PROCEDURE [dbo].Ranks_s
@IncludeLocations BIT = 0
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

SELECT [Rank] FROM [Rank] WHERE [Rank] <> 'Agency' ORDER BY SortOrder

END
GO

grant execute on Ranks_s to public
go

