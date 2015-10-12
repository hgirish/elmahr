@echo off

setlocal
pushd "%~p0"

set cnf=%1
if "%cnf%"=="" set cnf=Debug

nuget pack ElmahR.IoC.NInject.csproj -Verbosity detailed -Prop Configuration=%cnf% 