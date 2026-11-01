# First playable tranche: verification and handoff

Scope is items **1–6** in [MIGRATION_DECISIONS.md](MIGRATION_DECISIONS.md): a solo bounded dungeon game, followed by the user's playtest. Items 7–20 remain deferred; full authoring parity and Windows are outside the current todo. This is not completion of the full migration or its eight original gates.

Status (2026-11-01): **items 1–6 complete and ready for the requested Linux playtest. Implementation is stopped pending the user's feedback.** Final-code regression: **1,718 passed, zero failures/skips (12m55s)**. Full migration remains incomplete.

## Linux build

- Archive: `dist/roag-linux-x64-d48bfd62.tar.gz` (40,853,072 bytes).
- SHA-256: `f43fb0c72ca98d087f7408f82714a46270bc22c91ad7255c7216cf9b0de56861`.
- Source: clean `d48bfd62`, adding the requested boon/chest illustrations to the earlier startup-recovery fix. The original fully regressed `181c9c8c` and recovery `34e73fa8` packages remain preserved. **29 affected C# checks and 38 catalog checks pass** for the illustration change; the 1,718-test full baseline predates these presentation/diagnostic follow-ups. Native content identity/save schema/gameplay rules are unchanged, and actual existing native saves reload correctly.
- Extract, enter `roag-linux-x64`, run `./roag`. The package bundles its runtime and needs no SDK, Lua/LÖVE or sibling checkout. See [PLAY_LINUX.md](PLAY_LINUX.md) for source builds, controls and save locations.
- `BUILD.json`, `SHA256SUMS`, the adjacent archive checksum and bundled dependency/artwork/font notices identify and check the delivered files.

If the default `~/.roag/campaign.json` is an earlier prototype/content-version save, it is preserved and cannot resume this game. The error now gives new/resume commands for an unused separate native slot. For example, use `./roag.sh --new --save "$HOME/.roag/native-playtest.json"` once with an unused filename, then `./roag.sh --load --save "$HOME/.roag/native-playtest.json"`. Packaged commands use `./roag`. Keep selecting that path; a bare launch still selects the old default. This does not convert or overwrite the old save.

## Current-tranche coverage

| Item | Implementation and evidence |
| --- | --- |
| 1. Native production client | Handwritten typed C# runs against persistent actors/chunks and the shared timeline. Generated rules and the fixed-window adapter are reference-only test dependencies, excluded from production and the archive. New bounded runs do not materialize deferred living-world populations; seed-5 regression prevents an imported raider colliding with a native room. Existing development saves remain loadable. |
| 2. Original gameplay | Original class/action geometry, terrain, services, utilities, boon/proc and reward interactions have native/reference checks. The continuing rendered Ranger run exercises ordinary combat, growth, hazard evasion, utilities, chests and a keyed vault. Separate actual Adventurer gate/secret and Tinkerer resource checks supplement it. Food and the two hunger boons are explicitly deferred. |
| 3. Dungeon content | All 50 original controllers, 147 authored rooms and seven biome catalogs have native placement/behavior checks. The rendered seed-37 journey crosses all seven physical floors, defeats Thorn Warden, Mycelial Matriarch, Canal Lockmaster and Hollow Regent, and uses paid stage routes. All four bosses reached phase two in the resulting ordinary save. |
| 4. Rewards/build loop | The same run acquires assigned eligible boon offers, currency, XP/stat growth, health supplies, utility services and vault outcomes, retaining its build across saves. Focused checks cover one-time receipts and blocked/delayed elite rewards; the final Ranger has 13 assigned boons and level 10/240 XP. |
| 5. Presentation/input | Stored universal-atlas bindings, private inspection, visible locked attacks/timeline, phase and shield feedback, explored-floor minimap, keyboard/mouse/controller mappings, help and persistent presentation settings are implemented. Rendered controls/settings/map, combat and phase-two screens were inspected. All eight original PCM samples match byte-for-byte. |
| 6. Lifecycle/Linux delivery | Actual bundled apphost passes new-run, ordinary final death, terminal reload, fresh character/seed, exact continuing reload, victory reload and paid movement after victory. Extracted file hashes and direct launch without `DOTNET_ROOT` pass. The final-code full regression passes 1,718 tests with no failures or skips. |

## Evidence and reproduction

Evidence paths below are relative to `artifacts/migration/verification/`. Artifacts are ignored and may need copying or regeneration on another machine; source tools and this record remain tracked.

