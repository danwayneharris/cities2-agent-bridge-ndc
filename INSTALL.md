# Install Cities II Agent Bridge

**First: you must own and install Cities: Skylines II.** The bridge does not include the game and does not work with Cities: Skylines I. If the game is not installed, stop setup and ask the user to install their own copy first.

If reading this on GitHub, download the community ZIP and its `.sha256` file from https://github.com/FTPAiYT/cities2-agent-bridge-ndc/releases/latest. Compare the ZIP's SHA-256 to that checksum before extracting. Use the packaged release, not GitHub's automatically generated source-code archive. The repository's `artifacts` directory is intentionally absent; the release contains the compiled DLL and build manifest.

## Prompt to give your agent

> Read INSTALL.md and AGENTS.md in this extracted package. Verify the files and my game compatibility, explain the installation, then install the bridge if I have authorized installation. Do not close or restart my game, change a save, or start playing without my permission.

## 1. Inspect and verify

Extract the ZIP into a writable folder (not inside the ZIP viewer). PowerShell 7 is required. No administrator access or .NET SDK is needed to use the supplied DLL. Do not change system execution policy or disable security software as part of installation. If scripts are blocked, explain the exact restriction and let the owner review/unblock trusted downloaded files.

From the extracted folder:

```powershell
pwsh -NoProfile -File .\verify-package.ps1
```

Find the actual game directory. Ask the owner or use their Steam Library > Cities: Skylines II > Manage > Browse local files. Do not assume the game is on C:. It must contain `Cities2_Data\Managed\Game.dll`.

```powershell
pwsh -NoProfile -File .\install.ps1 -GamePath 'D:\SteamLibrary\steamapps\common\Cities Skylines II' -CheckOnly
```

Replace the example path. CheckOnly reads files and prints the destination; it neither installs nor communicates with the game. A fingerprint mismatch means this release is unverified against that game build: stop and report it; do not silently bypass the check.

The 0.4.3 prebuilt package targets the Windows Steam 1.6.2f1 assembly. Game Pass / Microsoft Store and other builds may have a different fingerprint. If the owner wants a source rebuild, follow [DEVELOPMENT.md](DEVELOPMENT.md) to generate a separate package against their installed assemblies. Rebuilding needs a .NET SDK and does not establish runtime compatibility. Do not change the downloaded release's hashes to force installation.

## 2. Install

If the game is open, ask the owner to save and close it. Never terminate it yourself. With installation authorized and the game closed:

```powershell
pwsh -NoProfile -File .\install.ps1 -GamePath 'D:\SteamLibrary\steamapps\common\Cities Skylines II'
```

The installer copies only `CitiesIIAgentBridge.dll` into the current user's `AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\CitiesIIAgentBridge`. Existing bridge DLL/PDB files are backed up under that folder before replacement; other mods and saves are untouched. Custom destinations are supported with `-Destination`, but only use one intentionally selected by the owner. Copy hashes are checked. Installation does not launch the game.

## 3. First connection

Have the owner launch the game and load a disposable test city or a backed-up save. Confirm the Cities II Agent Bridge section appears in Options. If it does not, inspect the game's log and installed path; do not claim a successful runtime install merely because file copying worked.

Pause the city manually. Read-only connection check:

```powershell
pwsh -NoProfile -File .\bridge.ps1 ping
```

Before gameplay, agree on duration, construction/spending scope, save behavior, and whether native computer control is allowed. Then have the owner enable **Options > Cities II Agent Bridge > Allow local bridge controls** in the loaded city. It resets off on city changes by design.

```powershell
pwsh -NoProfile -File .\bridge.ps1 pause_for_analysis
pwsh -NoProfile -File .\bridge.ps1 get_capabilities
pwsh -NoProfile -File .\bridge.ps1 save_checkpoint -ArgsJson '{"label":"before-agent-session"}'
```

Poll `get_operation` using the returned ID until `complete`; a queued save is not a verified save. Never reuse IDs from documentation or another city. Use docs/COMMANDS.md to discover current entities, positions and unlocked prefabs.

## Stop, resume and remove

`pwsh -NoProfile -File .\bridge.ps1 stop` writes a STOP latch. It disables further bridge controls, interrupts supported pending work at the next check and pauses a bounded simulation. It does not undo construction or stop a separate computer-use tool. If the bridge is unresponsive, pause manually in the game.

To resume after an intentional stop, obtain fresh permission, remove only `%LOCALAPPDATA%\CitiesIIAgentBridge\STOP`, and re-enable the in-game checkbox. Never clear the latch automatically.

To uninstall, have the owner save and close the game. Remove only the installed `CitiesIIAgentBridge.dll` (and any bridge PDB from an older installation). Do not delete the Mods folder or game saves. Existing bridge-created game objects are ordinary game objects, but loading a save after removal has not been independently tested for this community release; keep a backup. Optional journals/mailbox files can be retained for troubleshooting or removed separately at the owner's request.

## Troubleshooting

- Temporary mailbox file lock: version 0.4.1 retries publication and resumes when the lock clears. STOP and bounded simulation deadlines are still checked. It never turns disabled controls back on.
- Response timeout: a command may already have run. Inspect the response for the original request ID and the city state before deciding what to do; do not resubmit a mutation blindly.
- Stale heartbeat: verify the game and mod are running; never replay a timed-out construction request blindly.
- Controls disabled: enable the option after loading the city; check for an intentional STOP latch.
- Locked prefab/native placement error: inspect current unlocks and native preview results; don't override game state.
- Installation blocked because Cities2 is running: ask the owner to save and close it.
- Sharing diagnostics: review logs first. Do not upload city data or chat transcripts automatically.

