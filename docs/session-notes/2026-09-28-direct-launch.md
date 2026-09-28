# Direct launch investigation — 2026-09-28

Installed launcher config identifies Cities2.exe, empty standard exeArgs, Steam
platform. Its alternative no-code-mod mode passes --disableCodeModding; our helper
never supplies that flag. Runtime process confirms the same executable. Avoid
recording/forwarding ephemeral launcher session-token command-line arguments.

Implemented start-game.ps1: defaults to inspection; -Launch opt-in, ShouldProcess,
installed-config validation, working directory, refusal if any Cities2 is running.
No installation/settings changes, no kill or load action. Parser check and live
existing-process refusal passed. No launch executed. Current bridge ping succeeds,
controlEnabled=false, so a checkpoint cannot be requested and existing city state
is not established safe to discard. User confirms reduced test playset is default;
preservation on direct startup remains an acceptance check, not an assumption.

The direct executable path bypasses the launcher invocation itself. Native services
could still require Steam; actual startup/main-menu/mod-playset behavior remains
unverified. Next supervised test: enable controls, verify checkpoint completion,
close safely, invoke helper, verify test playset/mod loading, then manually load toy
save. No need to edit Steam launch options or force-kill the current session tonight.
