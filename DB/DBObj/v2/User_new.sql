use itraacv2
GO
/****** Object:  StoredProcedure [dbo].[User_new]    Script Date: 10/17/2010 22:31:09 ******/

-- EXEC [User_new] @TaxOfficeID = 10000001, @FirstName = 'Brenda', @LastName = 'Anderson', @Email = 'B@A.com', @DSNPhone = '1'
-- DELETE tblusers WHERE fname = 'brenda' AND lname = 'anderson'
-- delete tbltaxformagents where sigblock = 'anderson, brenda'

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
if not exists(select 1 from sysobjects where name = 'User_new')
	exec('create PROCEDURE User_new as select 1 as one')
GO
ALTER PROCEDURE [dbo].[User_new]
@TaxOfficeId int,
@FirstName VARCHAR(50),
@LastName VARCHAR(50),
@Email VARCHAR(100),
@DSNPhone VARCHAR(20)
AS BEGIN

SET XACT_ABORT ON;
BEGIN TRY

DECLARE @t TABLE(UserGUID uniqueidentifier)

DECLARE @i INT, @LoginName VARCHAR(15), @ErrorMessage VARCHAR(MAX)

-- take the lastname + start with first char of first name...
-- keep taking more first name letters until we establish a unique login name that's not already out there
WHILE (1=1) BEGIN
  SET @LoginName = @LastName + LEFT(@FirstName, 1) + ISNULL(CONVERT(VARCHAR, @i), '')
  IF NOT EXISTS(SELECT 1 FROM iTRAAC.dbo.tblUsers WHERE LoginName = @LoginName) BREAK
  SET @i = ISNULL(@i,1) + 1
  IF (@i >10) BEGIN RAISERROR('Couldn''t determine a unique login name in sproc: User_new', 16, 1) RETURN END
END

BEGIN TRAN

INSERT iTRAAC.dbo.tblUsers ( FName, LName, Email, DSNPhone, Active, LoginName )
OUTPUT INSERTED.RowGUID INTO @t
VALUES ( @FirstName, @LastName, @Email, @DSNPhone, 1, @LoginName)

DECLARE @UserID INT, @UserGUID UNIQUEIDENTIFIER 
SELECT @UserID = u.UserID, @UserGUID = t.UserGUID FROM iTRAAC.dbo.tblUsers u JOIN @t t ON t.UserGUID = u.RowGUID

INSERT iTRAAC.dbo.tblTaxFormAgents ( TaxOfficeID, UserID, Active, UserGUID, SigBlock )
VALUES (@TaxOfficeId, @UserID, 1, @UserGUID, @LastName + ', ' + @FirstName )

COMMIT TRAN

EXEC Users_s @UserGUID=@UserGUID

END TRY
BEGIN CATCH
  SET @ErrorMessage = ERROR_MESSAGE()
  IF (XACT_STATE() <> 0) ROLLBACK TRANSACTION;
  RAISERROR(@ErrorMessage, 16, 1)
END CATCH 

END
go

grant execute on [User_new] to public
go
