# Native offsets and terrain — 2026-10-02 04:20 PDT

Continue from Bridge 380eaee and NT 1d36d49. Raw offline edge execution is real but
has small coordinate mismatches. Same-runtime source/binary pure cuts agree. Add
a native CalculateOffsets probe at the completed entry scheduling boundary, plus
explicit native height-array captures for finishing. Keep operation bounded and
distinguish recomputed native helper outputs from actual worker-frame locals.

Terrain source dependency: TerrainHeightData holds heights/downscaledHeights,
resolution/downScaledResolution, scale, offset, hasBackdrop. TerrainUtils.SampleHeight
uses full-resolution bilinear samples or backdrop samples; no radius is guessed.
Archive complete ushort arrays with checksums, lengths and little-endian encoding.
Borrowed native storage is only read; no NativeArray disposal or ownership transfer.

Build passed with only existing updater warnings; 29 component contracts and
22 capture helpers pass compiled access checks. Runtime capture remains unverified.
Dan clarified that ~1 mm residuals are trivial unless they amplify. Stop pursuing
bitwise perfection; prioritize carrying computed state through finishing and
measuring final error/topology. Offset capture is supplementary; terrain enables
the required next stage. No new production behavior is introduced.

Deployed c803887 after completed checkpoint 74f4fe1abcc94af3bacdbb64308e2dee and
graceful close. Deployment/launch/checkpoint evidence is in artifacts/schedule-live
with terrain- prefixes. Current launched process was 44740; identifiers go stale.

Live captures succeeded: terrain-preview-02 (16 files), terrain-apply-02 (28),
single-edge linear control (16) and combined strength-0.5 variation (16). The latter
two were preview-only and their manifests live in NT artifacts/offline-research.
All tracing removed after each bounded pass. Finish now reports complete input,
including 16,777,216 height samples and 1,048,576 backdrop samples; content-addressed
binary archives deduplicate unchanged arrays. Native CalculateOffsets probes hold
one recomputed helper result per scheduled edge, explicitly not worker-frame locals.

NT executes all four stages with computed propagation; final residual remains
approximately 1 mm in these four captures. Terrain is actually sampled. Negative
tests confirm poisoned recorded intermediate outputs cannot replace computation.
Historical failing surface discrepancy remains unreproduced. An attempted ordinary
preview observation returned missing, so it cannot establish instrumentation effects.

A fifth capture, held-arch-six-01 (16 files), changes authored controls by up to
15.23 m and supplies the meaningful held-out geometry variation. Half-strength
Combined authored curves were unchanged and count only as equivalent-state evidence.
NT commit 3ac27d3 additionally executes both CalculateNodeGeometry iterations from
these existing captures; four cohort cases pass below 1.8 mm control error, with
ten pipeline checks. No additional Bridge code or live mutation for that extension.
