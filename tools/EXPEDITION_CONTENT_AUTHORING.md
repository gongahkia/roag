# Expedition content authoring

Expedition boards and combat setups are production JSON, not branches in `src/expedition/run.lua`.

## Chamber

Create one file in `content/expedition/chambers/`, add it to `manifest.json`, and use schema version `1`. A chamber owns its dimensions (8×7 through 14×10), semantic tile legend/grid, topology tags, and physical markers. Tile IDs are semantic (`floor`, `wall`, `water`, `spikes`, `gas`, `fire`, `breakable`, `volatile`, `door.closed`); Loveable Rogue rendering maps them later.

Required markers are one `player_spawn`, at least one `enemy_spawns`, and at least one `exits`. Optional markers are `caches`, `reinforcements`, and `boss_spawns`. Markers use one-based `x`/`y` coordinates. The validator rejects overlaps, invalid cells, disconnected walkable space, unreachable exits, and invalid dimensions.

## Encounter

Create one file in `content/expedition/encounters/`, add it to its manifest, and use schema version `1`. An encounter owns an archetype, stage, biome profiles, role min/max rows, threat preview base, spawn intent, elite/reinforcement settings, clear condition, reward intent, and topology compatibility tags.

The engine remains responsible for seeded selection, role-budget solving, safe spawn placement, AI, clear resolution, rewards, and combat. Ordinary chamber/encounter variation needs no gameplay Lua change. A genuinely new archetype, spawn intent, or clear behavior is an engine change and must be added deliberately.

## Workbench workflow

Launch Studio and select **Expedition Workbench**.

1. Open or create a chamber, paint semantic tiles, place markers, validate, and save.
2. Open or create an encounter, configure its role rows and constraints, validate, and save.
3. Select both in the left browser, open **Preview**, choose the stable seed, and use **Play** for an isolated actual `ExpeditionRun`.
4. Use reset/play preview after edits. Preview uses no account/profile/save store.

`Ctrl+S`, `Ctrl+Z`, `Ctrl+Y`, and `F5` are available in the Workbench. Shrinking a chamber requires explicit confirmation at the model level because it can discard tiles/markers.

## Headless validation

Run:

```bash
luajit tools/validate_expedition_content.lua
```

The regular Expedition analyzer consumes these same definitions through `ExpeditionRun.plan`.
