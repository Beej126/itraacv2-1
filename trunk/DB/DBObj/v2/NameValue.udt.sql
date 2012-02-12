--$Author: $
--$Date: $
--$Modtime: $

use iTRAACv2
go

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

/*
*/
-- nugget: http://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqlcommandbuilder.deriveparameters.aspx#3
-- nugget: If the login that DeriveParameters is run under does not have permission to access the TestType type, no error will be thrown - the method will return successfully, but the SqlCommand.Parameters collection will not contain the @UserName parameter. !!!!

if not exists(select 1 from sys.types where name = 'NameValue')
  create TYPE NameValue AS TABLE
  (
    Name varchar(100) PRIMARY KEY,
    [Value] VARCHAR(max)
  )
go

GRANT EXECUTE ON TYPE::NameValue TO PUBLIC --nugget: http://www.sqlteam.com/article/sql-server-2008-table-valued-parameters
GO

