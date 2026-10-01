# Hello Bridge: a minimal standalone mod

Start with [Mod.cs](Mod.cs) (game lifecycle) and [ProviderV1.cs](ProviderV1.cs)
(the optional automation entry point). The only command, `greet`, returns a greeting.
It does not change the city, add UI, create ECS components, or install hooks.
Without the bridge, the mod still loads but deliberately has no visible effect.

There is **no bridge DLL reference, submodule, NuGet SDK, or shared contract DLL**.
The exact `CitiesBridge.ProviderV1` type and method signatures form the convention;
JSON is the boundary. Newtonsoft.Json is supplied by the game. Copy this folder to
start experimenting, or add its provider file to a mod created with the official
CS2 modding template. Change `example.hello` to your own stable namespaced ID.
Keep the endpoint type name unchanged. Duplicate provider IDs are rejected.

## Build without deploying

Requires an installed CS2 game and a .NET SDK on PATH. From this repository root:

```powershell
.\example\build.ps1 -GamePath 'D:\SteamLibrary\steamapps\common\Cities Skylines II'
```

Omit GamePath if your CS2 toolchain configured CSII_INSTALLATIONPATH. The script
writes only `example/bin/HelloBridgeExample.dll` and compiler arguments. This tiny
managed-only example needs no Unity code generation; use the official toolchain
for real ECS/Burst mods. It is a learning sample, not a publishing template.

With the game closed, copy **only HelloBridgeExample.dll** into a separate local
mod folder under your CS2 user-data Mods directory, such as `Mods/HelloBridgeExample`.
Enable the example and your compatible locally built bridge in the test playset.
Do not copy the game's reference assemblies or put the example into the bridge's
mod folder. Launch a disposable toy save and pause. Building does not install it.

## Discover and call

Run from the bridge repository root:

```powershell
$catalog = .\bridge.ps1 -Command list_providers | ConvertFrom-Json
if (!$catalog.ok -or !$catalog.result.complete) { throw 'Discovery incomplete' }
$provider = @($catalog.result.providers | Where-Object id -eq 'example.hello')
if ($provider.Count -ne 1) { throw 'Example provider unavailable or ambiguous' }
$arguments = @{ provider='example.hello'; revision=$provider[0].revision;
    command='greet'; args=@{ name='Dan' } } | ConvertTo-Json -Depth 8 -Compress
.\bridge.ps1 -Command invoke_provider -ArgsJson $arguments
```

The successful result contains `{"message":"Hello, Dan!"}`. Discovery can succeed
before the mod is ready. Invocation requires a loaded paused city; this read-only
command does not require enabling bridge mutation controls. The handler independently
checks its own loaded flag and input constraints. OnDispose clears that flag.

DescribeV1 is deterministic and works while unloaded. The bridge checks descriptor
revision and dispatches on the game thread; your handler owns domain validation.
For mutations, declare readOnly=false and enforce your own state/entity/revision
checks too. Do not mark a command read-only merely to bypass controls.

## Verification and next reading

The standalone DLL compiles against the installed game. An offline test compiles
the real provider with the registry and models the loaded flag, verifying discovery,
greeting, invalid input and unloaded rejection:

```powershell
dotnet run --project tests/Example/Example.csproj "-p:GamePath=$gamePath"
```

The registry reference exists **only in that test**, never in the example mod.
This sample has not been deployed or verified in-game. See the full
[provider contract](../docs/MOD-PROVIDERS.md) and optional [MCP adapter](../adapter/README.md).
