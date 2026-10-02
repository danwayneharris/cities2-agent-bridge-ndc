# Final geometry stages and Burst context — 2026-10-02 05:12 PDT

Continue from d4c9420. NT 95a8ec6 records an ordinary, untraced preview/Apply that
also matches surfaces in the current Burst-disabled research session. Completion
barriers alone are therefore insufficient to explain missing historical failure.

Extend the bounded scheduling hook from five to eight native callsites: add
CalculateIntersectionGeometry, CopyNodeGeometry and UpdateNodeGeometry. Existing
generic collection supports their lookups and IntersectionData native scratch list.
Original jobs, schedules, dependencies and ownership remain unchanged; added
completion barriers remain an explicit instrumentation limitation.

Allow Burst only with explicit allowBurst=true at arm. Record BurstCompiler.IsEnabled
in trace and each capture. This records the setting, not proof a particular worker
was compiled/executed by Burst. No breakpoint or managed worker-frame assumption
is needed for schedule-boundary copies. Launcher gains explicit -EnableBurst;
default remains disabled for existing debugger workflows. Validate all eight native
call signatures and rejected missing/extra sites before deployment.

Build passes with the same two obsolete updater warnings. First offline transpiler
test preserved all eight signatures but the negative test still expected the old
4/6-site diagnostics. Correct the NT test to expect 7/9; do not weaken the guard.

Eight native scheduling signatures now pass offline comparison; seven/nine sites
are rejected. Evidence: NT artifacts/offline-research/eight-stage-schedules-02.json.
No live use yet; source committed before deployment.
