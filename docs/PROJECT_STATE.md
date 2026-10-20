# Vagabonds project state

**Last reconciled: 2026-10-20.** This is the master reference for the independent fantasy RPG. Update it with behavior, content, controls, saves, tooling or known-limit changes. [DEVELOPMENT.md](DEVELOPMENT.md) records repository guidance; [VERIFICATION.md](VERIFICATION.md) records executed acceptance checks.

Vagabonds is the party RPG extracted from Ithica at `c87d480`. Ithica remains a separate lunar colony game. Vagabonds owns its own application, Core, graphics, tests, assets, catalog and mappings. No gameplay or asset file is loaded from a sibling repository. [ORIGIN.md](ORIGIN.md) records provenance; archived Ithica guides under `history/ithica/` are not this product’s current rules.

## Implemented game

| Area | Current implementation |
| --- | --- |
| Runtime | C# / .NET 10, MonoGame DesktopGL 3.8.6 |
| Normal launch | `./vagabonds.sh`; `play.sh` and `rpg.sh` forward to it; no preview flag |
| Campaign | Four fixed companions; one persistent party, pack, quest and save |
| Areas | Hearth 36×26, Ashwood 48×32, Sunken Abbey 51×37 |
| Quest | Find iron key → open seal → defeat Hollow Abbot → recover ember relic → return to Elowen |
| Encounters | Five groups; seven enemy variants across six appearances |
| Abilities | Four classes × three abilities, plus Guard, Potion and Retreat |
| Survival | Shared potions/torches/rations/gold; light, stress, healing, camps and inn recovery |
| Combat | Initiative, intent telegraphs, guard, mark, stun, burn/bleed, healing/revival, experience and levels |
| Generation | Cellular forest/ponds; quest graph, socket WFC and cellular room erosion; validated protected routes |
| Persistence | Saved generated maps, seen terrain, cleared encounters, opened objects, hazards, battle order/status and RNG |
| Assets | 7,115 occupied crops / 11,887 records; 664 original PNGs / 3 archives; 22 derived reference images |
| Catalog | Complete schema-5 neutral descriptions, types, subjects/confidence, colors, layers and composition; original evidence preserved |

Rowan is a Warden, Vesper a Ranger, Iris an Arcanist and Sable a Vestal. Class equipment is fixed. Defeat returns the party to Hearth in poor condition, raises stress and costs a quarter of gold; persistent quest progress remains. There is no permadeath in the current prototype.

Successful exploration moves/waits spend resources and evolve active hazards every four steps; combat resolves its own turns. Invalid actions and inspection consume no time/resources/RNG. Inactive areas freeze. Roads, objective approaches and room crosses stay protected. The existing deterministic map/combat stream behavior and version-one fallback layouts are retained.

## Run, controls and scenes

```sh
./vagabonds.sh
./vagabonds.sh --load
./vagabonds.sh --seed 1337
./vagabonds.sh --scene town
./vagabonds.sh --scene forest
./vagabonds.sh --scene crypt
./vagabonds.sh --scene battle
./vagabonds.sh --scene victory
./vagabonds.sh --scene atlas
```

Default window 1440×900; minimum 1024×720. Supports `--window-size`, `--screenshot PATH`, `--frames N`, `--skip-intro`, `--save PATH` and `--help`. Headless, seed validation, generation reports, captures and graphical smoke checks write no player autosave.

WASD/arrows or click move; E/Enter interacts; Space/period waits; T uses a torch; R camps; H treats the selected companion; I opens the pack. Combat uses 1–6 and clicking targets. F1 opens the guide, F2 the atlas, F5 saves, F9 loads and Q saves/quits through the normal guarded autosave path. Full controls are in [RPG.md](RPG.md). The native atlas keeps neutral search and optional game notes; its origin-lunar fields describe inherited mapping provenance, not a second game mode.

## Projects and ownership

`Vagabonds.sln` contains four projects: `Vagabonds.Core` owns graphics-free expedition rules, generation, combat and persistence; `Vagabonds.Graphics` owns sprites, text, masks and camera; `Vagabonds.Client` owns the normal RPG window/input/interface/atlas; `Vagabonds.Core.Tests` owns RPG and extraction compatibility tests. All project and asset paths resolve within this checkout.

The original colony simulation, colony client, combined campaign and colony tests are not part of this solution. Inherited expedition field helpers and the lunar validation adapter remain as source compatibility code; they are not a playable lunar colony mode. Normal client loading/import rejects lunar field sessions.

## Saves and compatibility

The new default is `~/.vagabonds/campaign.json`. The inherited `lantern-rpg` version-two format and version-one reader retain their identities intentionally. New save location and application/assembly names are Vagabonds; original world/object/hero/sprite IDs are preserved.

`./vagabonds.sh --import-legacy PATH [--save DESTINATION]` validates an old RPG save, writes the selected new destination atomically and exits. It never writes the source and rejects a destination identical to it, colony saves and lunar field sessions. There is no implicit migration or modification of existing player saves. Use `--load` afterward.

## Artwork and authoring

All original artwork, catalog records, mappings, provenance and measured colors are included locally. The canonical complete catalog is tracked as `assets/catalog.json.gz`; `python3 tools/build_game_catalog.py` also creates the ignored readable JSON export. Queries/checks work in fresh clones without that export. Regenerate from authored inputs; do not hand-edit derived records or repaint source images.

The inherited `assets/ithica-expedition-bindings.json` is the RPG alias input, `ithica-generation.json` supplies generation rules and `ithica-expedition-sprites.json` is the runtime manifest. Their historical filenames are intentional. The full catalog also retains old lunar/colony bindings as origin metadata. `sprites[].asset` stays game-independent; `usage.rpg` records RPG binding usage. Native atlas registration of all sprites is not gameplay use. Read [ASSETS.md](ASSETS.md) before changing mappings.

## Verification and boundaries

The extraction passed locked restore/build, 61 core/import tests, 38 catalog tests, 74 offline-browser assertions, 1,000 seeds with zero fallback, 25 native graphical checks and all 15 canonical/native query comparisons. A fresh isolated clone repeated restore/build/core/catalog/quest checks, and a published copy ran outside both repositories with all sprites and safe CLI imports. Run locked restore, Release build, core tests, a headless quest, seed validation, catalog freshness/hash checks and the catalog suite; use native smoke and published-output checks for client/extraction changes. [VERIFICATION.md](VERIFICATION.md) records actual results and platform scope; configured CI is not evidence of execution.

The playable scope is a generated three-area vertical slice with five encounters and one finite quest. It has no colony-management mode, endless world, branching campaign, physical hauling, gear swapping, tactical walking/range/cover, automatic sprite animation or gamepad input. Source identities that were uncertain remain uncertain.
