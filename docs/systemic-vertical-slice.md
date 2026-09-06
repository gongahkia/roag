# Systemic vertical-slice plan: Hearthford mill-race worksite

## Diagnosis

The current medieval route is technically sound but not yet a satisfying systemic sandbox. A fresh browser session can create a seeded world, choose a courier, walk the 18×8 Jomon deck, reach the quay tally, accept the ironwork case, load it into the hold, and later read a durable result. Those are implemented and reachable. In the inspected session, walking from the tavern to the tally consumed five action minutes and accepting the burden consumed none.

The loop is still too sparse because the only walkable map is the fixed deck. The public tally is a source-bound interaction embedded in that deck, not a generated settlement space; all other station interactions are readouts or courier switching. `frontier.ts` can deterministically commit, reveal, and materialize regions, while `persistent-person.ts` can describe local people, but neither reaches the browser. `market.ts` persists seeded local bands and links Hearthford ironwork to the existing handoff, but terminal presentation does not expose a market condition. Cargo is real only for the one freight receipt. Delegation, autonomy, social memory, initial institutions, detailed rendering, and simulation are strong contracts but do not currently produce a player-directed local situation.

This leaves the player without a prompt tactical decision, a generated obstacle, a local agent with authority, or a reason to revisit a changed place. The superseded `?prototype` route and its engine/scripts are reference material only; none of the medieval route imports them.

The upstream Creep bitmap face was independently verified in Chromium: it loaded but Canvas 2D painted zero glyph pixels. The runtime uses a local outline build generated from that same pixel strike, so the Creep glyphs render without a runtime font host. This is a recorded presentation correction, not gameplay progress.

## What to transfer from Caves of Qud

The useful lesson is not Qud's science-fantasy content or its adventurer model. It is the density produced when physical space, encounter facts, tools, agents, and history all constrain the same immediate decision. Qud's official description emphasizes physical simulation, fully simulated NPCs, faction allegiances, turn-based sandbox choices, and procedural plus authored world history. Jomon should reinterpret those qualities as follows:

| Qud quality | Jomon interpretation |
| --- | --- |
| immediate verbs | walk from vessel to quay marker; inspect; commit cargo or request institutional credit; return and observe the result |
| generated consequences | a seed-derived mill-race incident selects a visible quay worksite and local pressure, not a generic quest list |
| physical interaction | the existing ironwork lot is consumed only by a physical repair action at the worksite |
| agent authority | the named Hearthford Mill Lease changes from available to relieved or owed and controls the credit approach |
| tactical pressure | a visible blocked worksite creates a bounded time-versus-cargo-versus-obligation choice, with no wall-clock simulation |
| discovery and feedback | the map marker, contextual prompt, market condition, journal command, and return readout explain the same cause and consequence |
| progression | the vessel household retains cargo, obligation, and local market effects; no invincible or endlessly customizable hero is required |

Jomon must not copy Qud's setting, mutations, cybernetics, factions, creatures, terminology, maps, prose, quests, visual identity, or character-customization model. It will not add combat, a general currency shop, a universal reputation score, procedural NPC populations, generic route travel, or prototype dependencies in this slice.

The requested procedural-generation video has no URL in the supplied brief. No video technique can therefore be attributed or implemented. The selected slice uses only existing reproducible seeded-stream principles: its incident kind and quay anchor are exact functions of the world seed and are validated on load/replay.

## Selected slice

Implement one **Hearthford mill-race worksite** in the existing physical quay approach.

- A deterministic seeded incident (`sluice-jam` or `silted-intake`) selects one visible `&` worksite marker in the existing quay cells. This changes the reachable opportunity and is inspectable before commitment.
- The marker is beyond Jomon's gangplank. It has one physical contextual prompt and no menu-only counterpart.
- A named, persistent **Hearthford Mill Lease** institution owns credit authority. Its state is `available`, `relieved`, or `owed`; it is the local authority from the already-authored settlement profile, not a generated generic faction.
- The player has two different resolutions:
  1. **Fit ironwork** requires the existing delivered ironwork lot, consumes it, commits a time-bearing repair action, relieves the lease, and lowers the local ironwork pressure.
  2. **Take lease credit** commits a longer time-bearing action after the same freight delivery, preserves the cargo, and leaves a visible owed obligation with the lease and continued pressure.
- The existing public tally remains the material source: accepting its burden, physically delivering it to the cargo hold, and returning to the worksite makes both resolutions available. Fitting consumes the case; credit preserves it but records the obligation and costs more action time.

This is higher leverage than a new commodity list, generic shop, route framework, or isolated NPC contract because it connects the browser map, cargo, settlement handoff, market projection, causal history, persistence, and institution outcome in one player-facing loop.

## Architecture and invariants

- Add a renderer-independent, bounded worksite state/reducer with only the generated incident, exact quay anchor, Mill Lease state, resolution, world minute, and causal sequence. It owns no geometry, raw money, hidden person, route, timer, or free prose.
- Reuse the typed `time-bearing-action` causal command with a closed worksite action ID; the replay projection carries a worksite result only after resolution. A public reducer validates active courier, exact marker occupancy, delivered cargo, and the complete source world before advancing time.
- Make the current local market projection depend on the validated worksite result as well as settlement-trade state. It remains local bands, not a buy/sell API.
- Bump only the necessary mutable-state, market, causal replay, and terminal contracts. Add a strict read-only v18→current bridge that reconstructs the seeded unresolved worksite and validates it; retain existing older bridges. Corrupt, forged, or replay-inconsistent records remain unavailable and are never overwritten while read.
- Overlay the generated worksite marker and prompt into the shared terminal model, then carry the same facts through the detailed-renderer adapter. Canvas input remains an adapter only.
- Accepted repair/credit actions explicitly advance the action clock; inspection, unavailable choices, blocked movement, and Escape remain zero-time.

## Acceptance criteria

1. Same seed and state produce the same incident, marker, options, state transition, market bands, and replay projection.
2. A browser player can leave Jomon, see the generated marker, inspect it, choose either resolution through keyboard input, and receive terminal feedback.
3. The repair branch requires and consumes the actual ironwork cargo; the credit branch retains it and records the Mill Lease obligation.
4. Both branches advance their documented time costs, append/replay a typed causal command, survive save/load and checkpoint compaction, and visibly change the worksite and local market/institution result on return.
5. Forged anchor, cargo, institution state, market state, command, version, or save record fails closed; legacy v18 worlds upgrade read-only.
6. Focused unit/integration tests, same-seed reproduction, replay/checkpoint, save/load/migration, detailed-terminal parity, real browser keyboard playthroughs for both branches, TypeScript, production build, and diff checks provide the recorded evidence. The known bundle guard and any unrelated flaky timeouts will be reported without altering their thresholds.
