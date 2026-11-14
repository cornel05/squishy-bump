<#
.SYNOPSIS  Batchmode Windows x64 build via SquishyBump.Editor.BuildScript.BuildWindows -> Builds/Windows/.
.EXAMPLE   powershell -ExecutionPolicy Bypass -File tools/build-windows.ps1
  Close the Editor first: batchmode cannot open a project that is already open.
#>
. (Join-Path $PSScriptRoot 'unity-common.ps1')
Set-Location $RepoRoot
if (Test-EditorOpen) { Write-Host 'Close the Unity Editor first (Temp/UnityLockfile exists).' -ForegroundColor Red; exit 1 }

$log = Join-Path $RepoRoot 'Logs/build-windows.log'
$exe = Join-Path $RepoRoot 'Builds/Windows/SquishyBump.exe'
$code = Invoke-UnityBatch $log @('-quit', '-buildTarget', 'Win64', '-executeMethod', 'SquishyBump.Editor.BuildScript.BuildWindows')
Select-String -Path $log -Pattern '\[BuildScript\]' | ForEach-Object { Write-Host "  $($_.Line.Trim())" }

if ($code -ne 0 -or -not (Test-Path $exe)) { Write-Host "BUILD: FAIL (exit $code, log: $log)" -ForegroundColor Red; exit 1 }
Write-Host "BUILD: PASS -> $exe" -ForegroundColor Green
