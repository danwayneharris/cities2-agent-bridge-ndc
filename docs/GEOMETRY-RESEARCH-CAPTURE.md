# Geometry research capture (local experimental command)

`get_geometry_research_capture` takes `roots`, an array of explicit
`{index,version}` identities, and optional `maxEntities` (default 2048, maximum
8192). The city must already be paused. It does not enable controls or auto-pause.
Compile-only verification:

```powershell
.\build.ps1 -OutputDirectory ./artifacts/research-build -CommunityRelease
.\tests\GeometryResearchCaptureTests.ps1 -GamePath <installed-game-directory>
```

The command is not deployed or live-qualified yet. Use live identities after
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
