param([string]$GamePath = [Environment]::GetEnvironmentVariable('CSII_INSTALLATIONPATH', 'User'))
$ErrorActionPreference = 'Stop'
if (!$GamePath) { throw 'Supply -GamePath or configure CSII_INSTALLATIONPATH with the CS2 toolchain.' }
$managed = Join-Path $GamePath 'Cities2_Data/Managed'
$sdkLine = (& dotnet --list-sdks | Select-Object -Last 1)
if ($sdkLine -notmatch '^([^ ]+) \[(.+)\]$') { throw 'A .NET SDK on PATH is required.' }
$compiler = Join-Path (Join-Path $Matches[2] $Matches[1]) 'Roslyn/bincore/csc.dll'
$out = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$options = @('/nologo', '/target:library', '/langversion:9.0', '/nostdlib+',
    ('/out:"' + (Join-Path $out 'HelloBridgeExample.dll') + '"'))
foreach ($name in @('mscorlib', 'System', 'System.Core', 'System.Runtime', 'netstandard', 'Game', 'Newtonsoft.Json')) {
    $path = Join-Path $managed ($name + '.dll')
    if (!(Test-Path -LiteralPath $path)) { throw "Missing game reference: $path" }
    $options += '/reference:"' + $path + '"'
}
$options += '"' + (Join-Path $PSScriptRoot 'Mod.cs') + '"'
$options += '"' + (Join-Path $PSScriptRoot 'ProviderV1.cs') + '"'
$rsp = Join-Path $out 'compile.rsp'
[IO.File]::WriteAllLines($rsp, $options)
& dotnet $compiler ('@' + $rsp)
if ($LASTEXITCODE -ne 0) { throw 'Example compilation failed' }
Write-Output "Built $(Join-Path $out 'HelloBridgeExample.dll'); nothing deployed."
