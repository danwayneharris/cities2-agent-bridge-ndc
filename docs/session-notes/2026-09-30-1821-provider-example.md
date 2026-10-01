# Minimal standalone provider example

Added example/ with a separate IMod and exact-name ProviderV1 endpoint exposing
one read-only greeting command. It has no bridge assembly/project dependency;
input validation and loaded/disposed rejection belong to the example. The build
script references only installed game assemblies and writes ignored bin output.
Linked the walkthrough from README and MOD-PROVIDERS.

Validation: standalone HelloBridgeExample.dll compilation passed. The offline
example contract test passed discovery, unloaded rejection, read-only dispatch,
greeting output, invalid input and disposal guard using the actual provider source.
The test models the loaded flag; it does not prove native mod loading. No example
deployment, playset edits, game operations or publication were performed.
