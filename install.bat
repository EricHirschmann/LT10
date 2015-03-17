@echo off

mkdir "%~dp0\log"

echo Building Full install
"%VS100COMNTOOLS%\..\IDE\devenv.exe" "src\EVEL.engine.sln" /rebuild Install /out "%~dp0\log\install.log"

@set BUILDERROR=%ERRORLEVEL%
@if not %BUILDERROR% == 0 goto :finish



echo Building Light install
"%VS100COMNTOOLS%\..\IDE\devenv.exe" "src\EVEL.engine.sln" /rebuild Install_NoFrame /out "%~dp0\log\install_noframe.log"

@set BUILDERROR=%ERRORLEVEL%
@if not %BUILDERROR% == 0 goto :finish



:finish
@if %BUILDERROR% == 0 (echo Build finished) else (echo Build failed!)
echo on
