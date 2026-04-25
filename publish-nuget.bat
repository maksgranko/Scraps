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
set success_count=0
set error_count=0

for %%f in ("%NUGET_FOLDER%\*.nupkg") do (
    echo   - Publishing %%~nxf...
    dotnet nuget push "%%f" -k %APIKEY% -s https://api.nuget.org/v3/index.json --skip-duplicate --timeout 300
    if !errorlevel! equ 0 (
        echo     SUCCESS
        set /a success_count+=1
    ) else (
        echo     FAILED
        set /a error_count+=1
    )
)

echo.
echo ==========================================
echo   Published: %success_count% packages
if %error_count% gtr 0 echo   Failed: %error_count% packages
echo   Done.
echo ==========================================
pause
