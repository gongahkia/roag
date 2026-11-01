# Roag

Roag is a C# / MonoGame turn-based dungeon game. The current delivery tranche
is a solo run: choose one of 15 classes, explore persistent physical floors,
acquire boons and utilities, clear three paired stages and fight the Hollow
Regent. It uses the imported Vagabonds world/chunk foundation and universal
sprite mappings, with handwritten native combat and a shared action timeline.

**Items 1–6 are ready for the requested Linux playtest.** Implementation is
stopped for your feedback; see the [build and verification handoff](docs/MIGRATION_PLAYTEST.md).
Party, survival, living-world integration and the
larger campaign remain approved later-tranche work. See the authoritative
[scope](docs/MIGRATION_DECISIONS.md), [current evidence](docs/MIGRATION_PROGRESS.md)
and [preservation checklist](docs/MIGRATION_PRESERVATION.md).

## Run locally

```sh
./tools/setup-dotnet.sh  # only if a .NET 10 SDK is not already available
./roag.sh
./roag.sh --new --seed 42 --save "$HOME/.roag/playtest.json"
./roag.sh --load --save "$HOME/.roag/playtest.json"
```

Normal launch resumes `~/.roag/campaign.json`, or starts a fresh native dungeon
when the slot is absent. Choose your fixed class before the first paid action.
WASD/arrows move one tile per press; F aims basic, R special, X movement skill,
G Guard, Space Wait, Enter interacts, U opens utilities, I/V inspect boons, Tab toggles the explored minimap,
F1 opens help, Escape cancels/opens the menu and F5 saves. Menus and thinking
consume no time. Mouse clicks inspect/interact/aim; they never plot walking.

[Linux play/build instructions](docs/PLAY_LINUX.md) describe the continuing save,
settings, controller mappings and self-contained packaging command:

```sh
python3 tools/package-linux.py
```

The native client needs no Lua/LÖVE runtime or sibling checkout. The generated
migration prototype is preserved in [Roag.Reference](src/Roag.Reference/README.md)
for tests and deferred-system references; it is excluded from the game.
Retained world/legacy previews and their authoring tools are development
capabilities, separate from the current bounded-dungeon release. Their earlier
[world guide](docs/RPG.md), [architecture](docs/WORLD.md) and
[origin](docs/ORIGIN.md) remain available as preservation references.

## Content authoring

`./dev content list`, `./dev content validate`, `./dev content inspect ID`, and `./dev preview character character.healer --seed 42` use the shared runtime content model. Definitions live under `content/`; [the authoring guide](docs/CONTENT_AUTHORING.md) covers references, scaffolding, removal, schemas and extension points. Run `./dev test` and review tooling at each substantial milestone. `./dev audit-world --seed 40 --samples 8` produces a JSON/SVG comparison; `./dev preview encounter boss.ash-knight --seed 42` exercises actual combat. `./dev verify-native` runs optional local X11/WSLg input acceptance using temporary saves (requires xdotool/ffmpeg and a built client).

## Agent development workflow

Ordinary world launches automatically retain bounded local diagnostics; use Menu → Mark this moment to capture an issue. `./dev sessions list` and `./dev sessions report ID` make those records readable to coding agents. `./dev playtest start` opens an isolated rendered game that agents can observe/drive, and `./dev playtest agent ID --model gpt-5.6-luna --reasoning low --objective 'Explore and report confusing interactions'` runs an optional bounded model tester. `./dev agent verify --profile playtest --detach` runs recorded regression checks without paid model calls. See [session recording and playtesting](docs/SESSION_PLAYTESTING.md).

`./dev agent context` summarizes the checkout; `./dev agent doctor` reports prerequisites; `./dev agent impact` suggests relevant checks. Use `./dev agent verify --profile full --detach` for durable verification with logs and reconnectable run IDs. Explicit task requirements/checkpoints retain handoffs and require fresh evidence before completion. Content merge patches, deterministic JSON scenarios and read-only save diagnostics support reproducible debugging. See the [agent workflow guide](docs/AGENT_TOOLING.md) for examples and Linux/WSL worker requirements.

If launch reports “World content differs from this build,” the earlier save is preserved. Follow the error's new-slot command, or use `./roag.sh --new --save "$HOME/.roag/native-playtest.json"` with an unused filename. Resume with `./roag.sh --load --save "$HOME/.roag/native-playtest.json"`; bare launches still select the original default slot.

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

The retained Vagabonds foundation includes WFC/BSP/cavern/ring generation, companions, equipment, work, needs and world simulation. Their integration into the later Roag campaign remains deferred; see the migration scope and evidence above.
