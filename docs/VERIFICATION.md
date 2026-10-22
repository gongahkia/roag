# Vagabonds verification

## Content authoring foundation (2026-10-22)

Release restore/build and **180 tests** passed: the retained 164 plus 16 content/CLI cases. Checks cover authored/embedded equivalence, stable references, unknown fields/handlers/sprites, invalid costs/nulls, edited profiles and abilities driving actual world commands, paid healing and invalid-action preservation, removing recruitment, safe scaffolding/deletion, schema reproducibility and deterministic interaction previews. `./dev content validate` passed for 78 definitions; character and healing previews exercised real Core creation/commands without player saves.

The native world smoke passed all seven checks for 120 frames at 1024×720, with a visually inspected world/cursor/route capture. It averaged 67.3 FPS in this local WSLg sample. Reports/captures are ignored under `artifacts/integration/`. No player save was read or modified. The independent Ithica reference remains development-only; source artwork/catalog/bindings are unchanged.

World format version two pins the authored content hash and safely rejects previous/incompatible world slots; no migration is claimed. Existing startup/reload, routing, interpolation and legacy tests remain passing.

## Clear outdoors with dungeon fog (2026-10-22)

Release build/publish passed with zero warnings/errors and all **164 tests** passed. Five additional visibility regressions cover the unobstructed surface view, unchanged clock/actors/resources/random state, distant/underground privacy, cached view refreshes after movement/edits, dungeon entry/return, persistent discovery and world-edge clipping. Save format and generation version remain unchanged.

The published client took actual generated stairs underground and back through keyboard input at 1024×720. The dungeon retained its radius-11 discovery (377 cells in the open entrance fixture), dark unknown ground and a selector without a route in unexplored terrain; returning restored the full outdoor view. Zoomed dungeon/surface captures and the updated guide were inspected. The seven world smoke checks passed for 360 frames at 1440×900; all 25 legacy graphical checks passed. Temporary slots/captures/reports are under ignored `artifacts/movement/`; preview smoke created no save and no player save was read or modified.

The larger clear outdoor view now batches backgrounds, terrain and props separately. One measured 360-frame published run averaged **60.9 FPS**, **20.50 ms p95 frame interval**, **0.302 ms p95 update**, and nine resident chunks. Host/compositor variation remains substantial: preceding captures ranged from 48–56 FPS before batching, while a contemporaneous old published build measured 63.7 FPS. These are local scenario measurements rather than a guaranteed frame rate. Partial edge tiles/sprites are scissored to the map, and moving dungeon actors are not drawn while their interpolated cell is hidden.

## Smooth travel, facing and route feedback (2026-10-22)

Release build passed with zero warnings/errors and all **159 tests** passed. Nine new graphics-free client cases cover fractional tile motion, exact endpoints, frame-rate independence, corner preservation, left/right facing with vertical retention, stair/long-transfer resets, hover state/RNG/clock preservation, unknown-cell privacy, preview invalidation, zoom, menus and tools.

Actual WSLg input at 1024×720 showed the cursor brackets and teal hover route, then a persistent gold route after moving the cursor outside the map. A paused click left the saved state unchanged; Space followed all five selected cells, and subsequent keyboard movement travelled left. Hover, queued-route and opposing-facing captures were inspected. All saves/captures are isolated under ignored `artifacts/movement/`; no player save was read or modified.

## Playable finite world and published acceptance (2026-10-21)

Executed locally on Fedora 44 x64 under WSL2/WSLg, SDK 10.0.402 / runtime 10.0.13, Release. Locked restore, build (zero warnings/errors), publish and **150 tests** passed: the retained 96 plus 54 generation/world/client regressions. Catalog freshness/source hashes and all **38 catalog tests** passed. The legacy headless quest completed and its 1,000-seed validation retained zero fallback zones. World macro validation passed 1,000 consecutive seeds; tests additionally cover both signed extremes and the seed whose mixed combat hash is zero.

