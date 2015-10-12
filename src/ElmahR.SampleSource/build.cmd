@echo off
setlocal

set msBuildDir=%WINDIR%\Microsoft.NET\Framework\v4.0.30319

set deployPath="%~dp0..\Deploy\"
echo %deployPath%
call :dequote %deployPath% unquotedDeployPath
echo %unquoted%
call :resolve "%unquotedDeployPath%ElmahR.SampleSource\" destPath
call :dequote %destPath% unquotedDestPath

if not exist %deployPath% md %deployPath%
if not exist %destPath% md %destPath%
rd /S /Q %destPath%
md %destPath%

call %msBuildDir%\msbuild.exe  ElmahR.SampleSource.csproj "/p:Configuration=Debug" "/p:PublishDestination="%destPath%"" /t:PublishToFileSystem

set msBuildDir=
set destPath=

goto :EOF

:resolve
set %2=%~f1 
goto :EOF

:dequote
setlocal
rem The tilde in the next line is the really important bit.
set dequoted=%~1
endlocal & set %2=%dequoted%
goto :eof