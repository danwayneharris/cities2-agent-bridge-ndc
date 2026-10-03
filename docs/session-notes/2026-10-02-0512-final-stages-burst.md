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

a26e58e deployed after checkpoint 52cc3407b0134edfa019f64b834789aa, graceful close,
and exact-baseline launch with Burst enabled. Four full-stage captures retained:
preview (24 files), Apply (36), linear control (24), arch held-out (24). The first
preview exceeded 15 s client deadline but recovered unchanged after heartbeat
resumed; tracing disarmed. NT now uses 45 s for these bounded research calls.

Historical 1.844445 m discrepancy reproduced with Burst enabled. Offline permanent
Initialize/Edge/Flatten map agree exactly, but Finish fails to apply one map entry
in native output. Linear and arch also diverge first at Finish. Preview passes all
eight stages. This makes dictionary substitution a concrete suspect: enumeration
does not establish keyed lookup equivalence. No claim of a Burst compiler defect.

Add bounded native hash-map bucket/chain copies and managed TryGetValue probes for
each stored key at completed boundaries. Use public GetUnsafeBucketData, validated
capacity/mask/index/cycle bounds; never write or dispose borrowed storage, serialize
addresses, or read unoccupied value slots. Research-only unsafe compilation is
needed for this public pointer API. This new addition is not yet deployed/validated.

Bucket-capture compile passes with only the existing updater warnings. It remains
undeployed; retain current runtime a26e58e until a checkpointed graceful restart.
