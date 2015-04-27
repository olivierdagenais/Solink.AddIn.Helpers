@echo off
.nuget\NuGet.exe restore
"C:\Program Files (x86)\MSBuild\12.0\bin\MSBuild.exe" /nologo build.proj /verbosity:minimal %*
