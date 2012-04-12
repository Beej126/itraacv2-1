/*
Missing Index Details from vehicle report - upfront query.sql - IMCMEUROA4VDB03.iTRAAC (sa (56))
The Query Processor estimates that implementing the following index could improve the query cost by 24.0492%.
*/

USE [iTRAAC]
GO
CREATE NONCLUSTERED INDEX idx_PurchaseDate
ON [dbo].[tblTaxFormPackages] ([PurchaseDate])
INCLUDE ([PackageID],[ClientID],[AgentID])
GO
