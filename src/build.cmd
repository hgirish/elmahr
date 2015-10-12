@echo off

if "%1"=="" echo Missing setup package version suffix && exit /b 1

cd ElmahR
call build.cmd

cd ..\ElmahR.SampleSource
call build.cmd

cd ..\Deploy
copy "EmptySdf\*.sdf" "ElmahR\App_Data"

del ElmahRSampleSetup.%1.zip /Y
7za a -r ElmahRSampleSetup.%1.zip ElmahR ElmahR.SampleSource README.txt

cd ..