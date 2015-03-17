@echo off

mkdir "%~dp0\log"

echo Building Full install
"%VS100COMNTOOLS%\..\IDE\devenv.exe" "src\EVEL.engine.sln" /rebuild Debug /out "%~dp0\log\debug.log"

@if %BUILDERROR% == 0 (echo Build finished) else (echo Build failed!)
echo on
