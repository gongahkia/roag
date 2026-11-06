# Native profile worker

Assigned checkout: `/home/gongahkia/coding/projects/roag-worktrees/profiles`, branch `codex/native-profile`, base `9c7c3ee4` (implementation baseline `0d5cbf3b`). The full active goal was read from `/home/gongahkia/.codex/attachments/8ba90282-fe56-4de0-bbf8-a2fd4b85d637/goal-objective.md`. Applicable supplied AGENTS instructions and migration decision/progress/preservation/tuning/boss/playtest/overview/parallel documents govern this stream. This C# checkout has no Next.js package/docs; no Next.js code is involved. The sibling Vagabonds checkout is read-only and has not been changed.

All eight aggregate acceptance gates remain open. This record distinguishes implementation, focused backend verification, actual rendered input and final integrated delivery. The root coordinator owns production campaign generation, final-role selection, shared migration records and aggregate goal status. No legacy profile or Lua runtime conversion is added.

## Preserved inventory

Pinned original source: Roag `d498fe0889050d2a23f2ff86d80db6084ed6467a`, `src/achievement_content.lua`, `src/achievements.lua`, `src/profile.lua`, `tests/meta_tests.lua`, original `main.lua` profile/save ordering. The original 22 stable IDs and four rewards are retained in handwritten `Profiles/AchievementCatalog.cs`.

| Achievement suffix | Committed native observation | Permanent boon reward |
| --- | --- | --- |
| first_guardian | Actual EliteDefeated with dead original elite | guardians_vow |
| uncovered_truth | Member discovers a real secret | prospectors_heart |
| chain_reaction | Three distinct boon IDs from one member in one causal chain | circuit_chorus |
| regents_fall | Canonical Hollow Regent victory with a conscious survivor after complete resolution | crownfire |
| first_blood | Member kills a hostile | — |
| pathfinder | Paid physical floor change | — |
| treasure_hunter | Actual chest claim | — |
| coin_purse | Actual currency collection leaves at least 50 shared gold | — |
| first_level | Member reaches level 2 | — |
| veteran | Member reaches level 5 | — |
| boon_bearer | Actual boon grant | — |
| stonebreaker | Member breaks barricade terrain | — |
| ore_seeker | Member breaks an ore deposit | — |
| gate_opener | Member opens timed gate | — |
| key_bearer | Member collects vault key | — |
| warded | Member collects ward flask | — |
| torchbearer | Member lights brazier | — |
| long_heal | Member's native SupplyCollected: healing or approved actual paid hunger restoration | — |
| pressure_survivor | Real confined pursuer arrives, root special-site hook | — |
| deeper_road | Actual member entry into physical stage 2 or deeper | — |
| sanctum_reached | Actual member entry into physical Final Sanctum | — |
| regent_awakened | Actual Regent phase-two event | — |

`first_guardian` preserves the original executed EliteDefeated condition across original biome elites despite its Thorn Warden hint. Chain Reaction extends the original single-player observation per individual member, preserving separate builds: three different companions' unrelated activations cannot combine into one person's chain. Delayed causal observations persist and prune with actual retained causal work. Food uses the already approved hunger-only conversion; stocking food does not earn its consumption badge. No new numerical tuning is introduced.

## Storage and lifecycle policy

New format `roag-native-profile/1`, compatibility boundary `roag-achievements/1`, independent of changing world/combat content hashes. Two checksummed generation records, alternating atomic replacement with flushed file and exact read-back, bounded 128 KiB input, strict IDs/fields/counters, refusal to overwrite corrupt/incompatible progress. Run IDs, permanent awards and terminal markers are copied into both generations before returning. New run reservations may skip an ID after IO failure; they never reuse one issued from a successfully replicated reservation.

Profiles default to `SAVE.profile/`; `--profile PATH` explicitly selects another directory. Playtest CLI rejects profiles outside its isolated control directory and profile symlinks. Preview factories stay ineligible unless explicitly enrolled; ordinary native startup and isolated real-input playtests enroll fresh runs. Existing saves lacking optional profile progress remain unchanged/ineligible on load; no achievements are inferred retroactively from builds or dead enemies. Existing Lua/prototype profiles are never read.

