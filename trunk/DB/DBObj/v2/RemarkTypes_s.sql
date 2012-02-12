--$Author: Brent.anderson2 $
--$Date: 4/30/10 4:49p $
--$Modtime: 4/30/10 4:48p $

/****** Object:  StoredProcedure [dbo].[RemarkTypes_s]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'RemarkTypes_s')
	exec('create PROCEDURE RemarkTypes_s as select 1 as one')
GO
alter PROCEDURE [dbo].RemarkTypes_s
@TableNames VARCHAR(50) = 'RemarkType' OUT
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

SELECT * FROM RemarkType

END
GO

grant execute on RemarkTypes_s to public
go

