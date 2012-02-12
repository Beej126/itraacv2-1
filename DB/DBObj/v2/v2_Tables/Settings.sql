USE iTRAACv2
GO

/*
delete settings where name like 'NFPrinter%'
*/

DROP TABLE Settings

CREATE TABLE Settings (
  SettingID UNIQUEIDENTIFIER DEFAULT(NEWSEQUENTIALID()) NOT null,
  TaxOfficeId int NOT null,
  [Name] VARCHAR(100) NOT null,
  [Value] VARCHAR(MAX) NOT NULL,
  Comments varchar(MAX) 
)

CREATE CLUSTERED INDEX ix_Name ON Settings (TaxOfficeId, [Name])
ALTER TABLE Settings ADD CONSTRAINT pk_SettingID PRIMARY KEY (SettingID)

INSERT Settings (TaxOfficeId, [Name], [Value]) VALUES (1 /*1 = central/global*/, 'AdminPassword', '0xE2B4D9D12BEB965771C4DE113B41B512AC669244')
INSERT Settings (TaxOfficeId, [Name], [Value]) VALUES (1, 'MaxClass1FormsCount', '10')
INSERT Settings (TaxOfficeId, [Name], [Value]) VALUES (1, 'RoughUSDToEuroRate', '0.7')
INSERT Settings (TaxOfficeId, [Name], [Value]) VALUES (1, 'TaxFormTotalCostMin', '20')
INSERT Settings (TaxOfficeId, [Name], [Value]) VALUES (1, 'OrderNumberRegEx', '(.*?)([NE]F[12]-[A-Z]{2}-[0-9]{2}-[0-9]{5,6})')
INSERT Settings (TaxOfficeId, [Name], [Value]) VALUES (1, 'FormReprintGraceDays', '30')

INSERT Settings (TaxOfficeId, [Name], [Value], Comments) VALUES (1, 'FormPrinterWidth', 96, 'characters' ) -- Epson FX-890, 30/216" line spacing, 12 CPI, 12" long German continuous paper
INSERT Settings (TaxOfficeId, [Name], [Value], Comments) VALUES (1, 'FormPrinterHeight', 83, 'characters' )

-- DELETE settings WHERE NAME = 'FormPrinterInitCodes' -- Settings_s 'HD'
INSERT Settings (TaxOfficeId, [Name], [Value], Comments) VALUES (1, 'FormPrinterInitCodes', 
  '0x 1B36 1B331E 1B7801 1B5700 1B6B01 1B4D 1B7000 1B46 1B35 1B2D00 0D 1B7401',
  --'40=reset to defaults'+
  '36=print ascii 129-156 as chars rather than control codes'+
  '33=n/216 line spacing (1Eh=30d)'+
  '78=Draft(0) LQ(1), '+
  '57=double wide off(0), '+
  '6B=typeface roman(0) sans-serif(1), '+
  '4D=12cpi, '+
  '70=proportional off(0), '+
  '46=bold off, '+
  '35=italic off, '+
  '2D=underline off(0), '+
  '74=char table PC437(1) Italic(0)' )

--//nugget: sending raw ESCape codes to dot matrix printer (Epson's "ESC/P" standard)
--//nugget: ESC/P reference manual: http://files.support.epson.com/pdf/general/escp2ref.pdf
--//ESC @   = reset
--//ESC 3 n = n/216" line spacing
--//ESC M   = 12 cpi
--//ESC k n = font: 0 = Roman, 1 = Sans serif font
--//ESC x n = quality: 0 = Draft, 1 = NLQ
--//ESC p 0 = turn off proportional
--//ESC 2   = 1/6" line spacing = 0.166666666666666


-- this was the first block of bytes sent to the printer... from what i can tell it's mostly defaults that don't really matter so i took what i needed and added it to the 'FormPrinterInitCodes' setting
-- 00 00 00 1B 01 -- not sure yet, maybe just some kind of signalling prefix
-- 40 45 4A 4C 20 31 32 38 34 2E 34 0A 40 45 4A 4C 20 20 20 20 20 0A --@EJL 1284.4 mode => http://www.undocprint.org/formats/printer_control_languages/ejl#enter_ieee_12844_command
-- 1B400D 1B7401 1B36 1B5200 1B50 -- 40 = reset & 0D makes it 'ready for next' command, 74=char table PC437(1), 52=char set USA(0), 50=10cpi

-- ******************************
-- 36=print ascii 129-156 as chars rather than codes! bingo! here's where they get the umlauts
-- ******************************

-- 1B28550100 0A -- 28550100="set unit" to m/3600 inch, here 0A=10 so 1/360 inch... seems like the default anyway 

-- '1B28430200 40 14' -- 28430200=set page length in defined unit, ml mh... 
--   mh = int( (pagelength * (1/unit)) / 256 ), ml = mod( (pagelength * (1/unit)) / 256 )
--   ml = 40hex = 64dec, mh = 14hex = 20dec
--   pagelength = ( (mh * 256) + ml ) * unit = (20 * 256 + 64) * 1/360 =  14 inches??? hmmmm should be 12

-- 1B28630400 48 00 F8 13 0D -- 28630400 = set page format (margins) top-low top-high bot-low bot-high
--   48h = 72d, F8h = 248d, 13h = 19d
--   formula: (high * 256 + low) * unit
--   top = 72 / 360 = 0.2 inches
--   bot = 13 * 256 + 248 / 360 = 9.93 inches??

UPDATE itraac.dbo.tblControlCentral SET CentralAdmin = 'Brent Anderson (370-6585)' 

select * from settings

------ _ -------------------------------- _ ---------- _ ----- _ ----------------------------------- _ -----
--    / \      ____   _       _____      / \          / /     | |      _   _  _____           __    | |  
--   / ^ \    / __ \ | |     |  __ \    / ^ \        / /      | |     | \ | ||  ___|\        / /    | |  
--  /_/ \_\  | |  | || |     | |  | |  /_/ \_\      / /     __| |__   |  \| || |__ \ \  /\  / /   __| |__
--    | |    | |  | || |     | |  | |    | |       / /      \ \ / /   |     ||  __| \ \/  \/ /    \ \ / /
--    | |    | |__| || |____ | |__| |    | |      / /        \ V /    | |\  || |____ \  /\  /      \ V / 
--    |_|     \____/ |______||_____/     |_|     /_/          \_/     |_| \_||______| \/  \/        \_/  
------------------------------------------------------------------------------------------------------------

