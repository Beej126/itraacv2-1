
--**************************************
-- for :Data Scriptor (INSERT Generator)
--**************************************
--Licensed for the General Public. No warranties expressed.
--**************************************
-- Name: Data Scriptor (INSERT Generator)
-- Description:This script backs up all the data from your database into a SQL script.
-- By: Joseph A. De Guzman
--
--
-- Inputs:Just run the script from the Query Analyzer, etc.
--
-- Returns:The script returns INSERT statements for each of your table's data.
--
--Assumes:None
--
--Side Effects:None
--This code is copyrighted and has limited warranties.
--Please see http://www.Planet-Source-Code.com/xq/ASP/txtCodeId.1052/lngWId.5/qx/vb/scripts/ShowCode.htm
--for details.
--**************************************

--***************************************************************
--* Data Scriptor
--*
--* Use to script data from a database. Returns a script you
--* could save to a file to repopulate the database.
--*
--* Author: Joseph A. De Guzman
--* tuldoklambat@gmail.com
--*
--* Reference:
--* http://tuldoklambat.blogspot.com
--* http://community.devpinoy.org/blogs/tuldoklambat/default.aspx
--*
--***************************************************************
SET NOCOUNT ON
-- create a temporary table to hold the table info of the current db
DECLARE @tables TABLE (
	seq int identity(1,1)
	,id int
	,name varchar(100))
INSERT INTO @tables
	SELECT s.id, s.name
	FROM sysobjects s
	WHERE s.xtype='U'
		--AND s.status > 0 ???
		--AND s.name = 'tblusers'
	ORDER BY s.name
-- create a temporary table to hold the columns of the table being process
DECLARE @columns TABLE (
	name varchar(100))
-- drop #temp table if it exists, this may happen if the running this sp previously was cancelled
-- or returns an exception
IF object_id('tempdb..#temp', 'U') IS NOT NULL
DROP TABLE #temp
-- create a temporary table to hold the constructed INSERT commands
CREATE TABLE #temp (
	seq	int identity(1,1)
	,cmd varchar(8000))
-- holds the sequence numbers
DECLARE @tabseq int
DECLARE @cmdseq int
-- holds table info
DECLARE @tablename varchar(100)
DECLARE @tableid int
-- usually "dbo"
DECLARE @user_name varchar(100)
SET @user_name = user_name()
-- holds the SELECT statement that's ran against the table to create the INSERT commands (@cmd)
DECLARE @select varchar(8000) 
-- holds the INSERT command
DECLARE @cmd varchar(8000)
-- holds the number of INSERT command for the table in process
DECLARE @cmd_count int
-- holds the field part of the constructed INSERT command
DECLARE @fields varchar(8000)
-- holds the VALUES part of the constructed INSERT command
DECLARE @values varchar(8000)
-- START SCRIPT CONSTRUCTION
PRINT 'SET NOCOUNT ON'
PRINT ''
PRINT 'USE ' + db_name()
PRINT ''
-- process each table
WHILE EXISTS(SELECT TOP 1 * FROM @tables)


    BEGIN
    -- reset these values for each table
    	SET @fields = ''
    	SET @select = ''
    	SET @values = ''
    	SELECT TOP 1 
    		@tabseq = seq,
    		@tableid = id, 
    		@tablename = name 
    	FROM @tables
    -- fetch the column info for the current table
    	INSERT INTO @columns
    		SELECT '['+name +']'
    FROM syscolumns 
    WHERE id = @tableid 
    ORDER BY colid
    -- construct @fields and @values string for each columns,
    -- this have an effect of looping through the @columns table
    	UPDATE @columns
    	SET @fields = @fields + name + ', '
    		,@values = @values + 'ISNULL('''''''' + CAST(' + name + ' AS varchar(8000)) + '''''''',''NULL'') + '', '' + '
    	SET @select = 'SELECT ''INSERT INTO ' + @user_name + '.' + @tablename+ ' (' + LEFT(@fields, LEN(@fields) - 1) + ') VALUES ('' + ' + LEFT(@values, LEN(@values) - 9) + ' + '')'' FROM ' + @tablename --+ ' where rowguid = ''00000000-0000-0000-0000-000000000000'''
    	--debug:PRINT @select
    	INSERT INTO #temp EXEC (@select)
    -- if @@ROWCOUNT return zero that means the SELECT statement above did not return any data
    -- which means the table is empty and therefore need not be scripted
    IF @@ROWCOUNT > 0


        BEGIN
        -- disable constraints before inserting data
        PRINT '-- Data population script for ' + @tablename + ' table.'
        PRINT 'SET IDENTITY_INSERT ' + @tablename + ' ON'
        PRINT 'ALTER TABLE ' + @tablename + ' NOCHECK CONSTRAINT ALL'
        PRINT ''
        SELECT @cmd_count = COUNT(*) FROM #temp
        -- construct INSERTS
        PRINT '-- Data population script for ' + @tablename + ' table (' + CAST(@cmd_count AS varchar(100)) + ' records).'
        WHILE EXISTS(SELECT TOP 1 * FROM #temp)


            BEGIN
            	SELECT TOP 1 @cmdseq = seq, @cmd = cmd FROM #temp 
            PRINT @cmd
            DELETE #temp WHERE seq = @cmdseq
        END
        -- enable constraints back
        PRINT ''
        PRINT 'ALTER TABLE ' + @tablename + ' CHECK CONSTRAINT ALL'
        PRINT 'SET IDENTITY_INSERT ' + @tablename + ' OFF'
        PRINT ''
    END
    DELETE #temp
    -- clear @columns table
    	DELETE @columns	
    	DELETE @tables WHERE seq = @tabseq
END
PRINT 'SET NOCOUNT OFF'
-- END SCRIPT CONSTRUCTION
DROP TABLE #temp
-- Licensed for the General Public. No warranties expressed.

		
