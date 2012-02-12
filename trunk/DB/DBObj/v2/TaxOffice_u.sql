use itraacv2
GO
/****** Object:  StoredProcedure [dbo].[TaxOffice_u]    Script Date: 10/17/2010 22:31:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
if not exists(select 1 from sysobjects where name = 'TaxOffice_u')
	exec('create PROCEDURE TaxOffice_u as select 1 as one')
GO
ALTER PROCEDURE [dbo].[TaxOffice_u]
@TaxOfficeId int,
@OfficeHours VARCHAR(100),
@AgencyLine1 VARCHAR(100),
@AgencyLine2 VARCHAR(100),
@AgencyLine3 VARCHAR(100),
@AgencyLine4 VARCHAR(100),
@Phone VARCHAR(20),
@POC_UserGUID uniqueidentifier
AS BEGIN

UPDATE iTRAAC.dbo.tblTaxOffices SET 
  OfficeHours = @OfficeHours,
  AgencyLine1 = @AgencyLine1,
  AgencyLine2 = @AgencyLine2,
  AgencyLine3 = @AgencyLine3,
  FormFootNoteLine1 = @AgencyLine4,
  Phone = @Phone,
  POC_UserGUID = @POC_UserGUID
WHERE TaxOfficeId = @TaxOfficeId

END
go

grant execute on [TaxOffice_u] to public
go
