# Ithica integration verification

**Recorded: 2026-10-20. Platform: macOS 15.7.7, Intel x64, .NET 10.0.13 / SDK 10.0.402, Release.** This is evidence for the implemented integration. Windows/Linux CI configuration is not a claim of native graphical testing there.

## Acceptance results

| Check | Result / scope |
| --- | --- |
| Locked restore | Passed for `Ithica.sln` |
| Release build / publish | Passed with zero warnings/errors; published normal client includes its runtime artwork, atlas index, manifests and credits |
| Core suite | **131 passed**: the original 97 cases plus 34 integration/continuation cases |
| Real integrated loops | **32 seeds, 0–31**, completed rescue, recovery, salvage combat, returned cargo, an actually constructed powered upgrade and exact final JSON continuation |
| Deeper loop | Command-driven test repaired pump, opened seal, cleared encounter, returned component and verified persistent water production |
| Lunar generation | **1,000 seeds, −500–499 / 2,000 sites**, valid, zero fallback |
| Native integrated smoke | **18 checks passed**, including direct map preparation, solo departure, decision freeze, keyboard action cost, field zoom bounds/anchor, actual rescue, combat, exact combat save, injuries/cargo, component construction/refunds and full atlas loading |
| Native scenes | Colony, preparation, surface, battle, structure, arrival and atlas rendered at 1440×900 and/or 1024×720; minimum-size overlays inspected |
| Published output | Integrated smoke passed from `/tmp` outside the repository using the published DLL and its copied artwork |
| Legacy behavior | Original colony/RPG core cases retained; fantasy headless quest remains the explicit `--preview` regression path |
| Original assets | All 664 PNG / 3 archive hashes retained; all 11,887 original source IDs/crops/identity records reconciled |
| Catalog | Schema 4 generated content fresh; 166 current selected IDs, 14 field aliases/IDs, 75 preview-only IDs, 6,874 unbound occupied regions |
| Offline atlas | **14 browser checks passed** in headless Chromium, including current/preview/unused counts, lunar search/metadata, pagination, actual palette pin/scene painting, JSON/PNG export, mobile width and no page errors |

The 18 native checks are automated within the actual MonoGame client, including its map-click and keyboard handler paths; they are not a claim of a long manual balance playthrough. No controller session or native Windows/Linux session was performed. Preview screenshots, smoke, headless runs and benchmarks do not write player saves.

The new cases cover all saved phases, variable/nonsequential party IDs in combat, one-time stock/cargo/component transfers, canceled/invalid actions, colony clock remainder and tick budgeting, no deliberation-time advancement, shared needs/work exclusion, paid care/refills, escorted oxygen, fractional morale preservation, emergency return, failed-component recovery, persistent revisits/access and non-destructive legacy import.

## Fedora WSL local setup

**Recorded: 2026-10-20. Platform: Fedora Linux 44, WSL2 x64, WSLg, .NET 10.0.13 / SDK 10.0.402, Release.** Installed the SDK with `./tools/setup-dotnet.sh` into `~/.local/share/ithica/dotnet`; the existing launcher finds it without PATH changes. SDL2 compatibility, OpenAL and Mesa libraries were already present, so no additional system packages were installed.

| Check | Result / scope |
| --- | --- |
| Locked restore | Passed for all five solution projects |
| Release build | Passed with zero warnings/errors |
| Core suite | 131 passed, zero failed/skipped |
| Integrated headless loop | Seed 42 completed rescue, salvage, returned cargo and a constructed powered suit service; deeper site discovered |
| WSLg graphical smoke | All 18 automated checks passed through `./ithica.sh` at 1024×720; 12 frames rendered and `/tmp/ithica-local-smoke.png` captured and inspected |

This validates local launch through WSLg. It does not establish native Windows or standalone Linux desktop coverage, a manual balance playthrough, audio playback or performance results. These checks did not write player saves. Run `./ithica.sh` from the repository to play.

## Generic catalog foundation

**Recorded: 2026-10-20, Fedora 44 / WSL2 x64.** Schema 5 adds game-independent `sprites[].asset` metadata and standard-library pixel measurement. This checkpoint is the foundation for ongoing neutral visual authoring and atlas controls, not a claim that every ambiguous motif has been individually identified.

