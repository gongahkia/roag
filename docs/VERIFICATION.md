# Vagabonds verification

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
