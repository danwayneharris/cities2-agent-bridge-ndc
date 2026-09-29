# Remembered controls implementation and deployment

Added RememberControl, default false, and FileLocation/AssetDatabase.LoadSettings
using the game's standard mod settings persistence. Retain AllowControl at preload
only when opted in. STOP and fault paths call RevokeControl, which sets Allow false
and invokes ApplyAndSave only on transition. Existing STOP file gates remain.
No settings are automatically enabled by installation. Both checkboxes must be
selected by the user. Changes apply globally across cities by explicit opt-in.

Source grounding: Game.Settings.Setting.ApplyAndSave invokes Apply and saves the
specific registered setting. NetworkTools' existing LucaModBase uses the same
LoadSettings pattern. No pinned shared code modified.

Build passed (two existing obsolete-updater warnings); native API/catalog checks
passed. Test harness targets net10 while installed SDK is net8: direct attempts
failed. Isolated net8 harness initially had a bad reference, corrected; recovery
suite reached 23 passing checks before client tests failed due to missing pwsh.
Full regression suite is NOT claimed passed. Native settings disk round-trip,
remember-on/off restart behavior and STOP persistence still need live validation.

User closed game; compiled DLL and installed Game.dll matched build manifest.
Previous installed DLL/PDB backed up, new DLL/PDB copied and DLL hash verified.
Game was not launched. Next: enable both options in UI, close/relaunch, inspect
controlEnabled, then validate turning Allow off and STOP behavior separately.
