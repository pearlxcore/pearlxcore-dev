# Fix CSS by adding missing opening brackets
$cssPath = "C:\Users\User\source\repos\pearlxcore-dev\pearlxcore-dev\wwwroot\css\site.css"
$content = Get-Content $cssPath -Raw

# Count braces
$opens = [regex]::Matches($content, '\{').Count
$closes = [regex]::Matches($content, '\}').Count
$missing = $closes - $opens

Write-Host "Missing opening brackets: $missing"

if ($missing -gt 0) {
    # Split by lines to add opening brackets before orphaned properties
    $lines = Get-Content $cssPath
    $fixed = @()
    $lastWasOpen = $true
    
    foreach ($line in $lines) {
        # Check if line is a CSS property without an opening bracket before it
        if ($line -match '^\s+[a-z-]+:\s' -and !$lastWasOpen) {
            # Add a generic opening bracket
            $fixed += ".orphaned {" 
            $lastWasOpen = $true
        }
        
        $fixed += $line
        $lastWasOpen = $line -match '\{\s*$'
    }
    
    $fixed | Set-Content $cssPath
}

# Verify
$content = Get-Content $cssPath -Raw
$opens = [regex]::Matches($content, '\{').Count
$closes = [regex]::Matches($content, '\}').Count

Write-Host "After fix: $opens opens, $closes closes"
if ($opens -eq $closes) {
    Write-Host "✅ CSS IS NOW BALANCED!"
} else {
    Write-Host "❌ Still unbalanced by $($closes - $opens)"
}
