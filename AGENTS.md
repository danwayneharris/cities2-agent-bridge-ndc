# Agent instructions

## Make the city their own

For a new city, ask the player what they want to call it and what theme or character they want. If they delegate naming, propose an original name that fits their vision. Do not impose a preset city name or plan. For an existing save, preserve its name unless the player asks to rename it. Use the chosen city name when referring to the city. Do not rename DLLs, namespaces or mailbox paths to customize a city.

First explain that the user must purchase and install **Cities: Skylines II**. This bridge includes no game and does not support Cities: Skylines I. Check for the installed game and required local tools before proceeding; do not imply that downloading these instructions supplies either the game or computer access.

Explain the in-game activation step before the first session: load the city, pause, then enable **Options â†’ Cities II Agent Bridge â†’ Allow local bridge controls**. Controls reset off on city load unless the user explicitly enabled Remember local bridge controls. Inspect live controlEnabled and rememberControl; never assume persistence or change these preferences without authorization. Installation alone is not activation or gameplay permission.

This package grants no authority over the user's computer or game. Obtain explicit gameplay permission and native computer-control permission before taking control. Installation permission alone does not authorize gameplay. Never close, kill, restart or launch the game without permission.

Read INSTALL.md and docs/COMMANDS.md. Verify the package first. Respect STOP; do not clear it without renewed permission. Keep analysis paused; use bounded simulation intervals, then inspect results. Do not query pause-producing analysis while an interval is intended to finish. Keep progress visible and report what completed, not merely what was queued.

For bridge 0.5.0, follow every `nextOffset` from building and asset queries until null while the city stays paused. Discard collected pages if `citySession` changes. Check `complete` and `errors` on service responses, even when the request succeeds. Asset discovery does not imply placement support: check `bridgePlacementSupported` and `locked`. Loaded assemblies do not establish working mod integrations; consult `get_capabilities`.

Start from live state. IDs and coordinates in examples are placeholders. Preserve a budget reserve, use preview/maxCost limits, and verify asynchronous operations. A failed batch leaves earlier work in place; inspect before retrying. Save a named checkpoint before major changes and verify completion. At the agreed end, save and leave paused unless instructed otherwise.

Optional journaling: `journal.ps1 start -Title 'City session'`, then `journal.ps1 note -Text 'Visible progress update'`, and `journal.ps1 finish`. Record observations, actions, results and concise explanations, never private reasoning. Transcript import requires a user-authorized, verified supported-format path; do not search other chats by default. Share no local records without permission.

The bridge is an interface, not an autonomous player. Some UI actions remain outside its command set. Be candid about unsupported or unverified behavior. Keep original package files intact so integrity verification continues to work; write session records in a separate directory or the generated journals/artifacts folders.

## District operations

Known 0.5.0 issue: named district creation/editing can apply successfully and then report an EntityCommandBuffer naming-notification error. Inspect a fresh atlas and the operation result before any retry. Never replay an uncertain mutation. Boundary edits without a name have completed normally. See RELEASE-NOTES.md.

Follow atlas/README.md for immutable snapshot pagination and export. Assignment readback does not prove service reach. Keep exported city data local unless the player authorizes sharing.

## Development docs

Read [junction snapshots](docs/JUNCTION-SNAPSHOTS.md) for the query contract and
[session notes](docs/session-notes/) for experiments and verification history.
Run documented commands from the repository root even when reading a doc under docs/.

## General Operating Philosophy

- At the beginning of each session, create a new .md doc in docs/session-notes/ that includes the \ date and time in the filename, and for each small incremental change during that session, document what the change was, whether it did or didn't work, or what was learned from testing it. Then commit that change to git along with the updated doc EVEN IF THE CHANGE DIDN'T WORK.  If the change was bad, manually revert (not git revert) and update the doc accordingly.  This is to ensure that all lessons are learned and we have a clear picture of our potentially messy journey.

## Optional mod automation

Read [MOD-PROVIDERS.md](docs/MOD-PROVIDERS.md) for the generic opt-in contract.
Individual mods own their provider APIs; bridge releases have no dependency on them.
