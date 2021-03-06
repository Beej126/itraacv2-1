USE [iTRAAC]
GO
/****** Object:  StoredProcedure [cp_parmsel_iTRAAC_NextPackageCode]    Script Date: 03/13/2012 11:03:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO

ALTER PROCEDURE [cp_parmsel_iTRAAC_NextPackageCode] 

	@JulianDate CHAR(5),
	@AgentID INT,
	@NextPackageCode CHAR(13) OUTPUT

AS

SET NOCOUNT ON

DECLARE @PackageCode CHAR(8)

SELECT @PackageCode=OfficeCode+'_'+@JulianDate 
FROM tblTaxFormAgents A 
	INNER JOIN tblTaxOffices O ON A.TaxOfficeID=O.TaxOfficeID
WHERE AgentID=@AgentID

SELECT @NextPackageCode=@PackageCode+'-'+CONVERT(CHAR(4),RIGHT(RIGHT(ISNULL(MAX(PackageCode),0),4)+10001,4))
FROM tblTaxFormPackages 
WHERE PackageCode like @PackageCode + '%'

