# Vagabonds

A standalone fantasy RPG in C# / .NET 10 and MonoGame. Begin alone in a seeded 4096×4096 world, recruit generated inhabitants, explore 24 towns and 48 adventure sites, build and dig, and shape a changing trade economy. Real-time exploration pauses for turn-based encounters; generated main arcs and permanent deaths leave consequences in the same persistent world.

Extracted from Ithica’s retained RPG at `c87d480`. Ithica remains the separate lunar colony game. This repository contains its own source, projects, tests, complete original artwork, catalog, mappings, atlas and authoring tools; it needs no sibling checkout, symlinks or submodules. [Project state](docs/PROJECT_STATE.md) · [Play guide](docs/RPG.md) · [Development](docs/DEVELOPMENT.md) · [Origin](docs/ORIGIN.md).

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

Space resumes/pauses time; WASD/arrows or click moves; E speaks or takes stairs; B builds; X digs; I opens the pack; J shows stories/history; M shows known places. F3 inspects procedural samples and generation stages. Combat has six actions with visible enemy intents. Existing saves retain the original three-area campaign and its controls. See the [play guide](docs/RPG.md).

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

This is a playable world foundation: fixed job recipes, sparse relationships, regional trade/raids and three main-arc families. Further content and simulation depth remain future work. Equipment is class-defined; tactical walking/range, gear swapping, directional animations and controller input are not implemented.
