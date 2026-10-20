# Vagabonds extraction verification

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
