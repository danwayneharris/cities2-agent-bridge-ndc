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
