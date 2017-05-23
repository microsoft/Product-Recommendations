@ECHO off

REM ******************************************
REM
REM Copyright (c) Microsoft Corporation. All rights reserved.
REM Licensed under the MIT license.
REM
REM Regenerates the Recommendations client
REM
REM ******************************************



REM download the most up-to-date swagger file or use a URL
REM SET swaggerLocation = swagger.json
SET swaggerLocation=https://localhost:44342/swagger/docs/v1

..\packages\AutoRest.0.17.3\tools\AutoRest.exe -Input %swaggerLocation% -Namespace Recommendations.Client -ModelsName Entities
