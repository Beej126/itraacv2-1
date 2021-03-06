USE [iTRAAC]
GO
/****** Object:  UserDefinedFunction [StatusFlags_f]    Script Date: 01/30/2012 10:18:26 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO

ALTER FUNCTION [StatusFlags_f](@StatusFlags INT)
RETURNS VARCHAR(50)
AS BEGIN

DECLARE @i INT, @str VARCHAR(50) 
SELECT @i = 0, @str = ''

WHILE(@i <= 40) BEGIN
  IF (@StatusFlags & POWER(CONVERT(BIGINT, 2), @i) = POWER(CONVERT(BIGINT, 2), @i)) SET @str = @str + CONVERT(VARCHAR, @i) + ', '
  SET @i = @i + 1
END

IF LEN(@str) = 0 SET @str = 'none, '
SET @str = SUBSTRING(@str, 1, LEN(@str)-1)
RETURN @str

END
