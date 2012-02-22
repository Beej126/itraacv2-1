--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 4/30/10 4:48p $

/****** Object:  StoredProcedure [dbo].[TaxForm_Returns_Search]    Script Date: 06/19/2009 15:58:30 ******/

/* testing:
exec TaxForm_Returns_Search '444'
*/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'TaxForm_Returns_Search')
	exec('create PROCEDURE TaxForm_Returns_Search as select 1 as one')
GO
ALTER PROCEDURE [dbo].TaxForm_Returns_Search
@SequenceNumber VARCHAR(50)
AS BEGIN

IF (LEN(@SequenceNumber ) < 6)
  SET @SequenceNumber = RIGHT('00000'+@SequenceNumber, 5)

SELECT 
  f.OrderNumber AS [Form #],
  p.TaxOfficeId,
  s.[Status],
  c.LName + ' (' + c.CCode + '), ' + c.FName AS Name,
  f.RowGUID AS TaxFormGUID
FROM iTRAAC.dbo.tblTaxForms f
CROSS APPLY dbo.TaxForm_Status_f(f.StatusFlags, f.LocationCode, f.Incomplete, f.InitPrt215, f.InitPrtAbw) s
JOIN iTRAAC.dbo.tblTaxFormPackages p ON p.RowGUID = f.PackageGUID
JOIN iTRAAC.dbo.tblClients c ON c.RowGUID = p.SponsorClientGUID
WHERE f.CtrlNumber = @SequenceNumber

END
GO

grant execute on TaxForm_Returns_Search to public
go
