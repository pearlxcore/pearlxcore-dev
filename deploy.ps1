# Deployment script for pearlxcore.dev to Ubuntu server
# Usage: .\deploy.ps1

param(
    [string]$Server = "192.168.50.146",
    [string]$User = "pearlxcore",
    [string]$RemoteRoot = "/var/www/pearlxcore.dev",
    [string]$PublishDir = (Join-Path $PSScriptRoot ".artifacts\publish"),
    [string]$SudoPassword = ""
)

$ReleaseName = (Get-Date).ToUniversalTime().ToString("yyyyMMdd_HHmmss")
$RemoteReleasesRoot = "$RemoteRoot/releases"
$RemoteSharedRoot = "$RemoteRoot/shared"
$RemoteUploadsRoot = "$RemoteSharedRoot/uploads"
$RemoteReleaseDir = "$RemoteReleasesRoot/$ReleaseName"
$RemoteLiveLink = "$RemoteRoot/publish"
$RemoteLegacyDir = "$RemoteReleasesRoot/legacy-$ReleaseName"
$RemotePostsDir = "$RemoteUploadsRoot/posts"
$RemoteAvatarsDir = "$RemoteUploadsRoot/avatars"
$RemoteCvDir = "$RemoteUploadsRoot/cv"

Write-Host "Starting deployment to $Server..." -ForegroundColor Cyan

if (Test-Path -LiteralPath $PublishDir) {
    Remove-Item -LiteralPath $PublishDir -Recurse -Force
}

# Step 1: Build Release version
Write-Host "`n[1/6] Building Release version into $PublishDir..." -ForegroundColor Yellow
dotnet publish -c Release -r linux-x64 --self-contained false -o $PublishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Build completed successfully" -ForegroundColor Green

# Clean out nested publish artifacts copied from the repository tree itself.
if (Test-Path -LiteralPath (Join-Path $PublishDir 'publish')) {
    Remove-Item -LiteralPath (Join-Path $PublishDir 'publish') -Recurse -Force
}

if (Test-Path -LiteralPath (Join-Path $PublishDir 'published')) {
    Remove-Item -LiteralPath (Join-Path $PublishDir 'published') -Recurse -Force
}

# Step 2: Inspect existing live target
Write-Host "`n[2/6] Inspecting existing live release..." -ForegroundColor Yellow
$PreviousLiveTarget = ssh "$User@$Server" "if [ -L '$RemoteLiveLink' ]; then readlink -f '$RemoteLiveLink'; fi" | Select-Object -First 1
if ($null -ne $PreviousLiveTarget) {
    $PreviousLiveTarget = $PreviousLiveTarget.Trim()
}
else {
    $PreviousLiveTarget = ""
}
Write-Host ("Previous live target: " + ($(if ($PreviousLiveTarget) { $PreviousLiveTarget } else { "(none - legacy directory)" }))) -ForegroundColor DarkGray

# Step 3: Prepare remote directories
Write-Host "`n[3/6] Preparing remote release directories..." -ForegroundColor Yellow
if ([string]::IsNullOrWhiteSpace($SudoPassword)) {
    ssh "$User@$Server" "mkdir -p '$RemoteReleasesRoot' '$RemoteUploadsRoot' '$RemotePostsDir' '$RemoteAvatarsDir' '$RemoteCvDir' && chmod -R a+rwX '$RemotePostsDir' '$RemoteAvatarsDir' '$RemoteCvDir'"
}
else {
    ssh "$User@$Server" "mkdir -p '$RemoteReleasesRoot' '$RemoteUploadsRoot' '$RemotePostsDir' '$RemoteAvatarsDir' '$RemoteCvDir' && printf '%s\n' '$SudoPassword' | sudo -S -p '' chmod -R a+rwX '$RemotePostsDir' '$RemoteAvatarsDir' '$RemoteCvDir'"
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "Remote directory preparation failed!" -ForegroundColor Red
    exit 1
}

# If this server still has an old physical publish directory, migrate its persistent uploads once.
ssh "$User@$Server" "if [ -d '$RemoteLiveLink' ] && [ ! -L '$RemoteLiveLink' ]; then cp -a '$RemoteLiveLink/wwwroot/images/posts/.' '$RemotePostsDir/' 2>/dev/null || true; cp -a '$RemoteLiveLink/wwwroot/images/avatars/.' '$RemoteAvatarsDir/' 2>/dev/null || true; cp -a '$RemoteLiveLink/wwwroot/files/cv/.' '$RemoteCvDir/' 2>/dev/null || true; mv '$RemoteLiveLink' '$RemoteLegacyDir'; fi"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to migrate existing live directory!" -ForegroundColor Red
    exit 1
}

# Step 4: Upload the new release
Write-Host "`n[4/6] Uploading release files to $RemoteReleaseDir..." -ForegroundColor Yellow
ssh "$User@$Server" "mkdir -p '$RemoteReleaseDir'"
scp -r "$PublishDir\*" "$User@${Server}:$RemoteReleaseDir"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Upload failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Upload completed successfully" -ForegroundColor Green

