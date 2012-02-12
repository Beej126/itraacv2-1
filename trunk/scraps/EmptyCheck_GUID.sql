--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 12/31/09 4:00p $

USE iTRAACv2
go

if not exists(select 1 from sysobjects where name = 'EmptyCheck_GUID')
	exec('create FUNCTION EmptyCheck_GUID() returns int begin return 0 end')
GO
ALTER FUNCTION dbo.EmptyCheck_GUID(@NewVal UniqueIdentifier, @OldVal UniqueIdentifier)
RETURNS UniqueIdentifier
AS BEGIN

DECLARE @Return UniqueIdentifier

SELECT @Return = CASE WHEN @NewVal IS NULL THEN @OldVal ELSE @NewVal END

RETURN @Return

END
go

GRANT EXECUTE ON dbo.EmptyCheck_GUID TO PUBLIC
go


