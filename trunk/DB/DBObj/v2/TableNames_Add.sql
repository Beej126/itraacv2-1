--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/****** Object:  StoredProcedure [dbo].[TableNames_Add]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'TableNames_Add')
	exec('create PROCEDURE TableNames_Add as select 1 as one')
GO
alter PROCEDURE [dbo].[TableNames_Add]
@TableNames VARCHAR(MAX) OUT,
@Add VARCHAR(MAX)
AS BEGIN
	
SET @TableNames = ISNULL(@TableNames + ',', '') + @Add

END
GO

grant execute on TableNames_Add to public
go

