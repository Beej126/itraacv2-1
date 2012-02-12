USE iTRAAC
go

/*************** tblSessionEvents */


------ _ -------------------------------- _ ---------- _ ----- _ ----------------------------------- _ -----
--    / \      ____   _       _____      / \          / /     | |      _   _  _____           __    | |  
--   / ^ \    / __ \ | |     |  __ \    / ^ \        / /      | |     | \ | ||  ___|\        / /    | |  
--  /_/ \_\  | |  | || |     | |  | |  /_/ \_\      / /     __| |__   |  \| || |__ \ \  /\  / /   __| |__
--    | |    | |  | || |     | |  | |    | |       / /      \ \ / /   |     ||  __| \ \/  \/ /    \ \ / /
--    | |    | |__| || |____ | |__| |    | |      / /        \ V /    | |\  || |____ \  /\  /      \ V / 
--    |_|     \____/ |______||_____/     |_|     /_/          \_/     |_| \_||______| \/  \/        \_/  
------------------------------------------------------------------------------------------------------------

if exists(select 1 from sysindexes where object_name(id) = 'tblSessionEvents' and name = 'CIX_tblSessionEvents')
  drop index tblSessionEvents.CIX_tblSessionEvents
go
if not exists(select 1 from sysindexes where object_name(id) = 'tblSessionEvents' and name = 'ix_SessionId')  
  create clustered index ix_SessionId on tblSessionEvents (SessionId) 
go
if not exists(select 1 from sysindexes where object_name(id) = 'tblSessionEvents' and name = 'ix_Class')
  create index ix_Class on tblSessionEvents (Category, TypeId, EventId) include (SessionId, SEId, EventDateTime)
go
