[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string]$SavePath,
    [Parameter(Mandatory)][string]$SaveSha256,
    [string]$GamePath=[Environment]::GetEnvironmentVariable('CSII_INSTALLATIONPATH','User'),
    [switch]$Launch,
    [switch]$EnableBurst,
    [string[]]$AdditionalArguments = @()
)
$ErrorActionPreference='Stop'
if((Get-FileHash -LiteralPath $SavePath -Algorithm SHA256).Hash -ne $SaveSha256){throw 'Baseline hash changed'}
. (Join-Path $PSScriptRoot 'save-metadata-identity.ps1')
$id=Get-SaveMetadataIdentity $SavePath
$exe=Join-Path $GamePath 'Cities2.exe'
if(!(Test-Path -LiteralPath $exe -PathType Leaf)){throw 'Game executable missing'}
if(Get-Process Cities2 -ErrorAction SilentlyContinue){throw 'Game already running'}
if(!(Get-Process steam -ErrorAction SilentlyContinue)){throw 'Licensed Steam client must be running'}
$arguments=@('--noSplash','--developerMode','--uiDeveloperMode',"--startGame=$id")
if (@($AdditionalArguments | Where-Object { $_ -notmatch '^--[A-Za-z][A-Za-z0-9-]*$' -or $_ -match '^--(startGame|burst-|batchmode|nographics)' }).Count) { throw 'Additional arguments must be simple opt-in flags, not lifecycle, graphics or Burst overrides' }
$arguments += $AdditionalArguments
if(!$EnableBurst){$arguments+='--burst-disable-compilation'}
[pscustomobject]@{Executable=$exe;Save=(Resolve-Path $SavePath).Path;MetadataId=$id;Arguments=$arguments;BurstRequested=[bool]$EnableBurst;Instrumentation='Native scheduling-boundary capture; independently verify runtime Burst state'}
if(!$Launch){return}
if($PSCmdlet.ShouldProcess($exe,'Launch authorized instrumented toy baseline')) {
    $working=Join-Path $env:TEMP ('cs2-geometry-research-'+[Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory $working | Out-Null
    [IO.File]::WriteAllText((Join-Path $working 'steam_appid.txt'),'949230',[Text.Encoding]::ASCII)
    $process=Start-Process -FilePath $exe -WorkingDirectory $working -ArgumentList $arguments -WindowStyle Normal -PassThru
    [pscustomobject]@{ProcessId=$process.Id;WorkingDirectory=$working;Status='Launch requested; independently verify city, pause, controls and runtime build'}
}
