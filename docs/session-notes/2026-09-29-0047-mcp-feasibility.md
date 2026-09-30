# MCP feasibility research — 2026-09-29 00:47 PDT

Documentation-only review of the current bridge transport, agent client, recent Network Tools adapter and MCP architecture options. No mailbox requests, game actions, code/configuration edits, package installs or builds. Preserve the pre-existing untracked autonomous-sprint session note.

## Findings

- Reviewed bridge.ps1, Mailbox/BridgeTick/Mod dispatch, new NetworkToolsAdapter, current RememberControl/STOP semantics, command docs, build constraints and recovery tests.
- Verified current primary MCP transport/tools/tasks specifications and official C# SDK project targets. Recorded protocol-version differences rather than assuming old handshake semantics.
- Recommended an external stdio MCP adapter over the existing mailbox first; embedded HTTP is feasible in principle but has significantly greater integration cost.
- Documented schemas, side-effect annotations, replay/unknown-outcome semantics, multi-agent ownership, pagination, cancellation and phased validation.
- Wrote ../MCP-FEASIBILITY.md. No tests executed and no live mailbox, game, settings or code changes. Preserve pre-existing 2026-09-29-0030-autonomous-sprint.md.