The profile freezes its reward snapshot when a fresh campaign is enrolled. Existing run chest rolls/pool do not gain rewards midway. Death commits earned achievements and a replicated terminal receipt before the world manifest. A failed death manifest cannot resurrect the earlier living state. Saving a newer campaign advances a monotonic current-run receipt; earlier worlds cannot resume. The client already rejects in-game rollback; profile receipts add stale/terminal identity safeguards. Atomic profile commits and the world manifest are separate files: if IO fails after a manifest replacement, the client reports the failure and retains the actual session for retry, without claiming that a failed write restored earlier gameplay. External deliberate editing/deletion of all profile and save data is outside these safeguards.

## Integration APIs

- `WorldSession.EnablePermanentProfile(savePath, profileDirectory = null)`: explicit fresh native campaign only, before paid actions.
- `WorldSession.FlushPermanentProfile()`: flush actual earned outcomes; ordinary successful party commands call it after the full action and scheduled consequences settle.
- `WorldSession.Load(..., profileDirectory = null)`: validates the independent continuing profile only when optional native progress exists.
- `WorldSession.RecordDungeonEntry(member, stage, sanctum = false)`: root calls on actual committed physical member arrival, using that floor's stage. The bounded stairs implementation derives the actual floor directly. Do not call for camera selection, route opening or global complex advancement.
- `WorldSession.RecordPursuerArrival(pursuer)`: root calls only after special-site arrival; it checks an actual living `core:enemy/extraction_pursuer`.
- `WorldSession.RecordComplexVictory(boss)`: canonical original Regent-complex completion, independently of selected world-final role. Verifies that the actual owning saved complex has its Stage4 boss receipt and Victory flag, then requires the dead Hollow Regent and a conscious survivor. Post-drain settlement also observes these completed complexes idempotently.
- `WorldSession.RecordCampaignVictory(boss)`: root calls after settling actual selected-final-role victory in Endgame. The hook checks selected identity/dead body/living survivor and awards Regent's Fall only for Hollow Regent. Other selected final bosses do not masquerade as the Regent.

Source/profile-specific tests and evidence are being prepared; no compile/test/rendered pass or finished item-20 integration is claimed yet. Root remains responsible for the above new-world entry, special-site and selected-final settlement call sites after combining streams. Full solo/four-party campaign journeys and refreshed Linux delivery remain mandatory.

## Initial focused backend evidence

`c9b0d654` records the catalog/store/types inventory. The subsequent hook/client/test implementation compiled all projects and passed **17 focused tests, zero failures/skips**, terminal exit 0, test duration 7s. Evidence retained inside this worktree at `artifacts/migration/verification/native-profile/focused-v1.{trx,log}` and `focused-v1-build.json`, including actual source/runner SHA-256 inventories, base/HEAD/dirty source status and SDK **10.0.402**. This is focused backend evidence only.

Exact command (all automation muted, ample temp directory):

```sh
TMPDIR=/home/gongahkia/.cache/roag-tests-tmp ALSOFT_DRIVERS=null SDL_AUDIODRIVER=dummy LIBGL_ALWAYS_SOFTWARE=1 ./tools/dotnet.sh test tests/Roag.Core.Tests/Roag.Core.Tests.csproj --filter 'FullyQualifiedName~NativeProfileStoreTests|FullyQualifiedName~DungeonTerrainTests.CommittedSecretAwards|FullyQualifiedName~DungeonTerrainTests.ActualPaidAttackObserves|FullyQualifiedName~DungeonTerrainTests.DifferentMembersAndUnrelated|FullyQualifiedName~DungeonTerrainTests.FinalDeathRetainsAwards|FullyQualifiedName~DungeonTerrainTests.OldNativeSavesRemain|FullyQualifiedName~DungeonTerrainTests.MissingProfileRejects|FullyQualifiedName~DungeonTerrainTests.EntryBadgesRequire' --logger 'trx;LogFileName=focused-v1.trx' --results-directory artifacts/migration/verification/native-profile
```

