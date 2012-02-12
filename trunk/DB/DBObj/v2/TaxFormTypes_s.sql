
--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/****** Object:  StoredProcedure [dbo].[TaxFormTypes_s]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'TaxFormTypes_s')
	exec('create PROCEDURE TaxFormTypes_s as select 1 as one')
GO
alter PROCEDURE [dbo].[TaxFormTypes_s] 
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

SELECT
  FormTypeID,
  FormName,
  FormType,
  CONVERT(INT, MaxCkOut) AS MaxCkOut,
  RecordSource,
  CodeName,
  Passive,
  Active
FROM iTRAAC.dbo.tblTaxFormTypes WHERE Active = 1

END
GO

grant execute on TaxFormTypes_s to public
go

