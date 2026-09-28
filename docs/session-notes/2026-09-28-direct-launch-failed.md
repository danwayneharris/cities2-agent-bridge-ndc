# Direct launch failed — 2026-09-28

Confirmed no Cities2 process and Steam running, then launched Cities2.exe without
save arguments using helper. PID 55280 remained alive but no new bridge session.
Player.log early error: platform service integration failed to initialize; then
asset database fatal error and repeated uninitialized-world NullReferenceExceptions.
Requested CloseMainWindow (false), waited 3 seconds, stopped only PID 55280. This
was our newly launched no-city process, not the user's prior city session.

Disabled helper -Launch (inspection remains available) because the native executable
path alone is insufficient in this launch environment. No Steam configuration,
launcher file, save, mod playset, or bridge control setting was edited. User confirms
Steam Play opens launcher. A Steam-context bypass and actual save loading are not
verified; native startGame option is source-confirmed only. No confidence to claim
a safe unattended restart/load loop. Control persistence reset source reviewed;
scoped save-bound authorization remains design work, not implemented.

This failure is retained as an incremental commit per repository policy. Do not
repeat the same bare launch expecting it to work or advertise helper as ready.
