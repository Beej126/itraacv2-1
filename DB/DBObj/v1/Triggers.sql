USE itraac
go

/****** Object:  Trigger [dbo].[INSERT_ACL_OffMgr]    Script Date: 01/27/2012 11:52:22 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

CREATE TRIGGER [dbo].[INSERT_ACL_OffMgr] ON [dbo].[tblOfficeManagers] FOR INSERT NOT FOR REPLICATION AS
DECLARE @RoleID INT
SELECT @RoleID=INSERTED.ManagerID FROM INSERTED
EXEC cp_parmins_DefaultManagerACL @RoleID

GO

/****** Object:  Trigger [dbo].[DELETE_Sponsor_Clients]    Script Date: 01/27/2012 11:52:23 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

CREATE TRIGGER [dbo].[DELETE_Sponsor_Clients] ON [dbo].[tblSponsors] FOR DELETE
AS
	DELETE FROM tblClients WHERE SponsorID = (SELECT SponsorID FROM DELETED)

GO

/****** Object:  Trigger [dbo].[INSERT_ACL_Agent]    Script Date: 01/27/2012 11:52:23 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

CREATE TRIGGER [dbo].[INSERT_ACL_Agent] ON [dbo].[tblTaxFormAgents] FOR INSERT NOT FOR REPLICATION AS
DECLARE @RoleID INT
SELECT @RoleID=INSERTED.AgentID FROM INSERTED
EXEC cp_parmins_DefaultAgentACL @RoleID

GO

