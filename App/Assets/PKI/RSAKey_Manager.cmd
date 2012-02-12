@echo off

::
::Brent Anderson
::
::This script provides a light wrapper around the commands required
::  to encrypt the <connectionStrings> section of the App.config file
::Basically so I don't have to remember the exact syntax anymore ;)
::  and to be a place where I can record reminders for how to get 
::  around the common gotchas.
::
::aspnet_regiis.exe is the main workhorse.  It only operates on web.config, so part of the process
::  is to temporarily rename App.config to web.config

::nugget: this terse syntax makes sure the current directory is the same as the location of this batch file
::nugget: especially necessary under "runas" administrator.
cd /d %~dp0

::<!-- nugget: helpful references for encrypting .config files: -->
::<!-- nugget:   http://labs.episerver.com/en/Blogs/Per/Archive/2007/11/Encrypt-your-connection-strings-for-multiple-machines/ -->
::<!-- nugget:   http://msdn.microsoft.com/en-us/library/ms998283.aspx -->
::<!-- nugget:   http://www.codeproject.com/KB/database/WebFarmConnStringsNet20.aspx -->
::<!-- nugget:   http://stackoverflow.com/questions/781658/web-config-encryption-using-rsaprotectedconfigurationprovider-bad-data-error -->

::<!-- nugget: two very crucial and non-obvious points to this whole process: -->
::<!-- nugget: 1) you must create a custom key container/provider in your app.config in order to have something that will carry to another machine -->
::<!-- nugget: 2) you must export the private keys with the -pri option of -px -->


::nugget: 2>&1 maps stderr to stdout so we can >nul: all output from a failed command to avoid cluttering the dos output
::see if aspnet_regiis is available in the current path...
aspnet_regiis >nul: 2>&1
::if not then set the path...
if %ERRORLEVEL% GTR 0 set path=%path%;c:\Windows\Microsoft.NET\Framework\v4.0.30319
::and try again...
aspnet_regiis >nul: 2>&1
::if it's still not found, error out (probably not installed yet... switch to older version if you want)
if %ERRORLEVEL% EQU 0 goto main
  echo.
  echo c:\Windows\Microsoft.NET\Framework\v4.0.30319 not found
  pause
  goto quit

:main

set CryptoProvider=ExportableRsaCryptoServiceProvider
set RSAKeyName=iTRAACv2_RSAKey

echo.
echo Current settings (easily changed directly in this batch file):
echo   RSAKeyName: %RSAKeyName%
echo   CryptoProvider: %CryptoProvider%
echo   Doman\UserName: %USERDOMAIN%\%USERNAME%
echo.
echo Tips:
echo   . One must probably run these commands as a local administrator.
echo   . To simply register the key on a new machine, hit "c"reate, then "i"mport.

:menu
echo.
set importexport=
set /p importexport=Create/Zap (delete)/Import/eXport/Encrypt/Decrypt/Quit [c/z/i/x/e/d/q]: 
echo.
if "%importexport%" equ "c" goto create
if "%importexport%" equ "z" goto delete
if "%importexport%" equ "i" goto import
if "%importexport%" equ "x" goto export
if "%importexport%" equ "e" goto encrypt
if "%importexport%" equ "d" goto decrypt
if "%importexport%" equ "q" goto quit

echo invalid selection
echo.
goto menu

:create
::create key container and mark for export
aspnet_regiis -pc "%RSAKeyName%" -exp
if %ERRORLEVEL% equ 1 echo. & echo If you get the error "The RSA key container could not be opened." & echo Try adding your current user (or group) to the ACLs on the existing files in & echo      C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys & echo (old school pathing: C:\Documents and Settings\All Users\Microsoft\...) & echo.
::grant access to key container to current user
aspnet_regiis -pa "%RSAKeyName%" "%USERDOMAIN%\%USERNAME%"

echo.
echo Grant RSA Key access to additional user?
echo e.g. "ASPNET" for ASP.Net WCF web service to access it's web.config ConnectionStrings.
set additionaluser=
set /p additionaluser=(leave blank to skip):
if "%additionaluser%" neq "" aspnet_regiis -pa "%RSAKeyName%" "%additionaluser%"

goto menu

:delete
aspnet_regiis -pz "%RSAKeyName%" 
goto menu

:import
::this also creates the key container
aspnet_regiis -pi "%RSAKeyName%" "%RSAKeyName%.xml" -exp
goto menu

:export
::-pri = include private keys  (NUGGET: ***** VERY IMPORTANT, YOU'LL GET A "BAD DATA" ERROR IF YOU DON'T DO THIS ******)
aspnet_regiis -px "%RSAKeyName%" "%RSAKeyName%.xml" -pri
goto menu

:encrypt
::aspnet_regiis is hard coded to operate web.config files but works fine on other config file types, interesting little gap Microsoft has left there but no biggie
if exist app.config copy App.config web.config

aspnet_regiis -pef "connectionStrings" "." -prov "%CryptoProvider%"

if not exist app.config goto menu
copy web.config App.config
erase>nul: web.config
goto menu

:decrypt
if exist app.config copy App.config web.config

aspnet_regiis -pdf "connectionStrings" "."

if not exist app.config goto menu
copy web.config App.config
erase>nul: web.config
goto menu

:quit
set importexport= 
set CryptoProvider=
set RSAKeyName=
set XMLFile=
set additionaluser=