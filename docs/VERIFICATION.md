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

## Neutral offline artwork atlas

**Recorded: 2026-10-20, Fedora 44 / WSL2 x64.** The offline atlas now defaults to artwork metadata and supports type, subject/claim confidence, measured color/coverage, primary color, exact RGB, layer, composition and sorting. Game casting is folded and excluded from ordinary search. URLs retain selection/filter/page state. Neutral metadata exports preserve original identity evidence, while every crop has an exact PNG download. The scene lab retains layered stable-ID JSON and rendered PNG export.

Executed validation: **33 generic tests passed**, including decoding and frozen-RGBA verification of every one of the **7,115** downloadable crops. `uv run --with playwright python tools/verify_asset_atlas.py --browser <existing Chromium headless shell>` passed **73 browser assertions** using Playwright 1.64.0. Checks exercised actual filters/sorts/pagination/URL reloads, subject thresholds, exact RGB and coverage, game-search opt-in, empty selection, real metadata/crop/scene downloads, scene painting/erasing, every embedded crop hash, original whole-image bytes for sensitive icons, 390px mobile overflow and zero script/network errors. Desktop/mobile screenshots in ignored `artifacts/atlas-verification/` were inspected. Source sheets also contain hidden RGB/partial-alpha values; PNG downloads now use generator-encoded frozen crops rather than a lossy canvas round trip.

Catalog regeneration/`--check` passed with original PNG/archive/crop hashes and game selections intact. Linux CI now configures the persistent browser check with pinned Playwright; no remote CI run is claimed here. Native atlas controls and final acceptance remain in progress.

## Neutral native artwork atlas

**Recorded: 2026-10-20, Fedora 44 / WSL2 x64.** Both normal and retained-preview atlases now use the shared `ArtworkCatalog.cs` metadata/query reader. The interface exposes pack/type/color/layer/composition/usage filters and sorting, subject/confidence/exact-RGB query tokens, exact palette/evidence details and optional game notes. F1 shows query syntax; F9 resets filters. Preview typing Q while the atlas is open now remains a search input, consistent with the normal client.

Executed validation: a clean Release build passed with zero warnings/errors and **131 core tests passed**. Actual WSLg smoke runs at 1024×720 passed **26 checks in Ithica** and **25 in the preview**, including loading all metadata/palettes/inspection references, high-confidence ghost lookup, default neutral versus optional game search, type/color/sort controls, invalid-query feedback and unchanged exact campaign JSON after filtering. `python3 tools/verify_native_atlas.py artifacts/native-atlas/current.json artifacts/native-atlas/preview.json` matched **15 native queries per client** against complete canonical Python result lists/order, including all 7,115 IDs and malformed color/hex/confidence/coverage cases.

Inspected keyboard-driven atlas captures at 1024×720 (ghost subject) and 1440×900 (513 green actors). Both accepted typed queries; the preview remained open while typing Q. The first normal-client input harness attempted focus before the window was viewable; its final captured query was verified, and subsequent targeted input waited for a visible window and checked focus before typing. Captures/reports live under ignored `artifacts/native-atlas/`; no player saves were written. The integrated headless rescue/salvage/construction loop and retained fantasy headless quest also completed. Catalog `--check` and all **33** generic tests passed. Final content/requirement audit continues separately.

## Final coverage audit: neutral identities and derived references

**Recorded: 2026-10-20, Fedora 44 / WSL2 x64.** The requirement audit found two remaining metadata issues and resolved them. Publisher enemy/NPC/player role labels now remain solely in original identity evidence, with neutral actor names/subjects such as Ghost. The one-pixel crop has an explicit gray-center-pixel name/coordinate rather than an inherited terrain label. All five ambiguous motifs remain explicitly unresolved rather than receiving invented identities.

