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
    # The Pipeline server is briefly unreachable during a domain reload (e.g. after a package change): retry.
    for ($i = 1; $i -le 6; $i++) {
        $out = & unity recompile $RepoRoot --strict 2>&1 | Out-String
        if ($out -notmatch 'No Pipeline instance') { break }
        Write-Host "  Editor busy (reloading), retry $i/6..."; Start-Sleep -Seconds 10
    }
    Write-Host $out.Trim()
    if ($LASTEXITCODE -ne 0) { Write-Host 'Compile: FAILED' -ForegroundColor Red; exit 1 }
    Write-Host 'Compile: OK'
    if ($PlayMode) { Write-Host 'PlayMode via the live Editor is not supported: close the Editor and rerun with -PlayMode.' -ForegroundColor Red; exit 1 }
    # `unity test` refuses an open project, so use the Pipeline command; results are fresh JSON, never an old file.
    $json = Join-Path $results 'EditMode.json'
    $raw = & unity command test run --mode editor --project-path $RepoRoot --result-only 2>&1 | Out-String
    Set-Content -Path $json -Value $raw -Encoding UTF8
    # ForEach-Object unrolls the array: Windows PowerShell 5.1 ConvertFrom-Json returns it as one object.
    try { $tests = @(($raw | ConvertFrom-Json) | ForEach-Object { $_ }) } catch { Write-Host "  EditMode: no results ($($raw.Trim()))" -ForegroundColor Red; exit 1 }
    # On failure the command returns an error envelope (names the failed tests) instead of the result list.
    if ($tests.Count -eq 1 -and $tests[0].PSObject.Properties['success'] -and -not $tests[0].success) {
        $tests[0].errors | ForEach-Object { Write-Host "  $($_.message)" -ForegroundColor Red }
        Write-Host '  Detail (file:line): close the Editor and rerun for the batchmode report in TestResults/EditMode.xml.'
        Write-Host 'VERIFY: FAIL' -ForegroundColor Red; exit 1
    }
    $bad = @($tests | Where-Object { $_.status -ne 'Passed' -and $_.status -ne 'Skipped' })
    Write-Host ("  EditMode: {0} passed, {1} failed, {2} skipped" -f @($tests | Where-Object status -eq 'Passed').Count, $bad.Count, @($tests | Where-Object status -eq 'Skipped').Count)
    foreach ($t in $bad) {
        $loc = [regex]::Match([string]$t.stackTrace, 'in (.+?):(\d+)')
        Write-Host "    FAIL $($t.fullName) $(if ($loc.Success) { "$($loc.Groups[1].Value):$($loc.Groups[2].Value)" })" -ForegroundColor Red
        Write-Host "         $(([string]$t.message).Trim())"
    }
    if (@($tests | Where-Object status -eq 'Passed').Count -eq 0 -or $bad.Count -gt 0) { $failed = $true }
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
        # Unity logs this notice for module asmdefs that have no scripts yet; it is not a CS warning and never fails verify.
        $empty = @(Select-String -Path $log -Pattern "Definition File '(.+?)' will not be compiled, because it has no scripts" | ForEach-Object { $_.Matches[0].Groups[1].Value } | Sort-Object -Unique)
        if ($empty.Count) { Write-Host "  note: $($empty.Count) empty module asmdef(s) not compiled yet (expected until code lands)" }
        if (-not (Show-Results $xml $m)) { $failed = $true; Write-Host "  log: $log (exit $code)" }
    }
}

if ($failed) { Write-Host 'VERIFY: FAIL' -ForegroundColor Red; exit 1 }
Write-Host 'VERIFY: PASS' -ForegroundColor Green
