/*
Missing Index Details from SQLQuery10.sql - IMCMEUROA4VDB03.iTRAAC_Reports (sa (61))
The Query Processor estimates that implementing the following index could improve the query cost by 67.1284%.
*/

USE [iTRAAC]
GO
CREATE NONCLUSTERED INDEX idx_TransTypeUsedDate
ON [dbo].[tblTaxForms] ([TransTypeID],[UsedDate])
INCLUDE ([TaxFormID],[OrderNumber],[PackageID])
GO
