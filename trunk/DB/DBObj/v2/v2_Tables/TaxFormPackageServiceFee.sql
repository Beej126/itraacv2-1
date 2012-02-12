USE iTRAACv2
GO

/****** Object:  Table dbo.TaxFormPackageServiceFee    Script Date: 09/21/2011 15:09:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF EXISTS(SELECT name FROM sysobjects WHERE NAME = 'TaxFormPackageServiceFee') DROP TABLE TaxFormPackageServiceFee
CREATE TABLE dbo.TaxFormPackageServiceFee(
  TaxFormPackageServiceFeeId INT NOT NULL,
	FormTypeID int NOT NULL,
	FormCount int NOT NULL,
	TotalServiceFee decimal(9,2) NOT NULL,
	Comment VARCHAR(MAX) -- ALTER TABLE TaxFormPackageServiceFee ADD Comment VARCHAR(max)
)
GO

/****** Object:  Index ix_unique    Script Date: 09/21/2011 15:14:19 ******/
CREATE UNIQUE CLUSTERED INDEX ix_unique ON dbo.TaxFormPackageServiceFee 
(
	FormTypeID ASC,
	FormCount ASC
)
GO

insert TaxFormPackageServiceFee (TaxFormPackageServiceFeeId, FormTypeID, FormCount, TotalServiceFee) values (1, 2, 1, 6)
insert TaxFormPackageServiceFee (TaxFormPackageServiceFeeId, FormTypeID, FormCount, TotalServiceFee) values (2, 1, 1, 4)
insert TaxFormPackageServiceFee (TaxFormPackageServiceFeeId, FormTypeID, FormCount, TotalServiceFee) values (3, 1, 2, 8)
insert TaxFormPackageServiceFee (TaxFormPackageServiceFeeId, FormTypeID, FormCount, TotalServiceFee) values (4, 1, 3, 12)
insert TaxFormPackageServiceFee (TaxFormPackageServiceFeeId, FormTypeID, FormCount, TotalServiceFee) values (5, 1, 4, 16)
insert TaxFormPackageServiceFee (TaxFormPackageServiceFeeId, FormTypeID, FormCount, TotalServiceFee) values (6, 1, 5, 20)
insert TaxFormPackageServiceFee (TaxFormPackageServiceFeeId, FormTypeID, FormCount, TotalServiceFee) values (7, 1, 6, 22)
insert TaxFormPackageServiceFee (TaxFormPackageServiceFeeId, FormTypeID, FormCount, TotalServiceFee) values (8, 1, 7, 24)
insert TaxFormPackageServiceFee (TaxFormPackageServiceFeeId, FormTypeID, FormCount, TotalServiceFee) values (9, 1, 8, 26)
insert TaxFormPackageServiceFee (TaxFormPackageServiceFeeId, FormTypeID, FormCount, TotalServiceFee) values (10, 1, 9, 28)
insert TaxFormPackageServiceFee (TaxFormPackageServiceFeeId, FormTypeID, FormCount, TotalServiceFee) values (11, 1, 10, 30)

insert TaxFormPackageServiceFee (TaxFormPackageServiceFeeId, FormTypeID, FormCount, TotalServiceFee, Comment) values (12, 1, -1, 2, 'Unexpired reprints')

/*
select * from TaxFormPackageServiceFee 


SELECT
  'insert TaxFormPackageServiceFee (FormTypeID, FormCount, TotalServiceFee) values (' +
  CONVERT(VARCHAR, FormTypeID) + ', ' +
  CONVERT(VARCHAR, FormCount) + ', ' +
  CONVERT(VARCHAR, TotalServiceFee) + ')'
FROM TaxFormPackageServiceFee
*/

