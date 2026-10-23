# Contextual dungeon populations

`game.vagabonds.encounters` selects authored group definitions. Each dungeon has one planned group on each underground floor (depths 1–3), alongside its existing guardian, caches and other inhabitants. Surface inhabitants and discovery placement remain separate. No encounter scales to the party's size or level, and no floor has a level gate.

Each `encounter.*` definition declares:

| Field | Meaning |
| --- | --- |
| `members` | One to four hostile character IDs; the first is the persistent leader. |
| `roles` | One role per member: `attacker`, `support`, `preparer` or `attrition`. |
| `tier` | Authored danger description: `patrol`, `warband` or `elite`. |
| `weight` | Relative selection weight, 1–100, among eligible alternatives. |
| `purposes` | Distinct site purposes: `ruin`, `mine`, `shrine`, `fort`. |
| `biomes` | Distinct biome enum values: `Grassland`, `Forest`, `Marsh`, `Highland`, `Badland`, `Snow`. |
| `minDepth`, `maxDepth` | Inclusive depths, within 1–3. |
| `tactics`, `reward` | Readable counterplay and an authored reward-profile reference. |

Role validation uses the character's actual behavior abilities. Healing or guarding other allies requires `support`; a prepared attack requires `preparer`; an `attrition` role needs a status or stress attack. A group can have at most one support and one preparer. Self-healing does not turn a creature into a support. The complete game pool must offer at least two eligible alternatives for every purpose/biome/depth combination, so narrowing a table cannot silently empty a dungeon context.

Selection uses a separate seed/site/depth stream, sorted stable IDs and authored weights. It first prefers groups not previously selected in that site's earlier depths. If all eligible definitions were already used, it avoids repeating the immediately previous group when possible. A repeated definition is reported separately from an avoidable repetition. Context-specific groups share the pool with broadly eligible patrols, permitting both local identity and an occasional unexpected encounter.

The nine shipped compositions include healer/bruiser wardens, a channeler/bowman ambush, bleeding/dread vigils, marked-prey patrols, fast prowler packs, a self-healing slime/wisp pairing, a healer-led procession, a four-member cinder retinue and a mixed broken guard. Elite groups have stronger fixed compositions and separate larger rewards using their related thematic equipment pools. They do not award guardian signature items.

## Placement and saves

Plans can be queried without generating terrain or actors. Successful stair travel or movement into a dungeon footprint activates that floor once. Starting a surface world therefore does not eagerly generate all 144 underground footprints. Inspection, `Reveal`, save loading and pure queries do not activate new populations.

Activation reads the actual 64×64 generated floor plus persistent terrain edits. A bounded flood finds walkable cells connected to the preserved stairs; a local search chooses a whole nearby cluster, avoiding occupied cells, solid caches, camps, stair approaches and cache approaches. It never carves terrain or changes discoveries to make a group fit. Stable IDs use `70000 + siteId * 16 + floor * 4`, with subsequent slots for followers.

The world saves one `EncounterPopulationRecord` per attempted site/floor. It records the selected definition and either a complete spawn or an explicit whole-group placement failure. Repeated activation, revisiting, defeat and loading do not duplicate or respawn members. A failed placement is recorded once rather than retried until it happens to succeed. Save validation checks records against the seeded plan, fixed IDs and authored membership. Removed, partial, duplicated or changed records are rejected.

```sh
./dev content inspect encounter.cinder-retinue
./dev content references character.cinder-channeler
./dev content validate
./dev preview encounter encounter.cinder-retinue --seed 42
./dev audit-encounters --seeds 42,1337,-42,2026
```

`EncounterPopulationPlan(siteId)` returns pure selections. `EnsureEncounterPopulation(siteId, floor)` is the explicit runtime/diagnostic activation entry point and returns the saved record, actor IDs, positions and whether it was newly created. Normal gameplay calls it through travel; external diagnostic callers should use an isolated world.

The population audit covers all 72 authored contexts, then explicitly activates every planned floor for each seed through the same runtime path. It compares forward/reverse visitation and repeated activation; checks connected, clear, unique positions and stable IDs; and verifies unchanged terrain deltas, world clock and combat/incident RNG. Reports include group and tier distributions, repeated versus avoidable selections, placement failures and elapsed time. This deliberately generates the full sampled underground world and costs more than ordinary lazy startup. It uses no player saves.

These checks establish consistency and usable placement. They do not establish difficulty, optimal tactics or whether repeated encounters remain interesting. Combine the report with `audit-counterplay`, native encounter fixtures and fresh-run playtesting before changing weights, composition or rewards. See [the research notes](ENCOUNTER_RESEARCH.md) for the primary sources and the distinctions between their approaches and this implementation.
