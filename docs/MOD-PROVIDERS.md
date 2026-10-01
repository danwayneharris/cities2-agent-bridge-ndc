# Mod provider protocol 1 (experimental)

The bridge has no dependency on individual mods. A participating mod optionally
exports a public static `CitiesBridge.ProviderV1` type in its own assembly with:

- `public static string DescribeV1()`
- `public static string InvokeV1(string command, string argumentsJson, string contextJson)`

No reference to the bridge DLL or common contract DLL is required. Normal mod use
must work without the bridge. Mod, bridge, and external adapter release versions
are independent; protocol 1 is the compatibility boundary, not any package version.

Describe returns a JSON object with protocol:1, stable namespaced id, nonempty
version string, and commands. Each command has name, description, explicit boolean
readOnly, and object inputSchema/outputSchema. The provider version identifies its
API, not necessarily its product release. Discovery computes a descriptor revision;
callers must return it when invoking. IDs and command names match
`^[a-z][a-z0-9_.-]{0,63}$`. Limits: 64 providers, 128 commands per provider, 64K
characters per description, 1M characters per result. Mailbox input remains 16KB.
Schemas describe the API; handlers remain responsible for input/domain validation.

`list_providers` returns providers, complete and errors without requiring a city or
automatically pausing. Presence is not readiness. `invoke_provider` accepts:

```json
{"provider":"example.routes","revision":"descriptor SHA256 from discovery","command":"inspect","args":{}}
```

Invocation runs on the game thread and currently requires a loaded paused city,
even for read-only provider calls. Writes also require controls enabled, no STOP,
and no bridge construction/batch operation in progress. Provider handlers must
validate their own tool conflicts, entity identities, readiness and stale revisions.
Context contains protocol, bridgeSession and citySession; do not retain entities
across sessions. Return a JSON object; accepted work is not completion. Long jobs
must return domain operation identities and provide explicit polling commands.

Describe must be deterministic, cheap and side-effect-free, without requiring a
loaded city. Discovery is cached per assembly and reset on city preload/disposal;
newly loaded assemblies are discovered on subsequent calls. Duplicate provider IDs
are disabled, malformed providers appear in errors, and wrong descriptor revisions
reject before execution. Unloaded/disabled optional integrations must reject from
their own handlers. A discovered assembly is not proof its mod is enabled/healthy.

The endpoint name is an explicit opt-in convention, not arbitrary reflection access.
Only its published commands are callable. In-process mods are trusted code: readOnly
is a contract, not a security sandbox. Do not label a pausing or mutating query read-only.

Old mod-specific mailbox commands were removed intentionally. Callers migrate using
provider-owned command descriptions. The bridge ships no aliases for particular mods.
Native game junction queries remain generic game operations. Historical session notes
and captures may describe the prior interface; they are not the current API contract.

Verification: generic registry tests with two unrelated toy providers; bridge compile
and API inventory check. Live discovery and a NetworkTools rail-merge preview/Apply regression passed on
2026-09-30 after a provider-owned activation-order fix. This is one consumer/case,
not universal provider compatibility. See the external [MCP adapter](../adapter/README.md).

## Minimal developer example

See [Hello Bridge](../example/README.md) for a standalone read-only mod, build instructions,
and discovery/invocation walkthrough. It needs no bridge assembly or submodule.
