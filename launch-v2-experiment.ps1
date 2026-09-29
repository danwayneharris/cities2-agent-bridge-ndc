[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string]$SavePath,
    [string]$GamePath=[Environment]::GetEnvironmentVariable('CSII_INSTALLATIONPATH','User'),
    [switch]$Launch
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'save-metadata-identity.ps1')
$id=Get-SaveMetadataIdentity $SavePath
$exe=Join-Path $GamePath 'Cities2.exe'
if(!(Test-Path -LiteralPath $exe -PathType Leaf)){throw 'Game executable missing'}
if(Get-Process Cities2 -ErrorAction SilentlyContinue){throw 'Game already running; refusing a second launch'}
if(!(Get-Process steam -ErrorAction SilentlyContinue)){throw 'Start the licensed Steam client first'}
$arguments=@('--noSplash',"--startGame=$id")
[pscustomobject]@{Executable=$exe;Save=(Resolve-Path $SavePath).Path;MetadataId=$id;Arguments=$arguments;Experimental=$true}
if(!$Launch){return}
if($PSCmdlet.ShouldProcess($exe,'Experimental Steam app-ID-hint launch of saved toy city')){
    $working=Join-Path $env:TEMP ('cs2-launch-v2-'+[Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory $working | Out-Null
    [IO.File]::WriteAllText((Join-Path $working 'steam_appid.txt'),'949230',[Text.Encoding]::ASCII)
    $p=Start-Process -FilePath $exe -WorkingDirectory $working -ArgumentList $arguments -WindowStyle Hidden -PassThru
    [pscustomobject]@{ProcessId=$p.Id;WorkingDirectory=$working;Status='Started; loading, pause and mod readiness require independent verification'}
}