An actual Core journey began alone at seed 42, recruited Lumi Hearth, followed generated roads and local terrain through **5,722 successful movement commands**, fought a naturally contacted encounter, took generated stairs to recover evidence, returned to Hollowwick 14, resolved the generated blight arc, advanced 300 more ticks and saved/restored the result. It ended at tick 6,022 with two surviving companions and 25 resident chunks. The verification driver planned known macro roads and local terrain while movement, discovery, encounters, combat, recruitment, shopping and resolution used ordinary Core commands. No actor teleport or campaign-state fixture supplied its progress. Separate tests cover digging/building and blocked-road recovery.

Actual native input at 1024×720 exercised pause/movement, conversation/recruitment, camp construction/rest, explicit save, inspector stage changes, ordinary resume and rejected F9 loading of an inherited field fixture. The selected temporary world retained its two companions, inventory and clock. Native captures of the inspector and a four-person battle were visually inspected; all four battle cards and abilities fit the minimum window.

A published-client keyboard check also opened the pack during combat: a number press consumed neither a turn nor random state, while closing the menu and pressing an ability performed the expected action. The generated-site preview passed its seven native checks at minimum size, with 18 resident chunks after prefetching the remote site.

The published output was copied outside this checkout to a temporary application directory and executed from `/tmp`. Its world render smoke passed all seven checks for 360 frames at 1440×900; its four-person battle passed all seven at 1024×720, and the original client passed all 25 legacy graphical checks. Headless world simulation, 1,000 macro seeds and source-preserving legacy import passed from that isolated distribution. All native fixtures/captures live under ignored `artifacts/world/`. No real player save was read or modified.

Fresh measured baseline: the published paused world rendered at **90.2 FPS average**, **13.31 ms p95 frame interval**, with **0.029 ms p95 client update** and nine resident chunks. A separate active 600-tick Core scenario measured **0.031 ms p95 / 3.70 ms maximum simulation tick**, conserved regional stocks/cargo and stayed at nine chunks. These measurements meet the 60 FPS / 8 ms simulation targets in these scenarios; they are not a guarantee for every seed, edit density or streaming transition. Chunk generation is synchronous behind prefetch and may hitch. Earlier warm-up captures varied with the host compositor. CI now includes world headless/macro/full-generation commands on the three configured operating systems; no new remote CI or native macOS/Windows execution is claimed.

## Finite world generation (2026-10-21)

The new generation tests pass alongside all 96 existing regressions: 1,002 macro seeds (−500 through 499 plus signed integer extremes), an overlapping model with more than 63 patterns, learned adjacency and forced contradictions, six culture footprints with constructive fallback/connectivity, chunk-order determinism, cross-chunk site slicing, eviction and persistent edits. The seed-42 full report generated all 24 towns and 48 sites on all four floors (216 footprints), with zero fallback regions and 4,138 deterioration/connection repairs reported separately. Samples learn 101, 111 and 99 patterns. Release build passed with zero warnings/errors. New sample maps and solver are original; existing PNGs/catalog/mappings were not modified.

## World simulation and persistence (2026-10-21)

All 124 tests passed in Release, including 28 world cases and the original 96. Coverage includes 1–4 party members with reordered/duplicate classes; solo start/recruitment/job vacancies; frame-rate independent ticks and pause reasons; regional stock/cargo conservation; complete road blockade → shortage/request → repair → resumed supply; building/digging/storage/stairs; invalid-action state/RNG preservation; exact saved combat continuation; chunk checksums and sample incompatibility; revival, immediate abandonment/wipe persistence, failed-save freezes/retry and new identities continuing the same world; dead patrons, explicit arc failure and continued simulation; and orphaned snapshots from interrupted saves. Fixtures are isolated temporary slots. `--validate-worlds 1000 --seed -500` passed the macro checks. `--world-headless` exercised actual recruitment, camp construction/rest and trade, then 600 simulation ticks with conserved regional resources and a bounded cache. This is a foundation simulation, with detailed tile movement nearby and scheduled regional consequences farther away.

Generation version two adds seeded, shuffled biome provinces with smoothly warped boundaries, grassy outdoor town spaces and explicit structural wall/door pins. Its fresh full seed-42 report generated 216 footprints in 4.92 seconds, with 14 bounded constructive fallback regions out of 864 and 4,445 separately reported repairs. The earlier zero-fallback result above describes the version-one unconstrained samples; it is not the current constrained result. Raw and repaired structure arrays are available for the client inspector. Null nested chunk, biome and battle objects are rejected as invalid data.

