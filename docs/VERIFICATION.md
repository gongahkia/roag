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

CI now configures the 14 generic tests on Linux alongside catalog freshness. That configuration is separate from the executed local results above. Systematic neutral visual authoring and presentation checks continue in later checkpoints.

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