| Check | Executed result / scope |
| --- | --- |
| Generic catalog tests | `python3 -m unittest discover -s tests/catalog -v`: 14 passed; PNG filters/bounds/hash rejection, every imported PNG's dimensions, all original record fields, every generic descriptor/palette, binding counts and neutral queries |
| JSON Schema | Draft 2020-12 validation passed for all 11,887 `asset` objects against `assets/reference/asset.schema.json` using an isolated `uv run --with jsonschema` environment |
| Regeneration / freshness | `python3 tools/build_game_catalog.py` then `--check` passed; all 664 original PNG / 3 archive hashes and every region's frozen RGBA crop hash retained |
| Query examples | High-confidence `--subject ghost` finds `urizen-onebit-r42-c110`; actor/green queries return measured green figures; a neutral search for game casting `Interference` returns zero |
| Game bindings | 166 current IDs, 75 preview-only IDs and 6,874 wholly unused sprites preserved; no gameplay or save-format changes |

CI configures the generic suite on Linux alongside catalog freshness. That configuration is separate from the executed local results above. Systematic neutral visual authoring and presentation checks continue in later checkpoints.

## Neutral visual authoring: character and two-bit checkpoint

**Recorded: 2026-10-20, Fedora 44 / WSL2 x64.** Added 36 character-body and 98 prop/stance series descriptions, 269 individual observations (including retained observations and all 125 occupied two-bit crops), and 17 explicitly inspected coordinate sheets covering 3,256 occupied sprites. The full review generator produced 54 sheets with exact-ID coverage of all 7,115 occupied crops; uninspected generated sheets do not count as visual evidence.

Pinned historical author PNG/JSON files were fetched explicitly into ignored `artifacts/upstream-urizen/`. Exact binary foreground-mask comparison produced 1,095 matches, including 81 previously unconfirmed crops. The evidence JSON retains source URLs/hashes and every alternative match; catalog generation checks each mask hash against the current crop. Only shared terms become medium-confidence silhouette claims, and original source identities remain unchanged.

Executed validation: **22 generic tests passed**, full regeneration and `--check` passed, and all **11,887** generic metadata objects passed the JSON Schema. New cases cover source confidence preservation, conflicting/wrong shape evidence, two-bit label corrections, matrix-edge exceptions, prone-pose uncertainty and tiny publisher-identified glyphs. All original PNG/archive/crop hashes and 166 current / 75 preview-only / 6,874 unused selections remain intact. Remaining art authoring and generic atlas controls are still in progress.

## Neutral visual authoring: terrain and objects, first section

**Recorded: 2026-10-20, Fedora 44 / WSL2 x64.** Inspected the five coordinate sheets for one-bit columns 0–24 and added individual descriptions for every previously undescribed uncertain crop in that section: structural textures, plants, animals, food, clothing, furniture, jewelry, containers and effects. Current authoring totals are 628 individual observations and 22 sheets covering 3,977 occupied crops. A cyan paw-print motif is classified as a symbol rather than inheriting the regional animal category. Portable publisher-labelled objects use consistent `item` grouping.

Executed validation: **25 generic tests passed**; deterministic regeneration/`--check`, all original source/crop hashes, and JSON Schema validation of all **11,887** generic objects passed. Added cases check qualified visual subject claims, footprint classification and portable-object grouping. Current/preview gameplay selections remain 166 / 75, with 6,874 wholly unused crops. Further art sections and atlas presentation remain in progress.

## Neutral visual authoring: structures, equipment and ornamental assemblies

**Recorded: 2026-10-20, Fedora 44 / WSL2 x64.** Inspected the five one-bit coordinate sheets for columns 26–50 and added 323 individual observations. Current totals are 951 individual observations and 27 sheets covering 4,433 occupied crops. Descriptions distinguish portable equipment, columns, gates, hanging banners, frames and heraldic fragments. Multi-cell motifs retain fragment status without invented faction identities or assembly sockets. Reviewed one-pixel frame corners remain contextual fragments with an explicit uncertainty note.

