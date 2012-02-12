--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 4/30/10 4:48p $

/* testing
declare @rc int
exec @rc = User_AdminOverride 'annoying'
select @rc

SELECT CONVERT(VARCHAR(max), HASHBYTES('SHA1', 'annoying_1'), 1)
SELECT [Value] FROM Settings WHERE [Name] = 'AdminPassword'
*/


/****** Object:  StoredProcedure [dbo].[User_AdminOverride]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'User_AdminOverride')
	exec('create PROCEDURE User_AdminOverride as select 1 as one')
GO
alter PROCEDURE [dbo].User_AdminOverride
@Password VARCHAR(100),
@Result varchar(10) = NULL OUT
AS BEGIN

set @Result = 'false'
IF (CONVERT(VARCHAR(MAX), HASHBYTES('SHA1', @Password), 1) = (SELECT [Value] FROM Settings WHERE [Name] = 'AdminPassword'))
  SET @Result = 'true'

END
GO

grant execute on User_AdminOverride to public
go


