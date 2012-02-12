use itraacv2
GO
/****** Object:  StoredProcedure [dbo].[User_Login]    Script Date: 10/17/2010 22:31:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
if not exists(select 1 from sysobjects where name = 'User_Login')
	exec('create PROCEDURE User_Login as select 1 as one')
GO
ALTER PROCEDURE [dbo].[User_Login]
@TaxOfficeID int,
@LoginName varchar(50)
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

SELECT 
  u.RowGUID as UserGUID,
  u.Fname + ' ' + u.LName AS Name,
  isnull(ua.IsAdmin, 0) as IsAdmin,
  isnull(ua.HasUnlockForm, 0) as HasUnlockForm,
  RTRIM(a.SigBlock) AS SigBlock
from iTRAAC.dbo.tblUsers u
left join UserAccess ua on ua.UserGUID = u.RowGUID
LEFT JOIN iTRAAC.dbo.tblTaxFormAgents a ON a.UserGUID = u.RowGUID AND a.TaxOfficeID = @TaxOfficeID
where u.LoginName = @LoginName

END
go

grant execute on [User_Login] to public
go
