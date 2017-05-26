@ECHO off

REM ******************************************
REM
REM Copyright (c) Microsoft Corporation. All rights reserved.
REM Licensed under the MIT license.
REM
REM Builds the Recommendations UI and copies the output to the Recommendations.WebApp project
REM
REM ******************************************

ECHO Downloading required packages...
CALL npm install

IF NOT %ERRORLEVEL%==0 EXIT /B -1

ECHO.
ECHO Building Recommendations UI...
CALL npm run build

IF NOT %ERRORLEVEL%==0 EXIT /B -1

SET assetsFolder=..\Recommendations.WebApp\UI\

ECHO.
ECHO Replacing the UI assets under '%assetsFolder%' ...

DEL /Q /F /S %assetsFolder%
XCOPY /YS build %assetsFolder%