These cases verify pinned original inventory, actual paid secret/attack observations, frozen pools, no time/RNG change, partial per-member causal saves, failed terminal manifest with retained awards/tombstone, fresh world retention, exact ordinary continuing saves, missing-profile refusal and absence of old-save conversion. Causal noncombination cases explicitly inject unit-event inputs into the observer; they are not represented as rendered gameplay. Storage fault tests use in-memory isolated records; lifecycle tests use unique temporary directories. No real user profile/save paths are used.

## Broader affected backend evidence

The second source batch passes **156 affected tests**, zero failures/skips, terminal exit 0, test duration **2m32s**, at `native-profile/affected-v2.{trx,log}`. `affected-v2-build.json` records the exact source/runner hashes and dirty source identity; it includes all new native profile checks plus native terrain/lifecycle, old client/save/chest/route and automation-isolation regressions. Exact filter:

```sh
TMPDIR=/home/gongahkia/.cache/roag-tests-tmp ALSOFT_DRIVERS=null SDL_AUDIODRIVER=dummy LIBGL_ALWAYS_SOFTWARE=1 ./tools/dotnet.sh test tests/Roag.Core.Tests/Roag.Core.Tests.csproj --filter 'FullyQualifiedName~NativeProfile|FullyQualifiedName~DungeonTerrainTests|FullyQualifiedName~WorldClientTests|FullyQualifiedName~SaveSnapshotTests|FullyQualifiedName~DungeonRouteTests|FullyQualifiedName~ChestTests|FullyQualifiedName~PlaytestBridgeTests' --logger 'trx;LogFileName=affected-v2.trx' --results-directory artifacts/migration/verification/native-profile
```

Review fixed an actual First Blood bug before integration: the world's live-hostility query returns false after death, so the observer checks committed dead native enemy identity. A real paid-kill regression now covers it. New cases also cover either generation's loss after run allocation/award/terminal, failed second award/terminal replica followed by retry, valid-checksum semantically corrupt newest payloads, all four future chest rewards and exact frozen offers, explicit preview eligibility, CLI custom-profile continuation/isolation, every free paged UI control at 1024×720/1440×900, actual stage/sanctum entry, solo/four-member canonical Regent victory and exclusion when nobody is conscious. These remain backend/fixture evidence, not natural full-party campaign completion.

`verify_native_journey.py` now copies an existing enrolled continuation's profile directory with its ordinary save/chunks; absent profile metadata never creates/enrolls an old fixture. `verify_native_profile.py` is prepared for natural rendered input; no rendered pass has yet occurred.

## Temporary user-requested pause — 2026-11-06

The coordinator relayed the user's explicit request to pause immediately. Profile feature work stopped; no new build/test/rendered probe was started after that request. No profile-owned process remains running. The last command batch was source editing plus lightweight `git diff --check`/Python syntax checks, not a C# build. The broader source batch's terminal result remains **156 passed, zero failures/skips, exit 0**; the first batch remains **17 passed, zero failures/skips, exit 0**. No rendered profile journey has been attempted and no final profile package exists.

Completed implementation commits (before this documentation checkpoint):

1. `c9b0d654` — native original achievement catalog, saved progress types and two-generation profile store.
2. `c3a5ff11` — committed event hooks, terminal/fresh-world lifecycle, client enrollment/isolated profile option, free paged presentation and initial 17 checks.
3. `88a8649b` — First Blood correction, broader lifecycle/canonical Regent/physical entry/future reward/IO/UI verification and prepared rendered probe; 156 affected checks pass.

Uncommitted follow-up source is deliberately preserved in this worktree for resume. It has **not** been compiled or tested:

