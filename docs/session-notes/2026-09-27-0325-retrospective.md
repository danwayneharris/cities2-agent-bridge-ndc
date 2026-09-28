# Retrospective: first local junction snapshot commit

Recorded 2026-09-28 from the commit and Network Tools session notes, not a new claim
that these checks occurred today. Branch dan/junction-snapshot; first commit dd0866b.

Added get_junction_snapshot, registered it in dispatcher/client/catalog/capabilities,
and added a native API/read-only check plus package allowlist entries. The query
captures a node, incident edges and generated lane identities; it requires a paused
city and bypasses automatic pausing. It does not enable game controls.

Installed Game.dll component metadata and PathNode.Equals IL were inspected.
An ambiguous Edge import initially failed compilation; a PathNode alias fixed it.
Compilation and verify-api/JunctionApiTests passed. Full .NET 10 mailbox tests and
PowerShell 7 packaging were not run with the available .NET 8 / Windows PowerShell.

The development DLL was later installed with owner authorization. On Sept 28 it
returned complete live snapshots with bridge controls disabled. A rail merge retained
three incident edges but lost two branch track connectors, leaving two mainline
connectors. These captures live in the sibling Network Tools repo, not this package.
Live query success does not establish train traversability or explain the native
curviness threshold; schema 1 lacks composition geometry and prefab limits.