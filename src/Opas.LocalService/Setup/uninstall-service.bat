@echo off
echo ========================================
echo OPAS Local Service Uninstaller
echo ========================================

REM Admin yetkisi kontrolü
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo Bu script yönetici yetkisi gerektirir!
    echo Sağ tık -> "Yönetici olarak çalıştır" seçin
    pause
    exit /b 1
)

echo [1/3] Servisi durduruyor...
sc stop "OPAS Local Service"

echo [2/3] Windows Service kaydını siliyor...
sc delete "OPAS Local Service"

echo [3/3] Dosyaları temizliyor...
if exist "C:\Program Files\OPAS" (
    rmdir /S /Q "C:\Program Files\OPAS"
)

echo.
echo ========================================
echo ✅ Kaldırma tamamlandı!
echo.
echo Not: Protocol handler registry kayıtları 
echo manuel olarak temizlenebilir:
echo HKEY_CLASSES_ROOT\opas
echo ========================================
pause
