@echo off
chcp 65001 >nul
cd /d "%~dp0"

set NUGET_FOLDER=%~dp0NuGet
if not exist "%NUGET_FOLDER%" mkdir "%NUGET_FOLDER%"

echo ==========================================
echo  Scraps: Build + Pack all projects
echo ==========================================
echo.

echo [1/3] Building solution...
dotnet build Scraps.sln -c Release
if errorlevel 1 (
    echo BUILD FAILED. Fix errors above and retry.
    pause
    exit /b 1
)

echo.
echo [2/3] Packing all projects into NuGet/...
dotnet pack Scraps.sln -c Release --no-build -o "%NUGET_FOLDER%"
if errorlevel 1 (
    echo PACK FAILED.
    pause
    exit /b 1
)

echo.
echo [3/3] Created packages:
dir /b "%NUGET_FOLDER%\*.nupkg" 2>nul

echo.
echo ==========================================
echo  Done. Packages are in: %NUGET_FOLDER%
echo ==========================================
pause
