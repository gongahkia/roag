# Roag C# integration

Gameplay implementation is authorized by the active migration goal, superseding the earlier pause. The design discussion is consolidated in [the migration decisions and plan](MIGRATION_DECISIONS.md). That document supersedes unresolved alternatives in [the initial overlap map](MIGRATION_OVERLAPS.md). See [progress and evidence](MIGRATION_PROGRESS.md) and the [preservation checklist](MIGRATION_PRESERVATION.md). Compilation alone does not establish feature parity.

## Source identities

- Original Roag: `d498fe0889050d2a23f2ff86d80db6084ed6467a` (recoverable in this repository's Git history).
- Imported Vagabonds: `38654e90e8546466b2ce9762032155f3890eaf3f`.
- Destination: this repository, product/project name Roag, C#/.NET 10 and MonoGame.
- Use only Vagabonds' sprites in the final build, selected through its generic universal atlas/catalog and stored mappings, with Roag's combat polish. Preserve source IDs, crops, credits and catalogs; original Roag art remains a reference rather than a final sprite source.

## Accepted design

1. Roag spatial tile combat replaces Vagabonds encounters everywhere.
2. Every party member has their own position and directly controlled actions, governed by action recovery/speed.
3. All simulation is action-driven. Thinking and rendering advance no world time.
4. Dungeon actions also advance the outside world's simulation.
5. Final party death ends the world; starting again creates a fresh world. All-down parties may fast-forward while existing nearby NPCs have a chance to rescue them before bleed-out.
6. Individuals can be downed and rescued with limited resources; abandonment is permanent death.
7. Dungeons persist through retreat and revisiting.
8. A randomly selected special subset has Roag extraction pressure/pursuers. Deliberate, signposted activation starts a world-time deadline that continues outside. Expiry applies announced dungeon-specific consequences: some lethal, others leaving altered revisitable remains. All leave persistent local aftermath; pursuers remain confined to the dungeon.
9. Dungeons occupy Vagabonds' landscape and underground terrain; blend both generators.
10. Preserve all existing Roag systems/content while adapting them to the unified world. There is no requested separate original-game mode.

The linked decision record adds the agreed four-person party, individual builds and contribution XP, split-party timeline/vision, downing and Guard rules, cooperation, survival/camping, day/night, dungeon geography and open campaign structure. Earthquakes, titans/kaiju and race-driven politics are deferred expansion work.

All four [additional boss concepts](MIGRATION_BOSS_PROPOSALS.md) are vetted. The decision record also defines delegated numerical tuning and concrete completion evidence: all agreed features, preservation coverage, relevant automated tests, rendered end-to-end verification and a playable Linux build. Windows is desirable where feasible; no user-playtest approval gate is required. Saves/profiles start from scratch with no legacy conversion. The active goal authorizes implementation of this agreed scope.

## Port strategy

The accepted destination is handwritten, idiomatic C# using the Vagabonds architecture. Where systems overlap, adapt Vagabonds to Roag's systems. Preserve original Roag behavior and content, using its source and tests as references.

The generated modules in `src/Roag.Core/Rules/Generated`, their dynamic value/collection runtime and the preliminary gameplay adapters are an earlier prototype, not the accepted production foundation or proof of parity. Replacement and subsequent cleanup are authorized. Original Lua files remain available as references; their eventual removal must follow verified native replacements. Genuine new gameplay ambiguities must be clarified with the user before dependent engineering work; routine engineering and delegated numerical tuning may proceed.

## Verification status

- Source trees were clean before import.
- Imported .NET package references restored after project rename.
- Earlier rule translation, world integration and rendered acceptance remain incomplete; the prototype is not accepted gameplay integration.
- The Lua files remain temporarily during verification and will be removed after replacements are checked.
- The discussion/consolidation changed planning documents only. Subsequent implementation verification is recorded in [MIGRATION_PROGRESS.md](MIGRATION_PROGRESS.md), with explicit limits on what each check proves.
