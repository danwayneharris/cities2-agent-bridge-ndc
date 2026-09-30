# 2026-09-29 0030 - Autonomous regression and split-point sprint

Verified merged main checkpoints: NT 2180aec, bridge f0d6882. New local dan/autonomous-regressions branches start from these. Existing NT UI package-lock change remains user-owned and unstaged. No publishing authorized.

Priorities: reusable live regression runner; fresh baseline discovery and independent Apply verification; representative toy networks; tangent-continuous pinned split nodes; bounded longer-route and slope architecture investigations. Preserve baseline saves, use unique checkpoints, stay paused, no force killing. Record failures as well as successes and separate offline/native/permanent/visual confidence.

Added nt_split forwarding, client/catalog/docs entries. Build passed and verify-api reports 61 reachable commands. No deployment or live split-command test yet. NT exposes eligible intermediate nodes and token-guarded mutation; hard planar split constraints are active even at zero strength. Existing controls/STOP dispatch remains in force.

## Sprint handoff - live deployment and verification

The earlier not-deployed note above describes the initial commit only. The bridge
was subsequently deployed and nt_split was exercised through the paused toy-save
regression runner. Rail two-split tests at strengths 0, 0.5 and 1 and a road two-split
test at 1 passed native preview and independent permanent Apply checks. One rail
single-split case retained a strict 3 mm outer-junction center-drift failure while
the actual split pin, tangents and connections passed. No human visual claim.

The existing native build_road commands also created a separately saved one-way
slip-lane fixture. Straight-road extension consolidated a degree-two node in the
first attempt; the client detected the unexpected topology and stopped. A revised
placement order successfully built the fixture. The NT slip regression passed.
Train prefabs remained locked, so the non-merging crossing was not constructed;
no unlock bypass or control-policy change was added.

Final verify-api.ps1 passes its installed native adapter checks and the 61-command
client/dispatcher/catalog contract. Detailed captures and per-case confidence live
in the sibling NetworkTools repository's NetworkTools.docs/session-notes and its
2026-09-29-sprint-report.md. Neither repository was pushed or published.

A unique final save checkpoint completed and its archive was verified:
CitiesIIAgentBridge-sprint-handoff-slip-applied-20260929-100209-cddaccc6.cok.
The game remains paused; original and generated slip baseline hashes are unchanged.
