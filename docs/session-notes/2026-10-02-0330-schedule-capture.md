# Scheduling-boundary instrumentation — 2026-10-02 03:30 PDT

Continue from 45e82bc after real worker-breakpoint capture exposed main-thread
invocation limitations. Implement opt-in, bounded wrappers at GeometrySystem's
existing native scheduling call sites. Original jobs still execute through their
original schedulers. Added completion barriers are explicit diagnostic changes.
No speculative production geometry fix. All other agents remain paused by Dan.

The patch must be hash/call-site gated, independently owned, and inert when not
armed. Capture failures must be reported without skipping native job execution.
Borrowed native arrays/lists/maps must never be disposed by the capture. Resolve
deferred arrays only in the boxed capture copy using the actual schedule list.

Implemented an optional GEOMETRY_RESEARCH_SCHEDULE_TRACE build. Ordinary builds
retain no Harmony dependency. The research build references the already installed
Harmony 2.2.2, gates the Game.dll hash, and replaces exactly five scheduling calls.
Controls, city-session identity, pause, managed/Burst-disabled launch and bounded
1-4 passes are checked before capture. Native jobs and scheduler arguments remain
unchanged. Explicit dependency completion changes execution timing and is recorded.

Compile passed with the two existing obsolete-updater warnings. The first capture
test failed because it expected a literal first command in the status-only expression.
Corrected that assertion to inspect membership for all four research commands.
Both capture test scripts now pass (29 installed component/buffer contracts;
19 capture helpers inspected). Archetypes serialize public component type names
and stable hash, with disposal limited to their own temporary component-type array.

NT NativeReplay ran the actual compiled transpiler over calls decoded from installed
GeometrySystem.OnUpdate. All five replaced calls preserve closed parameter/return
types and generic arguments. Removing or duplicating one call is rejected (4/6).
Report: NT artifacts/offline-research/schedule-transpiler-01.json. This is an offline
rewriter test, not evidence that Harmony patching or live capture works yet.
