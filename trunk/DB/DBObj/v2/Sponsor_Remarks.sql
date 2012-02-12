--$Author: Brent.anderson2 $
--$Date: 11/24/09 5:03p $
--$Modtime: 11/23/09 5:04p $

/* testing:
EXEC Customer_Search @CCode = 'm2155'
exec Sponsor_Remarks @SponsorGUID = 'CBB853B1-E6EF-4DB7-9B26-205E4E191838'
*/

/* consider eliminating duplicate entries as a conversion cleanup
"Sponsor_Remarks" proc is where the rubber hits the road

-- doing some fancy group by footwork trying to eliminate the duplicate entries that the previous system generated for both sponsor & client
SELECT
  SponsorGUID,
  CONVERT(UNIQUEIDENTIFIER, MAX(RowGUID)) AS RowGUID,
  Alert,
  [Date],
  Remarks,
  MAX(Name) AS Name
FROM (
  SELECT
    @SponsorGUID AS SponsorGUID,
    CONVERT(VARCHAR(36), r.RowGUID) AS RowGUID,
    convert(bit, r.StatusFlags & POWER(2,12)) AS Alert, --nugget: convert to bit turns anything >0 to 1, handy, eliminates the need for a "case" clause
    CONVERT(DATETIME, convert(VARCHAR, r.LastUpdate, 106) + 
      ' ' + CONVERT(VARCHAR, DATEPART(HOUR, r.LastUpdate)) + 
      ':00' ) AS [Date],
    r.Remarks,
    i.FName AS Name
  FROM @RowIDs i
  JOIN iTRAAC.dbo.tblRemarks r ON r.TableID = i.TableID AND r.RowID = i.RowID
) t
GROUP BY
  SponsorGUID,
  Alert,
  [Date],
  Remarks
ORDER BY [Date] desc

--AND (@IncludeResolved = 1
--  or Resolved = 0
--)
*/


/****** Object:  StoredProcedure [dbo].[Sponsor_Remarks]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

USE iTRAACv2
GO

if not exists(select 1 from sysobjects where name = 'Sponsor_Remarks')
	exec('create PROCEDURE Sponsor_Remarks as select 1 as one')
GO
alter PROCEDURE [dbo].[Sponsor_Remarks] 
@SponsorGUID UNIQUEIDENTIFIER,
@RemarkGUID UNIQUEIDENTIFIER = NULL
AS BEGIN

SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

IF (@RemarkGUID IS NOT NULL) BEGIN
  SELECT @SponsorGUID AS SponsorGUID, r.*, CONVERT(VARCHAR(50), NULL) AS [Member]
  FROM Remark_v r
  WHERE r.RowGUID = @RemarkGUID
END

ELSE BEGIN
  SELECT @SponsorGUID AS SponsorGUID, r.*, i.FName AS [Member]
  FROM dbo.Members_All_f(@SponsorGUID) i
  JOIN Remark_v r on r.TableID = i.TableID AND r.FKRowGUID = i.RowGUID
END

END
GO

grant execute on Sponsor_Remarks to public
go

