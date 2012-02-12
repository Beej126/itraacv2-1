--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/*
EXEC UserAccess_ByLogin @LoginName = 'brent.anderson2'
*/

/****** Object:  StoredProcedure [dbo].[UserAccess_ByLogin]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'UserAccess_ByLogin')
	exec('create PROCEDURE UserAccess_ByLogin as select 1 as one')
GO
alter PROCEDURE [dbo].[UserAccess_ByLogin]
@LoginName varchar(50)
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

SELECT 
  ua.*
from iTRAAC.dbo.tblUsers u
join UserAccess ua on ua.UserGUID = u.RowGUID
where u.LoginName = @LoginName

END
GO

grant execute on UserAccess_ByLogin to public
go

