param([string]$Capture = (Join-Path $PSScriptRoot '../../CS2-NetworkTools/NetworkTools.docs/session-notes/captures/preview-edges-20260928/79436-preview.json'))
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot '../src/PreviewJunctionResolver.cs')
function Assert-Status($expected, $ids, $rows, $complete=$true) {
    $r=[CitiesIIAgentBridge.PreviewJunctionResolver]::Resolve([string[]]$ids,[string[][]]$rows,$complete)
    if($r.Status -ne $expected){throw "Expected $expected; got $($r.Status)"}; return $r
}
$r=(Get-Content $Capture -Raw | ConvertFrom-Json).result
$rows=@($r.relatedPreviewEdges.edges | ForEach-Object {
    ,@("$($_.temp.original.index):$($_.temp.original.version)","$($_.startNode.index):$($_.startNode.version)","$($_.endNode.index):$($_.endNode.version)")
})
# Expected original incidence independently recorded in the live capture notes.
$ids=@('57331:1','79288:1','57330:1')
$resolved=Assert-Status 'resolved' $ids $rows
if($resolved.Candidates[0] -ne '54183:3'){throw 'Wrong replacement node'}
Assert-Status 'missing' $ids @($rows[0],$rows[1]) | Out-Null
Assert-Status 'ambiguous' $ids @($rows[0],$rows[1],$rows[2],$rows[0]) | Out-Null
Assert-Status 'incomplete' $ids $rows $false | Out-Null
Assert-Status 'ambiguous' @('a','b') @(@('a','1:1','2:1'),@('b','2:1','1:1')) | Out-Null
Assert-Status 'missing' @('a','b') @(@('a','1:1','2:1'),@('b','2:2','3:1')) | Out-Null
Assert-Status 'resolved' @('b','a') @(@('b','3:1','2:1'),@('a','2:1','1:1')) | Out-Null
Assert-Status 'unsupported' @('a','a') @(@('a','1:1','2:1')) | Out-Null
Write-Output 'PASS: actual C# resolver, saved capture, missing/duplicate/incomplete/ambiguous/version/reversal cases.'
