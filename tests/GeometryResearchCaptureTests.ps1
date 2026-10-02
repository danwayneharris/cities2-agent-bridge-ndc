param([Parameter(Mandatory)][string]$GamePath,
    [string]$BridgeDll=(Join-Path $PSScriptRoot '../artifacts/research-build/CitiesIIAgentBridge.dll'))
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $GamePath 'Cities2_Data/Managed/Colossal.Mono.Cecil.dll')
$game=[Colossal.Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $GamePath 'Cities2_Data/Managed/Game.dll'))
$bridge=[Colossal.Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path $BridgeDll).Path)
try {
    $source=[IO.File]::ReadAllText((Join-Path $PSScriptRoot '../src/GeometryResearchCapture.cs'))
    $names=@([regex]::Matches($source,'"(Game\.[A-Za-z.]+)"') | ForEach-Object {$_.Groups[1].Value} | Select-Object -Unique)
    if($names.Count -lt 20){throw 'Geometry capture type inventory unexpectedly empty'}
    foreach($name in $names) {
        $type=$game.MainModule.Types | Where-Object FullName -eq $name
        if(!$type -or !$type.IsValueType){throw "Missing value type: $name"}
        if(!($type.Interfaces | Where-Object {$_.InterfaceType.FullName -in @('Unity.Entities.IComponentData','Unity.Entities.IBufferElementData')})) {
            throw "Not component/buffer data: $name"
        }
    }
    $mod=$bridge.MainModule.Types | Where-Object FullName -eq 'CitiesIIAgentBridge.Mod'
    $methods=@($mod.Methods | Where-Object {$_.Name -match '^(GeometryResearchCapture|CaptureGeometryComponent|CaptureGeometryBuffer|CaptureGeometryValue)$'})
    if($methods.Count -ne 4){throw 'Missing capture methods'}
    foreach($method in $methods) {
        foreach($instruction in $method.Body.Instructions) {
            $operand=$instruction.Operand
            if($operand -isnot [Colossal.Mono.Cecil.MethodReference]){continue}
            if($operand.DeclaringType.FullName -eq 'Unity.Entities.EntityManager' -and $operand.Name -notin @('Exists','HasComponent','GetComponentData','GetBuffer','CompleteAllTrackedJobs')) {
                throw "Unexpected ECS access: $operand"
            }
            if($operand.Name -match '^(PauseAnalysis|set_selectedSpeed|Schedule|CreateCommandBuffer|SetComponentData)$') {
                throw "Unexpected mutation: $operand"
            }
        }
    }
    $dispatch=[IO.File]::ReadAllText((Join-Path $PSScriptRoot '../src/Mod.cs'))
    $statusExpression=[regex]::Match($dispatch,'bool statusOnly\s*=([^;]+);').Groups[1].Value
    foreach($command in @('get_geometry_research_capture','begin_geometry_schedule_trace','get_geometry_schedule_trace','end_geometry_schedule_trace')) {
        if(!$statusExpression.Contains('command == "' + $command + '"')) {throw "$command dispatcher would auto-pause"}
    }
    Write-Output "PASS: $($names.Count) installed component/buffer contracts and compiled ECS access surface. Live completeness unverified."
} finally { $bridge.Dispose(); $game.Dispose() }
