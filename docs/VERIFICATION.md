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

An isolated-clone build and an application run outside the checkout will be recorded as a separate follow-up. CI is configured across Linux/macOS/Windows; no remote CI run or new native macOS/Windows test is claimed.
