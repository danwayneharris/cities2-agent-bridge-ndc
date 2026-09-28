# Game launch v2 checkpoint

Separate branches created from NT PR#5 e82f033 and bridge PR#2 1392542.
Read the two new lifecycle/mod-control investigations. Corrected start-game.ps1
inspection to use internal SaveGameMetadata.cid rather than outer package CID.
The real toy save has distinct IDs, confirming this defect. Five fixture cases
pass: valid ID plus missing, ambiguous, malformed and oversized rejection.
Initial PowerShell 5 test lacked the Compression assembly; explicitly loading it
fixed the harness. Runtime launch is still disabled pending Steam-context testing.

Live bridge responds, but controlEnabled=false and the game remains running.
No city mutation, save, close, kill, restart, load, deployment or Steam settings
change was made. Unable to perform a verified checkpoint/restart/control loop
through the existing enabled capabilities. No arbitrary UI/reflection endpoint added.
Next: enable controls on the toy city and verify a unique checkpoint, or close the
game for the launch test. Then test correct metadata-ID delivery under Steam.
Save completion needs package verification; the new investigation found native
Save() return value alone is insufficient. Mod-control still needs a typed,
revision-aware update-thread adapter. Offline helper success is not live autonomy.