- `src/Roag.Core/Profiles/NativeProfileStore.cs`: validate direct pending award inputs; always replicate a checkpoint on retry even when its first record already advanced the current-run marker.
- `src/Roag.Core/World/WorldProfiles.cs` and `src/Roag.Core/Tactics/TacticalProfile.cs`: root-agreed `RecordComplexVictory(boss)` plus post-drain observation. Requires that the owning saved complex's actual Stage4 boss has a death receipt and Victory flag, then verifies actual dead Hollow Regent/conscious survivor, independently of selected Endgame victory. Root has not integrated this hook yet.
- `src/Roag.Client/Program.cs` and `tests/Roag.Core.Tests/Client/NativeProfileClientTests.cs`: extend playtest isolation to symlinks in the profile record/lock files, with a corresponding isolated test.
- `tests/Roag.Core.Tests/Profiles/NativeProfileStoreTests.cs`: first-checkpoint-replica failure/retry followed by either-generation loss.
- New untracked `tests/Roag.Core.Tests/Profiles/NativeComplexProfileTests.cs`: canonical non-final Regent-complex guard tests. These construct actual original complex terrain and explicit fixture settlement boundaries; they are backend checks, never rendered claims.
- `tools/migration/verify_native_journey.py`: before copying an already enrolled continuation, confirm readable existing profile records belong to that saved profile; preserve damaged replicas for native recovery. No new identity/profile is invented for old saves.

Resume from this state; do not discard/reset these edits. First review/compile the pending follow-up and run an appropriately narrow profile/complex/checkpoint/isolation batch in a coordinator-approved heavy slot. If it passes, commit the coherent follow-up and obtain a rendered slot for `verify_native_profile.py` (natural solo secret, free normal/large pages, exact save, final death, terminal reload and fresh-world retained future-pool unlock). A rendered attempt may fail and must retain its evidence honestly. The current prepared script has not been run.

Root integration still needs actual member-owned physical complex/sanctum entry hooks, special-site pursuer-arrival hook, canonical complex-victory settlement and selected-world-final victory settlement, review/cherry-pick of worker commits, merged focused/regression checks, full solo/four-member campaign journeys and updated reproducible Linux delivery. All eight aggregate gates remain open. The root coordinator alone owns the goal's paused status/resumption and shared migration records.

## Resumed follow-up — 2026-11-06

The coordinator explicitly resumed the same goal/worktree after the temporary pause. Preserved WIP was reviewed in place and finished. **26 focused checks pass**, zero failures/skips, terminal exit 0, test duration **8s**, under `native-profile/followup-v4.{trx,log}`. `followup-v4-build.json` pins the actual source, test runner and built-client file hashes, SDK 10.0.402, base/HEAD/dirty source identities and TRX counters. The earlier `followup-v3.log` is retained as a compile failure: the new complex test lacked the `Roag.Core.Content` namespace for ActorFactory. Adding that test-only import resolved it; no assertion/gameplay rule changed.

```sh
TMPDIR=/home/gongahkia/.cache/roag-tests-tmp ALSOFT_DRIVERS=null SDL_AUDIODRIVER=dummy LIBGL_ALWAYS_SOFTWARE=1 ./tools/dotnet.sh test tests/Roag.Core.Tests/Roag.Core.Tests.csproj -m:2 --filter 'FullyQualifiedName~NativeProfileStoreTests|FullyQualifiedName~NativeProfileClientTests|FullyQualifiedName~NativeComplexProfileTests' --logger 'trx;LogFileName=followup-v4.trx' --results-directory artifacts/migration/verification/native-profile
```

This verifies the resumed canonical non-final Regent-complex hook/guard, first-checkpoint-replica retry protection, direct pending-award validation and profile-record/lock symlink isolation. The continuation copier checks that existing profile records identify the real saved profile before copying; it never enrolls old saves. The prepared natural driver now accepts either a visibly discovered native secret type, verifies the actual committed SecretDiscovered receipt, and checks fresh level/XP/boon/gold reset. No rendered attempt has yet completed and the tested source remains an isolated worker implementation, awaiting root integration and aggregate acceptance.


## Natural rendered continuation and second user pause — 2026-11-06

The coordinator integrated the completed worker implementation onto main separately: `c9b0d654 → 3060346c`, `c3a5ff11 → 1af1be91`, `88a8649b → 63c3e261`, the first pause note `9d3f81e3 → b6efb062`, and `abb3ba52 → 7a1b61b0`. Main integration/regression belongs to root. This worker's last implementation HEAD remains **abb3ba5250b33c1235235754bf806e1d54a263de**; this documentation checkpoint leaves the rendered driver edits tracked but uncommitted. No production C# or content changed during these probes.

