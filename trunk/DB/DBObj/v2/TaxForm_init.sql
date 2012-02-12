--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 4/30/10 4:48p $

/****** Object:  StoredProcedure [dbo].[TaxForm_init]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

/* testing:
TaxForm_init 1
*/

if not exists(select 1 from sysobjects where name = 'TaxForm_init')
	exec('create PROCEDURE TaxForm_init as select 1 as one')
GO
alter PROCEDURE [dbo].TaxForm_init
@TableNames VARCHAR(1000) = NULL OUT
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

EXEC TransactionTypes_s @TableNames = @TableNames OUT

SET @TableNames = @TableNames + ',RemarkType'
SELECT * FROM RemarkType

SET @TableNames = @TableNames + ',TaxFormPackageServiceFee'
SELECT * FROM TaxFormPackageServiceFee

END
GO

grant execute on TaxForm_init to public
go

