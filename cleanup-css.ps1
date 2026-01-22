# Remove unused CSS sections from site.css
$cssFile = "wwwroot/css/site.css"
$content = Get-Content $cssFile -Raw
$originalSize = (Get-Item $cssFile).Length

# List of patterns to remove (CSS blocks that are definitely not used)
$toRemove = @(
    # Utterances (comments system CSS)
    '\.utterances\s*\{[^}]*\}.*?\.utterances-frame\s*\{[^}]*\}',
    
    # Editor styles (not used in your site)
    '\.editor-tabs\s*\{[^}]*(?:\{[^}]*\})*[^}]*\}',
    '\.editor-content\s*\{[^}]*(?:\{[^}]*\})*[^}]*\}',
    '\.editor-line-number\s*\{[^}]*\}',
    
    # Old/legacy styles (examples - add more as needed)
    '\.old-[a-z-]*\s*\{[^}]*\}',
    '\.deprecated-[a-z-]*\s*\{[^}]*\}'
)

$removedCount = 0
foreach ($pattern in $toRemove) {
    $matchCount = ([regex]::Matches($content, $pattern) | Measure-Object).Count
    if ($matchCount -gt 0) {
        Write-Host "Removing $matchCount instance(s) of: $pattern"
        $content = $content -replace $pattern, ""
        $removedCount += $matchCount
    }
}

# Clean up multiple blank lines
$content = $content -replace "`r`n\s*`r`n`r`n", "`r`n`r`n"

# Save cleaned version
$content | Set-Content $cssFile

$newSize = (Get-Item $cssFile).Length
$savedBytes = $originalSize - $newSize
$savedPercent = [math]::Round(($savedBytes / $originalSize) * 100, 2)

Write-Host ""
Write-Host "=== CLEANUP COMPLETE ===" -ForegroundColor Green
Write-Host "Sections removed: $removedCount"
Write-Host "Size before: $([math]::Round($originalSize/1024)) KB"
Write-Host "Size after: $([math]::Round($newSize/1024)) KB"
Write-Host "Saved: $([math]::Round($savedBytes/1024)) KB ($savedPercent%)"
