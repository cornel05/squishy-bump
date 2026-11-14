# Shared helpers, dot-sourced by the other tools/*.ps1 scripts. PowerShell 5.1 + 7.
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

function Get-ProjectUnityVersion {
    $line = Get-Content (Join-Path $RepoRoot 'ProjectSettings/ProjectVersion.txt') |
        Where-Object { $_ -match '^m_EditorVersion:\s*(\S+)' } | Select-Object -First 1
    if (-not $line) { throw 'm_EditorVersion not found in ProjectSettings/ProjectVersion.txt' }
    return $Matches[1]
}

# Returns the full path to Unity.exe matching ProjectVersion.txt, or throws with the fix command.
function Find-UnityEditor {
    $v = Get-ProjectUnityVersion
    $candidates = @()
    if ($env:UNITY_EDITOR) { $candidates += $env:UNITY_EDITOR }
    if (Get-Command unity -ErrorAction SilentlyContinue) {
        try {
            $json = & unity editors -i --json 2>$null | Out-String
            $candidates += (ConvertFrom-Json $json) | Where-Object { $_.version -eq $v } | ForEach-Object { $_.location }
        } catch { }
    }
    $hubCfg = Join-Path $env:APPDATA 'UnityHub/secondaryInstallPath.json'
    if (Test-Path $hubCfg) {
        $p = (Get-Content $hubCfg -Raw | ConvertFrom-Json)
        if ($p) { $candidates += (Join-Path $p "$v/Editor/Unity.exe") }
    }
    $candidates += (Join-Path $env:ProgramFiles "Unity/Hub/Editor/$v/Editor/Unity.exe")
    foreach ($c in $candidates) { if ($c -and (Test-Path $c)) { return (Resolve-Path $c).Path } }
    throw "Unity $v not found. Install it: unity install $v --accept-eula -y   (or set `$env:UNITY_EDITOR)"
}

function Test-EditorOpen { Test-Path (Join-Path $RepoRoot 'Temp/UnityLockfile') }

# Runs Unity in batchmode, waits, returns the exit code. Extra args are passed through.
function Invoke-UnityBatch([string]$LogFile, [string[]]$UnityArgs) {
    $unity = Find-UnityEditor
    New-Item -ItemType Directory -Force (Split-Path $LogFile) | Out-Null
    $all = @('-batchmode', '-nographics', '-projectPath', "`"$RepoRoot`"", '-logFile', "`"$LogFile`"") + $UnityArgs
    $p = Start-Process -FilePath $unity -ArgumentList $all -Wait -PassThru -NoNewWindow
    return $p.ExitCode
}

# Prints compiler errors/warnings from a Unity log (only our assemblies/scripts).
function Get-CompileIssues([string]$LogFile) {
    if (-not (Test-Path $LogFile)) { return @() }
    Select-String -Path $LogFile -Pattern '(error|warning) CS\d+' |
        ForEach-Object { $_.Line.Trim() } | Where-Object { $_ -match 'Assets[\\/]' } | Sort-Object -Unique
}
