@echo off
pushd "%~dp0"
pushd "%~dp0"
where /q msbuild
IF ERRORLEVEL 1 (
	call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\Tools\VsMSBuildCmd.bat"
)
popd

msbuild /p:Configuration=Release /t:Restore
@if %errorlevel% neq 0 exit /b %errorlevel%

msbuild /p:Configuration=Release /t:Rebuild
@if %errorlevel% neq 0 exit /b %errorlevel%
 
msbuild /p:Configuration=Release /t:Pack
@if %errorlevel% neq 0 exit /b %errorlevel%

move bin\Release\*.nupkg c:\nuget.local
popd