**Recorded: 2026-10-20, Fedora 44 x64 under WSL2/WSLg, SDK 10.0.402 / runtime 10.0.13, Release.** This records checks on the extracted application. The old source verification archive is provenance, not a claim of testing this fork on macOS or Windows.

## Executed acceptance checks

| Check | Result |
| --- | --- |
| Project restore | Regenerated locks for renamed projects, then locked restore passed |
| Release build / publish | Passed with zero warnings/errors; four-project Vagabonds solution |
| Core suite | 61 passed: 56 retained RPG cases and five extraction/import cases |
| Actual quest | Headless run completed with Victory true and the four original companions |
| Generation | 1,000 signed seeds −500 through 499 passed; zero fallback zones |
| Complete assets | All 710 tracked asset files were compared byte-for-byte with source Ithica `c87d480`; all matched |
| Catalog | Schema-5 regeneration freshness, all original PNG/archive/crop hashes and 38 catalog tests passed with no readable JSON export present |
| Offline atlas | 74 browser assertions passed, including compressed catalog, metadata/crop/scene downloads and all 7,115 exact crop RGBA hashes |
| Native client | Published Vagabonds client passed 25 actual graphical checks at 1024×720 under WSLg |
| Native atlas | All 15 native query/error cases matched canonical catalog ID lists/order in this repository |
| Visual inspection | Published minimum-size battle capture inspected; Vagabonds heading, party, battle and controls rendered |
| Repository boundary | All project references resolve inside this checkout; no source/asset/tool symlinks or submodules; no tracked file above 50 MiB |
| Save isolation | New default `~/.vagabonds/campaign.json`; imports preserve source bytes and exact continuation, reject self-overwrite and colony/field data |

The browser used Playwright 1.64.0 with the existing Chromium executable. Reports and captures are ignored under `artifacts/fork/`. No player save was read or modified. Test imports used temporary fixtures.

The existing Vagabonds initialization commits, GitHub remote and funding file are preserved. The user-owned untracked root AGENTS.md link was left untouched and is not part of the extraction commit. Source artwork, catalog and asset mappings were not renamed or reinterpreted.

## Isolated checkout and distribution acceptance

A fresh `git clone --no-local` of checkpoint `cdea68c` was created under a temporary directory without an Ithica sibling. Locked restore, clean Release build, all 61 core tests, the full headless quest, catalog freshness/hash checks and all 38 catalog tests passed there. The readable `assets/catalog.json` export was absent. All project references stayed inside the temporary checkout.

The published application and its copied runtime assets were then copied into another temporary directory and launched from a third working directory, outside both repositories. It passed all 25 native graphical checks and loaded all 7,115 sprites. A generated legacy RPG fixture was imported through the actual published CLI to a separate destination: source and imported bytes matched, the source was unchanged, and an attempted self-overwrite failed with exit code 1. No real player saves were involved. The temporary checkout/application/fixtures were removed after the check; persistent reports are `artifacts/fork/isolation.json` and `isolation-*.log`, with a captured `isolated-published.png`.

The initial staged whitespace check reported three inherited generated reference Markdown files with extra blank lines at EOF; those source asset files were preserved byte-for-byte, and the check passed with those three unchanged origin files excluded.

CI is configured across Linux/macOS/Windows; no remote CI run or new native macOS/Windows test is claimed.

## Audit fixes: safe startup (2026-10-21)

Executed locally under WSLg with SDK 10.0.402, Release. Locked restore and build passed with zero warnings/errors; all 75 tests passed, including 14 new graphics-free client startup cases. These cover exact saved continuation, a missing slot, explicit `--new`, invalid existing data, required `--load`, conflicting flags, explicit town/forest/atlas previews, frame limits, smoke mode and demo mode. The headless expedition completed with Victory true.

