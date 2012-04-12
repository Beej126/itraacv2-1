/*
Missing Index Details from SQLQuery1.sql - mwr-tro-lu\mssql2008.iTRAAC (sa (65))
The Query Processor estimates that implementing the following index could improve the query cost by 99.3385%.
*/

USE [iTRAAC]
GO
CREATE NONCLUSTERED INDEX idx_RowTableID
ON [dbo].[tblRemarks] ([RowID],[TableID])

GO
