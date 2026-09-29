$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '../save-metadata-identity.ps1')
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.IO.Compression
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('save-id-tests-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $testRoot | Out-Null
# Leave tiny fixtures available for inspection; no recursive deletion.
function Fixture($name, $entries) {
    $path = Join-Path $testRoot ($name+'.cok')
    $zip = [IO.Compression.ZipFile]::Open($path, [IO.Compression.ZipArchiveMode]::Create)
    try { foreach($key in $entries.Keys) {
        $writer = [IO.StreamWriter]::new($zip.CreateEntry($key).Open())
        try { $writer.Write($entries[$key]) } finally { $writer.Dispose() }
    } } finally { $zip.Dispose() }
    return $path
}
$id='1234567890abcdef1234567890abcdef'
$valid=Fixture 'valid' @{'city.SaveGameMetadata.cid'=$id;'city.SaveGameData.cid'=('a'*32)}
if((Get-SaveMetadataIdentity $valid) -ne $id){throw 'Wrong metadata ID'}
$invalid=@(
    (Fixture 'missing' @{'city.SaveGameData.cid'=$id}),
    (Fixture 'ambiguous' @{'a.SaveGameMetadata.cid'=$id;'b.SaveGameMetadata.cid'=$id}),
    (Fixture 'malformed' @{'city.SaveGameMetadata.cid'=('z'*32)}),
    (Fixture 'oversized' @{'city.SaveGameMetadata.cid'=($id+'x')})
)
foreach($path in $invalid){
    $rejected=$false
    try { Get-SaveMetadataIdentity $path | Out-Null } catch { $rejected=$true }
    if(-not $rejected){throw "Accepted invalid fixture $path"}
}
Write-Output 'PASS: valid metadata identity; missing, ambiguous, malformed and oversized identities rejected.'
