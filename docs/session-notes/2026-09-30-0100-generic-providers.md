# Generic provider sprint — 2026-09-30 01:00

Dan authorized autonomous work on new branches with complete removal of
mod-specific bridge code. First implement the provider boundary and migrate callers;
then measure the client path and prototype schema-driven MCP. No game is running
at initial inspection, so initial verification is offline, not native integration.
No legacy nt_* aliases will remain in the bridge. Historical docs/captures remain
as evidence; active instructions will describe the changed protocol.

## First implementation checkpoint

Generic discovery/dispatch replaces the named adapter and hardcoded command list.
Removed the external-mod assembly-name reporting allowlist as well. Native junction
queries have no third-party mod dependency and remain unchanged. Both components
can release independently; NT owns its optional Debug provider and command schemas.

13 registry checks passed, including two providers, duplicate rejection, revision
guards, mutation policy and discovery. Bridge compiles to non-deploying output and
56-command API verification passes. NT's first Compile failed due to fresh-worktree
missing NuGet assets; explicit restore resolved it and Compile now passes. Six
runner tests pass. Existing mailbox test run hit its net10 target with only SDK8
installed; investigate an override before changing project policy. No live tests.
