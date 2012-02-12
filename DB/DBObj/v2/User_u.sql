use itraacv2
GO
/****** Object:  StoredProcedure [dbo].[User_u]    Script Date: 10/17/2010 22:31:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
if not exists(select 1 from sysobjects where name = 'User_u')
	exec('create PROCEDURE User_u as select 1 as one')
GO
ALTER PROCEDURE [dbo].[User_u]
@UserGUID UNIQUEIDENTIFIER,
@TaxOfficeId int,
@FirstName VARCHAR(50),
@LastName VARCHAR(50),
@DSNPhone VARCHAR(20),
@Email varchar(100),
@SigBlock VARCHAR(50),
@Active bit
AS BEGIN

-- remember, in V2, the Agent table should generally be considered as "UserOfficeAssoc"

UPDATE iTRAAC.dbo.tblTaxFormAgents SET
  SigBlock = @SigBlock,
  Active = @Active
WHERE TaxOfficeID = @TaxOfficeID and UserGUID = @UserGUID

IF (@@RowCount = 0) BEGIN
  DECLARE @UserID INT
  SELECT @UserID = UserID FROM iTRAAC.dbo.tblUsers WHERE RowGUID = @UserGUID
  INSERT iTRAAC.dbo.tblTaxFormAgents ( TaxOfficeID, UserID, SigBlock, Active, UserGUID )
  VALUES (@TaxOfficeID, @UserID, @SigBlock, @Active, @UserGUID)
END

--if we're deactivating an agent, only deactivate the User if we're deactivating the Agent from the very last office they belong to
IF (@Active = 0) AND EXISTS(SELECT 1 FROM iTRAAC.dbo.tblTaxFormAgents WHERE UserGUID = @UserGUID AND ACTIVE = 1 AND TaxOfficeId <> @TaxOfficeId)
  SET @Active = 1

UPDATE iTRAAC.dbo.tblUsers SET
  FName = @FirstName,
  LName = @LastName,
  DSNPhone = @DSNPhone,
  Email = @Email,
  Active = @Active
WHERE RowGUID = @UserGUID


END
go

grant execute on [User_u] to public
go
