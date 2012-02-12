USE iTRAAC
GO
DECLARE @constraint sysname
SELECT @constraint = name FROM sys.objects WHERE name LIKE 'DF__tblUsers__Active%'
EXEC('ALTER TABLE tblusers DROP CONSTRAINT '+@constraint)
EXEC('ALTER TABLE tblusers ADD CONSTRAINT DF_tblUsers_Active DEFAULT(1) FOR Active')

