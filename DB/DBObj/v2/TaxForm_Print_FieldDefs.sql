--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:01p $

/****** Object:  StoredProcedure [dbo].[TaxForm_Print_FieldDefs]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

/* testing
*/

if not exists(select 1 from sysobjects where name = 'TaxForm_Print_FieldDefs')
	exec('create PROCEDURE TaxForm_Print_FieldDefs as select 1 as one')
GO
alter PROCEDURE [dbo].[TaxForm_Print_FieldDefs]
@FormTypeId int
AS BEGIN

SELECT
  REPLICATE(
    LEFT(RTRIM(ff.FieldName) +
      REPLACE(
        ' [R' + CONVERT(VARCHAR, ff.StartRow) +
        'C' + CONVERT(VARCHAR, ff.StartCol) +
        'L' + CONVERT(VARCHAR, ff.MaxLength) + '] ', '.0', '') +
      REPLICATE('#', ff.MaxLength), 
    ff.MaxLength)+' '
  , ABS(ff.MaxRows)) AS Data, 
  RTRIM(ff.FieldName) AS FieldName,
  CONVERT(INT, ff.StartRow) AS Row,
  CONVERT(INT, FLOOR(ff.StartCol)) AS Col,
  ff.MaxLength, 
  ff.MaxRows
FROM iTRAAC.dbo.tblFormFields ff
JOIN iTRAAC.dbo.tblTaxFormTypes ft ON ft.FormTypeID = ff.FormTypeID
WHERE ff.FormTypeID = @FormTypeId


END
GO

grant execute on TaxForm_Print_FieldDefs to public
go

