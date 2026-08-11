@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET 8 SDK was not found.
  echo Install the .NET 8 SDK or the Visual Studio 2022 .NET desktop development workload.
  pause
  exit /b 1
)

dotnet publish LibraryManagementSystem\LibraryManagementSystem.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o Publish
if errorlevel 1 (
  echo Build failed. Read the error messages above.
  pause
  exit /b 1
)

echo.
echo Portable build completed successfully.
echo Output folder: %~dp0Publish
pause
