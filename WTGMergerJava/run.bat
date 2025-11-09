@echo off
setlocal

set SCRIPT_DIR=%~dp0
set WC3LIBS_DIR=%SCRIPT_DIR%..\wc3libs

REM Build classpath
set CP=%SCRIPT_DIR%bin;%WC3LIBS_DIR%\*

REM Run WTGMerger
java -cp "%CP%" WTGMerger %*
