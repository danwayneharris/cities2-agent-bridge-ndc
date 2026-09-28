param([Parameter(Mandatory)][string]$GamePath,
    [string]$BridgeDll=(Join-Path $PSScriptRoot '../rebuilt/CitiesIIAgentBridge.dll'))
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $GamePath 'Cities2_Data/Managed/Colossal.Mono.Cecil.dll')
$game=[Colossal.Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $GamePath 'Cities2_Data/Managed/Game.dll'))
$bridge=[Colossal.Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path $BridgeDll).Path)
try {
    $contracts=@{
        'Game.Prefabs.TrackLaneData'=@('m_MaxCurviness','m_TrackTypes','m_FallbackPrefab')
        'Game.Prefabs.NetCompositionLane'=@('m_Lane','m_Position','m_Flags','m_Group','m_Index')
        'Game.Prefabs.NetCompositionData'=@('m_Width','m_Flags','m_State')
        'Game.Net.EdgeGeometry'=@('m_Start','m_End')
        'Game.Net.EdgeLane'=@('m_EdgeDelta','m_ConnectedStartCount','m_ConnectedEndCount')
        'Game.Net.Composition'=@('m_Edge','m_StartNode','m_EndNode')
        'Game.Tools.Temp'=@('m_Original','m_Flags')
        'Game.Net.Lane'=@('m_StartNode','m_MiddleNode','m_EndNode')
        'Game.Net.SubLane'=@('m_SubLane','m_PathMethods')
        'Game.Net.TrackLane'=@('m_Flags','m_SpeedLimit','m_Curviness','m_AccessRestriction')
        'Game.Net.CarLane'=@('m_Flags','m_SpeedLimit','m_Curviness','m_AccessRestriction')
    }
    foreach($name in $contracts.Keys) {
        $type=$game.MainModule.Types | Where-Object FullName -eq $name
        foreach($field in $contracts[$name]) {
            if(!($type.Fields | Where-Object { $_.Name -eq $field -and $_.IsPublic })) {throw "Missing public field: $name.$field"}
        }
    }
    $pathNode=$game.MainModule.Types | Where-Object FullName -eq 'Game.Pathfind.PathNode'
    foreach($name in @('GetOwnerIndex','GetLaneIndex','GetCurvePos','IsSecondary','Equals')) {
        if(!($pathNode.Methods | Where-Object {$_.Name -eq $name -and $_.IsPublic})) {throw "Missing PathNode accessor: $name"}
    }
    $mod=$bridge.MainModule.Types | Where-Object FullName -eq 'CitiesIIAgentBridge.Mod'
    $methods=@($mod.Methods | Where-Object Name -Like 'Junction*')
    if($methods.Count -ne 14){throw 'Expected snapshots and twelve local helpers'}
    $reads=@('Exists','HasComponent','HasBuffer','GetComponentData','GetBuffer','CreateEntityQuery')
    foreach($method in $methods) {
        foreach($instruction in $method.Body.Instructions) {
            $operand=$instruction.Operand
            if($operand -isnot [Colossal.Mono.Cecil.MethodReference]){continue}
            if($operand.DeclaringType.FullName -eq 'Unity.Entities.EntityManager' -and $operand.Name -notin $reads) {
                throw "Unexpected EntityManager operation: $operand"
            }
            if($operand.Name -match '^(PauseAnalysis|RequireControl|set_selectedSpeed|Schedule|CreateCommandBuffer)$') {
                throw "Unexpected control operation: $operand"
            }
        }
    }
    $source=[IO.File]::ReadAllText((Join-Path $PSScriptRoot '../src/Mod.cs'))
    if(!$source.Contains('bool statusOnly = command == "get_junction_snapshot" ||')) {throw 'Snapshot must bypass dispatcher auto-pause'}
    $snapshot=[IO.File]::ReadAllText((Join-Path $PSScriptRoot '../src/JunctionSnapshot.cs'))
    if(!$snapshot.Contains('simulation.selectedSpeed != 0')){throw 'Missing manually-paused guard'}
    Write-Output 'PASS: installed native contracts, compiled snapshot read operations, and no-auto-pause dispatch guard.'
    Write-Output 'These checks do not execute an ECS world or establish live connectivity.'
} finally { $bridge.Dispose(); $game.Dispose() }