The tracked-media audit accounted for **686 PNGs / 3 ZIPs**: the 664 original sprite PNGs plus 14 retained pack-preview composites and 8 rendered documentation references. The 22 derived images now have explicit reference-only classification, dimensions and hashes in `catalog.reference_images`, authored by `reference/derived-images.json`. Their composites are not new sprite regions. The old sprite-guide introduction and three generated pack-table headers now point to schema 5; headers were regenerated from frozen records through `write_pack_markdown`, without rebuilding the import audit.

Executed validation at this checkpoint: **35 generic tests passed**, all **11,887** asset objects passed schema validation, deterministic freshness/original-file/crop hashes passed, and all **73** real-browser assertions passed again. The generic suite checks complete tracked-media accounting and preserved publisher roles in original provenance. `git diff ed31f0a -- '*.png' '*.zip' assets/sprites.json` is empty. Final native-data refresh and completion evidence are recorded below.

## Generic catalogue completion acceptance

**Recorded: 2026-10-20, Fedora 44 / WSL2 x64.** Reconciled the completed library against the user's full game-independent catalogue objective, rather than current game bindings. All evidence below was inspected/executed locally; remote CI and native Windows/macOS rendering of the new atlas are not claimed.

| Requirement | Authoritative evidence / executed result |
| --- | --- |
| Every available source sprite | All 7,115 occupied crops across 664 originals; 54 recorded original-pixel inspection sheets; every uncertain crop outside the compositional matrix has an individual description |
| Whole repository / archive coverage | All 686 discoverable/tracked PNGs accounted for as 664 source files + 22 explicit derived references; all 3 ZIPs pass CRC, and all 662 archived images match indexed original bytes |
| Generic identity/type/subjects/colors/layers | `sprites[].asset`, 1,706 individual observations (1,704 Urizen + 2 icon exceptions), 36 body / 98 prop series, 137 icon families; exact foreground palettes, crop bounds, subject confidence/basis and provenance; all 11,887 asset objects validate against the schema |
| Uncertainty and future pivots | Five ambiguous motifs stay explicitly unresolved; source confidence remains 2,138 high / 440 medium / 4,537 low. Original source identities and publisher-role labels are preserved; game roles stay outside neutral names/subjects/classification |
| Fragments, variants and rendering limits | Composition/duplicates/variant groups retain original IDs; glyphs/prompts expose source-supported labels. No animation, direction set, collision, sockets or automatic assembly is invented |
| Machine access / source of truth | Canonical JSON, standard-library queries, confidence/color/layer/composition filters, sorting/facets and JSON/CSV/ID export; full canonical/viewer metadata agreement and all 7,115 embedded crop PNG RGBA hashes verified |
| Offline human access | 73 real-browser assertions passed against the final data, including URLs, pagination, confidence, measured/exact colors, actual downloads, scene painting/erasing, mobile viewport and no script/network errors; screenshots inspected |
| Native access | Final-data Release build: zero warnings/errors. Actual WSLg smoke: 26 current / 25 preview checks passed; 15 complete query-result/order comparisons per client matched canonical Python results. Keyboard-driven atlas captures at 1024×720 and 1440×900 inspected |
| Game/source preservation | All 131 game tests passed; integrated and retained-preview headless loops completed. All 664 original PNG / 3 archive hashes and frozen crop/identity records unchanged; 166 current / 75 preview-only / 6,874 unused selections retained; no player saves written |
| Repeatability / rollback | Final generic suite: 36 passed. Deterministic regeneration/`--check`, schema validation and clean diffs passed; implementation and verification committed in checkpoints, preserving concurrent user commits |

A final enlarged-icon check added individual observations for the rounded color face and filled color particle motif, with family descriptions covering their differing geometry. The generic suite also protects archive-to-original image coverage. The latest browser artifacts are in `artifacts/atlas-verification/`; final native reports are in `artifacts/native-atlas/` and were rechecked after the metadata cleanup. Main entry points are `assets/catalog.json`, `assets/atlas.html`, `tools/query_assets.py` and the F2 native atlas. Source/artwork ideas and current gameplay remain separate. No required work for this catalogue scope remains.

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