All attempts use isolated directories inside this worktree, ordinary rendered keyboard/mouse/playtest controls, null audio/software rendering and the exact frozen **followup-v4** Debug client. Every one of its **699 runtime files** matches the tested client SHA-256 inventory. A separate proof checked **450 production source/content files**, all unchanged from the tested batch. Runtime/source inventories are retained in `native-profile/rendered-v2-runtime-proof.json`; final runtime/death evidence is `native-profile/rendered-v3/pause-proof.json`. These are focused worker artifacts, not a merged release or full-campaign acceptance.

Exact initial invocation:

```sh
ALSOFT_DRIVERS=null SDL_AUDIODRIVER=dummy LIBGL_ALWAYS_SOFTWARE=1 xvfb-run -a python3 tools/migration/verify_native_profile.py artifacts/migration/verification/native-profile/rendered-v1 --large-text --budget 1200 > artifacts/migration/verification/native-profile/rendered-v1.log 2>&1
```

`rendered-v1` did not pass. The generic ranged/cardinal-biased driver repeatedly dodged a diagonal Ruin Skitter instead of using the Adventurer's ordinary diagonal basic strike. No gameplay rule was changed. The supervisor stopped only this owned driver, released held keys, delivered an ordinary Save/capture/stop through the playtest protocol, and preserved the actual continuing state at **clock 45120**, floor1 tile215,170, HP23, run1 and three earned badges. The driver ended **130** deliberately; `driver-loop-checkpoint.json` retains raw requests, responses and observations, and `result.json` explicitly records `passed:false` with the driver loop reason. No rollback, HP/build/position/inventory/profile edits or injected achievement occurred.

The driver was improved to finish visible adjacent enemies through normal eight-direction Adventurer attacks. The same saved world/profile continued, without rolling another world:

```sh
ALSOFT_DRIVERS=null SDL_AUDIODRIVER=dummy LIBGL_ALWAYS_SOFTWARE=1 xvfb-run -a python3 tools/migration/verify_native_profile.py artifacts/migration/verification/native-profile/rendered-v2 --runtime artifacts/migration/verification/native-profile/rendered-v1/runtime --continue-from artifacts/migration/verification/native-profile/rendered-v1 --large-text --budget 1200 > artifacts/migration/verification/native-profile/rendered-v2.log 2>&1
```

`rendered-v2` exactly loaded/resaved that clock45120 manifest before paid input, killed the previously circling skitter with an ordinary diagonal strike, claimed real chests and supplies, climbed real stairs, defeated the original **Thorn Warden** and selected the next stage route. Floor arrival continuations were checked at clock56040/floor2 and101085/floor3. Eleven badges were earned naturally, including First Guardian and its **core:boon/guardians_vow** reward. The original boss has an actual dead body and saved boss-exit receipt100032. The current run retains its original empty starting-achievement snapshot and its eligible chest pool excludes Guardian's Vow. The coordinator explicitly selected this already earned real reward for the lifecycle check instead of indefinitely seeking a particular secret. The probe was saved/captured/stopped at **clock117173**, stage2/floor3 tile339,280, HP22, run1; driver exit130 records an intentional scope checkpoint, not a game failure. `reward-scope-checkpoint.json` retains the ordinary protocol receipts. **Secret discovery was not verified.**

Exact final invocation:

```sh
ALSOFT_DRIVERS=null SDL_AUDIODRIVER=dummy LIBGL_ALWAYS_SOFTWARE=1 xvfb-run -a python3 tools/migration/verify_native_profile.py artifacts/migration/verification/native-profile/rendered-v3 --runtime artifacts/migration/verification/native-profile/rendered-v2/runtime --continue-from artifacts/migration/verification/native-profile/rendered-v2 --large-text --unlock guardian --budget 1200 > artifacts/migration/verification/native-profile/rendered-v3.log 2>&1
```

