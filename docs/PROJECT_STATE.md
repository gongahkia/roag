# Vagabonds project state

**Last reconciled: 2026-10-21.** This is the master reference for the independent fantasy RPG. Update it with behavior, content, controls, saves, tooling or known-limit changes. [DEVELOPMENT.md](DEVELOPMENT.md) records repository guidance; [VERIFICATION.md](VERIFICATION.md) records executed acceptance checks.

Vagabonds is the party RPG extracted from Ithica at `c87d480`. Ithica remains a separate lunar colony game. Vagabonds owns its own application, Core, graphics, tests, assets, catalog and mappings. No gameplay or asset file is loaded from a sibling repository. [ORIGIN.md](ORIGIN.md) records provenance; archived Ithica guides under `history/ithica/` are not this product’s current rules.

## Implemented game

The new finite-world backend lives under `Vagabonds.Core.World`, with `--world-headless`, `--world-report` and `--validate-worlds N` diagnostics. It has a 4096×4096 surface and three underground floors, 24 towns, six cultures/factions, 48 sites, authored overlapping-WFC samples and bounded caches. Generated inhabitants can join the lone traveller; real-time ticks drive local routines, regional stocks, caravans, shortages, repair/delivery requests and raids. Commands cover construction, digging, storage, trade, turn-based combat, revival and permanent death. Versioned world manifests save immutable chunk snapshots atomically. The graphical campaign is still the default at this backend checkpoint. See [WORLD.md](WORLD.md) for architecture and scope.

| Area | Current implementation |
| --- | --- |
| Runtime | C# / .NET 10, MonoGame DesktopGL 3.8.6 |
| Normal launch | `./vagabonds.sh` resumes the selected save if present; `play.sh` and `rpg.sh` forward to it; `--new` explicitly starts over |
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

Player click routes stay on discovered terrain and prefer paths avoiding currently visible hazards. Changes to hazards outside the party's sight do not influence routing. If no discovered path exists, explore manually first. The headless verification driver may plan across the full map, then uses the same paid movement and combat as before.

## Run, controls and scenes

```sh
./vagabonds.sh
./vagabonds.sh --load
./vagabonds.sh --new --seed 1337
./vagabonds.sh --scene town
./vagabonds.sh --scene forest
./vagabonds.sh --scene crypt
./vagabonds.sh --scene battle
./vagabonds.sh --scene victory
./vagabonds.sh --scene atlas
```

Default window 1440×900; minimum 1024×720. Supports `--new`, `--window-size`, `--screenshot PATH`, `--frames N`, `--skip-intro`, `--save PATH` and `--help`. All explicit scenes (including town), frame-limited captures and graphical smoke checks start in preview mode without automatic save loading or autosaving; explicit `--load` can capture an existing campaign. Headless, seed validation and generation reports do not read or write player saves.

WASD/arrows or click move; E/Enter interacts; Space/period waits; T uses a torch; R camps; H treats the selected companion; I opens the pack. Combat uses 1–6 and clicking targets. F1 opens the guide, F2 the atlas, F5 saves, F9 loads and Q saves/quits through the normal guarded autosave path. Full controls are in [RPG.md](RPG.md). The native atlas keeps neutral search and optional game notes; its origin-lunar fields describe inherited mapping provenance, not a second game mode.

## Projects and ownership

`Vagabonds.sln` contains four projects: `Vagabonds.Core` owns graphics-free expedition rules, generation, combat and persistence; `Vagabonds.Graphics` owns sprites, text, masks and camera; `Vagabonds.Client` owns the normal RPG window/input/interface/atlas; `Vagabonds.Core.Tests` owns RPG, extraction compatibility and graphics-free client startup/reload tests. All project and asset paths resolve within this checkout.

The original colony simulation, colony client, combined campaign and colony tests are not part of this solution. Inherited expedition field helpers and the lunar validation adapter remain as source compatibility code; they are not a playable lunar colony mode. Normal client loading/import rejects lunar field sessions.

Startup, F9 and legacy import share `Campaign.LoadRpg`, which validates and rejects lunar field sessions before returning a replacement campaign. Failed F9 loads report an error and preserve the current campaign, pending route and autosave mode. The generic Core `Campaign.Load` remains available for retained field-session compatibility.

## Saves and compatibility

The new default is `~/.vagabonds/campaign.json`. The inherited `lantern-rpg` version-two format and version-one reader retain their identities intentionally. New save location and application/assembly names are Vagabonds; original world/object/hero/sprite IDs are preserved.

Normal launches resume the selected slot when it exists, preserving the seed and exact progress; unreadable or invalid existing saves fail without silently creating a replacement campaign. An absent slot starts a new expedition using `--seed` (default 42). Use `--new [--seed N]` to explicitly start over; normal saving/quitting then replaces that selected slot. `--load` requires an existing save and cannot be combined with `--new`. Resumed campaigns skip the initial field guide; F1 still opens it.

`./vagabonds.sh --import-legacy PATH [--save DESTINATION]` validates an old RPG save, writes the selected new destination atomically and exits. It never writes the source and rejects a destination identical to it, colony saves and lunar field sessions. There is no implicit migration or modification of existing player saves. Use `--load` afterward.

## Artwork and authoring

All original artwork, catalog records, mappings, provenance and measured colors are included locally. The canonical complete catalog is tracked as `assets/catalog.json.gz`; `python3 tools/build_game_catalog.py` also creates the ignored readable JSON export. Queries/checks work in fresh clones without that export. Regenerate from authored inputs; do not hand-edit derived records or repaint source images.

The inherited `assets/ithica-expedition-bindings.json` is the RPG alias input, `ithica-generation.json` supplies generation rules and `ithica-expedition-sprites.json` is the runtime manifest. Their historical filenames are intentional. The full catalog also retains old lunar/colony bindings as origin metadata. `sprites[].asset` stays game-independent; `usage.rpg` records RPG binding usage. Native atlas registration of all sprites is not gameplay use. Read [ASSETS.md](ASSETS.md) before changing mappings.

## Verification and boundaries

The extraction passed locked restore/build, 61 core/import tests, 38 catalog tests, 74 offline-browser assertions, 1,000 seeds with zero fallback, 25 native graphical checks and all 15 canonical/native query comparisons. A fresh isolated clone repeated restore/build/core/catalog/quest checks, and a published copy ran outside both repositories with all sprites and safe CLI imports. Run locked restore, Release build, core tests, a headless quest, seed validation, catalog freshness/hash checks and the catalog suite; use native smoke and published-output checks for client/extraction changes. [VERIFICATION.md](VERIFICATION.md) records actual results and platform scope; configured CI is not evidence of execution.

The playable scope is a generated three-area vertical slice with five encounters and one finite quest. It has no colony-management mode, endless world, branching campaign, physical hauling, gear swapping, tactical walking/range/cover, automatic sprite animation or gamepad input. Source identities that were uncertain remain uncertain.