Executed validation: **26 generic tests passed**, deterministic catalog freshness/original-file and crop-hash checks passed, and all **11,887** generic objects passed Draft 2020-12 schema validation. Original identities and 166 current / 75 preview-only / 6,874 unused selections are unchanged. Further art sections and atlas controls remain in progress.

## Neutral visual authoring: machinery and technology

**Recorded: 2026-10-20, Fedora 44 / WSL2 x64.** Inspected the five one-bit sheets for columns 52–76 and added 273 individual descriptions, including modular machine panels/supports, conduit fragments, building/vehicle silhouettes, handheld devices, colored cards and banknote-like markings. Current totals are 1,224 individual observations and 32 sheets covering 5,080 occupied crops. Currency-like glyphs do not establish denominations; modular shapes do not supply functional connections.

Executed validation: **26 generic tests passed**, all **11,887** metadata objects passed Draft 2020-12 schema validation, and regeneration/freshness plus original PNG/archive/crop hashes passed. The builder now rejects invalid authored composition values, and the full-record contract checks their vocabulary. No game bindings or source images changed. Remaining art sections and atlas controls are still in progress.

## Neutral visual authoring: spacecraft, celestial artwork and symbols

**Recorded: 2026-10-20, Fedora 44 / WSL2 x64.** Inspected the five one-bit sheets for columns 78–102, with enlarged crops for dense symbol rows, and added 345 individual descriptions. Current totals are 1,569 individual observations and 37 inspected sheets covering 5,581 occupied crops. Coverage includes craft silhouettes/fragments, celestial quadrants, small figure/prop poses, beam motifs, playing-card ranks, status symbols, moon-like disk shapes and the multicolored edge glyph. Descriptions retain uncertainty about species, animation, device functions, card values and symbol meanings.

Executed validation: **26 generic tests passed**, all **11,887** metadata objects passed schema validation, and deterministic freshness plus original-file/crop hashes passed. No source images or game bindings changed. Bottom/edge figure exceptions, icon/control inspection and atlas controls remain in progress.

## Neutral visual authoring: complete Urizen inspection

**Recorded: 2026-10-20, Fedora 44 / WSL2 x64.** Inspected the remaining four bottom figure sheets and revisited edge/short-series exceptions, adding 135 individual descriptions. All 5,676 occupied Urizen crops are covered by 41 explicitly inspected pages. Current neutral authoring contains 1,704 individual observations plus the 36 body / 98 prop series. Every uncertain crop outside the main series now has an individual description; no occupied record remains at category-only or inspected-without-description status.

Original misleading working labels remain preserved in the frozen source fields. Neutral names describe the actual exceptional creature/pose instead of inheriting a neighboring hammer, bucket or profession. The generic matrix relationship is restricted to the reviewed main area; out-of-matrix source grouping remains available in the original fields. Species and animation remain evidence-scoped.

Executed validation: **27 generic tests passed** after correcting a new test's mistaken assumption that the old bottom-row working label was also a structured matrix field. All **11,887** metadata objects passed schema validation; catalog freshness and original PNG/archive/crop hashes passed. Icon/control visual inspection, richer icon descriptions and atlas controls remain in progress.

## Neutral visual authoring: icon/control packs and complete inspection

**Recorded: 2026-10-20, Fedora 44 / WSL2 x64.** Inspected all six Godot and seven input contact sheets. Every one of the 7,115 occupied crops now has a recorded inspection reference across 54 sheets. Multi-source inspection entries retain each exact source/crop/hash; regeneration validates them against the frozen import. There are 1,704 individual Urizen observations, 36 body / 98 prop series, and 137 authored neutral icon-family descriptions. All 786 prompts expose control, category, visible appearance and size; keyboard A and controller A retain separate variant groups. The 146 publisher-labelled glyphs expose exact characters/code points using the runtime font's shared punctuation aliases. The unknown multicolored edge glyph remains uncertain.

