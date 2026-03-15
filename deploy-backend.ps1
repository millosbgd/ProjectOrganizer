# =====================================================
# Backend Deployment Script - ProjectOrganizer
# =====================================================
# Automatski build i deploy backend-a na Azure App Service
# =====================================================

param(
    [string]$ResourceGroup = "rg-projectorganizer-dev",
    [string]$AppName = "ProjectOrganizer"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Backend Deployment Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if Azure CLI is installed
Write-Host "Checking Azure CLI..." -ForegroundColor Yellow
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Azure CLI is not installed!" -ForegroundColor Red
    Write-Host "Install from: https://aka.ms/installazurecliwindows" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Azure CLI found" -ForegroundColor Green
Write-Host ""

# Check if logged into Azure
Write-Host "Checking Azure login..." -ForegroundColor Yellow
$account = az account show 2>$null
if (-not $account) {
    Write-Host "❌ Not logged into Azure!" -ForegroundColor Red
    Write-Host "Run: az login" -ForegroundColor Yellow
    exit 1
}
Write-Host "✓ Logged into Azure" -ForegroundColor Green
Write-Host ""

# Navigate to backend directory
$backendPath = Join-Path $PSScriptRoot "backend\ProjectOrganizer.Api"
if (-not (Test-Path $backendPath)) {
    Write-Host "❌ Backend directory not found: $backendPath" -ForegroundColor Red
    exit 1
}

Set-Location $backendPath
Write-Host "✓ Changed directory to: $backendPath" -ForegroundColor Green
Write-Host ""

# Clean previous build
Write-Host "Cleaning previous build..." -ForegroundColor Yellow
if (Test-Path "publish") {
    Remove-Item -Recurse -Force "publish"
}
if (Test-Path "app.zip") {
    Remove-Item -Force "app.zip"
}
Write-Host "✓ Cleaned" -ForegroundColor Green
Write-Host ""

# Build in Release mode
Write-Host "Building backend in Release mode..." -ForegroundColor Yellow
dotnet publish -c Release -o ./publish
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Build successful" -ForegroundColor Green
Write-Host ""

# Create ZIP archive
Write-Host "Creating ZIP archive..." -ForegroundColor Yellow
Set-Location publish
Compress-Archive -Path * -DestinationPath ../app.zip -Force
Set-Location ..
Write-Host "✓ ZIP created: app.zip" -ForegroundColor Green
Write-Host ""

# Deploy to Azure
Write-Host "Deploying to Azure App Service..." -ForegroundColor Yellow
Write-Host "  Resource Group: $ResourceGroup" -ForegroundColor Cyan
Write-Host "  App Name: $AppName" -ForegroundColor Cyan
Write-Host ""

az webapp deploy `
    --resource-group $ResourceGroup `
    --name $AppName `
    --src-path app.zip `
    --type zip `
    --async false

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Deployment failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "✓ Deployment successful!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Backend URL: https://$AppName.azurewebsites.net" -ForegroundColor Cyan
Write-Host "API URL: https://$AppName.azurewebsites.net/api" -ForegroundColor Cyan
Write-Host ""

# Cleanup
Write-Host "Cleaning up temporary files..." -ForegroundColor Yellow
Remove-Item -Recurse -Force "publish"
Remove-Item -Force "app.zip"
Write-Host "✓ Cleanup complete" -ForegroundColor Green
Write-Host ""

# Return to original directory
Set-Location $PSScriptRoot

Write-Host "Done! 🚀" -ForegroundColor Green
