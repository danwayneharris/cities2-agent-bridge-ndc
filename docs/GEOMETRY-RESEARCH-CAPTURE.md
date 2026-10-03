# Geometry research capture (local experimental command)

`get_geometry_research_capture` takes `roots`, an array of explicit
`{index,version}` identities, and optional `maxEntities` (default 2048, maximum
8192). The city must already be paused. It does not enable controls or auto-pause.
Compile-only verification:

```powershell
.\build.ps1 -OutputDirectory ./artifacts/research-build -CommunityRelease
.\tests\GeometryResearchCaptureTests.ps1 -GamePath <installed-game-directory>
```

The raw/scheduled capture path has bounded live qualification recorded in
[the map-capture handoff](session-notes/2026-10-02-0615-map-capture-qualified.md).
This does not qualify every debugger helper or arbitrary network. Use live identities after
checkpoint/session verification. Capture original and temporary roots in one call
when both exist. Serialize and retain the entire response unchanged.

It completes tracked jobs once and then reads the selected component/buffer types
synchronously on the main thread. This synchronization can change timing and must
be recorded as instrumentation. It is a post-barrier state observation, not an
internal job-boundary snapshot or proof of ordinary UI execution.

Full instance fields (including private fields) are retained for the listed types;
enums retain their underlying numeric value, entity references retain index/version,
and buffers retain order. Every entity reference in successfully read values expands
the closure. This can traverse the whole connected network; use toy fixtures.
Absent components are explicit. Uncaptured/unsupported values produce errors.
Entity count, buffer length, field depth and a 15-second read-time budget are bounded.
An incomplete capture contains an explicit frontier and errors and cannot qualify
as complete replay input. The barrier itself is outside that read-time budget.

`complete` only describes closure over the declared types. Terrain arrays, query
membership, job-local scratch maps and unlisted types are outside this contract.
Source type schemas and assembly module identity are included; preserve installed
assembly hashes and operation/build/save identity in the surrounding run manifest.
Do not label the raw schema as NetworkTools world schema 2: a validated converter
must explicitly map fields and reject missing required data.

## Native job breakpoint helpers

The compiled Bridge also provides public static debugger entry points on
CitiesIIAgentBridge.Mod:

- `CaptureGeometryEntityJob(object job, int index, string phase, string operationId)`
- `CaptureGeometryChunkJob(object job, ArchetypeChunk chunk, string phase, string operationId)`
- `CaptureGeometryLocal(object value, string label, string operationId)`
- `CaptureGeometryLocals(IDictionary locals, string operationId)`

Use these only in verified managed GeometrySystem job frames while all other
workers are debugger-suspended. Phase must be entry, exit or intermediate; retain
the actual breakpoint/frame/location evidence separately. Native Burst execution
cannot be observed this way. Do not cache or reuse job/chunk handles after resuming.
For entity jobs, index is the actual Execute index, not an entity index.

The helpers consume existing lookups/handles and never use EntityManager or wait
for jobs. Chunk snapshots include the actual chunk identities and component arrays;
entity jobs include the full m_Entities array and the current Execute root. They
expand referenced entities through available lookup types, preserve full fields
and ordered buffers, and record unsupported inputs explicitly. NativeList locals
can be recorded while their allocation is still alive. Finishing's readable shared
height map and named scalar/branch locals can be batched using a Hashtable passed
to CaptureGeometryLocals, avoiding separate resumptions between those reads. The
finishing job's readable shared
height map can be recorded; flattening's ParallelWriter cannot be read and is
explicitly uncaptured. Capture a readable map at a valid surrounding frame instead.

Every call writes a unique JSON file beneath LocalApplicationData/CitiesIIAgentBridge/
geometry-research and returns its path. The operationId links snapshots with the
external run manifest, save/session/build hashes and debugger frame evidence.
Calls retain serialized values only, and never dispose a job's native storage.
Reading a chunk through a writable type handle may mark its change version; this
and debugger suspension are instrumentation effects and must be distinguished from
ordinary UI reproduction. This helper path is compile/API checked, not yet live
qualified. Run tests/GeometryJobCaptureTests.ps1 against the installed game.

## Bounded managed-finishing experiment

Research schedule builds support `finishExecution: "managed"` when arming
`begin_geometry_schedule_trace`. Default is `"native"`. **Managed mode changes
execution and can change the resulting network surface**; it is not a read-only
capture. Use only an authorized, checkpointed toy case. It runs the original
FinishEdgeGeometryJob.Execute on the main thread after dependency completion,
resolving its deferred entity array from the completed producer. Other stages and
unarmed passes use their original schedulers. Execution mode is recorded in status
and each capture. Pass count, controls, STOP, pause and city-session limits remain.
This tests a compatibility hypothesis; it does not install a persistent fix.

## Publication and consumer boundary

See [the publication note](session-notes/2026-10-03-0304-PR-offline-capture.md) for the
paired offline replay PR and verification. Capture schemas are generic game
geometry diagnostics; consumer algorithms and runtime corrections belong to
consumer repositories. No consumer mod is required to build or use the bridge.

Schedule tracing requires an explicit `-ResearchHarmonyPath` build and explicit
arming. Ordinary builds omit the scheduling patch. Do not arm the research
transpiler alongside another GeometrySystem scheduler transpiler; interoperability
has not been qualified. Raw captures and game binaries remain local/ignored.
