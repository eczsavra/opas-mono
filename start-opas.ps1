# OPAS - Start Script
# Backend + Frontend Build & Run

Write-Host "🔨 Building Backend..." -ForegroundColor Cyan
Set-Location C:\OPAS
dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Backend build successful!" -ForegroundColor Green
    
    # Start Backend in new terminal
    Write-Host "🚀 Starting Backend API..." -ForegroundColor Cyan
    Start-Process pwsh -ArgumentList '-NoExit', '-Command', 'cd C:\OPAS\src\Opas.Api; dotnet run'
    
    # Wait for backend to initialize
    Write-Host "⏳ Waiting for backend to initialize (5 seconds)..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
    
    # Start Frontend
    Write-Host "🚀 Starting Frontend..." -ForegroundColor Cyan
    Set-Location C:\OPAS\apps\web
    npm run dev
} else {
    Write-Host "❌ Backend build failed!" -ForegroundColor Red
    Write-Host "Press any key to exit..."
    $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
}

