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

## Existing mailbox suite on the installed SDK

Added opt-in BridgeTestFramework property; its default remains net10.0. The local
SDK8 build passes with -p:BridgeTestFramework=net8.0. `dotnet run` still chose the
default net10 launch path, so build and execute the resulting DLL explicitly.
The first DLL run passed the core/recovery suites then stopped because pwsh was
absent. The existing CIAB_TEST_PWSH override set to powershell.exe allowed the full
suite to pass, including 23 recovery checks and 10 real PowerShell mailbox checks.
Full output: 2026-09-30-mailbox-tests.log. All use isolated fake mailboxes.

```powershell
dotnet build tests/MailboxTests.csproj -p:BridgeTestFramework=net8.0 "-p:GamePath=$gamePath"
$env:CIAB_TEST_PWSH = (Get-Command powershell.exe).Source
dotnet tests/bin/Debug/net8.0/MailboxTests.dll
```

Corrected stale activation instructions: controls reset unless the user opted
into remembering them. No control preference was changed by this sprint.

Added an explicit non-fetching referencing.Registry to input/output validation,
covering nested $id scopes as well as direct external $ref rejection. Eighteen
adapter tests pass. This uses the installed validator API; no external resource
retrieval or agent configuration changes are needed.


Bounded review remainder: the prototype injects reserved `_bridge` metadata into
provider input schemas. Closed allOf-style compositions may reject that extension;
a nested payload/envelope revision deserves focused compatibility tests before a
public SDK promise. Documented this limitation instead of silently rewriting
arbitrary schemas. No specific consumer schema is hardcoded in the adapter.

## Sprint handoff

Final offline results: 18 adapter tests, 13 C# provider contract checks, complete
existing mailbox suite (including 23 recovery and 10 real-shell client checks),
and actual synthetic MCP stdio all pass. Hardened adapter also passed an actual
read-only MCP discovery/state call against the running consumer after the final
live suite. Consumer-specific cases and evidence remain in its repository.

No native bridge source was changed this sprint. No deploy/restart is needed for
Python adapter changes. No host config, control preference, publication or remote
Git operation was performed. Next bridge product priority is the generic MCP
payload envelope/schema composition boundary plus a small independent developer
example and packaging/compatibility checks. Keep release versioning independent
from any consuming mod; defer broad refactoring until this contract is clearer.
