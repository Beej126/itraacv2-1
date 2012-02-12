USE master;

EXEC sp_configure "clr enabled", 1
RECONFIGURE

/****** Object:  SqlAssembly SQLCLR    Script Date: 10/16/2008 12:28:24 ******/
IF EXISTS(SELECT * FROM sys.objects WHERE name = 'split' AND type = 'FT')
  DROP FUNCTION dbo.Split

IF EXISTS(SELECT * FROM sys.objects WHERE name = 'RegexMatches' AND type = 'FT')
  DROP FUNCTION dbo.RegexMatches

IF EXISTS(SELECT * FROM sys.objects WHERE name = 'RegexReplace' AND type = 'FS')
  DROP FUNCTION dbo.RegexReplace

IF EXISTS(SELECT * FROM sys.objects WHERE name = 'Concat' AND type = 'AF')
  DROP AGGREGATE dbo.[Concat]

IF EXISTS(SELECT * FROM sys.objects WHERE name = 'OpenQueryCLR' AND type = 'PC')
  DROP PROCEDURE dbo.OpenQueryCLR

IF EXISTS(SELECT * FROM sys.assemblies WHERE name = 'SQLCLR')
  DROP ASSEMBLY SQLCLR

ALTER DATABASE MASTER SET TRUSTWORTHY ON;

-- master..xp_cmdshell 'dir d:\sqldata\'

CREATE ASSEMBLY SQLCLR AUTHORIZATION dbo FROM 'c:\sqldata\SQLCLR.dll' WITH PERMISSION_SET = UNSAFE
go

CREATE FUNCTION dbo.Split(@Source nvarchar(4000), @Delimiter nvarchar(4000))
RETURNS  TABLE (
	SeqNo int NULL,
	Value nvarchar(4000) NULL
) WITH EXECUTE AS CALLER
AS 
EXTERNAL NAME SQLCLR.Split.InitMethod
GO

CREATE FUNCTION dbo.RegexMatches(@Input nvarchar(4000), @Pattern nvarchar(4000))
RETURNS  TABLE (
	Match nvarchar(4000) NULL
) WITH EXECUTE AS CALLER
AS 
EXTERNAL NAME SQLCLR.RegexMatches.InitMethod
GO

CREATE FUNCTION dbo.RegexReplace(@Input nvarchar(4000), @Pattern nvarchar(4000), @Replacement nvarchar(4000))
RETURNS nvarchar(4000) WITH EXECUTE AS CALLER
AS 
EXTERNAL NAME SQLCLR.UserDefinedFunctions.RegexReplace
GO

CREATE AGGREGATE dbo.[CONCAT](@value NVARCHAR(4000))
RETURNS nvarchar(4000)
EXTERNAL NAME SQLCLR.[Concat]
GO

CREATE PROCEDURE dbo.OpenQueryCLR
	@ConnStr nvarchar(4000),
	@Query nvarchar(4000)
WITH EXECUTE AS CALLER
AS
EXTERNAL NAME SQLCLR.StoredProcedures.OpenQueryCLR
GO
