$ErrorActionPreference = "Stop"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   Building TrayAlarm.exe (Windows Tray App)" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $csc)) {
    Write-Error "C# compiler not found at $csc"
    exit 1
}

Write-Host "Compiling C# source files..." -ForegroundColor Yellow

$sources = Get-ChildItem -Path "src\*.cs" | Select-Object -ExpandProperty FullName
$refs = "System.dll,System.Core.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Data.dll"

& $csc /nologo /target:winexe /optimize+ /win32icon:app.ico /out:TrayAlarm.exe /r:$refs $sources

if ($LASTEXITCODE -eq 0) {
    Write-Host "[SUCCESS] TrayAlarm.exe built successfully!" -ForegroundColor Green
    Write-Host "Executable: $(Resolve-Path .\TrayAlarm.exe)" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Build failed!" -ForegroundColor Red
    exit 1
}
