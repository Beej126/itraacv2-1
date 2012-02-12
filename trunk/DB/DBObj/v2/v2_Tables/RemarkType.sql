USE iTRAACv2
go


-- select * from remarktype order by remarktypeid
-- delete remarktype where remarktypeid = 21

DROP TABLE RemarkType
-- truncate table RemarkType
-- select * from remarktype

CREATE TABLE RemarkType (
  RemarkTypeId INT NOT NULL PRIMARY KEY,
  Title VARCHAR(50) NOT NULL,
  CategoryId VARCHAR(20) NULL,
  Alert BIT NOT NULL DEFAULT(0)
)


INSERT RemarkType VALUES (3, 'Vehicle VIN', NULL, 0)
INSERT RemarkType VALUES (7, 'Vehicle Make', NULL, 0)
INSERT RemarkType VALUES (8, 'Vehicle Model', NULL, 0)
INSERT RemarkType VALUES (9, 'Vehicle Year', NULL, 0)

INSERT RemarkType VALUES (12, 'Weapon Serial Number', NULL, 0)
INSERT RemarkType VALUES (16, 'Weapon Model', NULL, 0)
INSERT RemarkType VALUES (17, 'Weapon Make', NULL, 0)
INSERT RemarkType VALUES (13, 'Weapon Caliber', NULL, 0)

INSERT RemarkType VALUES (4, 'Reprinted', NULL, 0)
INSERT RemarkType VALUES (6, 'Suspended', 'CUSTOMER_DING', 1)
INSERT RemarkType VALUES (10, 'Over Unpriced PO Limit', 'FORM_VIOLATION', 1)

-- iTRAAC business logic prevents user removing Alert status on any RemarkTypes with a CategoryId since those should be driven by other specific logic flows (i.e. Suspend button, PCS button)
-- but v2 completely eliminates "Bar" status which is more cohesively subsumed by an infinite suspension
-- so we must therefore leave the Bar Alerts open for the user to disable by leaving the Category blank
INSERT RemarkType VALUES (11, 'Barred', NULL, 1)

INSERT RemarkType VALUES (14, 'Voided', 'FORM_STATUS', 1)
INSERT RemarkType VALUES (15, 'Service Fee Changed', NULL, 0)

INSERT RemarkType VALUES (20, '"Split" Forms', 'FORM_VIOLATION', 1)
INSERT RemarkType VALUES (21, 'Other', 'FORM_VIOLATION', 1)
INSERT RemarkType VALUES (22, 'PCS''ed', 'CUSTOMER_DING', 1)
INSERT RemarkType VALUES (23, 'Deactivated Household', 'CUSTOMER_DING', 1)
INSERT RemarkType VALUES (24, 'Cust.Status Change', 'CUSTOMER', 0)
INSERT RemarkType VALUES (25, 'Lost', 'FORM_STATUS', 1)

------ _ -------------------------------- _ ---------- _ ----- _ ----------------------------------- _ -----
--    / \      ____   _       _____      / \          / /     | |      _   _  _____           __    | |  
--   / ^ \    / __ \ | |     |  __ \    / ^ \        / /      | |     | \ | ||  ___|\        / /    | |  
--  /_/ \_\  | |  | || |     | |  | |  /_/ \_\      / /     __| |__   |  \| || |__ \ \  /\  / /   __| |__
--    | |    | |  | || |     | |  | |    | |       / /      \ \ / /   |     ||  __| \ \/  \/ /    \ \ / /
--    | |    | |__| || |____ | |__| |    | |      / /        \ V /    | |\  || |____ \  /\  /      \ V / 
--    |_|     \____/ |______||_____/     |_|     /_/          \_/     |_| \_||______| \/  \/        \_/  
------------------------------------------------------------------------------------------------------------

