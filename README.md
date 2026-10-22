# Vagabonds

A standalone fantasy RPG in C# / .NET 10 and MonoGame. Begin alone in a seeded 4096×4096 world, recruit generated inhabitants, explore 24 towns and 48 adventure sites, tunnel, craft equipment and fight a seed-selected final threat from a pool of 48 boss archetypes. Turn-based party encounters, shared actor needs, timed work and a changing economy use one persistent world.

Extracted from Ithica’s retained RPG at `c87d480`. Selected generation, work and social mechanisms from Ithica’s replayed history are adapted into this fantasy game. This repository contains its own source, projects, tests, complete original artwork, catalog, mappings, atlas and authoring tools; it needs no sibling checkout, symlinks or submodules. [Project state](docs/PROJECT_STATE.md) · [Play guide](docs/RPG.md) · [Development](docs/DEVELOPMENT.md) · [Origin](docs/ORIGIN.md).

## Run locally

Install the .NET 10 SDK, or use the optional per-user installer:

```sh
./tools/setup-dotnet.sh
./vagabonds.sh
./vagabonds.sh --load
./vagabonds.sh --new --seed 1337
```

`play.sh` and `rpg.sh` are aliases. On Windows or with a system SDK, run `dotnet run --project src/Vagabonds.Client --configuration Release`. The SDK wrapper can reuse an existing per-user SDK; those installations are tools, not dependencies on another repository.

Normal launches resume `~/.vagabonds/campaign.json` when it exists, or start a new expedition when it does not. `--save PATH` selects another slot. `--new [--seed N]` explicitly starts over; saving or quitting then replaces the selected save. `--load` requires an existing save. Preview scenes and frame-limited captures do not autosave. To copy an existing fantasy RPG save into the new save slot without changing its source:

```sh
./vagabonds.sh --import-legacy ~/.regolith/lantern.json
./vagabonds.sh --load
```

The import command validates and copies the campaign, then exits without a window. It accepts an explicit `--save PATH` destination. Lunar colony campaigns and field sessions are not RPG imports.

The world client is mouse-only: hover for a route preview, click ground to travel, and click people, caches or stairs to approach and interact. Play/Pause, Party, Build, Work, Craft, Actions, Journal and World map are visible buttons; Menu opens the guide and procedural inspector. Click an ability and a fighter in combat. Right-click stops travel or closes a panel. The world stays large, with a close RPG camera and bounded zoom. Existing legacy campaigns retain their original controls. See the [play guide](docs/RPG.md).

## Content authoring

`./dev content list`, `./dev content validate`, `./dev content inspect ID`, and `./dev preview character character.healer --seed 42` use the shared runtime content model. Definitions live under `content/`; [the authoring guide](docs/CONTENT_AUTHORING.md) covers references, scaffolding, removal, schemas and extension points. Run `./dev test` and review tooling at each substantial milestone. `./dev audit-world --seed 40 --samples 8` produces a JSON/SVG comparison; `./dev preview encounter boss.ash-knight --seed 42` exercises actual combat. `./dev verify-native` runs optional local X11/WSLg input acceptance using temporary saves (requires xdotool/ffmpeg and a built client).

## Build and inspect

```sh
./tools/dotnet.sh restore Vagabonds.sln --locked-mode
./tools/dotnet.sh build Vagabonds.sln --configuration Release --no-restore
./tools/dotnet.sh test Vagabonds.sln --configuration Release --no-build
./vagabonds.sh --world-headless
./vagabonds.sh --validate-worlds 1000 --seed -500
./vagabonds.sh --world-report
./vagabonds.sh --scene world-inspector
./vagabonds.sh --headless
./vagabonds.sh --validate-seeds 1000 --seed -500
./vagabonds.sh --scene atlas
python3 tools/build_game_catalog.py --check
python3 -m unittest discover -s tests/catalog -v
```

World diagnostics validate the new simulation/generation without player saves; the retained `--headless` mode completes the legacy quest. Scene captures and smoke checks also avoid automatic saving. [World architecture](docs/WORLD.md) · [Executed verification](docs/VERIFICATION.md).

## Complete artwork and mappings

All **7,115 occupied sprites / 11,887 indexed regions**, **664 original PNGs**, **3 original archives**, original credits, crop IDs, mapping evidence and generic descriptions are included. The complete [schema-5 catalog](assets/catalog.json.gz) is losslessly compressed; queries read it directly. Regeneration also creates the ignored readable `assets/catalog.json` export.

```sh
python3 tools/query_assets.py --type actor --color green --limit 10
python3 tools/query_assets.py --subject ghost --subject-confidence high --format ids
python3 tools/build_game_catalog.py
```

Open [assets/atlas.html](assets/atlas.html) directly for neutral searches, exact crop downloads and layered scene experiments. Historical `ithica-*` asset filenames and origin-specific catalog fields are deliberately preserved; the expedition aliases and `usage.rpg` provide the RPG mappings. [Asset ownership and authoring](docs/ASSETS.md) · [Original credits](assets/CREDITS.txt).

The game now includes WFC/BSP/cavern/ring layouts, generated companions, boss variations, loot/equipment, timed work and shared hunger/fatigue/injury with bounded memories and trust. Long-form balance still needs playtesting. Background gathering/production remains coarse; tactical walking/range, directional animation and controller input are not implemented.
