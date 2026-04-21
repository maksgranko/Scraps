@echo off
chcp 65001 >nul
cd /d "%~dp0"

set NUGET_FOLDER=%~dp0NuGet

echo ==========================================
echo  Scraps: Publish to NuGet.org
echo ==========================================
echo.

if not exist "%NUGET_FOLDER%\*.nupkg" (
    echo No packages found in %NUGET_FOLDER%
    echo Run pack.bat first.
    pause
    exit /b 1
)

set /p APIKEY=Enter NuGet.org API Key: 
if "%APIKEY%"=="" (
    echo API Key is required.
    pause
    exit /b 1
)

echo.
echo Publishing packages...
for %%f in ("%NUGET_FOLDER%\*.nupkg") do (
    echo   - Publishing %%~nxf...
    dotnet nuget push "%%f" -k %APIKEY% -s https://api.nuget.org/v3/index.json --skip-duplicate
)

echo.
echo ==========================================
echo  Done.
echo ==========================================
pause
