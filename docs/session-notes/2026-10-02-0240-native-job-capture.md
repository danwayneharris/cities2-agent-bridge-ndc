# Native job capture helpers — 2026-10-02 02:40 PDT

Continue from da796d8. Previous increment made progress, with compiled world-closure
capture and explicit API verification. Live debugger ownership remains unconfirmed;
no attach/restart/deployment attempted. This increment prepares helpers for one-shot
capture from native managed job breakpoints, including lookups, chunk inputs, actual
job entity arrays, map contents and explicit local values.

These helpers must not call EntityManager or complete jobs from a stopped job (that
can wait on the very job being inspected). They consume the job's existing lookup
and chunk handles while valid and retain only serialized values. Invocation requires
all other worker threads suspended by the debugger and a verified active job frame.
Partial/unsupported data must be labelled; captures are instrumentation, not an
ordinary UI trace. Native Burst jobs cannot be intercepted through this path.

Implemented entity-job, chunk-job, individual-local and batched-local capture. Standalone Bridge build passes with existing updater deprecation warnings. Installed native job/height-map contracts and 16 compiled helper methods pass inspection: no EntityManager calls, job completions, scheduling, disposal of borrowed native containers or direct native setter calls. Live invocation remains unverified.
Dan confirmed all other agents are paused and authorized commandeering/restarting the game. Fresh checkpoint operation4162ee59eded496189ff00128ddf4c97 completed; save SHA256041446DF549599A9E99AC51295A4E1AF32168E61C2002285302F1AB1B7737F54. Identified stale service PID46288 as unity-devtools-mcp.dll1.2.0, stopped that process only. GamePID54932 remained responsive. A single fresh eval then failed before execution with no Mono greeting; no repeated retry. Graceful restart planned with verified checkpoint preserved.

Live capture transport initially rejected the new command in the client ValidateSet before sending any request. Added the research command to this branch's CLI and command catalog; the original terrain-profile CLI remains unchanged. This is a transport integration omission, not a native capture result.

Live results: gracefully closed game54932, deployed symbol-bearing Bridge from08b874e (DLL DBA09ADDC4BDBEC1F30783A15F7B33AB42A910DE99BE567675FDDE026E71D9D8), then launched checksummed baseline with managed geometry/debug UI flags. New gamePID45892, citySession92099620a3114491b9369126b8e38362. Wantagh/population0/selectedSpeed0/controls true verified. Native BurstCompiler.IsEnabled read at a Bridge.Tick breakpoint returned false. GUI debug endpoint now reachable. Existing NetworkTools DLL remains48EF3D8D36626D73C61F80133850509F6E8688337F05CF185533225636D0AEE9.
The raw closure capture collected210 entities in approximately115ms, with five NetGeometryData failures caused by embedded EntityArchetype native pointers. Supplementary capture33c5b586daec49e4a896a99e32cf2a5e records80 scalar/value prefab fields successfully (16 per prefab), excluding those archetype handles. Baseline raw response and supplement retained in artifacts/research-debug. No incomplete raw capture is claimed complete.
Native initialization entry helper invocation succeeded: snapshotsfc852d0c84ba4dc2a89f3214c4166966,462498d870b8451aaae5af2e42c15d27,fdddd8c2a0054468a7f8ae07ac4dd3ce,57e8094e29314c44a27463afacf16314 under the local Bridge geometry-research folder capture six temporary-node roots and111 entities. Same five prefab pointer gaps apply. Long interactive holds disturbed preview validation; these are diagnostic runs, not ordinary UI evidence. One read-only preview-state poll timed out while paused; inspected actual state instead of retrying any unknown mutation.
Automatic conditional capture failed to resolve this in the breakpoint condition. Return-offset breakpoints did not provide exit captures; step-out returned INVALID_ARGUMENT. Removed all such probes. An unconditional CalculateEdgeGeometryJob entry breakpoint did fire, but helper invocation there timed out because the main thread remained in native code; no helper ran. Flatten entry also fired. All debugger breakpoints were removed and game resumed. These results expose a limitation of main-thread-routed debugger method invocation at worker-job stops, not an established geometry divergence.
Next implementation: schedule-boundary tracing around the original native jobs, completing explicit dependency handles before pre/post capture. Preserve actual query/list membership and native job execution, and mark added synchronization as diagnostic. Do not assume the raw worker-entry captures are atomic merely because the debugger stopped a worker.
