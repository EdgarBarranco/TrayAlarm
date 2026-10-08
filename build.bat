@echo off
setlocal
echo ============================================
echo   Building TrayAlarm.exe (Windows Tray App)
echo ============================================

set CSC_PATH=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC_PATH%" (
    echo Error: C# compiler not found at %CSC_PATH%
    pause
    exit /b 1
)

echo Compiling C# source files...
"%CSC_PATH%" /nologo /target:winexe /optimize+ /win32icon:app.ico /out:TrayAlarm.exe /r:System.dll,System.Core.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Data.dll src\*.cs

if %ERRORLEVEL% equ 0 (
    echo [SUCCESS] TrayAlarm.exe built successfully!
    echo Location: %cd%\TrayAlarm.exe
) else (
    echo [ERROR] Build failed! Check compiler output above.
    pause
    exit /b 1
)
