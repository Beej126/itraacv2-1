declare @p3 varchar(1000)
declare @p5 uniqueidentifier
exec dbo.Remark_u @TaxOfficeId=10000001,@UserGUID='AB30A6B8-330C-49AE-B7D7-A4F7C0E34FAF',@TableNames=@p3 output,@SponsorGUID='9302B079-3307-4097-A5B8-7FC6BBC4D3AB',@RowGUID=@p5 output,
@Alert=default,
@Title=default,
@Remarks=NULL,
@FKRowGUID='A8318A56-310B-4BAA-AE36-3F2EE6944C3A',
@RemarkTypeId=10
select @p3, @p5

DECLARE @file BIT
SET @File = 1

SELECT POWER(2,1) | (CASE WHEN @FILE = 1 THEN POWER(2,2) ELSE 0 END)




EXEC TaxForm_ReturnFile @TaxFormGUID = 'C9C4CCD4-0674-4217-95DB-6FC9B715D471', -- uniqueidentifier
    @UserGUID = 'AB30A6B8-330C-49AE-B7D7-A4F7C0E34FAF', -- uniqueidentifier
    @TaxOfficeCode = 'HD', -- varchar(4)
    @TaxOfficeId = 10000001,
    @File = 1

exec dbo.TaxForm_u @TaxOfficeId=10000001,@TaxOfficeCode='HD',@UserGUID='AB30A6B8-330C-49AE-B7D7-A4F7C0E34FAF',@RowGUID='C9C4CCD4-0674-4217-95DB-6FC9B715D471',@IsFiling=1,@UsedDate='Jun 18 2011 12:00:00:000AM',@TransTypeID=7,@Description='replacement iphone',@VendorGUID='80CD9E42-0AE6-46CB-BE0D-DC642A4364FC',@TotalCost=3000.0000,@CurrencyUsed=2,@CheckNumber=NULL,@LocationCode=NULL,@Incomplete=0,@StatusFlags=671088641
EXEC TaxForm_s @GUID = 'C9C4CCD4-0674-4217-95DB-6FC9B715D471'
SELECT iTRAAC.dbo.StatusFlagsBits_f(StatusFlags), * FROM iTRAAC.dbo.tblTaxForms WHERE OrderNumber = 'NF1-HD-10-01611'



DECLARE @File BIT, @RFStatusFlags int
SET @File = 0
SET @RFStatusFlags = POWER(2,1) | @FILE * POWER(2,2) 
SELECT @RFStatusFlags

SELECT 'Already ' + CASE
  WHEN StatusFlags & POWER(2,5) > 0 THEN 'VOIDED'
  WHEN @File = 0 AND StatusFlags & POWER(2,1) > 0 THEN 'RETURNED' + isnull(' [' + convert(varchar, ReturnedDate, 106)  + ']', '') 
  WHEN @File = 1 AND StatusFlags & POWER(2,2) > 0 THEN 'FILED' + isnull(' [' + convert(varchar, FiledDate, 106) + ']', '')
END
FROM iTRAAC.dbo.tblTaxForms
WHERE RowGUID = 'C9C4CCD4-0674-4217-95DB-6FC9B715D471'
AND StatusFlags & (power(2,1) | power(2,2) | power(2,5)) > 0

RAISERROR(@Error, 16,1) RETURN(-1) 

DELETE iTRAAC.dbo.tblPPOData WHERE TaxFormID IN (
SELECT TaxFormID FROM FormPackageClient_v WHERE CCode = 'a0999'
)

UPDATE FormPackageClient_v SET 
  FormStatusFlags = FormStatusFlags & ~POWER(2,1) & ~POWER(2,2),
  FileAgentID = NULL,
  FileUserGUID = NULL,
  FiledDate = NULL,
  RetAgentID = NULL,
  ReturnUserGUID = NULL,
  ReturnedDate = NULL,
  LocationCode = null
WHERE CCode = 'a0999'


SELECT
  f.RowGUID,
  f.StatusFlags,
  f.ReturnedDate,
  f.ReturnUserGUID,
  ru.FName + ' ' + ru.LName AS ReturnedBy,
  fu.FName + ' ' + fu.LName AS FiledBy,
  f.LocationCode,
  f.*
FROM iTRAAC.dbo.tblTaxForms f
LEFT JOIN iTRAAC.dbo.tblUsers ru ON ru.RowGUID = f.ReturnUserGUID
left JOIN iTRAAC.dbo.tblUsers fu ON fu.RowGUID = f.FileUserGUID
where f.RowGUID = 'C9C4CCD4-0674-4217-95DB-6FC9B715D471'






DROP PROC testme
AS begin
RAISERROR('kablooey!!', 16,1) RETURN(-1) 
END


DROP PROC testme2
AS begin

BEGIN TRY
BEGIN TRAN

EXEC testme

PRINT 'made it!!!'

COMMIT TRAN
END TRY
BEGIN CATCH
  IF @@trancount > 0 ROLLBACK TRANSACTION
  PRINT 'didnt make it'
  DECLARE @error VARCHAR(1000)
  SET @error = ERROR_MESSAGE()
  RAISERROR(@error, 16,1)
END CATCH 

END