`rendered-v3` exactly loaded/resaved the117173 manifest and again checked the same actual dead original guardian receipt and old-pool exclusion. Free achievements browsing visited all eight pages at1024×720, both normal/default and large Sans settings across these attempts; clock/HP stayed fixed while browsing. Representative first-page screenshots were personally inspected: earned Guardian's Vow and still-locked Uncovered Truth/Chain Reaction remain readable without clipping. The earned page displays11/22 permanent badges and1/4 reward unlocks. Screenshot appearance alone is not used as lifecycle proof.

The user then explicitly requested another temporary pause. **No new gameplay, build/test or implementation work was started after that request.** The owned driver was frozen, held keys released, the in-progress ordinary action allowed to settle, and ordinary Save/capture/stop delivered. By this boundary the actual Spore Herald100034 had downed the traveller via `core:ability/spore_burst` at141963; actual bleeding then ended the traveller at **clock142289**. The saved sole party member100000 is **Dead/HP0**, `AwaitingContinuation=true`, floor3 tile294,246, same native profile identity and **run1**, eleven permanent badges, pending[]. No resurrection, survivor rollback or earlier living checkpoint was used. `user-pause-checkpoint.json` retains full request/response/observation receipts and `user-pause-checkpoint.png` the saved terminal frame. `pause-proof.json` records the actual enemy/downed/death receipts and unchanged runtime. The driver exited **143** deliberately after the client acknowledged stop. No profile-owned driver/client process remains running.

**Exact terminal reload, fresh-world creation/reload and Guardian's Vow inclusion in that fresh future pool have not been run.** This is a saved real terminal checkpoint, not a completed lifecycle pass. `result.json` remains `passed:false`, `outcome:user-requested-pause`, `interrupted:true`, with `actualPartyDeathSaved:true`, `exactTerminalReloadVerified:false` and `freshWorldVerified:false`. Secret discovery, chain reaction, canonical Regent completion, full solo/four-party campaign completion and Linux package acceptance remain outside the natural evidence here. Focused backend evidence remains17 +156 +26 passing checks from the earlier described source batches; no tests ran during the rendered probes or after this pause.

Preserved tracked WIP is only `tools/migration/verify_native_profile.py`: adjacent diagonal attack handling, actual continuation support, a visible MossSigil approach, selectable original guardian/secret reward, exact old/future pool assertions, and ordinary failure checkpoint preservation. Python syntax and `git diff --check` passed before v3 launch; no fresh checks were started after the pause. The exact executed v3 driver is also retained under `rendered-v3/verify_native_profile.py`, SHA-256 `f34b4d9844327504bc90bd1d7772cb5c5de5c4bffd98a95c1ffb2241e512bb71`; its unchanged supporting journey copy is retained there too. Navigation memory uses only captured visible cells, never hidden save geometry to select actions. Saves are inspected after input solely to verify actual receipts.

After the user explicitly resumes and root authorizes the one-client slot, continue from the **latest terminal v3** checkpoint with the preserved frozen runtime (do not resume the earlier living v1/v2 checkpoints):

```sh
ALSOFT_DRIVERS=null SDL_AUDIODRIVER=dummy LIBGL_ALWAYS_SOFTWARE=1 xvfb-run -a python3 tools/migration/verify_native_profile.py artifacts/migration/verification/native-profile/rendered-v4 --runtime artifacts/migration/verification/native-profile/rendered-v3/runtime --continue-from artifacts/migration/verification/native-profile/rendered-v3 --large-text --unlock guardian --budget 1200 > artifacts/migration/verification/native-profile/rendered-v4.log 2>&1
```

That continuation must verify exact terminal reload first, preserve the eleven permanent badges, use the normal fresh-world control, verify a new seed/run serial with fresh living level1/XP0/no boons/gold0, verify Guardian's Vow only enters the fresh eligible chest pool, exactly save/reload that fresh state, and inspect final screenshots. Preserve every failure/current checkpoint. Then document and commit the completed rendered driver/evidence, report to root, and leave all eight aggregate gates open. Root owns merged regression, actual production world entry/pursuer/victory integration, full party/campaign journeys and reproducible Linux delivery. The branch/worktree/runtime/profile and WIP remain intact for resume; root owns goal pause status.
