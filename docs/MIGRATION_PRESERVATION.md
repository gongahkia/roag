# Migration preservation checklist

Implementation is authorized by the active goal. The approved decisions in [MIGRATION_DECISIONS.md](MIGRATION_DECISIONS.md) govern this checklist. An unchecked row remains unfinished even if the prototype or imported code compiles.

[Original source inventory](migration/original-source-inventory.json) records every original Lua source module, its hash, named functions, literal content references and original test paths at `d498fe0889050d2a23f2ff86d80db6084ed6467a`. Regenerate with `python3 tools/migration/inventory_sources.py`. Literal references are an audit index, not an enumeration of registered content: computed IDs and variants must also be checked when each family is ported. The preserved sources and Git revision remain available until their replacements are verified.

[Original enemy inventory](migration/original-enemy-inventory.json) enumerates all 50 registered enemy definitions across 13 AI families, including dynamically assembled biome elites, expansion regulars, squads and idea enemies. It records exact original stats/rewards/flags and source hashes. Regenerate with the optional LuaJIT/lupa reference environment using `python tools/migration/inventory_enemies.py`. This is preservation evidence, not native enemy behavior or final sprite bindings.

Statuses: **reference** = retained original; **imported** = retained Vagabonds code, integration pending; **partial** = handwritten implementation exists with limited evidence; **verified** requires native gameplay integration and appropriate checks. No family is fully verified yet.

