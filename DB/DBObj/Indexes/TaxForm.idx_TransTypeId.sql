/*
Missing Index Details from vehicle report - upfront query.sql - IMCMEUROA4VDB03.iTRAAC (sa (56))
The Query Processor estimates that implementing the following index could improve the query cost by 46.5923%.
*/

USE [iTRAAC]
GO
CREATE NONCLUSTERED INDEX idx_TransTypeID
ON [dbo].[tblTaxForms] ([TransTypeID])
INCLUDE ([TaxFormID],[OrderNumber],[PackageID])
GO
