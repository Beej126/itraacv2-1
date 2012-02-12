USE iTRAACv2
go

IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.tables WHERE TABLE_NAME = 'Rank')
  CREATE TABLE [Rank] (RankID INT identity(1,1), [Rank] VARCHAR(10), SortOrder INT)
  --drop table [Rank]
go

IF NOT EXISTS(SELECT 1 FROM [Rank])
begin
  INSERT [Rank] ([Rank])
  SELECT [Rank] FROM iTRAAC.dbo.tblSponsors
  WHERE [Rank] NOT IN ('', '02')
  GROUP BY [Rank]
  ORDER BY ISNUMERIC(SUBSTRING([Rank], 2, 10)), LEFT([Rank],1),
    CASE WHEN ISNUMERIC(SUBSTRING([Rank], 2, 10)) = 1 THEN convert(int, SUBSTRING([Rank], 2,10)) ELSE ASCII(LEFT([Rank],1)) END
    
  UPDATE [Rank] SET SortOrder = RankID
  ALTER TABLE [Rank] DROP COLUMN RankId
  
  CREATE CLUSTERED INDEX ix_SortOrder ON [Rank] (SortOrder)
  
  UPDATE [Rank] SET SortOrder = SortOrder + 1 WHERE SortOrder > 2
  INSERT [Rank] ([Rank], SortOrder) VALUES ('Contractor', 3)
  -- SELECT * FROM [rank]
end

