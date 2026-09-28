# Topological preview resolution — 2026-09-28

Added pure C# PreviewJunctionResolver. It intersects endpoints of uniquely mapped
preview edges for the entire original incidence set, explicitly rejecting absent,
duplicate, incomplete and ambiguous mappings. Retained original-match observations;
connectedSnapshot separately follows the resolved temporary replacement node.

Actual resolver compiled/executed on saved 79436 capture: resolves 54183:3, not
54185:3. Missing-edge, duplicate, incomplete, two-common-endpoint, version mismatch,
reversed ordering and duplicate expected-edge cases pass. Bridge build and native
read-only API checks pass. No deployment or game query this step. New native snapshot
path remains live-unverified; validationReady false.

Potential conservative limitation: split preview edges or partial reconstruction
produce an ambiguous/missing result. Do not infer identity from matching positions.
Fresh capture must prove generated junction lanes and preview lifecycle next.
