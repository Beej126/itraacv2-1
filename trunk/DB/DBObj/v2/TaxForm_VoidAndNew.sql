
--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/****** Object:  StoredProcedure [dbo].[TaxForm_VoidAndNew]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'TaxForm_VoidAndNew')
	exec('create PROCEDURE TaxForm_VoidAndNew as select 1 as one')
GO
alter PROCEDURE [dbo].[TaxForm_VoidAndNew]
@TaxOfficeId int,
@TaxOfficeCode varchar(10),
@UserGUID UNIQUEIDENTIFIER,
@TableNames VARCHAR(MAX) = NULL OUT,

@VoidTaxFormGUID UNIQUEIDENTIFIER,
@SponsorGUID UNIQUEIDENTIFIER = NULL OUT,
@NewTaxFormGUID UNIQUEIDENTIFIER = NULL OUT,
@ServiceFee DECIMAL(9,2) = NULL OUT
AS BEGIN

/*
the basic business logic here is that it seems like someone paying $2 should get the "clock reset" on the expiration date
vs simply reprinting the existing one with the same expiration date
hence we're generating a new form and voiding the old one
*/

SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

DECLARE
  @OldFormRemark VARCHAR(900),
  @NewFormRemark VARCHAR(900),
  @NewPackageGUID UNIQUEIDENTIFIER,
  @NewOrderNumber VARCHAR(20)
  
-- pull the current sponsor/household of the sponsoring individual on this form to be voided
-- yes the sponsor could have changed over time through divorce/remarriage, etc... it makes for interesting scenarios
-- the way to remember the subtlety is that packages are tied to an individual (i.e. client records)
-- and those clients can migrate between households (i.e. sponsor records)
SELECT
  @SponsorGUID = c.SponsorGUID,
  @NewFormRemark = 'Replacement for Voided PO#: ' + OrderNumber 
FROM iTRAAC.dbo.tblTaxForms f
JOIN iTRAAC.dbo.tblTaxFormPackages p ON p.RowGUID = f.PackageGUID
JOIN iTRAAC.dbo.tblClients c ON c.RowGUID = p.SponsorClientGUID
WHERE f.RowGUID = @VoidTaxFormGUID

-- create the new replacement form
EXEC TaxFormPackage_new @TaxOfficeId = @TaxOfficeId,
    @TaxOfficeCode = @TaxOfficeCode,
    @UserGUID = @UserGUID,
    --@TaxFormGUIDs = @TaxFormGUIDs OUT,
    @FormTypeID = 1, -- int
    @FormCount = -1, -- -1 indicates that we want the special $2 reprint price
    @SponsorGUID = @SponsorGUID,
    --@ClientGUID = @ClientGUID,
    @Pending = 0,
    @Remarks = @NewFormRemark,
    @ServiceFee = @ServiceFee OUT,
    @TaxFormPackageGUID = @NewPackageGUID OUT
    --@PackageCode = '', -- varchar(50)
    --@DebugText = '' -- varchar(max)
    
-- obtain the new formId, so UI can pull it up & print it
SELECT 
  @NewTaxFormGUID = RowGUID, 
  @OldFormRemark = 'Replaced by Form#: ' + OrderNumber
FROM iTRAAC.dbo.tblTaxForms WHERE PackageGUID = @NewPackageGUID

-- void this form 
UPDATE iTRAAC.dbo.tblTaxForms SET StatusFlags = StatusFlags | POWER(2,5) WHERE RowGUID = @VoidTaxFormGUID

-- log the void/replacement remark
EXEC Remark_u @TaxOfficeId = @TaxOfficeId,
    @UserGUID = @UserGUID,
    @TableNames = @TableNames out, -- varchar(1000)
    --@RowGUID = NULL, -- uniqueidentifier
    --@Alert = NULL, -- bit
    --@Title = '', -- varchar(50)
    @Remarks = @OldFormRemark,
    --@DeleteReason = '', -- varchar(200)
    --@AlertResolved = '', -- varchar(200)
    @FKRowGUID = @VoidTaxFormGUID,
    @RemarkTypeId = 14 -- 14 == void


END
GO

grant execute on TaxForm_VoidAndNew to public
go



