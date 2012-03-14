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

IF EXISTS(SELECT * FROM sys.objects WHERE name = 'concat' AND type = 'AF')
  DROP AGGREGATE dbo.[concat]

IF EXISTS(SELECT * FROM sys.objects WHERE name = 'OpenQueryCLR' AND type = 'PC')
  DROP PROCEDURE dbo.OpenQueryCLR

IF EXISTS(SELECT * FROM sys.assemblies WHERE name = 'SQLCLR')
  DROP ASSEMBLY SQLCLR

ALTER DATABASE MASTER SET TRUSTWORTHY ON;

-- master..xp_cmdshell 'dir d:\sqldata\' -- 

CREATE ASSEMBLY SQLCLR AUTHORIZATION dbo FROM 'd:\sqldata\SQLCLR.dll' WITH PERMISSION_SET = UNSAFE
go

CREATE FUNCTION dbo.Split(@Source nvarchar(max), @Delimiter nvarchar(max))
RETURNS  TABLE (
	SeqNo int NULL,
	Value nvarchar(max) NULL
) WITH EXECUTE AS CALLER
AS 
EXTERNAL NAME SQLCLR.Split.InitMethod
GO

CREATE FUNCTION dbo.RegexMatches(@Input nvarchar(max), @Pattern nvarchar(max))
RETURNS  TABLE (
	Match nvarchar(max) NULL
) WITH EXECUTE AS CALLER
AS 
EXTERNAL NAME SQLCLR.RegexMatches.InitMethod
GO

CREATE FUNCTION dbo.RegexReplace(@Input nvarchar(max), @Pattern nvarchar(max), @Replacement nvarchar(max))
RETURNS nvarchar(max) WITH EXECUTE AS CALLER
AS 
EXTERNAL NAME SQLCLR.UserDefinedFunctions.RegexReplace
GO

CREATE AGGREGATE dbo.[concat](@value nvarchar(max), @delimiter nvarchar(max))
RETURNS nvarchar(max)
EXTERNAL NAME SQLCLR.[concat]
GO

CREATE PROCEDURE dbo.OpenQueryCLR
	@ConnStr nvarchar(max),
	@Query nvarchar(max)
WITH EXECUTE AS CALLER
AS
EXTERNAL NAME SQLCLR.StoredProcedures.OpenQueryCLR
GO
