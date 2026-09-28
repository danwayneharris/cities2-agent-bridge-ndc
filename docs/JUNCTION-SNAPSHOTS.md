# Read-only junction snapshots (development extension)

`get_junction_snapshot` accepts a live network node identity: `index` and `version`.
Find it with `get_network`. Pause manually first. This query rejects an unpaused
city even with bridge controls enabled; it bypasses dispatcher auto-pause. It sends
no build commands, changes no entities, and does not enable controls. Keep controls
disabled for observational work. Installation and live validation are separate steps.

```powershell
# Replace the example values with a freshly queried node identity.
.\bridge.ps1 -Command get_junction_snapshot -ArgsJson '{"index":123,"version":1}'
```

The result contains schemaVersion, snapshotId, citySession, UTC time, simulation
frame, incident edge IDs, owners (junction plus incident edges), and native lanes.
Owners include stored curves, endpoint node positions, SubLane references and their
PathMethods. Lane rows include their curve, owner, start/middle/end PathNodes, and
TrackLane/CarLane flags, curviness, speed limits and access restriction IDs when
present. Updated/Created tags are reported as observations, not completion signals.

Each PathNode has a snapshot-local equalityId assigned using the game's actual
PathNode.Equals. Equal IDs within ONE snapshot mean exactly equal native PathNodes.
Do not compare these IDs between snapshots. The other fields use public accessors:
ownerIndex (no entity version), laneIndex, curvePosition and secondary. Do not use
rounded coordinates or owner index alone to infer connectivity. PathNode equality
is local graph evidence, not proof of permitted travel direction, pathfinding reach,
vehicle compatibility, switch behavior, or a completed network rebuild.

Bounds: 64 incident edges and 4096 unique lanes. Inspect complete and errors before
using the snapshot; unavailable/deleted/temporary references, absent required
buffers/curves/endpoints and limits are explicit. complete means the bounded read
succeeded, NOT that the junction is connected. No spatial search or pagination is
used. Far endpoint node positions are included, but their other incident networks
are outside scope. Snapshots are immediate observations, not retained atlas pages.

Build with build.ps1 into rebuilt, then run verify-api.ps1 and
`tests/JunctionApiTests.ps1 -GamePath <installation>`. The added tests read installed
Game.dll and compiled bridge IL with Colossal.Mono.Cecil; they check public fields,
accessors, allowed EntityManager read calls and the no-auto-pause guard. They do not
instantiate Unity ECS. Source inspection established PathNode.Equals compares its
private UInt64 m_SearchKey exactly; no private-field reflection is used in the mod.

Live acceptance remains pending: query a paused test junction, preserve its result,
reshape the merge, query again, and compare native track connections. Confirm the
query rejects while unpaused without changing speed. Verify a train traverses the
junction separately. No claim about the rail angle threshold has been established.

Reviewed game fingerprint:
AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A.
This is a local development extension of 0.5.0, not the upstream release binary.