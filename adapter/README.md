# Generic provider MCP adapter (development)

This optional external Python process discovers opt-in providers from the running
bridge. It has no knowledge of individual mods. The game mod does not depend on
Python or MCP; mod developers can use the mailbox/provider contract directly.

From the bridge repository root, with uv installed:

```powershell
uv sync --project adapter
uv run --project adapter python adapter/server.py --mailbox "$env:LOCALAPPDATA\CitiesIIAgentBridge" --intents "$env:LOCALAPPDATA\CitiesIIAgentBridgeClient\intents"
```

The server speaks MCP over stdio. Configure an MCP host to launch this command;
starting it in a terminal alone does not register tools with an agent. Provider
tools appear after discovery. `bridge_status` and `bridge_discover` remain available
when the game is absent. This prototype exposes provider commands, not the entire
native bridge command catalog. Use the generic Python client for native queries.

Invocation requires `_bridge` containing the current `session`, `citySession`, and
a unique 32-character hexadecimal `intent`. The intent is recorded before request
publication. Recover an uncertain operation with its original intent and identical
arguments; never generate a new intent merely because a mutation timed out. Recovery
does not republish a request. Keep intent records outside the live mailbox.

The game remains responsible for control permissions, STOP, pause requirements and
domain validation. Use one controller at a time. Discovery alone does not establish
that a provider is ready. Descriptor revisions reject stale catalogs. Product
versions and provider protocol versions are independent: a bridge release need not
track a mod release, and a mod without an enabled bridge keeps working normally.

Verification:

```powershell
uv run --project adapter python adapter/test_adapter.py
uv run --project adapter python adapter/stdio_smoke.py
```

These use synthetic providers/mailboxes and do not establish native game behavior.
The standard-library `bridge_client.py` can also be imported without installing MCP.
PowerShell remains useful for Windows bootstrap and build entry points; persistent
Python owns repeated transport and test orchestration. This path does not currently
emit the optional PowerShell journal events; retain response and intent captures.

Hardening: cancelled MCP calls retain serialization until their bounded mailbox
worker finishes. Failed rediscovery clears the callable catalog. Schemas may use
local fragment references (`#/$defs/...`), but external references are rejected;
validation never needs network access. Validation errors before dispatch do not
report a previous operation's request ID.


Current scope limits: MCP adds a reserved `_bridge` property to each input schema.
Simple object/property schemas and local definitions are supported; a provider
whose closed schema composition rejects extra root properties may need an adapter
schema-envelope revision. A provider argument named `_bridge` is rejected explicitly.
This is an experimental interface, not a promise to support every JSON Schema
composition. Native commands remain available through the generic Python client;
there is no full native-command MCP catalog or host configuration installed here.
