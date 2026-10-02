# Managed finishing experiment — 2026-10-02

Integrated research through e2472d1 into dan/terrain-observation-sprint. The consumer
research reproduces an identity-dependent native finishing lookup mismatch. Add an
explicit, bounded `finishExecution: managed` scheduling-trace option to test original
managed Finish Execute on the same job data. This is a game-mutating execution
intervention, NOT read-only instrumentation or a production fix. Defaults to native;
controls, pause, city-session and maximum-pass guards still apply. Other stages and
unarmed passes retain their original scheduler. Build/test/live results pending.

Compilation passed with only the existing obsolete-updater warnings; native field/
component/helper checks passed, and the consumer harness verified eight scheduler
signatures plus missing/extra-site rejection. Deployed SHA256
70D14ED0705962498522559694903D935F67AEA2F21AD2C4A819D0C48EA519CE after verified toy
checkpoint and graceful restart, Burst still enabled. Both bounded preview and
permanent intervention captures replay through all eight offline geometry stages.
The five affected edge surfaces match exactly after Apply and directed lane identities
remain unchanged at the three observed junctions. This is evidence for the execution
hypothesis, not a persistent fix, broad compatibility or vehicle traversal.

Trace unpatched after each one-pass experiment. Final checkpoint verified:
CitiesIIAgentBridge-managed-finish-experiment-completed-20261002-142424-207b8a77.cok.
Wantagh remains paused, citySession0ab65c9f145047f693e031f2f667e6b1. Runtime NT04792dd.
Consumer-specific assertions/captures remain in NT; this Bridge option is generic.
Await Dan's scope choice before persistent runtime changes. No push/publication.
