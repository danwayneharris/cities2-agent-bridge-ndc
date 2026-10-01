# Bounded terrain debugger observation

The existing optional Unity helper on the preserved local debug-plugin-setup
branch was used against a paused CS2 1.6.2f1 toy city. UnityDevtools.Mcp 1.2.0
completed an actual stdio initialize/tools-list/status exchange; the game advertised
a debugger-enabled beacon and zero held suspensions. A separate read-only
Game.Simulation.TerrainSystem type lookup successfully attached and returned the
base RenderTexture, cascade RenderTexture and CPU height-array fields.

The short-lived MCP helper exited normally after the lookup. No breakpoint,
held suspend window, terrain mutation or gameplay control was introduced. Type
availability is not numeric terrain sampling or a visual test.

Installed source shows GetHeightData exposes the CPU representation obtained from
the network-adjusted cascade; the base height texture is separate GPU data. The
existing generic sample_terrain command should not be described as pre-network
terrain. A future base/deformed comparison needs explicit sampling provenance and
GPU/readback timing, not deletion assumed to reconstruct an original surface.

No bridge code, schema or product dependency changed in this sprint. Its main-based
worktree remains independent of the consumer's profile implementation. The older
local debugger setup commit e797920 was preserved and is not silently merged here.
