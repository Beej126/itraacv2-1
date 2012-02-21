INSERT tblUsers
        ( FName ,
          LName ,
          LoginName ,
          PKISerialNumber ,
          Email ,
          DSNPhone ,
          UserLevel ,
          StatusFlags ,
          RowGUID ,
          Password ,
          CreateDate ,
          Active
        )
VALUES  ( 'Rafael.Wunsch' , -- FName - varchar(25)
          '' , -- LName - varchar(25)
          'Rafael.Wunsch' , -- LoginName - varchar(15)
          '' , -- PKISerialNumber - varchar(15)
          '' , -- Email - varchar(75)
          '' , -- DSNPhone - varchar(8)
          9 , -- UserLevel - smallint
          3 , -- StatusFlags - int
          NEWID() , -- RowGUID - uniqueidentifier
          'XXXXXXXXXXXXXXXXXXXXXXXX' , -- Password - varchar(255)
          GETDATE() , -- CreateDate - datetime
          1 -- Active - bit
        )
SELECT * FROM tblUsers WHERE UserID = SCOPE_IDENTITY() --1504

10001022	Rafael.Wunsch		Rafael.Wunsch				9	3	4BAC30B9-F850-41E2-ABA9-FA7272CDB6A4


        
INSERT tblTaxFormAgents ( TaxOfficeID, UserID, SigBlock, Active,
                                StatusFlags, RowGUID, UserGUID )
VALUES  ( 10000001, -- TaxOfficeID - int (10000001 = HD)
          10001022, -- UserID - int
          'Rafael Wunsch', -- SigBlock - varchar(50)
          1, -- Active - bit
          0, -- StatusFlags - int
          NEWID(), -- RowGUID - uniqueidentifier
          '4BAC30B9-F850-41E2-ABA9-FA7272CDB6A4'  -- UserGUID - uniqueidentifier
          )
          
          exec cp_parmsel_iTRAAC_TaxOfficeAccess 1504,1
        
-- update tblUsers SET LoginName = 'Brent.Anderson2' WHERE lName = 'anderson'
DELETE tblUsers WHERE LoginName = 'AndersonB'
SELECT * FROM tblusers WHERE LoginName = 'andersonb'
SELECT * FROM tblTaxOffices

declare @p3 int
set @p3=0
declare @p4 varchar(100)
set @p4=NULL
declare @p5 int
set @p5=NULL
declare @p6 int
set @p6=NULL
declare @p7 int
set @p7=NULL
exec cp_parmsel_UserID 'andersonb','XXXXXXXXXXX',@p3 output,@p4 output,@p5 output,@p6 output,@p7 output
select @p3, @p4, @p5, @p6, @p7