## Local Network Tools diagnostic branch

Our local dan/junction-snapshot branch adds junction and preview queries not present
in the upstream community release. Obtain this branch from the maintainer; publishing
a separate fork is deferred. With a .NET SDK, build without installing:

```powershell
$game = [Environment]::GetEnvironmentVariable('CSII_INSTALLATIONPATH', 'User')
.\build.ps1 -GamePath $game -OutputDirectory ./rebuilt -CommunityRelease
.\tests\JunctionApiTests.ps1 -GamePath $game
.\verify-api.ps1
```

For a local DLL installation, follow the explicit hash-checked backup/copy procedure
in sibling [NetworkTools bootstrap](../CS2-NetworkTools/BOOTSTRAP.md#junction-development-dependencies)
(path relative to repository root). Do not run the release installer against an
unpackaged rebuilt DLL or change original package hashes. Close the game before
copying, enable the local mod in the toy playset, load/pause manually, and leave
controls off for get_junction_snapshot/get_junction_preview. File checks are not
live-query validation. The game version and compiled API contracts must match.

## Coding-agent plugin setup

Our development workflow uses the skills-only cs2-modding plugin. It does not install
the game, SDK, bridge, or debugger. It is optional for building/running the mod.
Using a Codex CLI that supports plugin commands:

```powershell
codex plugin marketplace add CitiesSkylinesModding/agents-plugins
codex plugin add cs2-modding@csmodding
codex plugin list
```

Start a new agent session and confirm cs2-modding skills are available; there is no
MCP server for this knowledge plugin. See the
[marketplace installation guide](https://github.com/CitiesSkylinesModding/agents-plugins#install)
and [official marketplace guidance](https://developers.openai.com/plugins/build/plugins).
The separate unity-devtools and coherent-gameface plugins are not prerequisites
for our mailbox diagnostic workflow. Plugin installation does not authorize game edits.

## Starting directly without the launcher (experimental local helper)

The installed CS2 1.6.2f1 Launcher/launcher-settings.json specifies
`../Cities2.exe` with an empty standard exeArgs array. `start-game.ps1` validates
that configuration and can invoke that executable directly without editing Steam
launch options, deleting launcher files, or changing saves/configuration:

```powershell
.\start-game.ps1                  # Inspect only
.\start-game.ps1 -Launch -WhatIf  # Preview the action
.\start-game.ps1 -Launch          # Start only if Cities2 is not running
```

Uses user CSII_INSTALLATIONPATH or explicit -GamePath. It refuses unfamiliar launcher
configuration and duplicate game instances. No auto-continue/load flags are supplied.
A started process is not proof that Steam services, mods, or the menu are ready.
Direct launch has not yet been exercised: the existing game contains a city and
bridge controls are off, so no verified checkpoint/shutdown was possible.

The user's default is currently the reduced test playset. The helper does not
select or modify playsets. No standard playset argument appears in the installed
launcher configuration; this does not prove that direct startup preserves active
mods. Verify the intended playset/mods after the first launch, before loading or
saving a city. Steam may need to be running/authenticated; no Steam startup settings
are changed by this helper. Do not forward launcher session tokens from an existing
process. Plain Steam launch may still use its configured launcher entry.

For a restart, first confirm the disposable city and enable bridge controls manually,
request save_checkpoint and poll get_operation to successful completion. Record the
checkpoint name. Prefer graceful shutdown; do not force-kill with an unverified save.
Automatic load/restore and save-and-stop orchestration are not implemented here.

### Explicit save loading at startup

The installed Game.SceneFlow.GameManager.ParseOptions supports `startGame=` and
AutoLoad resolves its Colossal.Hash128 through AssetDatabase.global, dispatching
SaveGameData/SaveGameMetadata to Purpose.LoadGame. The helper accepts an explicit
existing `.cok` file, validates its adjacent `.cok.cid` (32 hexadecimal digits), and
passes only `--startGame=<id>`. It never picks a save by fuzzy name or loads by default:

```powershell
.\start-game.ps1 -SavePath 'C:\path\to\your test save.cok'          # Inspect
.\start-game.ps1 -SavePath 'C:\path\to\your test save.cok' -Launch  # Explicit load
```

This extends the earlier no-auto-load helper: without SavePath it still starts at
the menu. Source inspection establishes option support, not successful loading in
this environment. Verify the active playset before using SavePath; loading can
resume simulation according to game/save settings. No automatic pause is promised.
The named test save was found uniquely and its identity validated read-only. No
restart or load was executed during this investigation. The asset must be indexed
by the game's asset database; existence of a file alone does not establish that.

### Live test result: direct launch disabled

The direct executable experiment failed on this installation: Player.log reported
platform-service initialization failure followed by fatal asset-database errors
and repeated uninitialized-world exceptions. No fresh bridge heartbeat appeared.
The failed process was stopped after a graceful-close request failed; no city had
been loaded. The helper now permits inspection only and rejects -Launch until a
Steam-context launch path is verified. Earlier launch examples document the
experiment, not a currently supported restart workflow.

Steam's ordinary Play action still opens the Paradox launcher (user confirmed).
A launcher-bypass configuration preserving Steam's environment may work, but was
not configured or verified. Do not rewrite Steam settings, spoof launcher tokens,
or disable platform services to make this experiment pass. Native --startGame
support is established from source; actual loading/playset retention remains untested.

Control persistence is deliberately disabled in Settings defaults and Mod.OnPreload.
A scoped test-session authorization would need to bind to the actual loaded save
identity, expire, and honor STOP and city transitions. It is not implemented; do
not remove the reset simply to automate restarts. Manual checkbox activation remains
necessary for bridge checkpoints on the currently installed version.