| Family / original source modules | Native destination and required conversion | Status / evidence |
| --- | --- | --- |
| Timing, RNG: `scheduler`, `rng`, timing in `game` | `Tactics/ActionTimeline`, `ActionTiming`, `RoagRandom`; one world clock, effects before readiness, independent stream state, no offscreen parking | Partial: foundation tests and action-driven world-clock bridge; production routing and remaining scheduled effects pending |
| Class catalog/geometry: `classes`, `targeting` | `Tactics/ClassCatalog`, `AttackGeometry`; 15 original classes, costs, IDs, actual world coordinates | Partial: catalog used by world class selection; 1,072 reference footprints checked. Native basic/special/dash handlers present; complete boon/terrain/interaction resolution and client integration pending |
| Core execution: `game`, `world` | Native world combat command/preview/event pipeline; one actor health authority and terrain source | Partial: `TacticalCombat` executes against actual world actors/tiles with save/clock integration. Client cutover, remaining class/effect handlers, AI and reward pipeline pending; generated adapter still present |
| Class resources: `idea_duelist`, `idea_geomancer`, `idea_companions`, `idea_stormcaller`, `idea_wayfinder` | `ClassResources`, `TacticalClassActions`, `TacticalTerrain`; per-actor parry, bulwark, spirit/sentry, charge/ward, beacon; explicit ownership, offscreen scheduling | Partial: native execution, independent same-class ownership, exact saves, offscreen work and terrain/room eligibility tested. Full event/proc/pickup integrations and rendered client exposure pending |
| Boons: `boons`, `boon_content`, `boon_expansion_content`, `boon_patterns`, `idea_boon_content` | `Tactics/Boons`: typed catalog and event/proc pipeline; all rarity stacks, conditions, effects, transformations, causal ancestry, utility behavior | Partial: all 103 definitions, ownership/arithmetic/memory, geometry, 60 condition evaluations, independent dispatch and 1,896 effect-targeting cases checked against original Lua. Native executor handles 24/24 (food adaptation pending) effect kinds against real actors; burning/echoes use the world timeline/save and reference-safe corpse cleanup. Native public commands and scheduled work now drive finite causal chains, verified across 75 original-game decisions and actual world saves. Whole-batch snapshots, lineage, per-owner sight, stable owner order, reversal and hazard entry are connected. Owned modifiers/seven forms, conditional previews and immediate dice/Vow are connected to actual commands, with 59 original-game decision comparisons. Physical currency/healing and pickup/drop effects now have 24 original-game decision comparisons. Food/chest acquisition, mirror/utility interactions, world/environment listeners and authoring/client integration remain pending; food overflow conversion awaits clarification |
| Progression/rewards: `rewards`, progression in `content` | Individual six-stat growth; contribution XP and one-time reward receipts; shared loot, assigned boons, adapted food | Partial: `CharacterBuild` preserves original growth across 45 class/seed scenarios, independently and through JSON saves. Native contribution XP, dead-share redistribution/downed eligibility, member growth and saved fractional shares are tested against world state. Physical currency/healing and both pickup/drop boon handlers now run natively with original command comparisons and saved receipts. Authored enemy reward bindings, food/assigned-boon acquisition and production routing pending |
| Core content: `content` | Typed enemies, abilities, supplies, chests, stage rules and map definitions | Reference; literal-ID index plus dynamic enumeration needed |
| Enemy AI: `ai`, `regular_ai`, `elite_ai`, `enemy_squads`, `squad_ai`, `idea_enemies`, `idea_sentinel` | `EnemyCatalog`, `EnemyPlanning`, `TacticalEnemyTurns`; persistent actors, species motives, visible intent, physical pursuit, preserved elite/secondary mechanics | Partial: all 50 original definitions bound to native stats/rewards; 15 basic/boss/biome-elite controllers match 2,400 original decisions. World action timing, locked target identity and save continuation checked. Remaining 35 controllers, phase/support effects, species selection, world navigation, special-site confinement and production routing pending |
| Generation: `generation`, `rooms`, `room_expansion`, `corridor_branches`, `biomes`, `floors` | World-space complexes, all authored rooms/rotations/biomes, connected floors and exits, persistent edits | Partial foundation only: imported structural/WFC room identity now supports native Wayfinder, without a dungeon-wide fallback. Original Roag room/generation content is still reference; world generator not yet blended |
| Stage/finale: `stage`, `finale` | Selected special sites with deliberate activation, persistent deadlines/aftermath; preserved bosses and open campaign | Reference; prototype challenges not accepted as completion |
| Terrain: `terrain`, `idea_terrain`, `idea_environment`, `idea_environment_content`, `idea_embers`, `idea_silt`, `idea_webs`, `idea_mirrors` | Shared movement/projectile/vision/work geometry and scheduled effects, reflection, fire, webs, silt, original environmental content | Reference |
| Interactions: `idea_interactions`, `idea_interaction_content`, `idea_oaths`, `interaction_menu`, `review_features` | Native world services/fixtures/utilities/oaths with original eligibility and resource accounting | Reference |
| Persistence/profile: `persistence`, `profile`, `achievements`, `achievement_content`, `love_storage`, `run_seed` | Native campaign and permanent profile; exact pending work; terminal-world safeguards; no legacy migration | Partial: native pending attacks/recovery/actor metadata validated and round-trip tested; split-floor and all-down saves supported. Profile, remaining effects/deadlines/reward receipts and full terminal lifecycle pending |
| Visibility/camera: `visibility`, `camera`, `spectator` | Controlled member live sight, shared explored map, floor-aware camera and inspection; no hostile-information leakage | Reference |
| Presentation: `attack_outline`, `combat_numbers`, `hit_feedback`, `playback`, `audio_cues`, `presentation_settings`, `pixel_ui`, `ui_fonts`, `title_menu`, `idea_presentation`, `main`, `conf` | MonoGame input/rendering/audio/settings, keyboard/controller/mouse, tactical previews/timeline and party/world UI | Reference; actual interactive rendered journeys pending |
| Art bindings: `kenney_symbols`, `kenney_terrain`, `onebit_shader`, `onebit_visuals`, `visual_assets`, `visual_juice`, `visual_monochrome`, `visual_packs`, `visual_palette`, `visual_slots`, `sprite_audit` | Preserve presentation capabilities using Vagabonds catalog sprites only; stable IDs, crop provenance and credits; retire original sprite bindings | Reference; approved art adaptation, not permission to drop polish |
| Authoring: `studio_atlas`, `studio_atlas_runtime`, `studio_atlas_store`, `studio_audio`, `studio_audio_runtime`, `studio_audio_samples`, `studio_audio_store`, `studio_document`, `studio_project`, `studio_theme`, `studio_theme_store`, `studio_world`, `studio_world_runtime`, `diagnostics` | Native content/atlas/audio/theme/world authoring, validation, persistence and preview capabilities | Reference; imported tools require per-capability mapping |

