@echo off
echo ========================================
echo OPAS Local Service Installer
echo ========================================

REM Admin yetkisi kontrolü
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo Bu script yönetici yetkisi gerektirir!
    echo Sağ tık -> "Yönetici olarak çalıştır" seçin
    pause
    exit /b 1
)

echo [1/4] Servis dosyalarını kopyalıyor...
if not exist "C:\Program Files\OPAS" mkdir "C:\Program Files\OPAS"
copy /Y "%~dp0..\bin\Release\net8.0\win-x64\publish\*" "C:\Program Files\OPAS\"

echo [2/4] Windows Service kaydediyor...
sc create "OPAS Local Service" binPath= "C:\Program Files\OPAS\OPASlocal.exe" start= auto DisplayName= "OPAS Local Service"

echo [3/4] Protocol handler kaydediyor...
regedit /s "%~dp0register-protocol.reg"

echo [4/4] Servisi başlatıyor...
sc start "OPAS Local Service"

echo.
echo ========================================
echo ✅ Kurulum tamamlandı!
echo.
echo Servis Adı: OPAS Local Service
echo Kurulum Dizini: C:\Program Files\OPAS\
echo Protocol: opas://
echo Port: 8080
echo.
echo Test için: http://localhost:8080/health
echo ========================================
pause
