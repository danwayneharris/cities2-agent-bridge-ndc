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

## First successful launch/load trial (19:26 PDT)

User closed the game and authorized the test. Added launch-v2-experiment.ps1:
explicit -Launch opt-in, refuse running game, require Steam process and existing
save, derive metadata ID, create an isolated temporary working directory containing
steam_appid.txt (949230), launch Cities2.exe hidden with --noSplash and --startGame.
No Steam settings or installation files changed; no helper launch guard removed.

Process22132 survived startup. New bridge session82107e1e6c4646088a0c6cd8940adcb7
reported loading=false/gameMode=Game. Native log explicitly reports starting from
bridge test - rail smoothing breaks merge junction, metadata ID
077cc7bce1b5119320c289201eb525ab. Read-only city query reports Wantagh,
population0, selectedSpeed0, controls disabled. Test mod initialization includes
NetworkTools, bridge, Anarchy, FindIt, MoveIt and UnifiedIconLibrary.

This verifies one launch -> exact saved city -> paused trial without launcher
interaction. Save/exit/repeat and mod-control remain unverified. Controls are still
disabled, so no checkpoint or mod mutation attempted. The temporary app-ID hint
is left in its unique working directory for inspection; no persistent Steam
launch options, game installation, or saved city files were edited.

## Visible-window retry

User reported the first run had no visible window. Enumerating CS2's UnityWndClass
found it hidden; restoring it made it visible, but the user subsequently reported
it hung and closed it. Earlier bridge evidence established loading and paused state,
not interactive responsiveness. The cause of the hang is not established.

At the user's explicit request, changed the experimental launcher's WindowStyle
from Hidden to Normal and retried the same metadata ID in a new isolated app-ID
working directory. Process28332 started. This supersedes the hidden-window launch
choice for this user-facing experiment. Responsiveness and load success still
require observation; do not describe this retry as a proven hang fix.
