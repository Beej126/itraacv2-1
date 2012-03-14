--$Author: Brent.anderson2 $
--$Date: 5/05/10 4:33p $
--$Modtime: 5/03/10 1:54p $

/* testing:
exec Customer_search @OrderNumber = 'NF1-__-__-00012'
exec Customer_search @LastName='camacho', @FirstName='jo'
exec Customer_search @CCode='m7886'
exec dbo.Customer_Search @SSN='%0999'

*/
/****** Object:  StoredProcedure [dbo].[Customer_Search]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'Customer_Search')
	exec('create PROCEDURE Customer_Search as select 1 as one')
GO
alter PROCEDURE [dbo].[Customer_Search] 
@LastName VARCHAR(1000) = NULL,
@FirstName VARCHAR(1000) = NULL,
@CCode VARCHAR(1000) = NULL,
@SSN VARCHAR(1000) = NULL,
@DodId VARCHAR(1000) = NULL,
@OrderNumber VARCHAR(1000) = NULL,
@TransactionTypeID VARCHAR(1000) = NULL
WITH EXECUTE AS OWNER
AS BEGIN

SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

DECLARE @TaxFormJoin VARCHAR(1000), @GroupBy VARCHAR(1000), @Notes VARCHAR(1000)
SELECT @TaxFormJoin = '', @GroupBy = '', @Notes = ''
IF (@OrderNumber IS NOT null) BEGIN
  SET @TaxFormJoin = 'JOIN iTRAAC.dbo.tblTaxFormPackages p on p.SponsorClientGUID = c.RowGUID '+
                     'JOIN iTRAAC.dbo.tblTaxForms f on f.PackageGUID = p.RowGUID '
  SET @GroupBy = 'group by c.SponsorGUID, c.LName, c.SuffixName, c.FName, c.CCode, c.SSN'
  SET @Notes = ', master.dbo.Concat(f.OrderNumber, '','') as Notes'
END


SET @LastName = iTRAAC.dbo.fn_DynParm('WHERE c.LName like ''?%''', @LastName)
SET @FirstName = iTRAAC.dbo.fn_DynParm('and c.FName like ''?%''', @FirstName)
SET @CCode = iTRAAC.dbo.fn_DynParm('WHERE c.CCode = ''?''', @CCode)
SET @SSN = iTRAAC.dbo.fn_DynParm('WHERE c.SSN like ''?''', @SSN)
SET @DodId = iTRAAC.dbo.fn_DynParm('WHERE c.DoDId = ''?''', @DodId)
SET @OrderNumber = iTRAAC.dbo.fn_DynParm(@TaxFormJoin + 'WHERE f.OrderNumber like ''?%''', @OrderNumber) -- *CRUCIAL* trailing % wildcard is specifically necessary to handle a,b,c postfixes for the Duplicate OrderNumber fix (see "Conversion\One Time Data Cleanups\TaxForm - Duplicate OrderNumber Fix.sql")
SET @TransactionTypeID = iTRAAC.dbo.fn_DynParm(@TaxFormJoin + 'WHERE f.TransTypeID = ?', @TransactionTypeID)

EXEC("
SELECT
  c.SponsorGUID,
  c.Lname + isnull(' ('+c.SuffixName+')','') +', ' + c.FName AS Name,
  c.CCode"
  +@Notes+"
FROM iTRAAC.dbo.tblClients c "
+@LastName
+@FirstName
+@CCode
+@SSN
+@DodId
+@OrderNumber
+@TransactionTypeID
+@GroupBy+"
order BY c.Lname + ', ' + c.FName"
)

END
GO

grant execute on Customer_Search to public
go

