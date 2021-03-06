/****** Object:  StoredProcedure [sp_ProcSearch]    Script Date: 10/27/2009 15:34:24 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

USE master
go

if not exists(select 1 from sysobjects where name = 'sp_ProcSearch')
	exec('create PROCEDURE sp_ProcSearch as select 1 as one')
GO
alter proc [sp_ProcSearch]
@txt varchar(255)
as
begin

declare @wildcard varchar(257)
set @wildcard = '%' +replace(@txt, '_', '[_]')+ '%'

select
  o.[name],
  substring (c.[text], patindex(@wildcard, c.[text]) -50, patindex(@wildcard, c.[text]) +255)
from syscomments c (nolock)
join sysobjects o (nolock) on o.id = c.id
where c.text like @wildcard
and o.[name] not like '[_]%'
and o.[name] not like 'sys%'
and o.[name] not like 'sp[_]sel[_]%'
and o.[name] not like 'sp[_]ins[_]%'
and o.[name] not like 'sp[_]upd[_]%'
and o.[name] not like '__[_]cft[_]%'
and o.[name] not like 'ctsv[_]%'
and o.[name] not like 'tsvw[_]%'
and o.[name] not like 'del[_]%'
and o.[name] not like 'ins[_]%'
and o.[name] not like 'sel[_]%'
and o.[name] not like 'upd[_]%'
and o.[name] not like 'ms[_]bi%'
and o.[name] not like 'msmerge[_]contents%'
and o.[name] <> replace(replace(@txt, '[', ''), ']', '')

end
