<#
.SYNOPSIS  Launch the built Windows player directly into L1..L5 for visual and gameplay testing.
.EXAMPLE   powershell -ExecutionPolicy Bypass -File tools/play-level.ps1 -Level 3
.EXAMPLE   powershell -ExecutionPolicy Bypass -File tools/play-level.ps1 -Level 4 -Rule Rank -Perf 30
#>
param(
    [Parameter(Mandatory = $true)]
    [ValidateRange(1, 5)]
    [int]$Level,
    [ValidateSet('', 'Survival', 'Rank')]
    [string]$Rule = '',
    [double]$Perf = 0,
    [switch]$Wait
)

$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Exe = Join-Path $RepoRoot 'Builds/Windows/SquishyBump.exe'
if (-not (Test-Path $Exe)) {
    throw "Missing $Exe. Close Unity Editor and run: powershell -ExecutionPolicy Bypass -File tools/build-windows.ps1"
}

$argList = @('-level', "$Level")
if ($Rule) { $argList += @('-rule', $Rule) }
if ($Perf -gt 0) {
    $perfStr = $Perf.ToString([System.Globalization.CultureInfo]::InvariantCulture)
    $argList += @('-perf', $perfStr)
}

Write-Host "Launching L${Level}: $Exe $($argList -join ' ')"
if ($Wait) {
    $proc = Start-Process -FilePath $Exe -ArgumentList $argList -PassThru -Wait
    exit $proc.ExitCode
}
else {
    Start-Process -FilePath $Exe -ArgumentList $argList | Out-Null
}
