# Geometry research capture — 2026-10-02

Isolated branch dan/offline-geometry-capture from terrain-profile 9ffceb8. Main
Bridge and original investigation worktrees remain unchanged. Dan explicitly
requested extensive up-front instrumentation/capture for offline research.

First increment: a paused, main-thread, dependency-closure capture command. It
completes tracked jobs before reading and labels that synchronization intervention.
This is a post-barrier observation, not an internal pre/post-job trace. It preserves
full value fields for selected geometry components and ordered buffers, distinguishes
absent/uncaptured data, and reports frontier/error limits explicitly. No deployment
or live use yet. Native stage-boundary instrumentation remains required.

Compile-only build passed (two pre-existing obsolete updater warnings). The installed-type test rejected Game.Common.Hidden: the capture inventory used the wrong namespace. No live capture was attempted. Preserve this failed contract check before correction. The sandbox account blocked script execution; running the existing build under the toolchain owner succeeded without changing execution policy.

Corrected Hidden to Game.Tools.Hidden using installed-source identity. Build and all 29 installed component/buffer contracts now pass; compiled EntityManager call inspection allows reads and the explicit completion barrier only. This is compile/API validation, not runtime capture qualification. No game restart, deployment or mutation occurred. Capture command bypasses dispatcher auto-pause and rejects an unpaused city.
