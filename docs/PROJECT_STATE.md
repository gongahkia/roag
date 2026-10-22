# Vagabonds project state

**Last reconciled: 2026-10-22.** Master reference for the independent fantasy RPG. Update alongside behavioral changes; [DEVELOPMENT.md](DEVELOPMENT.md) gives repository guidance and [VERIFICATION.md](VERIFICATION.md) records executed checks.

Vagabonds is the RPG extracted from Ithica at `c87d480`, now extended with an independent finite procedural world. Ithica remains a separate lunar colony game. This repository owns its application, Core, graphics, tests, assets, catalog and mappings; it loads no gameplay or assets from a sibling checkout. [ORIGIN.md](ORIGIN.md) records provenance. Archived Ithica guides under `history/ithica/` describe inherited source, not current gameplay.

## Implemented world

| Area | Current implementation |
| --- | --- |
| Runtime | C# / .NET 10, MonoGame DesktopGL 3.8.6; graphics-free deterministic Core |
| Launch | Resume selected world/legacy slot if present; absent slot or explicit `--new` starts a world |
| Geography | Finite 4096×4096 surface, three underground floors; 64×64 tile chunks |
| Content | 24 towns, six cultures/factions, six seeded biome provinces, 48 adventure sites |
| Generation | Global terrain fields, rivers, connected roads, whole-site overlapping WFC and separate repairs |
| WFC | Authored semantic 3×3 samples, 99–111 learned patterns, culture palettes, structural/entrance pins, eight attempts then constructive fallback |
| Streaming | Seamless global coordinates; 3×3 prefetch, 25 resident chunks, eight resident footprints |
| Party | Start alone as a generated traveller; recruit generated inhabitants, active party 1–4, stable actor IDs |
| Exploration | Real time with pause; smooth tile travel/camera, horizontal sprite facing, cursor selector, hover/active route overlays, discovered-only routing |
| Combat | Four classes and existing abilities; initiative, intentions, status effects, mana, stress, revival and levels |
| Building | Horizontal digging, material yields/food cost, walls/floors/doors/bridges/storage/camps, protected stair links |
| Simulation | 5 Hz, one real second per game minute; workers, stocks, demand prices, actual caravan cargo, road reroutes/waits, shortages, raids |
| Stories | Generated trade/blight/succession arcs; real entity references, alternative compact outcomes, repair/delivery requests and history |
| Failure | Downed companions revive; abandonment/wipe permanently kills and immediately saves; new lone traveller can continue same world |
| Persistence | `vagabonds-world` v1 manifest, generation v2/sample hash pinning, immutable checksummed chunk snapshots, exact combat/RNG continuation |
| Inspection | In-game F3 samples/constraints/counts/raw/final/repair views; reports, headless validation and native smoke |

New and loaded worlds begin paused. Space toggles time. Menus, conversations, combat and focus loss freeze the clock; there is no offline progression. Fine movement operates within 100 tiles, while production and trade run regionally farther away. Recruitment creates a job vacancy. Complete road blockades change regional routing; shortages generate delivery requests and affect prices, and repairing roads allows supply to recover. Detailed and regional simulation share the same actor identities and cargo.

Town exteriors are grassy; structural regions contain pinned walls/doors around roads/plazas. Biome regions are shuffled per seed with smoothly warped borders. Footprints are generated coherently before chunk slicing; chunk visitation cannot change layouts. Edits and discovery are separate from base terrain and survive eviction/save/load. F3 retains raw structures and highlights later repairs. Seed 42's full report has 14 fallback regions out of 864 and 4,445 post-collapse repairs; diagnostics report these explicitly. Stairs cannot be removed/built over and digging cannot create vertical shafts.

A main arc accepts evidence after a patron dies, and can resolve through the original town or a different compact. Severe sustained shortages can explicitly fail it. The world continues after resolution or failure. A wiped party leaves a recoverable storage cache; deaths must be saved before either continuation choice is accepted. Generated successor heroes receive new stable IDs while earlier deaths and the changed world remain.

## Controls and tools

The cursor brackets its current map cell. Hovering previews a known route in teal; clicking keeps the selected route in gold during travel, including when the cursor leaves the map. Paused clicks prepare travel for Space. Menus and combat hide exploration overlays; build/dig retain the selector. Travellers and inhabitants slide between committed cells, face left/right with horizontal movement and retain facing on vertical movement. Stairs, loads and long transfers reset interpolation; collision, costs and the simulation clock remain grid-based.

