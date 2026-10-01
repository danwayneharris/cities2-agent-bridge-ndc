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

## Persistent-client / MCP first attempt

Pinned official Python MCP SDK 2.2.0 and jsonschema 4.26.0 in a uv lockfile.
Implemented generic descriptor-driven tools and durable intent records before
mailbox publication. No mod-specific tool names appear in the adapter. The initial
11-test run failed three MCP assertions: SDK v2 exposes snake_case Python fields
although constructors accept wire aliases. Synthetic heartbeat tests also exposed
a fixture writer race; fix the fixture and surface thread failures instead of
ignoring them. This failed checkpoint is retained per project convention.

The existing mailbox suite's net10 target still defeated a CLI net8 override;
new standalone provider tests run on the installed SDK8 without changing that suite.

## SDK correction and transport evidence

Corrected MCP SDK 2.2 model attribute access to snake_case and made the synthetic
heartbeat writer serialize its replacement writes. Eleven adapter tests now pass.
An actual SDK client/server stdio handshake discovers and invokes an independent
synthetic provider successfully (adapter/stdio_smoke.py). No game was involved.

The retained 12-pair synthetic benchmark used a 250 ms mailbox cadence:
PowerShell subprocess median 475 ms, persistent Python median 227 ms. See
provider-client-benchmark-20260930.json. This isolates process startup plus the
synthetic transport; it does not measure Unity, active journaling or agent latency.

NetworkTools now consumes the generic Python client from its own runner. No
NetworkTools mapping belongs in this repository. Runtime provider discovery and
native preview/Apply still require live validation before claiming compatibility.

## Live consumer verification and release boundary

The paused toy NetworkTools rail-merge regression passed through list_providers /
invoke_provider on 2026-09-30, including native preview and independently inspected
permanent Apply. The first attempt exposed a consumer activation-order bug (saved
preferences overwrote its requested mode); the fix belongs in NetworkTools, not
bridge routing. This validates one live consumer, not arbitrary mods or traversal.
Evidence is retained in the consumer repository's provider-rail-merge-fixed-20260930
capture and report. No game algorithms were added to the bridge.

Removed remaining false-valued capability placeholders naming particular mods.
Final bridge source compiles and verify-api passes (56 commands). This metadata-only
cleanup is not deployed yet; the live tested DLL predates those removals. Added
adapter/README.md with optional MCP installation, scope and intent recovery.

The two products release independently. Provider protocol compatibility is the
boundary; consumer-specific activation, geometry, selection and tests remain with
the consumer. No Python/MCP requirement is introduced into either game's runtime.
