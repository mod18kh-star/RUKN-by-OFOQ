$root = "C:\OFOQ.Market1"
$manifestPath = Join-Path $PSScriptRoot "FILES_MANIFEST.txt"

if (-not (Test-Path $root)) {
    Write-Host "PROJECT NOT FOUND: $root" -ForegroundColor Red
    exit 1
}

$expected = Get-Content $manifestPath | Where-Object {
    $_ -and $_ -notmatch '^Total new C# files:'
}

$missing = @()
foreach ($relative in $expected) {
    $windowsRelative = $relative -replace '/', '\\'
    $target = Join-Path $root $windowsRelative

    if (Test-Path $target) {
        Write-Host "[OK]      $windowsRelative" -ForegroundColor Green
    }
    else {
        Write-Host "[MISSING] $windowsRelative" -ForegroundColor Red
        $missing += $windowsRelative
    }
}

Write-Host ""
Write-Host "Expected: $($expected.Count)"
Write-Host "Missing : $($missing.Count)"

if ($missing.Count -eq 0) {
    Write-Host "SUCCESS: ALL PAYMENT API NEW FILES EXIST." -ForegroundColor Green
    exit 0
}

Write-Host "ERROR: SOME PAYMENT API FILES ARE MISSING." -ForegroundColor Red
exit 2