## Retained Vagabonds capabilities

Use sibling revision `38654e90e8546466b2ce9762032155f3890eaf3f` as read-only reference. The imported C# implementations are useful foundations; baseline tests do not prove action-driven integration.

- [ ] World terrain/chunk streaming, structures, stairs, excavation/building, resources, work orders and repairs.
- [ ] Town production, trade, occupations, caravans and real cargo accounting.
- [ ] Residents, routines, factions, relationships, memories, dialogue, quests and consequences.
- [ ] Wildlife, migration, habitat resources, reproduction and persistent individual deaths.
- [ ] Calendar, day/night, seasons and weather, all driven by committed actions.
- [ ] Equipment/affixes/crafting and retained abilities adapted to Roag classes, recovery and resources; no separate mana/talent progression.
- [ ] Encounters/guardians/rewards/discoveries/endgame adapted to physical combat and one-time receipts.
- [ ] Typed content schemas, generic asset catalog/universal atlas, existing mappings, credits, diagnostics and preview tools.
- [ ] Native world snapshots, validation and deterministic continuation expanded for all new tactical state.

## Agreed integration and additions

- [ ] Four members, fixed independent classes/builds, recruit existing people, shared supplies, individual equipped items.
- [ ] Weighted damage/heal/shield/control XP, owner attribution, downed contributors and permanent boon assignment.
- [ ] One FFX-style action timeline everywhere; move costs an action; invalid commands/inspection/rendering consume no time.
- [ ] Leader-following, deliberate splitting/regrouping, local combat and distant groups, tile occupancy, separate sight/shared exploration.
- [ ] Locked dodgeable attacks, friendly fire, initially inspectable targeting motives and hidden offscreen hostile timeline entries.
- [ ] Directional paid Guard; cooperative actions with additional recovery for an already recovering helper.
- [ ] Five-own-turn bleed-out, damage shortens countdown, medicine/action revival at 25%, repeat downing resets five, corpse gear.
- [ ] All-down fast-forward, physical NPC rescue, final-party death ends campaign; fresh world retains new-game profile unlocks.
- [ ] Hunger-only food, gradual well-fed regeneration, starvation, fatigue/injuries, configurable paid auto-eating.
- [ ] Sustained rest/work/watch, hearing/awareness interruptions, alarms, campfires and shelter/traps.
- [ ] Offscreen routines/effects; natural repopulation without restoring dead identities/rewards.
- [ ] Spatial dungeon complexes, optional bosses, surface/underground distance correspondence, physical exits and appropriate portals.
- [ ] Special-site activation/deadlines, retreat/reentry, announced lethal/nonlethal persistent aftermath and confined pursuers.
- [ ] Four approved new bosses, preserved original roster, seeded final boss, fixed role-based difficulty, early access, continued play after victory.
- [ ] Exact save/resume, no in-game rollback, no terminal-world resurrection, no old-save/profile conversion.
- [ ] Actual rendered interactive verification and reproducible Linux delivery; feasible Windows build/runtime status accurately recorded.
- [ ] Verified retirement of Lua/LÖVE and generated/prototype runtime dependencies.

Race politics, earthquakes, titans/kaiju and general disaster generation remain deferred. Numerical tuning is delegated; new gameplay choices require clarification before dependent implementation.

## Acceptance evidence

The eight completion gates in the decision document remain open. Record concrete evidence and its scope in [MIGRATION_PROGRESS.md](MIGRATION_PROGRESS.md); never infer integration or rendered acceptance from isolated unit tests.
