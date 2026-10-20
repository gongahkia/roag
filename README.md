# Vagabonds

A standalone fantasy party RPG in C# / .NET 10 and MonoGame: four companions leave Hearth, explore Ashwood and the Sunken Abbey, recover an ember relic and bring it home. Exploration and combat share a top-down map, with visible encounters, enemy intents, stress, light, supplies, persistent generated sites and exact saves.

Extracted from Ithica’s retained RPG at `c87d480`. Ithica remains the separate lunar colony game. This repository contains its own source, projects, tests, complete original artwork, catalog, mappings, atlas and authoring tools; it needs no sibling checkout, symlinks or submodules. [Project state](docs/PROJECT_STATE.md) · [Play guide](docs/RPG.md) · [Development](docs/DEVELOPMENT.md) · [Origin](docs/ORIGIN.md).

## Run locally

Install the .NET 10 SDK, or use the optional per-user installer:

```sh
./tools/setup-dotnet.sh
./vagabonds.sh
./vagabonds.sh --load
./vagabonds.sh --seed 1337
```

`play.sh` and `rpg.sh` are aliases. On Windows or with a system SDK, run `dotnet run --project src/Vagabonds.Client --configuration Release`. The SDK wrapper can reuse an existing per-user SDK; those installations are tools, not dependencies on another repository.

New saves use `~/.vagabonds/campaign.json`. To copy an existing fantasy RPG save into the new save slot without changing its source:

```sh
./vagabonds.sh --import-legacy ~/.regolith/lantern.json
./vagabonds.sh --load
```

The import command validates and copies the campaign, then exits without a window. It accepts an explicit `--save PATH` destination. Lunar colony campaigns and field sessions are not RPG imports.

WASD/arrows or click to move; E interacts; T uses a torch; R camps; F1 opens the guide; F2 opens the full sprite atlas. Combat has six actions with visible enemy intents. See the in-game guide or [play guide](docs/RPG.md) for all controls.

## Build and inspect

```sh
./tools/dotnet.sh restore Vagabonds.sln --locked-mode
./tools/dotnet.sh build Vagabonds.sln --configuration Release --no-restore
./tools/dotnet.sh test Vagabonds.sln --configuration Release --no-build
./vagabonds.sh --headless
./vagabonds.sh --validate-seeds 1000 --seed -500
./vagabonds.sh --scene atlas
python3 tools/build_game_catalog.py --check
python3 -m unittest discover -s tests/catalog -v
```

Headless mode completes the actual quest without graphics or a player save. Scene captures and smoke checks also avoid player autosaves. [Executed verification](docs/VERIFICATION.md) distinguishes local results from configured CI.

## Complete artwork and mappings

All **7,115 occupied sprites / 11,887 indexed regions**, **664 original PNGs**, **3 original archives**, original credits, crop IDs, mapping evidence and generic descriptions are included. The complete [schema-5 catalog](assets/catalog.json.gz) is losslessly compressed; queries read it directly. Regeneration also creates the ignored readable `assets/catalog.json` export.

```sh
python3 tools/query_assets.py --type actor --color green --limit 10
python3 tools/query_assets.py --subject ghost --subject-confidence high --format ids
python3 tools/build_game_catalog.py
```

Open [assets/atlas.html](assets/atlas.html) directly for neutral searches, exact crop downloads and layered scene experiments. Historical `ithica-*` asset filenames and origin-specific catalog fields are deliberately preserved; the expedition aliases and `usage.rpg` provide the RPG mappings. [Asset ownership and authoring](docs/ASSETS.md) · [Original credits](assets/CREDITS.txt).

This is a finite three-area RPG prototype with one quest, not a completed branching campaign. Equipment is class-defined; tactical walking/range, gear swapping, directional animations and controller input are not implemented.
