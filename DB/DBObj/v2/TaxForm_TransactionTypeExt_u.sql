--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:01p $

/****** Object:  StoredProcedure [dbo].[TaxForm_TransactionTypeExt_u]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER On
GO

USE iTRAACv2
GO


/* testing
*/

if not exists(select 1 from sysobjects where name = 'TaxForm_TransactionTypeExt_u')
	exec('create PROCEDURE TaxForm_TransactionTypeExt_u as select 1 as one')
GO
--declaring the parms as varchar(max) so we don't forget to bump them up if we change the column lengths
--the field length metadata is provided to the client from the up front select so inputs will be properly constrained prior to getting here
ALTER PROCEDURE [dbo].[TaxForm_TransactionTypeExt_u]
@RowGUID UNIQUEIDENTIFIER,
@TaxFormGUID UNIQUEIDENTIFIER,
@VIN VARCHAR(max) = null,
@SerialNumber varchar(max) = null,
@Make varchar(max),
@Model varchar(max),
@Year VARCHAR(MAX) = null,
@Caliber varchar(max) = null
AS BEGIN

DECLARE @ExtendedFieldsCode varchar(100)
SELECT @ExtendedFieldsCode = t.ExtendedFieldsCode
FROM iTRAAC.dbo.tblTaxForms f
JOIN iTRAAC.dbo.tblTransactionTypes t ON t.TransTypeID = f.TransTypeID
WHERE f.RowGUID = @TaxFormGUID

IF (@ExtendedFieldsCode = 'Weapon')
BEGIN
  UPDATE TaxForm_Weapon SET
    [Serial Number] = @SerialNumber,
    Make = @Make, 
    Model = @Model,
    Caliber = @Caliber
  WHERE RowGUID = @RowGUID

  IF (@@ROWCOUNT = 0)
    INSERT TaxForm_Weapon (RowGUID, TaxFormGUID, [Serial Number], Make, Model, Caliber)
    VALUES (ISNULL(@RowGUID, NEWID()), @TaxFormGUID, @SerialNumber, @Make, @Model, @Caliber )
    
  DELETE TaxForm_Vehicle WHERE TaxFormGUID = @TaxFormGUID --make sure we don't leave lingering data in the other transaction type if somebody did that by accident
END

ELSE IF (@ExtendedFieldsCode = 'Vehicle')
BEGIN
  UPDATE TaxForm_Vehicle SET
    VIN = @VIN, 
    Make = @Make,
    Model = @Model,
    [Year] = @Year
  WHERE RowGUID = @RowGUID

  IF (@@ROWCOUNT = 0)
    INSERT TaxForm_Vehicle (RowGUID, TaxFormGUID, VIN, Make, Model, [Year])
    VALUES (ISNULL(@RowGUID, NEWID()), @TaxFormGUID, @VIN, @Make, @Model, @Year )
    
  DELETE TaxForm_Weapon WHERE TaxFormGUID = @TaxFormGUID --make sure we don't leave lingering data in the other transaction type if somebody did that by accident
END

ELSE
BEGIN
  SET @ExtendedFieldsCode = '[TaxForm_TransactionTypeExt_u] @ExtendedFieldsCode = ''' + @ExtendedFieldsCode + ''' not yet supported.'
  RAISERROR(@ExtendedFieldsCode, 16,1)
END

END
GO

grant execute on TaxForm_TransactionTypeExt_u to public
go

