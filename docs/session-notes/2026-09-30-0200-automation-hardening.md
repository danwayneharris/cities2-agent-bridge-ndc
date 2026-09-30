# Automation hardening sprint ? 2026-09-30 02:00

Starting checkpoint 9a0c33e, branch dan/automation-hardening. Independent generic
provider/MCP work; consumer integration stays outside this repository. User permits
checkpointed toy tests, graceful visible lifecycle and local commits only.

Initial review: cancellation of asyncio.to_thread can release the adapter lock
while the worker is still publishing/waiting. Also a schema failure can report the
previous call's request ID, and output schemas are not checked at discovery.
Address these before expanding live coverage. Existing session remains paused.


First fixes: cancellation now drains the bounded worker before releasing the lock,
including repeated cancellation; pre-dispatch validation errors clear stale request
correlation; output schemas are checked during discovery. Fourteen adapter tests
pass and the actual synthetic MCP stdio smoke passes. No game mutation was needed.
Updated stale command count to the verified 56-command inventory.

Second review fixes: a failed explicit rediscovery now clears the old tool catalog.
Schema references are restricted to local fragments, preventing validation from
fetching external URLs. Local $defs remain supported. Sixteen adapter tests pass.
Live MCP stdio read-only consumer discovery/state also passed; consumer-owned
script and capture live in the consumer repository. Road regression suite ongoing.

Transport validation now rejects falsey non-object arguments instead of silently
coercing them to {}, and rejects NaN/Infinity before publication. Seventeen adapter
tests pass, synthetic actual-stdio smoke passes, and all 13 C# provider registry
checks pass. Guards remain enforced in the bridge, not only in the Python client.
