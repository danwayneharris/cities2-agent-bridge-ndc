# Read-only package identity extraction; never expands or edits the save.
function Get-SaveMetadataIdentity {
    param([Parameter(Mandatory)][string]$SavePath)
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $file = Get-Item -LiteralPath $SavePath
    if ($file.PSIsContainer -or $file.Extension -ne '.cok') { throw 'Expected an existing .cok save.' }
    $zip = [IO.Compression.ZipFile]::OpenRead($file.FullName)
    try {
        $entries = @($zip.Entries | Where-Object { $_.FullName.EndsWith('.SaveGameMetadata.cid', [StringComparison]::OrdinalIgnoreCase) })
        if ($entries.Count -ne 1) { throw 'Expected exactly one metadata identity in the save package.' }
        if ($entries[0].Length -ne 32) { throw 'Metadata identity must contain exactly 32 bytes.' }
        $reader = [IO.StreamReader]::new($entries[0].Open())
        try { $id = $reader.ReadToEnd() } finally { $reader.Dispose() }
        if ($id -notmatch '^[0-9a-fA-F]{32}$') { throw 'Malformed metadata identity.' }
        return $id.ToLowerInvariant()
    } finally { $zip.Dispose() }
}
