# Save startup option — 2026-09-28

Installed GameManager.ParseOptions at extracted line ~405 accepts startGame= and
parses Colossal.Hash128; AutoLoad at ~1241 distinguishes saved games from maps and
calls Load(GameMode.Game, Purpose.LoadGame, asset). This makes a new bridge load
command unnecessary for startup loading. Added optional exact SavePath to external
helper, reading its adjacent .cid and constructing a single validated argument.

Located bridge tes - rail broken and working3.cok uniquely under user Saves;
asset ID 22caccf2edcc1c399e11baedc6fa2ec4. Inspection-only helper output validates it.
No file mutation, game launch/load, or shutdown occurred. During observation the
user's process changed from 19644 to 40080 and heartbeat reported loading=true;
left this process alone. Runtime playset retention and loading remain unverified.
Source extraction kept outside repo; no game source committed.
