# One fantasy party RPG

The user authorized implementation on 2026-10-22 after the history replay. Vagabonds remains the game and owns the streamed world, generated companions, fantasy setting, bindings, input and save/exploration safeguards. Ithica at `070558d1332010b54cd7da080fb23bfa4f24cd17` is a reference for reusable simulation, generation and work systems. Its independent local checkout is ignored under `artifacts/references/ithica`; the game must not depend on that directory. The source can always be recovered from the recorded commit.

## Agreed behaviour

- One playable fantasy game and campaign, with party development and exploration as the main activity. Settlement management is optional.
- All regions, dungeons, discoveries and endgame threats exist from the start. No mandatory dungeon order or story unlocks. Tunnelling is a valid approach.
- Punishing consequences, readable danger and consistent deterministic rules. Curated boss archetypes have generated variations; defeating the world's selected final boss completes the run.
- Preserve turn-based party combat. Thinking/targeting costs no time; completed rounds advance the shared world clock.
- One authoritative actor identity across background jobs, recruitment and combat. Construction takes actual work/time and can use background workers.
- Adapt useful needs, memories, trust, work, repair and incident mechanisms to fantasy. Do not transplant lunar lore, suit oxygen or mandatory colony infrastructure.
- Keep Vagabonds' sprite bindings, smooth movement, facing, hover routes, outdoor visibility and dungeon fog.
- No save migration project. Invalid/incompatible slots must still be rejected safely and never silently overwritten.
- Make agent authoring practical across characters, interactions, abilities, enemies, bosses, items, recipes and generation: stable IDs, validated data, reference queries, scaffolding, deletion checks, deterministic previews and documented extension points.
- Review developer-tool usefulness at each substantial milestone. Turn repeated manual checks into reusable commands; use those commands to validate actual gameplay content.

Later input preference: the world client is mouse-only, with visible controls for every action. Keep the 4096×4096 world and use a closer RPG exploration camera; the world map supplies the overview. Existing legacy controls and source sprite bindings remain intact.

Clarified: everyone follows the same needs, fatigue and injury rules. Class, level, equipment and traits augment those shared defaults; party members do not become a separate simulated species.

## Implementation checkpoints

1. Shared content model and developer entry point, with runtime-backed character/ability/interaction definitions, reference validation, authoring commands and tests.
2. World variety audit and generation improvements using overlapping WFC plus purpose-specific layouts/noise. Seed comparison, connectivity, fallbacks/repairs and visual inspection must be reported separately.
3. Authored boss roster with variations, richer enemy behaviours, freely accessible final encounters, victory state and round-based world time.
4. Timed construction and worker assignments with exact material reservations/refunds, identity preservation, fantasy social/repair systems and the clarified simulation policy.
5. Integrate player feedback, inspections, content/encounter previews, documentation and full acceptance checks. Preserve old safeguards throughout.

Each checkpoint requires implementation, meaningful automated checks, usable developer inspection, updated project state/verification and a cohesive local commit. This file records direction and progress, not a claim that the planned systems are already shipped.

## Progress

- Checkpoint 1 committed as `2bbf9dc`: runtime content and unified authoring commands.
- Checkpoint 2 implemented: generation profiles, purpose-specific layouts, coherent terrain and reproducible multi-seed JSON/SVG audits. Executed evidence is recorded in VERIFICATION.md.
- Checkpoint 3 implemented: 48 archetypes/variants, saved authored intentions, open endgame/victory, loot/equipment and completed-round time.
- Checkpoint 4 implemented: shared bodies, timed/reserved work, worker identity, rest, social memory/trust, conditional incidents and optional wear/repair/crafting.
- Checkpoint 5 implemented: runtime previews for all content families, reusable native acceptance, completed controls/guides and final verification.

All five checkpoints are complete for this integration. This does not claim a finished content/balance pass or full settlement-scale simulation; PROJECT_STATE.md records those practical limits.
