use itraac
go

-- this runs on the central server in an agent job
/*
ALTER PROC AirForce_Keep_Alive 
AS begin 
  UPDATE [mwr-tro-ra].itraac.dbo.tblcontrollocal SET LoginMode = LoginMode | 1, StatusFlags = StatusFlags & ~1
  UPDATE [mwr-tro-sp].itraac.dbo.tblcontrollocal SET LoginMode = LoginMode | 1, StatusFlags = StatusFlags & ~1
  UPDATE [mwr-tro-ge].itraac.dbo.tblcontrollocal SET LoginMode = LoginMode | 1, StatusFlags = StatusFlags & ~1
END
*/

-- this runs on each airforce database
if not exists(select 1 from sysobjects where name = 'AirForce_Kill_Logins')
	exec('create PROC AirForce_Kill_Logins as begin select 1 end')
go

ALTER PROC AirForce_Kill_Logins
AS begin 
  UPDATE tblcontrollocal SET LoginMode = LoginMode & ~1, StatusFlags = 1
END
