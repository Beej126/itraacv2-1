::%1 = plan name
::%2 = server

::if you get OLE DB errors, open properties on SQL Server's running account (e.g. "NT AUTHORITY\SYSTEM") and check on a "User Mapping" to "msdb" with a good full access role like "db_ssisadmin"

dtutil /sql "Maintenance Plans\%1" /en File;"%_cwd%\%1.dtsx";0 /sources mwr-tro-%2\mssql2008 /sourceu %SQLCMDUSER% /sourcep %SQLCMDPASSWORD%