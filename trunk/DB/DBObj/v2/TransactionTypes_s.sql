--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 3/18/10 4:54p $

/* testing:
declare @TableNames varchar(1000)
exec TransactionTypes_s @GUID=null, @TableNames=@TableNames out
select @TableNames as TableNames
*/

/****** Object:  StoredProcedure [dbo].[TransactionTypes_s]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'TransactionTypes_s')
	exec('create PROCEDURE TransactionTypes_s as select 1 as one')
GO
alter PROCEDURE [dbo].TransactionTypes_s
@TableNames varchar(1000) = null OUT
WITH EXECUTE AS OWNER
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

SET @TableNames = 'TransactionType,TaxForm'

-- Table[0] - TransactionType
SELECT
  Active,
  TransTypeID AS TransactionTypeID,
  TransactionType,
  ConfirmationText,
  CASE WHEN Active = 1 THEN 
    CASE WHEN ConfirmationText IS NOT NULL THEN 2 ELSE 1 END
    ELSE 0 END as TriState, -- 0 = deactive, 1 = active, 2 = active and 'special'
  ExtendedFieldsCode
FROM iTRAAC.dbo.tblTransactionTypes
ORDER BY Active DESC, SortOrder


-- this proc gets called way up front in app initialization in order to init the vehicle and weapons extension tables and established their parent-child relationship with TaxForm
-- so we also need to throw back and empty TaxForm row to be the parent table definition for the relationship to the extension tables
-- Table[1] - TaxForm
EXEC TaxForm_s @GUID = NULL


DECLARE curs CURSOR LOCAL FAST_FORWARD FOR
  SELECT ExtendedFieldsCode FROM iTRAAC.dbo.tblTransactionTypes WHERE ExtendedFieldsCode IS NOT NULL
  
DECLARE @ExtendedFieldsTable VARCHAR(50)

OPEN curs
WHILE (1=1) BEGIN
  FETCH NEXT FROM curs INTO @ExtendedFieldsTable
  IF (@@FETCH_STATUS <> 0) BREAK
  
  SET @ExtendedFieldsTable = 'TaxForm_' + @ExtendedFieldsTable
  SET @TableNames = @TableNames + ',' + @ExtendedFieldsTable
  
  EXEC('select * from ' + @ExtendedFieldsTable + ' where 0=1')
END


DEALLOCATE curs

END
GO

grant execute on TransactionTypes_s to public
go

