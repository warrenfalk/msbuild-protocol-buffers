@echo off
pushd "%~dp0"
pushd "%~dp0"
where /q msbuild
IF ERRORLEVEL 1 (
	call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\Tools\VsMSBuildCmd.bat"
)
popd

nuget restore
msbuild /p:Configuration=Release /t:Rebuild
@if %errorlevel% neq 0 exit /b %errorlevel%
 
nuget pack MsBuild.ProtocolBuffers.nuspec
@if %errorlevel% neq 0 exit /b %errorlevel%

move *.nupkg c:\nuget.local
popd