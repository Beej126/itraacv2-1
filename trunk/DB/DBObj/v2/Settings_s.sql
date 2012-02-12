
--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/****** Object:  StoredProcedure [dbo].[Settings_s]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

-- Settings_s 'HD'

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'Settings_s')
	exec('create PROCEDURE Settings_s as select 1 as one')
GO
alter PROCEDURE [dbo].[Settings_s]
@TaxOfficeCode VARCHAR(2),
@TaxOfficeId INT = NULL out
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

SELECT @TaxOfficeId = TaxOfficeId FROM iTRAAC.dbo.tblTaxOffices WHERE OfficeCode = @TaxOfficeCode

-- global
SELECT
  [Name], 
  -- if the string begins with "0x", treat it as a hex representation of bytes, so convert it to binary and then back to the string equivalent to return to the client
  CASE WHEN SUBSTRING([Value], 1, 2) = '0x' THEN CONVERT(varchar, CONVERT(VARBINARY(MAX), REPLACE([Value],' ', ''), 1))
  ELSE [Value] END as [Value] 
FROM Settings WHERE TaxOfficeId = 1

-- local
SELECT [Name], [Value] FROM Settings WHERE TaxOfficeId = @TaxOfficeId

END
GO

grant execute on Settings_s to public
go

