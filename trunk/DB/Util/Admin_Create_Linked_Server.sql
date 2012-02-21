
-- EXEC Admin_Create_Linked_Server 'mwr-tro-hd\mssql2008'
-- sp_linkedservers

alter PROC Admin_Create_Linked_Server
@RemoteServerName sysname
AS BEGIN

/****** Object:  LinkedServer [MWR-TRO-BA]    Script Date: 01/21/2012 20:11:47 ******/
EXEC master.dbo.sp_addlinkedserver @server = @RemoteServerName, @srvproduct='SQL Server'
EXEC master.dbo.sp_addlinkedsrvlogin @rmtsrvname=@RemoteServerName,@useself='False',@locallogin=NULL,@rmtuser='sa',@rmtpassword='$(SQLCMDPASSWORD)'
EXEC master.dbo.sp_serveroption @server=@RemoteServerName, @optname='collation compatible', @optvalue='true'
EXEC master.dbo.sp_serveroption @server=@RemoteServerName, @optname='data access', @optvalue='true'
EXEC master.dbo.sp_serveroption @server=@RemoteServerName, @optname='dist', @optvalue='false'
EXEC master.dbo.sp_serveroption @server=@RemoteServerName, @optname='pub', @optvalue='false'
EXEC master.dbo.sp_serveroption @server=@RemoteServerName, @optname='rpc', @optvalue='true'
EXEC master.dbo.sp_serveroption @server=@RemoteServerName, @optname='rpc out', @optvalue='true'
EXEC master.dbo.sp_serveroption @server=@RemoteServerName, @optname='sub', @optvalue='true'
EXEC master.dbo.sp_serveroption @server=@RemoteServerName, @optname='connect timeout', @optvalue='0'
EXEC master.dbo.sp_serveroption @server=@RemoteServerName, @optname='collation name', @optvalue=null
EXEC master.dbo.sp_serveroption @server=@RemoteServerName, @optname='lazy schema validation', @optvalue='false'
EXEC master.dbo.sp_serveroption @server=@RemoteServerName, @optname='query timeout', @optvalue='0'
EXEC master.dbo.sp_serveroption @server=@RemoteServerName, @optname='use remote collation', @optvalue='true'

END
go