--$Author: Brent.anderson2 $
--$Date: 11/24/09 5:03p $
--$Modtime: 11/23/09 5:04p $

/* testing:
EXEC Customer_Search @CCode = 'm2155'
exec TaxForm_Remarks @SponsorGUID = 'CBB853B1-E6EF-4DB7-9B26-205E4E191838'
*/

/****** Object:  StoredProcedure [dbo].[TaxForm_Remarks]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'TaxForm_Remarks')
	exec('create PROCEDURE TaxForm_Remarks as select 1 as one')
GO
alter PROCEDURE [dbo].[TaxForm_Remarks] 
@TaxFormGUID UNIQUEIDENTIFIER = null,
@RemarkGUID UNIQUEIDENTIFIER = null
AS BEGIN

SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

SELECT r.*
FROM Remark_v r
WHERE r.TableID = 14
AND ( (@RemarkGUID IS NOT NULL AND r.RowGUID = @RemarkGUID) or (@RemarkGUID IS NULL and r.FKRowGUID = @TaxFormGUID) )
AND r.RemarkTypeId NOT IN (3,7,8,9, 12,13,16,17) --vehicle and weapon remarks are displayed differently since they're really data fields and not "remarks"

END
GO

grant execute on TaxForm_Remarks to public
go

