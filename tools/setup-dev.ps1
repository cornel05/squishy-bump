<#
.SYNOPSIS  Per-clone setup for Squishy Bump. Idempotent: safe to run any number of times.
.EXAMPLE   powershell -ExecutionPolicy Bypass -File tools/setup-dev.ps1
#>
. (Join-Path $PSScriptRoot 'unity-common.ps1')
Set-Location $RepoRoot
$summary = [ordered]@{}

git lfs install --local | Out-Null
git lfs pull
$summary['Git LFS'] = 'installed + pulled'

$unity = Find-UnityEditor
$summary['Unity Editor'] = $unity

$merge = Join-Path (Split-Path $unity) 'Data/Tools/UnityYAMLMerge.exe'
if (-not (Test-Path $merge)) { throw "UnityYAMLMerge not found at $merge" }
$m = $merge -replace '\\', '/'
git config --local merge.unityyamlmerge.name 'Unity SmartMerge (UnityYAMLMerge)'
git config --local merge.unityyamlmerge.driver "'$m' merge -h -p --force --fallback none %O %B %A %A"
git config --local merge.unityyamlmerge.recursive binary
git config --local mergetool.unityyamlmerge.cmd "'$m' merge -p `"`$BASE`" `"`$REMOTE`" `"`$LOCAL`" `"`$MERGED`""
git config --local mergetool.unityyamlmerge.trustExitCode false
$summary['Merge driver'] = $m

git config --local core.hooksPath tools/githooks
$summary['Git hooks'] = 'core.hooksPath = tools/githooks'

$py = Get-Command python -ErrorAction SilentlyContinue
$summary['Python'] = if ($py) { (& python --version 2>&1) } else { 'MISSING: uv python install 3.12 --default' }

Write-Host "`nSquishy Bump dev setup" -ForegroundColor Green
$summary.GetEnumerator() | ForEach-Object { Write-Host ('  {0,-14} {1}' -f $_.Key, $_.Value) }
Write-Host "`nNext: open the project with 'unity open .' (or Unity Hub), then run tools/verify.ps1"