A freshly published client launched from `/tmp` with temporary fixtures. Ordinary launch followed by Q retained the saved seed, iron key and completed objectives; explicit town preview followed by Q left the save bytes unchanged; `--new --seed 7` followed by Q saved the requested new campaign. Its native smoke passed all 25 checks at 1024×720. Reports/capture are ignored under `artifacts/fixes/startup-*`. No player save was read or modified. Native quit automation used held key events after short synthetic key taps proved unreliable; exact state continuation is asserted by the automated startup tests.

## Audit fixes: discovered routing (2026-10-21)

Release build passed with zero warnings/errors and all 83 tests passed. Eight routing regressions cover a discovered destination with an unknown shortcut, no discovered path, unknown/out-of-bounds destinations, hidden hazard changes, visible hazard avoidance, hazardous-only routes, blocking object approaches and the original seed-42 destination `(11, 10)`. Planning leaves saved state and RNG unchanged. The existing complete-expedition tests and actual headless quest passed with the verification driver's full-map planning retained.

## Audit fixes: RPG-only reloads (2026-10-21)

Release build passed with zero warnings/errors and all 96 tests passed. Thirteen additional cases cover shared RPG-only loading, both startup load modes, exact battle continuation, generic field-session compatibility and the actual F9 handler. The handler is invoked without constructing a graphics window: lunar, invalid, malformed and missing saves preserve the active battle, RNG, pending route and both enabled/disabled autosave settings; a valid save restores exact battle continuation and enables autosaving.

A fresh published client launched from `/tmp` passed eight acceptance scenarios: automatic/explicit startup rejected lunar fixtures with exit 1 and unchanged bytes; invalid/malformed startup data produced handled errors without overwriting files; actual F9 input rejected lunar data while F5 still saved the active RPG; valid F9 input restored the saved key/seed and Q autosaved that campaign; the headless quest completed; and all 25 native smoke checks passed at 1024×720 without changing the selected save. Reports/capture are ignored under `artifacts/fixes/final-*` and `reload-*`. All fixtures were temporary test data, with no player saves read or modified.

Final repository checks also passed: 1,000 consecutive seeds −500 through 499 with zero fallback zones, catalog freshness/original artwork hashes and all 38 catalog tests. Native results remain local WSLg checks, not evidence of a new remote CI or macOS/Windows run.

## Integration checkpoint 2: world structure (2026-10-22)

Release suite passed all 182 tests, including the 1,002 signed/extreme macro seeds, layout connectivity/stairs, cache order/seams, generation-profile validation and actual runtime profile changes. The new `./dev audit-world --seed 40 --samples 8` exercised 136 complete footprints: six families, 136 unique raw layouts, one walkable component and required stairs throughout. No WFC fallbacks occurred in this sample. Repairs are counted separately: towns 99, WFC sites 813, caverns 28, BSP/ring layouts zero. Eight-seed SVG sheet was rendered and visually inspected; rooms/corridor forts, open caverns, radial sanctuaries and WFC structures are distinguishable. This is structural evidence, not a playtest judgement about rewards or encounter variety.

Dogfooding exposed destructive erosion/repair interactions in intact structures; erosion is now limited to ruins. Generation presets use the same validated content definitions as runtime; references, scaffolding and previews support this family. Schema regenerated and content validation passed. JSON/SVG/PNG reports and logs are ignored under `artifacts/integration/`. Gradient noise is adapted from pinned Ithica `070558d`; runtime remains independent. Generation version is now three; incompatible saves are rejected before replacement. No player save was read or changed. Native graphical acceptance will be repeated after the combat/simulation integration; no additional graphical run is claimed at this checkpoint.

## Integration checkpoint 3: party adventures (2026-10-22)

Release suite passed all 188 tests. New coverage exercises every one of 48 authored bosses through four rounds of actual combat, distinct phase/minion designs across at least 16 families, seed-selected final threat variation, depth access before any arc progress, fixed intentions/targeting, health phases, free thinking versus exactly five shared ticks per completed round, immediate final-boss run completion, one-time caches and equipment conservation/stat effects. Existing exact battle/save continuation and safe startup/F9 tests passed with the new plans/adventure state. Content/schema validation and encounter preview/tooling commands passed; reports are ignored under `artifacts/integration/`.

