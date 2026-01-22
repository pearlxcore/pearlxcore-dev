# CSS Analyzer - Find unused selectors by comparing against .cshtml files
param(
    [string]$CssFile = "wwwroot/css/site.css",
    [string]$ViewsPath = "Views"
)

# Read all .cshtml files
$htmlContent = ""
Get-ChildItem -Path $ViewsPath -Filter "*.cshtml" -Recurse | ForEach-Object {
    $htmlContent += Get-Content $_.FullName -Raw
    $htmlContent += "`n"
}

# Read CSS
$cssContent = Get-Content $CssFile -Raw
$cssLines = Get-Content $CssFile

# Extract selectors from CSS (simplified)
$selectors = @()
$inRule = $false
$currentRule = ""

foreach ($line in $cssLines) {
    # Match CSS selectors (everything before opening brace)
    if ($line -match "^[^{]+\{" -and $line -notmatch "^\s*/\*") {
        $selector = $line -replace "\s*\{.*", ""
        $selector = $selector.Trim()
        if ($selector -and $selector.Length -gt 0) {
            $selectors += $selector
        }
    }
}

Write-Host "=== CSS CLEANUP ANALYSIS ===" -ForegroundColor Cyan
Write-Host "Total selectors found: $($selectors.Count)"
Write-Host ""

# Check which selectors are used in HTML
$unusedCount = 0
$unusedSelectors = @()

$selectors | Select-Object -Unique | ForEach-Object {
    $selector = $_
    
    # Extract class names and IDs from selector
    $classes = [regex]::Matches($selector, "\.[a-zA-Z0-9_-]+") | ForEach-Object { $_.Value }
    $ids = [regex]::Matches($selector, "#[a-zA-Z0-9_-]+") | ForEach-Object { $_.Value }
    
    $found = $false
    
    # Check if any class/id from this selector appears in HTML
    foreach ($class in $classes) {
        if ($htmlContent -match [regex]::Escape($class)) {
            $found = $true
            break
        }
    }
    
    foreach ($id in $ids) {
        if ($htmlContent -match [regex]::Escape($id)) {
            $found = $true
            break
        }
    }
    
    # Some selectors don't have classes/ids (like element selectors), mark as found
    if (!$classes -and !$ids) {
        $found = $true
    }
    
    if (!$found -and $selector -notmatch "^\s*$") {
        $unusedCount++
        $unusedSelectors += $selector
        if ($unusedCount -le 10) {
            Write-Host "  ⚠️ Possibly unused: $selector"
        }
    }
}

Write-Host ""
Write-Host "Possibly unused selectors: $unusedCount"
Write-Host ""
Write-Host "Note: This is a rough estimate. Review manually before removing."
Write-Host "Recommendation: Use browser DevTools Coverage tab for accurate analysis."
