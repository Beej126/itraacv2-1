USE [iTRAAC]
GO
/****** Object:  StoredProcedure [cp_parmupd_iTRAAC_TaxForm]    Script Date: 04/06/2012 13:51:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO


ALTER PROCEDURE [cp_parmupd_iTRAAC_TaxForm]

    @TaxFormID int,
    @OrderNumber varchar(16),
    @CtrlNumber int,
    @PackageID int,
    @FormTypeID int,
    @TransTypeID int,
    @GooodsServicesID int,
    @VendorID int,
    @StatusFlags int,
    @Description varchar(170),
    @RetAgentID int,
    @FileAgentID int,
    @InitPrt215 datetime,
    @InitPrtAbw datetime,

    @TotalCost decimal(8,2),
    @CheckNumber varchar(25),
    @CurrencyUsed smallint

AS

-- 2012-04-06: we can finally populate a proper DateUsed!! yay!
DECLARE @UsedDate DATETIME
SELECT @UsedDate =
    CASE WHEN CHARINDEX(CHAR(9), @Description) >0 THEN DATEADD(DAY, 
      CONVERT(INT, replace(SUBSTRING(@Description, CHARINDEX(CHAR(9), @Description)+1, 500),char(9), '')), '2000-01-01') ELSE NULL END

UPDATE tblTaxForms
SET
    tblTaxForms.OrderNumber = @OrderNumber,
    tblTaxForms.CtrlNumber = @CtrlNumber,
    tblTaxForms.PackageID = @PackageID,
    tblTaxForms.FormTypeID = @FormTypeID,
    tblTaxForms.TransTypeID = @TransTypeID,
    tblTaxForms.GooodsServicesID = @GooodsServicesID,
    tblTaxForms.VendorID = @VendorID,
    tblTaxForms.StatusFlags = @StatusFlags,
    tblTaxForms.[Description] = @Description,
    tblTaxForms.RetAgentID = @RetAgentID,
    tblTaxForms.FileAgentID = @FileAgentID,
    tblTaxForms.InitPrt215 = @InitPrt215,
    tblTaxForms.InitPrtAbw = @InitPrtAbw,
    tblTaxForms.UsedDate = @UsedDate

WHERE 
    (tblTaxForms.TaxFormID = @TaxFormID)

-- Maintain FormCounts
DECLARE @ClientID INT
SELECT @ClientID = ClientID FROM tblTaxFormPackages WHERE PackageID = @PackageID
EXEC cp_parmupd_FormCounts @ClientID

IF @FormTypeID<>3 BEGIN
    IF NOT EXISTS(SELECT * FROM tblPPOData WHERE TAXFORMID=@TaxFormID) BEGIN
        INSERT INTO tblPPOData (
            TaxFormID,
            TotalCost,
            CheckNumber,
            CurrencyUsed
        )
        VALUES (
            @TaxFormID,
            @TotalCost,
            'N/A',
            @CurrencyUsed
        )
    END ELSE BEGIN
        UPDATE tblPPOData 
        SET tblPPOData.TotalCost = @TotalCost,
            tblPPOData.CheckNumber = @CheckNumber,
            tblPPOData.CurrencyUsed = @CurrencyUsed
        WHERE (tblPPOData.TaxFormID = @TaxFormID)
    END
END

IF @@ERROR <> 0
    RETURN 1