if ([string]::IsNullOrWhiteSpace($SudoPassword)) {
    ssh "$User@$Server" "chmod -R a+rX '$RemoteReleaseDir' '$RemotePostsDir' '$RemoteAvatarsDir' '$RemoteCvDir'"
}
else {
    ssh "$User@$Server" "chmod -R a+rX '$RemoteReleaseDir' && printf '%s\n' '$SudoPassword' | sudo -S -p '' chmod -R a+rX '$RemotePostsDir' '$RemoteAvatarsDir' '$RemoteCvDir'"
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to set release permissions!" -ForegroundColor Red
    exit 1
}

# Step 5: Wire persistent shared assets into the release, then switch the live symlink.
Write-Host "`n[5/6] Switching live release atomically..." -ForegroundColor Yellow
ssh "$User@$Server" @"
set -e
mkdir -p '$RemoteReleaseDir/wwwroot/images' '$RemoteReleaseDir/wwwroot/files'
if [ -d '$RemoteReleaseDir/wwwroot/images/posts' ]; then
    cp -a '$RemoteReleaseDir/wwwroot/images/posts/.' '$RemotePostsDir/' 2>/dev/null || true
fi
if [ -d '$RemoteReleaseDir/wwwroot/images/avatars' ]; then
    cp -a '$RemoteReleaseDir/wwwroot/images/avatars/.' '$RemoteAvatarsDir/' 2>/dev/null || true
fi
if [ -d '$RemoteReleaseDir/wwwroot/files/cv' ]; then
    cp -a '$RemoteReleaseDir/wwwroot/files/cv/.' '$RemoteCvDir/' 2>/dev/null || true
fi
rm -rf '$RemoteReleaseDir/wwwroot/images/posts' '$RemoteReleaseDir/wwwroot/images/avatars' '$RemoteReleaseDir/wwwroot/files/cv'
ln -sfnT '$RemotePostsDir' '$RemoteReleaseDir/wwwroot/images/posts'
ln -sfnT '$RemoteAvatarsDir' '$RemoteReleaseDir/wwwroot/images/avatars'
ln -sfnT '$RemoteCvDir' '$RemoteReleaseDir/wwwroot/files/cv'
ln -sfnT '$RemoteReleaseDir' '$RemoteLiveLink'
chmod -R a+rX '$RemoteReleaseDir'
printf '%s\n' '$SudoPassword' | sudo -S -p '' chmod -R a+rX '$RemotePostsDir' '$RemoteAvatarsDir' '$RemoteCvDir'
"@

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to switch live release!" -ForegroundColor Red
    exit 1
}

# Step 6: Restart service and verify health.
Write-Host "`n[6/6] Restarting service and running health check..." -ForegroundColor Yellow
if ([string]::IsNullOrWhiteSpace($SudoPassword)) {
    ssh "$User@$Server" "sudo systemctl restart pearlxcore"
}
else {
    ssh "$User@$Server" "printf '%s\n' '$SudoPassword' | sudo -S -p '' systemctl restart pearlxcore"
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "Service restart failed!" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 3

$HealthCheckPassed = $false
for ($attempt = 1; $attempt -le 5; $attempt++) {
    ssh "$User@$Server" "curl -fsS http://127.0.0.1:5000/ >/dev/null"
    if ($LASTEXITCODE -eq 0) {
        $HealthCheckPassed = $true
        break
    }

    Start-Sleep -Seconds 2
}

if (-not $HealthCheckPassed) {
    Write-Host "Health check failed, attempting rollback..." -ForegroundColor Red

    if ($PreviousLiveTarget) {
        if ([string]::IsNullOrWhiteSpace($SudoPassword)) {
            ssh "$User@$Server" "ln -sfnT '$PreviousLiveTarget' '$RemoteLiveLink' && sudo systemctl restart pearlxcore"
        }
        else {
            ssh "$User@$Server" "ln -sfnT '$PreviousLiveTarget' '$RemoteLiveLink' && printf '%s\n' '$SudoPassword' | sudo -S -p '' systemctl restart pearlxcore"
        }
    }
    else {
        if ([string]::IsNullOrWhiteSpace($SudoPassword)) {
            ssh "$User@$Server" "rm -f '$RemoteLiveLink' && mv '$RemoteLegacyDir' '$RemoteLiveLink' && sudo systemctl restart pearlxcore"
        }
        else {
            ssh "$User@$Server" "rm -f '$RemoteLiveLink' && mv '$RemoteLegacyDir' '$RemoteLiveLink' && printf '%s\n' '$SudoPassword' | sudo -S -p '' systemctl restart pearlxcore"
        }
    }

    exit 1
}

Write-Host "`nDeployment completed successfully." -ForegroundColor Green
Write-Host "Live release: $RemoteReleaseDir" -ForegroundColor Cyan
Write-Host "Active path: $RemoteLiveLink" -ForegroundColor Cyan