| Check | Result / evidence |
| --- | --- |
| Pre-final full integration | 1,716 passed, zero failed/skipped, 14m21s; `native-production/final-regression.{trx,log}`. Exact clean `3ea5ce0a` source/assemblies in `final-build.json` / immutable `final-runner/`. Predates the population/map/hover corrections. |
| Final population/save/routes/map | 69 passed, zero failed/skipped, 1m58s; `native-production/bounded-population.{trx,log}`. |
| Final-code full integration | **1,718 passed, zero failed/skipped, 12m55s**; `native-production/release-regression.{trx,log}`, clean `181c9c8c` identities in `release-build.json`, immutable `release-runner/`. |
| Actual continuing full journey | `native-production/muted-regent-finish/result.json`: `fullRunVerified: true`; victory at 374,088, paid movement to 374,162, exact saved/relaunched position/clock/full build/victory. |
| Final package victory continuation | `native-production/release-victory-continuation/`: exact cross-version continuing load, another paid move to 374,236, exact reload with victory preserved. |
| Final package fresh seed-5 lifecycle | `native-production/release-seed5-lifecycle/`: natural terminal death/reload at clock 4,785, fresh seed/living character at clock zero, exact fresh reload; 53 decision iterations, `lifecycleVerified: true`. |
| Final package map/input | `native-production/release-map/`: default explored map, real Tab/I keys, nonacting map click, paid keyboard step, exact reload. |
| Actual gate and secret | `native-production/rendered-gate-secret/`: pressure plate, occupied closure postponement, physical closing after departure, explicit basic strike revealing the cracked cache. |
| Final-code settings/resource integration | `native-production/release-settings-resource/`: actual Debug client from the same final game source; Tinkerer selection/aimed sentry, paid physical keyboard step, free help/menu/utility/inspection, every presentation control, exact resource/build/settings/save reload at clock 273. Sans/large/high-contrast/reduced-motion/mute preferences persist. Its OpenAL null backend initializes successfully, with no audible output. Earlier evidence remains in `native-presentation/interactive-navigation/`. |
| Boss visual inspection | `native-production/boss-frames/` and `muted-boss-frames/`: read-only coherent frames, including Canal/Regent locked attacks and phase two. Victory, terminal and fresh-run package frames also inspected. |
| Package checks | `native-production/release-package.log`, `release-checksums.log`, `release-help.log`, extracted package `release-extracted/`. |
| Post-handoff incompatible-save recovery | 17 startup/lifecycle checks pass, zero failures/skips, 10s; `native-production/startup-recovery-final.{trx,log}`. The user's real old manifest was copied into `startup-rejection/`; both original and copy remain byte-identical. The refreshed package passes file hashes and prints correct absolute packaged-launcher new/resume commands in `startup-package-rejection.log`. Its rendered fresh-launch/input/save/reload check passes under `startup-package-map/`. |
| Requested boon/chest illustrations | All 103 boons have stored imported-atlas icon bindings. `boon-sprites.{trx,log}` passes 29 affected checks; `boon-catalog-tests-final.log` passes 38 catalog checks and `boon-catalog-check.log` verifies regeneration. `rendered-boon-sprites/` naturally reaches a paid chest, checks three illustrated choices at 1024×720 with normal/large text, claims one and reloads the same illustrated build. `rendered-owned-boon-sprites/` checks all 13 owned icons across five pages from an existing completed save, free font/paging changes and exact continuation. Choice/owned screens were inspected. Updated archive file hashes pass under `boon-package-checksums.log`; actual packaged illustrated paging/save checks are under `packaged-owned-boon-sprites/`. |

The full seed-37 journey is **one continuously saved run across versioned clients and checkpoints**, rather than one uninterrupted invocation. Its chain is `packaged-journey` → `packaged-journey-continued` → `continued-hazard-routing` → `continued-real-input` → `packaged-final-journey` → `muted-final-journey` → `muted-regent-finish`. Each transfer used the latest ordinary saved slot, preserved its chunk snapshots and observed map, and checked handoff position, clock and complete player build. Driver-only navigation/input loops were corrected without editing game positions, damage, inventory or campaign saves. The last two package checks use the final shipped binary.

Run the regression and rebuild delivery with:

```sh
TMPDIR=/path/with/space ./tools/dotnet.sh test tests/Roag.Core.Tests/Roag.Core.Tests.csproj
python3 tools/package-linux.py
```

The wrapper uses the repository SDK; see PLAY_LINUX.md for setup. For rendered probes install Xvfb/xdotool, build the client or extract the package, then use a **new** evidence directory:

```sh
ALSOFT_DRIVERS=null SDL_AUDIODRIVER=dummy LIBGL_ALWAYS_SOFTWARE=1 \
  xvfb-run -a python3 tools/migration/verify_native_journey.py /path/to/fresh-evidence \
  --runtime /path/to/roag-linux-x64 --seed 37 --budget 6000
```

The probe uses actual visible input and ordinary saves. Defeat or a budget limit is reported as such, never as a successful full run. `--lifecycle --seed 5` checks the terminal/fresh-run path; `verify_native_map.py` checks map/input. Rendered journey clients now always force OpenAL null/SDL dummy audio, including resumed processes. SDL dummy alone did not ensure silence in earlier runs.

## Verification limits and next step

Verified here on Linux x64 using actual MonoGame rendering under Xvfb/software OpenGL, ordinary mouse controls and real X11 key states. A physical gamepad and audible output quality have not been tested. PCM preservation, controller state/mapping tests and silent device initialization do not establish those physical-device results. The complete rendered run samples Ranger/three stage biomes; the catalog/reference/native suites provide the broader class/enemy/biome coverage. It is not a manual playthrough of every class or route.

Implementation is now stopped for the user's playtest. Do not start party, survival, living-world, extra bosses, profile progression or other items 7–20 automatically. Retain their implementations and preservation references.
