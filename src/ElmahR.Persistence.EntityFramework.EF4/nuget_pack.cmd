@echo off

setlocal
pushd "%~p0"

set cnf=%1
if "%cnf%"=="" set cnf=Debug

nuget pack ElmahR.Persistence.EntityFramework.EF4.csproj -Verbosity detailed -Prop Configuration=%cnf%