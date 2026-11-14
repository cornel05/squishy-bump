<#
.SYNOPSIS  Compile + run EditMode tests (optionally PlayMode). Exit 0 only if everything passes.
.EXAMPLE   powershell -ExecutionPolicy Bypass -File tools/verify.ps1 [-PlayMode]
  Editor open (Temp/UnityLockfile): uses the live Editor through the Unity CLI.
  Editor closed: runs Unity in batchmode. Results go to TestResults/.
#>
param([switch]$PlayMode)
. (Join-Path $PSScriptRoot 'unity-common.ps1')
Set-Location $RepoRoot
$results = Join-Path $RepoRoot 'TestResults'
New-Item -ItemType Directory -Force $results | Out-Null
$modes = @('EditMode') + $(if ($PlayMode) { 'PlayMode' } else { @() })
$failed = $false

function Show-Results([string]$xml, [string]$mode) {
    if (-not (Test-Path $xml)) { Write-Host "  ${mode}: no results file (compile error?)" -ForegroundColor Red; return $false }
    [xml]$r = Get-Content $xml -Raw
    $run = $r.'test-run'
    Write-Host ("  {0}: {1} passed, {2} failed, {3} skipped ({4})" -f $mode, $run.passed, $run.failed, $run.skipped, $run.result)
    foreach ($t in $r.SelectNodes("//test-case[@result='Failed']")) {
        $loc = [regex]::Match([string]$t.failure.'stack-trace', 'in (.+?):(\d+)')
        $where = if ($loc.Success) { "$($loc.Groups[1].Value):$($loc.Groups[2].Value)" } else { '' }
        Write-Host "    FAIL $($t.fullname) $where" -ForegroundColor Red
        Write-Host "         $(([string]$t.failure.message).Trim())"
    }
    return ([int]$run.failed -eq 0 -and [int]$run.total -gt 0)
}

if (Test-EditorOpen) {
    Write-Host 'Editor is open: using the live Editor.'
    & unity recompile $RepoRoot --strict
    if ($LASTEXITCODE -ne 0) { Write-Host 'Compile: FAILED' -ForegroundColor Red; exit 1 }
    Write-Host 'Compile: OK'
    foreach ($m in $modes) {
        $xml = Join-Path $results "$m.xml"
        & unity test $RepoRoot --mode $m -o $xml | Out-Null
        if (-not (Show-Results $xml $m)) { $failed = $true }
    }
} else {
    Write-Host 'Editor is closed: running batchmode.'
    foreach ($m in $modes) {
        $xml = Join-Path $results "$m.xml"; $log = Join-Path $results "$m.log"
        Remove-Item $xml -ErrorAction SilentlyContinue
        $code = Invoke-UnityBatch $log @('-runTests', '-testPlatform', $m, '-testResults', "`"$xml`"")
        $issues = @(Get-CompileIssues $log)
        $issues | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
        if (@($issues | Where-Object { $_ -match 'error CS' }).Count -gt 0) { Write-Host 'Compile: FAILED' -ForegroundColor Red; exit 1 }
        Write-Host ("Compile: OK ({0} warnings)" -f $issues.Count)
        if (-not (Show-Results $xml $m)) { $failed = $true; Write-Host "  log: $log (exit $code)" }
    }
}

if ($failed) { Write-Host 'VERIFY: FAIL' -ForegroundColor Red; exit 1 }
Write-Host 'VERIFY: PASS' -ForegroundColor Green
