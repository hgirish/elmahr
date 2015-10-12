@echo off

setlocal
pushd "%~p0"

nuget pack ElmahR.Persistence.EntityFramework.SqlServerCompact.nuspec -Verbosity detailed