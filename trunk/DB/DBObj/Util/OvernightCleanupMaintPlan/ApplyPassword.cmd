@echo off
sed s/Password=/Password=%SQLCMDPASSWORD%/i OvernightCleanup.dtsx >OvernightCleanup.t.dtsx
sed s/pwd=/pwd=%SQLCMDPASSWORD%/i OvernightCleanup.t.dtsx >OvernightCleanup_pwd.dtsx
erase >nul: OvernightCleanup.t.dtsx
start /wait "wait" cmd /k "@echo use this window to deploy 'OvernightCleanup_pwd.dtsx' which temporarily contains the required password & @echo then file will be auto deleted when this window is closed"
erase OvernightCleanup_pwd.dtsx
