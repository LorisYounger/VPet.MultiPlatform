@echo off
setlocal
rem Usage: mklink.bat [VPet repository root] [existing application output directory]
set "mainRoot=%~1"
if not defined mainRoot set "mainRoot=%VPET_ROOT%"
if not defined mainRoot (
    if exist "%~dp0..\VPet-Simulator.Windows\mod\0000_core\" (
        set "mainRoot=%~dp0.."
    ) else (
        set "mainRoot=%~dp0..\.."
    )
)
for %%I in ("%mainRoot%\VPet-Simulator.Windows\mod") do set "vpetModSource=%%~fI"
if not exist "%vpetModSource%\0000_core\" (
    echo Missing main repository mod\0000_core: "%vpetModSource%"
    exit /b 1
)
set "appDir=%~2"
if not defined appDir set "appDir=%~dp0bin\Debug\net10.0\win-x64"
if not exist "%appDir%\" (
    echo Build the application first, or supply its output directory: "%appDir%"
    exit /b 1
)
for %%I in ("%appDir%\mod") do set "vpetModLink=%%~fI"
if exist "%vpetModLink%\" (
    powershell.exe -NoProfile -Command "$entry = Get-Item -Force -LiteralPath $env:vpetModLink; if ($entry.LinkType -eq 'SymbolicLink' -and ($entry.Target -contains $env:vpetModSource)) { exit 0 }; Write-Host ('Existing mod directory has a different target or is a real directory: ' + $env:vpetModLink); exit 1"
    if errorlevel 1 exit /b 1
    echo Using existing mod link: "%vpetModLink%"
    exit /b 0
)
mklink /d "%vpetModLink%" "%vpetModSource%"
exit /b %errorlevel%
