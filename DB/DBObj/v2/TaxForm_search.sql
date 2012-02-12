--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 4/30/10 4:48p $

/****** Object:  StoredProcedure [dbo].[TaxForm_search]    Script Date: 06/19/2009 15:58:30 ******/

/* testing:
exec TaxForm_search '444'
*/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'TaxForm_search')
	exec('create PROCEDURE TaxForm_search as select 1 as one')
GO
alter PROCEDURE [dbo].TaxForm_search
@SequenceNumber VARCHAR(50)
AS BEGIN

IF (LEN(@SequenceNumber ) < 6)
  SET @SequenceNumber = RIGHT('00000'+@SequenceNumber, 5)

SELECT 
  f.OrderNumber AS [Form #],
  CASE
    WHEN f.LocationCode = 'Lost' THEN 'Lost'
    WHEN f.Incomplete = 1 THEN 'Incomplete'
    WHEN f.StatusFlags & POWER(2, 5) = POWER(2, 5) THEN 'Voided'
    WHEN f.StatusFlags & POWER(2, 2) = POWER(2, 2) THEN 'Filed'
    WHEN f.StatusFlags & POWER(2, 1) = POWER(2, 1) THEN 'Returned'
    ELSE 'Unreturned' END AS [Status],
  c.LName + ' (' + c.CCode + '), ' + c.FName AS Name,
  f.RowGUID AS TaxFormGUID
FROM iTRAAC.dbo.tblTaxForms f
JOIN iTRAAC.dbo.tblTaxFormPackages p ON p.RowGUID = f.PackageGUID
JOIN iTRAAC.dbo.tblClients c ON c.RowGUID = p.SponsorClientGUID
WHERE f.CtrlNumber = @SequenceNumber

END
GO

grant execute on TaxForm_search to public
go
