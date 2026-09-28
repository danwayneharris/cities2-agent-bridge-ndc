[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$GamePath = [Environment]::GetEnvironmentVariable('CSII_INSTALLATIONPATH', 'User'),
    [string]$SavePath,
    [switch]$Launch
)
$ErrorActionPreference = 'Stop'
if (!$GamePath) { throw 'Provide -GamePath or configure user-scoped CSII_INSTALLATIONPATH.' }
$root = [IO.Path]::GetFullPath($GamePath)
$exe = Join-Path $root 'Cities2.exe'
$configPath = Join-Path $root 'Launcher/launcher-settings.json'
if (!(Test-Path -LiteralPath $exe -PathType Leaf)) { throw "Missing executable: $exe" }
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$declared = [IO.Path]::GetFullPath((Join-Path (Split-Path $configPath) $config.exePath))
if ($config.gameId -ne 'cities_skylines_2' -or $declared -ne $exe) {
    throw 'Installed launcher configuration does not identify the expected CS2 executable.'
}
if (@($config.exeArgs).Count -ne 0) {
    throw 'Launcher now specifies arguments; review them before using this helper.'
}
$launchArguments = @()
$saveIdentity = $null
if ($SavePath) {
    $save = Get-Item -LiteralPath $SavePath
    if ($save.PSIsContainer -or $save.Extension -ne '.cok') { throw 'SavePath must be an existing .cok save file.' }
    $cid = (Get-Content -LiteralPath ($save.FullName + '.cid') -Raw).Trim()
    if ($cid -notmatch '^[0-9a-fA-F]{32}$') { throw 'Save identity must be a 32-digit hexadecimal asset ID.' }
    $saveIdentity = $cid.ToLowerInvariant()
    $launchArguments = @("--startGame=$saveIdentity")
}
$running = @(Get-Process -Name Cities2 -ErrorAction SilentlyContinue)
$plan = [pscustomobject]@{
    Executable = $exe
    WorkingDirectory = $root
    Arguments = $launchArguments
    SaveIdentity = $saveIdentity
    AlreadyRunning = $running.Count -gt 0
    Action = $(if ($Launch) { 'Launch requested' } else { 'Inspection only; use -Launch to start' })
}
$plan
if (!$Launch) { return }
throw 'Direct launch failed platform initialization in live testing. Launch disabled pending a verified Steam-context implementation; see INSTALL.md.'
if ($running.Count) { throw 'Cities2 is already running. No process was stopped or restarted.' }
if ($PSCmdlet.ShouldProcess($exe, 'Launch CS2 directly without the Paradox launcher')) {
    # Only the explicitly selected native save-load argument; no settings/save writes.
    $start = @{ FilePath = $exe; WorkingDirectory = $root; WindowStyle = 'Hidden'; PassThru = $true }
    if ($launchArguments.Count) { $start.ArgumentList = $launchArguments }
    $process = Start-Process @start
    [pscustomobject]@{ ProcessId = $process.Id; Status = 'Process started; game readiness and Steam/mod services not yet verified' }
}
