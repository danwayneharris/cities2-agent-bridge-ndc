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