Space pause/resume; WASD/arrows or click known ground to move; E/Enter speaks or takes stairs; R recruits in conversation, T opens trade. B selects building, 1–6 selects a material/structure, X selects digging, and clicking adjacent terrain submits the operation. R rests at a camp; I opens the pack/revival/abandonment; J shows arcs, requests and history; M shows known places. F resolves the main arc at an eligible town; V fulfills a delivery request. Combat uses 1–6 and fighter targets. F1 guide, F3 generation inspector, F5 save, F9 load, F11 fullscreen, Ctrl+Q/window close saves and quits normal play. [RPG.md](RPG.md) has the complete guide.

```sh
./vagabonds.sh --new --seed 1337
./vagabonds.sh --scene world
./vagabonds.sh --scene world-site
./vagabonds.sh --scene world-battle
./vagabonds.sh --scene world-inspector
./vagabonds.sh --world-headless
./vagabonds.sh --world-report
./vagabonds.sh --validate-worlds 1000 --seed -500
./vagabonds.sh --world-smoke --frames 360 --screenshot artifacts/world.png
./vagabonds.sh --scene atlas
```

Default window 1440×900, minimum 1024×720. Explicit scenes and frame-limited captures ignore saves and disable automatic saving; explicit `--load` reads the selected slot. Explicit F5 saves; successful F9 resumes autosaving. World F9 accepts only compatible world saves; restart to open a legacy slot. Invalid loads preserve the current session, route, panel and autosave mode. Headless/report/seed checks do not read or write player saves.

## Legacy compatibility

Existing `lantern-rpg` v1/v2 slots remain playable through the original client with original maps, IDs, rules and exact continuation. Four fixed companions explore Hearth 36×26, Ashwood 48×32 and Sunken Abbey 51×37; iron key → seal → Abbot → relic → return. Defeat retains the old recovery rules. No world migration rewrites these saves. Explicit town/forest/crypt/battle/victory/atlas scenes and the old headless/report/seed/native checks retain this mode. [LEGACY_RPG.md](LEGACY_RPG.md) documents its controls, including Q quit and F2 atlas.

The generic Core `Campaign.Load` retains inherited field compatibility for source callers. Legacy startup, F9 and import use `Campaign.LoadRpg`, which rejects lunar field sessions. World startup dispatches by format and rejects them as well. Failed load attempts do not install a replacement session.

## Saves and boundaries

Default selected slot is `~/.vagabonds/campaign.json`; `--save PATH` selects another. An existing unreadable/invalid slot fails without silently replacing it. `--new` explicitly starts over; normal saving/quitting then replaces that selected slot. `--load` requires a save and cannot combine with `--new`. No real player slot is used by validation tooling.

World saves pair the JSON manifest with `<slot>.chunks/`. Chunk snapshots are immutable SHA-256 files, flushed before atomically replacing the manifest. Save/load retains actors, party, clock, inventories/cargo, statuses, arc/tasks/history, edits, discovery, storage, battle order and random state. Generation v2 and the exact authored sample hash are required to prevent silent regeneration changes. Unreferenced snapshots are retained so backups of earlier manifests remain recoverable; automatic snapshot garbage collection is deferred. Back up both manifest and snapshot directory.

`--import-legacy PATH [--save DESTINATION]` validates/copies an old RPG slot atomically, exits and leaves the source untouched; it rejects self-overwrite, colony saves and field sessions. The inherited `lantern-rpg` format name, object IDs and asset filenames retain their identities intentionally.

## Projects, assets and scope

The solution contains Core (world/legacy rules, generation, persistence and simulation), Graphics (sprites/text/camera), Client (world/legacy presentation/input/atlas) and Core.Tests (graphics-free rules and client startup/reload tests). All references and content resolve within this checkout. No colony game is shipped.

All 7,115 occupied crops / 11,887 catalog records, 664 original PNGs / three archives and 22 derived reference images remain local. Original IDs, artwork, provenance, uncertain identities and measured colors are preserved. The canonical schema-5 catalog is tracked as `assets/catalog.json.gz`; the readable export is ignored. Historical `ithica-expedition-bindings.json`, `ithica-generation.json` and runtime manifest names are intentional. New structural samples are original authoring inputs in `assets/world/samples.json`. Read [ASSETS.md](ASSETS.md) before changing sprite mappings.

This is a playable world foundation. Jobs use fixed recipes, relationships are sparse, distant people use regional production rather than individual hauling, and local routines use simple movement. Arc families share an evidence/return structure with different requirements/outcomes. Construction is personal, not settlement management. Worlds are finite; settlement-scale social simulation, gear swapping, tactical walking/range/cover, directional animation, gamepad input and a sample painting editor remain outside this implementation. Chunk generation is synchronous behind bounded prefetch and may cause occasional hitches; measured local timings are recorded separately from performance targets. [WORLD.md](WORLD.md) describes the architecture and research sources.
