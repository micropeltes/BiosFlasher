@echo off
rem =====================================================================
rem  Build aplikasi BIOS Flasher (WinForms .NET 7) menjadi satu file EXE
rem =====================================================================
setlocal
cd /d "%~dp0"

echo === BIOS Flasher - build ===
echo.

dotnet publish BiosFlasher.csproj -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true -o dist
if errorlevel 1 goto :err

echo.
echo SELESAI: %~dp0dist\BiosFlasher.exe
echo (file tunggal, tidak perlu install .NET di komputer lain)
pause
exit /b 0

:err
echo.
echo *** BUILD GAGAL ***
pause
exit /b 1
