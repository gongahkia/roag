# Roag

A standalone fantasy RPG in C# / .NET 10 and MonoGame. Begin alone in a seeded 4096×4096 world, recruit generated inhabitants, explore 24 towns and 48 adventure sites, tunnel, craft equipment and fight a seed-selected final threat from a pool of 48 boss archetypes. Turn-based party encounters, shared actor needs, timed work and a changing economy use one persistent world.

Extracted from Ithica’s retained RPG at `c87d480`. Selected generation, work and social mechanisms from Ithica’s replayed history are adapted into this fantasy game. This repository contains its own source, projects, tests, complete original artwork, catalog, mappings, atlas and authoring tools; it needs no sibling checkout, symlinks or submodules. [Project state](docs/PROJECT_STATE.md) · [Play guide](docs/RPG.md) · [Development](docs/DEVELOPMENT.md) · [Origin](docs/ORIGIN.md).

## Run locally

Install the .NET 10 SDK, or use the optional per-user installer:

```sh
./tools/setup-dotnet.sh
./roag.sh
./roag.sh --load
./roag.sh --new --seed 1337
```

`roag.sh` is the game launcher. On Windows or with a system SDK, run `dotnet run --project src/Roag.Client --configuration Release`. The SDK wrapper can reuse an existing per-user SDK; those installations are tools, not dependencies on another repository.

Normal launches resume `~/.vagabonds/campaign.json` when it exists, or start a new expedition when it does not. `--save PATH` selects another slot. `--new [--seed N]` explicitly starts over; saving or quitting then replaces the selected save. `--load` requires an existing save. Preview scenes and frame-limited captures do not autosave. To copy an existing fantasy RPG save into the new save slot without changing its source:

```sh
./roag.sh --import-legacy ~/.regolith/lantern.json
./roag.sh --load
```

The import command validates and copies the campaign, then exits without a window. It accepts an explicit `--save PATH` destination. Lunar colony campaigns and field sessions are not RPG imports.

The world client is mouse-only: hover to highlight a tile, click ground to travel, and click people, caches or stairs to approach and interact. Left-drag selects tiles; right-click opens relevant actions, including digging and building. Solid props and characters block movement. Play/Pause, Party, Work, Actions, Journal and World map are visible buttons; Menu opens the guide and procedural inspector. Click an ability and a fighter in combat. Right-click a panel to close it; Stop cancels travel. The world stays large, with a close RPG camera and bounded zoom. Existing legacy campaigns retain their original controls. See the [play guide](docs/RPG.md).

## Content authoring

`./dev content list`, `./dev content validate`, `./dev content inspect ID`, and `./dev preview character character.healer --seed 42` use the shared runtime content model. Definitions live under `content/`; [the authoring guide](docs/CONTENT_AUTHORING.md) covers references, scaffolding, removal, schemas and extension points. Run `./dev test` and review tooling at each substantial milestone. `./dev audit-world --seed 40 --samples 8` produces a JSON/SVG comparison; `./dev preview encounter boss.ash-knight --seed 42` exercises actual combat. `./dev verify-native` runs optional local X11/WSLg input acceptance using temporary saves (requires xdotool/ffmpeg and a built client).

## Agent development workflow

Ordinary world launches automatically retain bounded local diagnostics; use Menu → Mark this moment to capture an issue. `./dev sessions list` and `./dev sessions report ID` make those records readable to coding agents. `./dev playtest start` opens an isolated rendered game that agents can observe/drive, and `./dev playtest agent ID --model gpt-5.6-luna --reasoning low --objective 'Explore and report confusing interactions'` runs an optional bounded model tester. `./dev agent verify --profile playtest --detach` runs recorded regression checks without paid model calls. See [session recording and playtesting](docs/SESSION_PLAYTESTING.md).

`./dev agent context` summarizes the checkout; `./dev agent doctor` reports prerequisites; `./dev agent impact` suggests relevant checks. Use `./dev agent verify --profile full --detach` for durable verification with logs and reconnectable run IDs. Explicit task requirements/checkpoints retain handoffs and require fresh evidence before completion. Content merge patches, deterministic JSON scenarios and read-only save diagnostics support reproducible debugging. See the [agent workflow guide](docs/AGENT_TOOLING.md) for examples and Linux/WSL worker requirements.

If launch reports “World content differs from this build,” preserve that save and start a separate slot with `./roag.sh --new --save "$HOME/.vagabonds/expedition.json"` (choose an unused filename). Resume that slot with `--load --save` and the same path.

## Build and inspect

```sh
./tools/dotnet.sh restore Roag.sln --locked-mode
./tools/dotnet.sh build Roag.sln --configuration Release --no-restore
./tools/dotnet.sh test Roag.sln --configuration Release --no-build
./roag.sh --world-headless
./roag.sh --validate-worlds 1000 --seed -500
./roag.sh --world-report
./roag.sh --scene world-inspector
./roag.sh --headless
./roag.sh --validate-seeds 1000 --seed -500
./roag.sh --scene atlas
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