A minimum-size `--scene world-boss --world-smoke --frames 120` native WSLg run passed all seven checks. Its capture was visually inspected: four party members, boss/minion, intent target labels, skills and final-threat objective render without overlap at 1024×720. Measured average 105.9 FPS, update p95 0.357 ms in this local sample. No player save was used. Balance and long-form encounter enjoyment still require playtesting; diagnostic previews do not establish either. Shared needs and timed work are the next checkpoint.

## Integration checkpoint 4: shared actors and work (2026-10-22)

Release suite passed all 202 tests. New rules checks cover party/resident needs parity, recruitment/dismissal identity, delayed construction, partial cancellation and exactly-once refunds, worker paths/continued identity after recruitment, invalidated targets, authored work/costs and exact dismantling receipts, full work/body/memory/incident-RNG save continuation, timed rest/danger interruption, repair/crafting, gathering work/cooldown and malformed simulation saves. Existing instant-construction scenarios were updated to wait for work completion; exploration and storage assertions remain. Every work recipe and incident also ran through its real preview/handler. Content/schema validation and tooling review passed for 135 definitions across 14 kinds. Custom profiles/handlers are validated.

The actual `--world-headless` scenario passed recruitment, timed camp construction/rest, trade and regional stock/cargo conservation after 600 additional ticks. Its accounting now subtracts the baseline counters after timed preparation, rather than assuming preparation consumes zero time. Reproducible recipe, incident and shared-actor previews are ignored under `artifacts/integration/`. Simulation concepts were adapted from Ithica's pinned Community/Workforce/Maintenance/Crew rules into fantasy actors; lunar physics and colony prerequisites were not imported. No player save was read or modified. Graphical input acceptance follows in checkpoint 5.

## Integration checkpoint 5: published acceptance and tooling (2026-10-22)

Locked restore and Release build/publish passed with zero warnings/errors. All 210 .NET tests passed, including runtime preview coverage for all content families, every authored ability/boss/recipe/incident, invalid authoring commands/IDs, content schema freshness and exact saved continuation. Final review found that a fallen friend's memory could update actor stress without updating its combat unit; the shared memory handler now synchronizes both. A regression saves/loads that state exactly and rejects a deliberately mismatched actor/unit fixture. Save validation also rejects incompatible intent targets.

`./dev verify-native --dll artifacts/integration/publish/Vagabonds.Client.dll` passed all 15 local X11/WSLg checks: delayed/reserved work, worker assignment, construction, exact cancellation refund, timed rest, crafting, equipment, movement, rejected F9 state preservation, clean exits, boss encounter, menu consumes no turn/RNG/time, party ability, final-boss completion and a fresh run. Victory uses an explicitly wounded-boss fixture; it is not an unassisted balance playthrough. Minimum-size equipment/work/victory captures were inspected; controls/resources/body/gear and feedback fit at 1024×720. The reusable script uses temporary slots and rendering/focus readiness, replacing the earlier ad hoc input script that failed to quit reliably. No player save was read or modified.

Both published headless scenarios passed: the retained legacy quest reported Victory true, and the new world completed recruitment/timed camp/rest/trade plus regional conservation over 950 total ticks. The latter measured simulation tick p95 0.358 ms and maximum 0.981 ms in this local sample. Catalog freshness/original artwork hash checks and all 38 Python catalog tests passed; no tracked asset differed from the pre-integration tree. No source/project runtime references point into the ignored Ithica checkout.

Developer-tool review now reports 135 valid definitions across fourteen kinds, strict references/schema/handler checks, runtime-backed previews and reproducible JSON/SVG/native scenarios. Actual dogfooding prompted world variety auditing, full ability/supporting-content previews, CLI typo/path-ID rejection and reusable native input acceptance. Structural audits remain at 136 connected footprints across six families with separately measured repairs/fallbacks. Long-form encounter balance, reward pacing and subjective world excitement still require playtesting. No new remote CI, native Windows or native macOS run is claimed.

The final published client was also launched directly from `/tmp`, outside the repository working directory, and passed seven native world smoke checks at 1024×720 (120 frames, average 62.6 FPS, update p95 0.743 ms). Its surface capture was inspected. The original asset/catalog files remain unchanged; the referenced SDK installation is a shared development tool, not an Ithica repository/runtime dependency.
