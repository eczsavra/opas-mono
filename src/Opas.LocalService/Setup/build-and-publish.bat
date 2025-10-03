@echo off
echo ========================================
echo OPAS Local Service Build & Publish
echo ========================================

cd /d "%~dp0.."

echo [1/3] Cleaning previous builds...
if exist "bin\Release" rmdir /S /Q "bin\Release"
if exist "obj" rmdir /S /Q "obj"

echo [2/3] Building and publishing...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false

echo [3/3] Copying setup files...
copy /Y "Setup\*.bat" "bin\Release\net8.0\win-x64\publish\"
copy /Y "Setup\*.reg" "bin\Release\net8.0\win-x64\publish\"

echo.
echo ========================================
echo ✅ Build tamamlandı!
echo.
echo Çıktı Dizini: bin\Release\net8.0\win-x64\publish\
echo.
echo Kurulum için:
echo 1. publish klasörünü hedef bilgisayara kopyala
echo 2. install-service.bat'ı yönetici olarak çalıştır
echo ========================================
pause
