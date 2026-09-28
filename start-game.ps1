[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$GamePath = [Environment]::GetEnvironmentVariable('CSII_INSTALLATIONPATH', 'User'),
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
$running = @(Get-Process -Name Cities2 -ErrorAction SilentlyContinue)
$plan = [pscustomobject]@{
    Executable = $exe
    WorkingDirectory = $root
    Arguments = @()
    AlreadyRunning = $running.Count -gt 0
    Action = $(if ($Launch) { 'Launch requested' } else { 'Inspection only; use -Launch to start' })
}
$plan
if (!$Launch) { return }
if ($running.Count) { throw 'Cities2 is already running. No process was stopped or restarted.' }
if ($PSCmdlet.ShouldProcess($exe, 'Launch CS2 directly without the Paradox launcher')) {
    # No auto-load/continue flags, Steam configuration changes, or save operations.
    $process = Start-Process -FilePath $exe -WorkingDirectory $root -WindowStyle Hidden -PassThru
    [pscustomobject]@{ ProcessId = $process.Id; Status = 'Process started; game readiness and Steam/mod services not yet verified' }
}
