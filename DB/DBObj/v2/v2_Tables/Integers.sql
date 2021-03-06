USE iTRAACv2
go

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.tables WHERE TABLE_NAME = 'integers')
BEGIN
  SELECT TOP 10000 IDENTITY(int,1,1) AS Number INTO Integers
  FROM sys.objects s1
  CROSS JOIN sys.objects s2 
  CROSS JOIN sys.objects s3
  ALTER TABLE Integers ADD CONSTRAINT PK_Ints PRIMARY KEY CLUSTERED (Number)
END

--SELECT COUNT(1) FROM dbo.Integers