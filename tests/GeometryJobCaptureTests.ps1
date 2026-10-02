param([Parameter(Mandatory)][string]$GamePath,
    [string]$BridgeDll=(Join-Path $PSScriptRoot '../artifacts/research-build/CitiesIIAgentBridge.dll'))
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $GamePath 'Cities2_Data/Managed/Colossal.Mono.Cecil.dll')
$game=[Colossal.Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $GamePath 'Cities2_Data/Managed/Game.dll'))
$bridge=[Colossal.Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path $BridgeDll).Path)
try {
    $geometry=$game.MainModule.Types | Where-Object FullName -eq 'Game.Net.GeometrySystem'
    foreach($job in @('InitializeNodeGeometryJob','CalculateEdgeGeometryJob','FlattenNodeGeometryJob','FinishEdgeGeometryJob')) {
        $type=$geometry.NestedTypes | Where-Object Name -eq $job
        if(!$type -or !$type.IsValueType){throw "Missing native job: $job"}
        $rootField=if($job -in @('InitializeNodeGeometryJob','FlattenNodeGeometryJob')){'m_EntityType'}else{'m_Entities'}
        if(!($type.Fields | Where-Object {$_.Name -eq $rootField -and $_.IsPublic})){throw "Missing native job root field: $job.$rootField"}
    }
    $finish=$geometry.NestedTypes | Where-Object Name -eq 'FinishEdgeGeometryJob'
    $map=$finish.Fields | Where-Object Name -eq 'm_EdgeHeightMap'
    if($map.FieldType.FullName -notlike 'Unity.Collections.NativeParallelHashMap*'){throw 'Finishing map contract changed'}
    $mod=$bridge.MainModule.Types | Where-Object FullName -eq 'CitiesIIAgentBridge.Mod'
    foreach($entry in @('CaptureGeometryEntityJob','CaptureGeometryChunkJob','CaptureGeometryLocal','CaptureGeometryLocals')) {
        if(!($mod.Methods | Where-Object {$_.Name -eq $entry -and $_.IsStatic -and $_.IsPublic})){throw "Missing debugger entry point: $entry"}
    }
    $methods=@($mod.Methods | Where-Object {$_.Name -match '^(CaptureGeometry|ValidateGeometryJob|GeometryCaptureError|GeometryDiagnosticEnvelope|SaveGeometryDiagnostic)' -and $_.Name -notin @('CaptureGeometryComponent','CaptureGeometryBuffer')})
    foreach($method in $methods) {
        foreach($instruction in $method.Body.Instructions) {
            $operand=$instruction.Operand
            if($operand -isnot [Colossal.Mono.Cecil.MethodReference]){continue}
            if($operand.DeclaringType.FullName -eq 'Unity.Entities.EntityManager') {throw "EntityManager call inside debugger helper: $operand"}
            if($operand.DeclaringType.FullName -like 'Unity.*' -and $operand.Name -match '^(set_|Set|Complete|Schedule|Dispose|Add|Remove|Clear)') {
                throw "Unexpected native mutation/lifetime operation: $operand"
            }
        }
    }
    Write-Output "PASS: native job entry/map contracts and $($methods.Count) compiled helper methods inspected; runtime invocation still unverified."
} finally { $bridge.Dispose(); $game.Dispose() }
