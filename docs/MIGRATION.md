# Roag C# integration

Implementation in progress. This file records scope and evidence; compilation alone does not establish feature parity.

## Source identities

- Original Roag: `d498fe0889050d2a23f2ff86d80db6084ed6467a` (recoverable in this repository's Git history).
- Imported Vagabonds: `38654e90e8546466b2ce9762032155f3890eaf3f`.
- Destination: this repository, product/project name Roag, C#/.NET 10 and MonoGame.
- Original asset IDs, crops, credits and catalogs are preserved alongside Roag's Kenney artwork.

## Accepted design

1. Roag spatial tile combat replaces Vagabonds encounters everywhere.
2. Every party member has their own position and directly controlled actions, governed by action recovery/speed.
3. All simulation is action-driven. Thinking and rendering advance no world time.
4. Dungeon actions also advance the outside world's simulation.
5. Full party defeat ends the world; starting again creates a fresh world.
6. Individuals can be downed and rescued with limited resources; abandonment is permanent death.
7. Dungeons persist through retreat and revisiting.
8. A seeded, persistent subset has Roag extraction pressure/pursuers.
9. Dungeons occupy Vagabonds' landscape and underground terrain; blend both generators.
10. Preserve all existing Roag systems/content while adapting them to the unified world. There is no requested separate original-game mode.

## Port strategy

The original deterministic modules are statically translated into compiled C# methods in `src/Roag.Core/Rules/Generated`. A small native value/collection library preserves their numeric, ordered-action and content semantics. The game does not parse or execute Lua source and does not depend on a Lua VM. Generation tools read the pinned source from Git; they are development-only. C# integration and presentation adapters are handwritten. Existing rule tests are translated as a regression reference; obsolete LÖVE-specific introspection tests need native presentation replacements, not silent exclusions reported as passes.

## Verification status

- Source trees were clean before import.
- Imported .NET package references restored after project rename.
- Rule translation, native parity tests, world integration and rendered acceptance are in progress.
- The Lua files remain temporarily during verification and will be removed after replacements are checked.
