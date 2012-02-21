use MASTER
go

-- this proc is intended for binding to CTRL-F1 macro key in SQL Server Management Studio

-- sp_SelectAllShortcut 'tbltaxforms'

if not exists(select 1 from sysobjects where name = 'sp_SelectAllShortcut')
	exec('create PROCEDURE sp_SelectAllShortcut as select 1 as one')
GO 
alter PROCEDURE sp_SelectAllShortcut
@tablename VARCHAR(100)
AS
EXEC('select top 100 * from ' + @tablename)
GO
