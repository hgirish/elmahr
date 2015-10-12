@echo off

setlocal
pushd "%~p0"

set cnf=%1
if "%cnf%"=="" set cnf=Debug

nuget pack ElmahR.Core.csproj -Verbosity detailed -Prop Configuration=%cnf%