Executed validation: **32 generic tests passed**. They cover all occupied inspection/useful-description evidence, neutral icon shape queries, prompt device/state separation, glyph code points and full canonical/viewer metadata/export agreement. All **11,887** generic objects passed schema validation. Regeneration and `--check` passed, preserving all source PNG/archive/crop hashes and current/preview selections. Runtime font regeneration selected the same 154 IDs / 60 bindings / 94 glyphs. Neutral JSON and CSV exports retain original identity evidence and subject-level confidence. Atlas interface implementation/interaction verification remains in progress.

## Reproduction

```sh
./tools/dotnet.sh restore Ithica.sln --locked-mode
./tools/dotnet.sh build Ithica.sln --configuration Release --no-restore
./tools/dotnet.sh test Ithica.sln --configuration Release --no-build
./ithica.sh --headless
./ithica.sh --validate-seeds 1000 --seed -500
./ithica.sh --campaign-benchmark --samples 32 --seed 0
./ithica.sh --smoke-test --window-size 1024x720 --screenshot /tmp/ithica-smoke.png
./rpg.sh --headless
python3 tools/build_game_catalog.py --check
./tools/dotnet.sh publish src/Ithica.Client --configuration Release --self-contained false --output artifacts/ithica
```

To validate a published copy, run `Ithica.Client.dll` using an installed .NET 10 runtime from a working directory outside the repo, with `--smoke-test`. Run native captures/checks sequentially; use already-built binaries when checking several scenes to avoid concurrent builds of the same project. Performance samples must run without competing benchmarks/builds.

The offline browser check opened `assets/atlas.html` directly with Playwright/Chromium, exercised selection/search/filters, painted a sprite and downloaded real JSON/PNG exports. The temporary runner and captured test files are outside the repository; the catalog freshness/original-hash check is the persistent automated asset check in CI.

## Interface review

The native client retains its established bitmap font, palette, rectangular surfaces and sprite icons. Relevant screens were inspected at normal/minimum sizes; new gameplay callbacks queue into Update rather than advancing simulation from drawing.

| Category | Inspected evidence | Result |
| --- | --- | --- |
| Typography | Native colony/field/preparation/arrival/atlas captures; logical tick and supply figures | Bitmap text remains legible, with fixed-width numbers and wrapped descriptions |
| Surfaces / targets | Resident selection, pack controls, action buttons, modal boundaries and notice overlay | Dense desktop targets use at least 40 px; modal/notice clicks are kept out of map movement |
| Motion | Static phase transitions, existing press feedback, user-initiated route pacing | No new timed entrance animation; presentation timing does not drive logical turns |
| Icons | Original sprite crops in current game/atlas, masks and original preview | No new icon library or invented animation set; source identity and casting remain separate |
| Performance | Lazy viewer images, queued callbacks, tick-start budget, combined/colony samples | No full distant-site update; actual measured limits retained in PERFORMANCE.md |

A new font/icon set was rejected because the established pixel art and mapping contract already provide the visual language. Simulating every inactive site's cells was rejected for this delivery in favor of saved state and explicit campaign-time deadlines. No arbitrary large-population frame-rate guarantee is inferred from these checks.

## Rollback checkpoints

The work is committed in cohesive checkpoints; use `git log --oneline` for the final hashes and prefer targeted reverts when preserving subsequent shared history.

| Commit | Boundary |
| --- | --- |
| `d9a275c` | Recorded design/acceptance baseline |
| `a9eb189` | Consolidated Ithica projects, tools and retained preview paths |
| `e5fc53a` | Shared clock/residents/expeditions/unified saves and initial integration coverage |
| `daa6ecb` | One normal client, map preparation, field UI, atlas and headless loop |
| `80d6067` | Single collapse settlement and deeper continuation/failure coverage |
| `89c5d72` | Schema-4 lunar casting, current/preview separation and atlas metadata |
| `9516106` | Recovery/escort identity, retained frame budget, failure-safe component recovery and 131 passing cases |
| `64b4d91` | Native atlas text focus and authoritative recruited-resident inspection |

Later checkpoints finish recovery/escort/timing/UI safeguards, record measurements and consolidate the final references. The temporary integration plan is removed only after acceptance; its implemented decisions live in [PROJECT_STATE.md](PROJECT_STATE.md), [ARCHITECTURE.md](ARCHITECTURE.md) and the specialist guides. No remote push is part of this work.
