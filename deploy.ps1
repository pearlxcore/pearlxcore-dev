# Deployment script for pearlxcore.dev to Ubuntu server
# Usage: .\deploy.ps1

param(
    [string]$Server = "192.168.50.146",
    [string]$User = "pearlxcore",
    [string]$RemotePath = "/home/pearlxcore/pearlxcore.dev"
)

Write-Host "Starting deployment to $Server..." -ForegroundColor Cyan

# Step 1: Build Release version
Write-Host "`n[1/4] Building Release version..." -ForegroundColor Yellow
dotnet publish -c Release -o ./publish

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Build completed successfully" -ForegroundColor Green

# Step 2: Stop remote service
Write-Host "`n[2/4] Stopping remote service..." -ForegroundColor Yellow
ssh "$User@$Server" "sudo systemctl stop pearlxcore || true"
Start-Sleep -Seconds 2

# Step 3: Upload files via SCP
Write-Host "`n[3/4] Uploading files to server..." -ForegroundColor Yellow
scp -r ./publish/* "$User@${Server}:$RemotePath"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Upload failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Upload completed successfully" -ForegroundColor Green

# Step 4: Clear compressed CSS, restart service
Write-Host "`n[4/4] Clearing compressed CSS and restarting service..." -ForegroundColor Yellow
ssh $User@$Server "rm -f $RemotePath/wwwroot/css/site.css.br $RemotePath/wwwroot/css/site.css.gz && sudo systemctl start pearlxcore"

Write-Host "`n✅ Deployment completed!" -ForegroundColor Green
Write-Host "Visit: http://$Server" -ForegroundColor Cyan
