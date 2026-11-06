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


## Resumed natural solo lifecycle proof — 2026-11-06

Root explicitly resumed the same goal and authorized this bounded rendered slot. The preserved branch remained 141b6d26 plus the identical tracked driver WIP; the terminal v3 world/profile/runtime were neither edited nor recreated. The exact latest-terminal `rendered-v4` command documented above completed **PASS, terminal exit 0** in process session 50111. The client acknowledged ordinary stop and exited; no profile-owned driver/client process remains. No rebuild or additional C# tests were needed.

The source journey remains the actual paid seed 42 Adventurer, guardian victory and Spore Herald/bleeding final death from v1→v2→v3. V4 first loaded/saved/reloaded the full terminal manifest at 142289, same run 1/native profile identity, sole party member dead and `AwaitingContinuation=true`. It then browsed the normal/large achievement pages without advancing time, reconfirmed the actual original dead guardian receipt and exclusion of Guardian's Vow from this old campaign's frozen eligible pool, and saved/reloaded the terminal state again. **All complete parsed terminal JSON manifests match the preserved v3 manifest**, including actors, equipment/builds, inventory, profile progress, RNG, pending timeline/events and world references. This assertion compares full parsed JSON state, not raw serialization bytes.

Only the normal **Begin a fresh run** control advanced to a new campaign: seed **-1017265640**, **run2**, clock 0. The same permanent profile retained **all 11 badges** and Guardian's Vow. The new founder is alive at HP24, level 1/XP0, with no boon stacks and gold 0; per-run earned is empty and the saved creation snapshot contains the retained eleven achievements. Guardian's Vow is now in the **fresh future eligible chest pool**, and remained excluded from the old pool. No current-run offers/build were retroactively changed. The fresh complete parsed manifest was saved/reloaded and equals the final manifest. Both native profile replicas agree on `currentRun:2`, `nextRun:3`, `terminalThrough:1` and the same eleven earned IDs. No earlier living save or alternate world was loaded, and no survivor resurrection, profile/HP/inventory/position edits, injected achievements or rerolls occurred.

`native-profile/rendered-v4/result.json` reports `passed:true` with this explicit solo guardian/death/fresh-world scope, `secretDiscovery:false`, `completeCampaign:false`, `partyJourney:false` and `linuxPackage:false`. `evidence-proof.json` independently records the actual full-manifest comparisons, same profile identity, terminal/fresh state, both profile records, **66 ordinary input receipts**, exact source/runtime/driver hashes and limitations. All **699 frozen runtime files** and **450 production source/content files** still match the earlier followup-v4 tested inventories. Build metadata identifies worker HEAD 141b6d26 plus the recorded driver WIP; no production source changed. The retained exact executed driver SHA-256 is `f34b4d9844327504bc90bd1d7772cb5c5de5c4bffd98a95c1ffb2241e512bb71`, and its supporting journey SHA-256 is `c5bd8b1da1bbd5414723456778822e0ec43af439ed027d5aa12a7b1b1b2ec969`.

Personally inspected final screenshots `07-real-terminal-resumed.png`, `09-profile-retained-new-world.png` and `10-fresh-resumed.png` show the ended run, retained earned Guardian's Vow with the other reward rows still locked, and the fresh living level 1 founder. The original normal and large 1024×720 paged presentation checks remain valid. Inputs, exact saved-state snapshots, client logs, screenshots, runtime and profile records stay in the ignored worktree evidence directory. Prior v1 driver-loop and v2 scope-checkpoint attempts, interrupted v3 and the missing-import compile failure are retained rather than replaced. Successful v4 does not erase those limitations.

**Assigned profile backend plus this natural solo terminal/fresh-world lifecycle scope is now verified.** This is an isolated bounded native runtime from the worker's tested source, not the later merged living-world runtime. Natural secret discovery, same-member three-boon chain reaction, canonical Regent completion, full solo/four-member campaign journeys, new-boss/world integration and reproducible Linux delivery are not established by this probe. Root has already integrated production profile commits through 7a1b61b0 and owns merged regressions and its physical entry/special-site/canonical-complex/selected-final call sites. No further profile production API is required by this probe. Root must still review/cherry-pick this completed driver/evidence documentation and the 141b6d26 pause handoff as desired, then validate profile behavior with the final aggregate runtime. All eight aggregate acceptance gates remain open; this worker does not mark the full goal complete.


## Read-only merged integration audit — 2026-11-06

Requested by root after main integrated the pause/driver commits as 12af67f4/d580a7a0. Audited main **18f66075be04e8fed5e17c6ed060f37da7a9f506**, comparing original achievement execution/content from **d498fe0889050d2a23f2ff86d80db6084ed6467a**. Main was read only; no production edit, cherry-pick, build/test or rendered run occurred. `native-profile/integration-audit/source-proof.json` pins nine relevant file hashes. They matched the audited main commit when the audit finished. This is source review, not additional passing runtime evidence.

All 22 original stable IDs/names and all four reward IDs remain present. Original executed trigger correspondence:

| Achievement | Merged native committed source / policy |
| --- | --- |
| first_guardian → guardians_vow | Actual owning stage < 4 boss death receipt emits EliteDefeated; original observer also accepted this event without a player-source condition. |
| uncovered_truth → prospectors_heart | SecretDiscovered from the actual member's ordinary attack/entry secret resolution. |
| chain_reaction → circuit_chorus | Three distinct BoonActivated IDs, same member and retained causal chain; delayed boss pulses now retain their saved causal ChainId before pruning. |
| regents_fall → crownfire | Full post-drain settled canonical Regent complex Victory + own Stage 4 death receipt + actual dead Hollow Regent + conscious survivor; bounded canonical settlement retained. |
| first_blood | Actual member EntityKilled with dead native enemy target. |
| pathfinder | Actual member FloorChanged; TileEntered alone does not award it. |
| treasure_hunter | Member ChestClaimed after actual claim. |
| coin_purse | Member CurrencyCollected with resulting shared gold≥50; stocking/trading alone is not a retroactive balance scan. |
| first_level | Actual member-target LevelUp reaches≥2. |
| veteran | Actual member-target LevelUp reaches≥5. |
| boon_bearer | Member BoonGranted. |
| stonebreaker | Member TerrainBroken from actual attack-broken barricade. |
| ore_seeker | Member OreBroken from actual attacked ore feature. |
| gate_opener | Member TimedGateOpened from paid triggering entry. |
| key_bearer | Member VaultKeyCollected from actual key-tile entry. |
| warded | Member WardFlaskCollected from actual ward-tile entry. |
| torchbearer | Member BrazierLit from actual feature entry. |
| long_heal | Member SupplyCollected from actual healing or the approved paid hunger-restoration adaptation. |
| pressure_survivor | Public RecordPursuerArrival requires an actual living extraction_pursuer; **no production caller or PursuerArrived producer currently exists**. |
| deeper_road | Member FloorChanged/TileEntered resolves actual authored physical floor Stage≥2, independent of complex-global Stage/RouteHistory. |
| sanctum_reached | Same member-owned actual Stage 4 floor, not a generic depth7 tile or an unrelated boss arena. |
| regent_awakened | Actual Hollow Regent PhaseTwo flag after RegentPhaseChanged, preserving the original event-based condition. |

One source-level integration dependency remains. The originally reported traversal finding is explicitly withdrawn after checking legal current previews:

1. **Unwired Under Pressure trigger.** `WorldProfiles.cs:65` and `TacticalProfile.cs:76` expose the arrival hook, but there is no production caller at this audited HEAD. Thus an otherwise enrolled campaign cannot earn the original pressure_survivor badge through current native production flow. Root's special-site integration must call it only after the real confined pursuer is committed, never site registration, activation preview or an unspawned deadline; ordinary post-action profile flush must follow. The helper alone is not evidence that special-site arrival works.
2. **Withdrawn: dash-corner traversal failure is not reachable under current rules.** The earlier audit correctly noticed that CommitMovement physically traverses preview.Cells before emitting its entry events, but incorrectly assumed a legal non-cardinal multi-cell path. PreviewMovement requires cardinal movement (`TacticalClassActions.cs:32`); every class has dash distance≤3 and ice extends only up to 3. EscapeScroll also requires cardinal direction and length 1–3 (`TacticalUtilities.cs:57`). Full authored floor rectangles are 140×90, sanctum 23×17. A straight path this short cannot enter and leave either rectangle. Shade swap, Wayfinder beacon return and ReturnToken each publish only a single landing, regardless of their reachability search; they do not traverse the search path. CommitMovement emits its source-floor entry before CommitStairs moves/observes the destination. Shoves/pulls update position and emit each step immediately. There is **no valid current-rule reproducer** of the claimed mismatch, and no current observer fix is warranted. Keep the direct public and observer current-position guards unchanged. Only future non-cardinal/long traversed movement would justify revisiting event-position handling.

No additional concrete privacy or exact-save incompatibility was found in these hooks. DungeonFloorAt resolves bounded/parcel-owned authored rectangles; independent campaign-boss room IDs are not treated as sanctum identity. Physical badges never use the camera, selected member, room selection, or complex-global stage. Recording only adds saved profile/event facts, without Reveal or chunk discovery edits; display feedback separately requires Visible.Contains(event.Position). The profile panel intentionally shares permanent achievement labels without publishing remote map coordinates. An actual remote member's real entry can contribute to the shared profile while the controlled member's private map remains private.

Future-pool behavior remains correct at source: explicit fresh EnablePermanentProfile is the only place that appends the four rewards from StartingAchievements. Later award/flush updates permanent records/cache, not AvailableBoons or deterministic chest rolls. Load validates existing optional profile state without allocating an identity, inferring earned badges, settling completed complexes, or rebuilding pools. Old saves with absent profile remain null/ineligible. New physical complex branch/room namespaces are not profile compatibility keys. Regent's Fall remains canonical Regent-specific even if another enemy is selected as world final; the selected-final RecordCampaignVictory API has no production caller at this HEAD and remains root's separate campaign-settlement responsibility.

Minimal meaningful merged checks, proposed rather than run:

- In a newly enrolled actual 19-floor complex, make a paid one-step/stair entry into an unchosen Stage 2 branch while Run.Stage is still 1; assert deeper_road exactly once and no inferred route history. Enter actual Stage 4 and assert sanctum_reached once. Generic same-depth caves and a separate campaign-boss arena must grant neither. Save/load/resave the complete state and unchanged pool/offers.
- Select/inspect a parked deep member and choose a paid route without crossing its seal: no entry badge or new discovery. Then let that member actually enter while another member's private vision is elsewhere: award the shared profile while leaving the other member's position/readiness/visible map unchanged. Keep wrong supplied-stage, nonexistent/dead member API rejection checks.
- A legal cardinal paid dash crossing a real floor boundary ends inside that rectangle and should award its stage badge; a swap/return only observes its real landing. Do not add a fabricated diagonal/current dash-corner test or weaken the actual-position guard. The original proposed failing-corner test is withdrawn.
- With real post-drain resolution, kill a canonical non-world-final Regent and retain a conscious survivor: verify its complex Victory/receipt, crownfire award, frozen current pool and exact continuing state. Compare mutual final-party death and other-boss kills: no crownfire. Existing worker fixtures cover these guards, but not the complete merged nineteen-floor journey.
- When root wires special-site arrival, prove activation/preview/reentry-before-arrival grants nothing, real delayed confined arrival grants pressure_survivor once, and continued save preserves that outcome. Re-run the existing focused NativeProfileStoreTests/DungeonProfileTests/NativeComplexProfileTests plus these new integration cases in a root-approved slot; no full suite is requested by this audit.

Root challenged the proposed traversal fix before implementation and required legal current-rule reachability. The worker checked all current class/utility/forced movement previews and withdrew that false positive. No traversal change or test run is requested. Root also confirmed the currently unwired pursuer/selected-final calls are expected item 17/19 integration dependencies. No worker production patch is proposed for those root-owned call sites.

Root reported its separately executed merged 18f Release batch: **90 of 93 passed, three NativeComplexProfileTests failed**. This worker did not run that batch. The three fixtures installed terrain-only complexes (`Populated=false`) and manually inserted a Regent, which violates main's stronger full complex population validator. Root will replace that fixture setup with real full InstallDungeonComplex population and its actual registered canonical boss; preserving production validation is the correct response. Earlier isolated worker 26-test results remain attributable to their own frozen source and do not claim these later merged fixtures pass. A rerun of the corrected merged cases is still required.

The completed worker's scoped solo v4 lifecycle evidence remains attributable only to its frozen bounded runtime. Main still needs its merged rendered/profile regression and final full-game acceptance. No aggregate gate is closed by this source audit. Worker remains available for root's merged rendered verification.


## Traversal audit correction — 2026-11-06

At root's request the worker rechecked main 18f66075 read-only before any traversal change. The purported dash-corner finding was an avoidable audit error: eight-direction basic attacks do not imply eight-direction movement. Actual preview constraints and full floor rectangle sizes rule out the hypothesized legal path. No current class/utility path, forced movement or stair ordering provides a reachable reproducer. The original finding and proposed failing test above have been corrected in place, and root was immediately told to leave current guards unchanged. No fabricated geometry/event or tiny runtime test was used, and no heavy run/build occurred. The other source-audit mappings, expected item 17/19 dependencies, privacy/save findings and separately reported merged fixture limitations remain unchanged. This correction is source reasoning, not new passing runtime evidence.


## Merged Release full-world town journey — 2026-11-06

Root requested an actual rendered integration journey against its immutable `native-complexes/client-v1/runner/bin/Roag.Client/release` runtime, with its full four-complex explicit world scene now committed as **bfdea9b336aedf361b65be1623febeba1ed5f499**. This worker added optional `--runtime` to the owned `verify_native_world.py`, passed it to Client, and added free normal/large profile inspection, free second-member/private policy inspection, and saved-world verification. No main source, frozen runtime, gameplay state, position, HP, clock or supplies were edited. No build or new C# test was run.

```sh
TMPDIR=/home/gongahkia/.cache/roag-tests-tmp ALSOFT_DRIVERS=null SDL_AUDIODRIVER=dummy LIBGL_ALWAYS_SOFTWARE=1 xvfb-run -a python3 tools/migration/verify_native_world.py artifacts/migration/verification/native-profile/merged-world-v1 --runtime /home/gongahkia/coding/projects/roag/artifacts/migration/verification/native-complexes/client-v1/runner/bin/Roag.Client/release --budget 700 > artifacts/migration/verification/native-profile/merged-world-v1.log 2>&1
```

**PASS, terminal exit 0**, process session 14768. Client acknowledged ordinary stop and exited; no owned gameplay process remains. **Zero observation timeouts, zero input replays and zero restart attempts.** The driver retains Client's 45-second boundary, records any actual deadline, releases held movement keys and passively awaits the same pending boundary during bounded grace rather than restarting/replaying input; this timeout branch was not exercised. On failure it preserves an ordinary checkpoint when a completed boundary permits it. A post-run one-line error-path guard also avoids overwriting a preexisting evidence directory when construction failed before a journal was created; Python syntax/diff checks passed, but that error path is not rendered evidence.

Actual natural sequence: seed 42 at dawn; rendered atlas class cards and chose Ranger through ordinary controls; free permanent profile showed 0/22 and 0/4; town market sold one finite food for 2 gold, changing gold 25→23 and food 8→9, with actual paid clock 2160000→2160087. **393 ordinary exploration actions** found **Corin Elm, fixed Stormcaller**, then actual recruitment spent one action at clock 2194191→2194278. His carried medicine joined the shared pack (3→4), and the two members retained their separate classes/builds/resources. The Ranger stayed HP20; Stormcaller HP21.

The normal two-member policy page rendered. Selecting Corin via the ordinary 2 key changed only the viewed member, without advancing clock 2194278; his private view and selected party-policy page rendered. A real wait-for-observation interval consumed no game time. Ordinary Save→stop→`--load`→Save preserved the **complete parsed continuing manifest exactly**, including selected member **80002**, ready member **100000**, both builds, queues/events/RNG and all world references. Saved profile identity and run 1 stayed unchanged. Minimum 1024×720 normal and large Sans world/profile pages were exercised; representative class, market, fixed-adventurer, selected private-view, party-policy and large-profile frames were personally inspected and readable. This is two adjacent members' normal/private inspection, not a distant split-party journey.

Saved-state inspection was used solely as post-input evidence, never to steer the traveller. Both actual market save and continuing save contain **four populated complexes**, each with **nineteen floors and ten registered original bosses**; the counts/ownership summaries and native profile identity survive the exact continuation. All complex contents are validated by the actual native loader. There was no physical travel to these complexes in this town journey, no synthetic entrant, and no inferred boss/profile outcome. The profile remains empty, consistent with the actions performed.

Source identity differs from this worker's production branch. Root's frozen **2220-file source manifest** records base HEAD **6fc3f8c949e9115630f17850da46274ac20e7fe6** plus the archived three startup/test changes subsequently committed as bfdea9b3. Its original `source.json`, reviewed 10-case backend result and runner manifest were copied into this probe as provenance. The worker's `build.json` identifies driver branch 30f5db73 plus its own Python WIP, not the compiled runtime's production identity. **All 699 copied runtime files match root's client-v1 runner manifest**, and all 699 files in root's frozen runtime still match afterward: nothing was copied over root's runner. `runtime-source-proof.json` retains every hash and exact executed driver copies/hashes. `evidence-proof.json` retains independent final party/selected/profile/complex facts, runtime comparisons, **449 input receipts**, zero-timeout count, final manifest SHA256 and explicit limitations. Existing source snapshots and prior worker bounded lifecycle evidence remain separate.

Evidence lives under `artifacts/migration/verification/native-profile/merged-world-v1/`, with terminal log at `merged-world-v1.log`. This proves the merged Release explicit world scene renders and supports a real market/exploration/recruitment/private-page/exact-continuation path while retaining populated complexes. It does **not** prove outdoor/dungeon split, entrance/complex travel, individual stairs, ecology/special deadlines, new bosses, world victory, profile final-death/fresh-world retention on this merged runtime, or reproducible Linux delivery. Root's separate 1846-case regression remains independent. No full-game gate is closed by this town result.

Root's next assignment is to continue this exact two-member world/profile through paid rumour, free expedition journal and physical entrance travel once its tested immutable entrances-v2 Release runtime is supplied. Preserve the latest merged-world-v1 continuing save and selected party state; do not restart/reroll. Design may proceed, but dependent rendered travel waits for that supplied runtime.


## Continued paid rumour / expedition journal — 2026-11-06

Root's next explicit assignment extends the actual two-member merged-world-v1 campaign through a real paid resident rumour, free normal/large expedition journal and exact continuing save, then physical travel. The first stage completed **PASS, exit 0**, process session 50244, with no build/injected state/restart/observation timeout. Root's immutable `entrances-v2` Release runtime was used; all 699 copied runtime files match its runner manifest. Its 2224-file frozen source manifest is pinned at checkpoint **d2dccacd4791eeee8a32c13bfb175bd726167608**, containing archived entrance/journal changes now committed as **4c91b02c**. Source/runner/review metadata and exact executed drivers are retained in this probe separately from the worker branch.

```sh
TMPDIR=/home/gongahkia/.cache/roag-tests-tmp ALSOFT_DRIVERS=null SDL_AUDIODRIVER=dummy LIBGL_ALWAYS_SOFTWARE=1 xvfb-run -a python3 tools/migration/verify_native_entrance.py artifacts/migration/verification/native-profile/rumour-world-v1 --runtime /home/gongahkia/coding/projects/roag/artifacts/migration/verification/native-complexes/entrances-v2/runner/bin/Roag.Client/release --continue-from artifacts/migration/verification/native-profile/merged-world-v1 --budget 400 > artifacts/migration/verification/native-profile/rumour-world-v1.log 2>&1
```

The new runtime first loaded/resaved/reloaded the entire old continuing manifest exactly before input: party 100000/80002, selected Stormcaller 80002, same native profile identity/run 1, both living. No absent entrance state was injected by that load. The ready Ranger was selected with ordinary T, walked 27 paid observed-terrain approach actions to visible resident **Hana Lark**, finished her free dialogue and selected her actual offered rumour. It spent the native action at **2196540→2196627**, preserving the original discovery report and adding one remembered labyrinth entrance. Neither a remote actor coordinate nor hidden saved geometry guided navigation.

Normal and large Sans 1024×720 expedition journal first/next/previous pages were rendered and personally viewed. Coordinates/history wrap readably inside the panel. The paid public report is **Amberwick 14 labyrinth (3140,404), 2940 surface tiles east** from the actual current Ranger position (319,523). Reading/paging/settings consumed no clock or member-selection change. The report marks knowledge, not explored terrain; there was no physical entrance visit in this stage. A real Save→stop→ordinary `--load`→Save again preserved the complete parsed manifest exactly, same party/profile/run 1 and all four populated nineteen-floor complexes. KnownEntrance count changed 0→1 only after the actual paid rumour. The final current save, handoff and observed-map knowledge are ready for physical travel; client stopped cleanly.

Evidence is `native-profile/rumour-world-v1/{result.json,evidence-proof.json,runtime-source-proof.json,reported-entrances.json,00-original-continuation-campaign.json,07-rumour-exact-campaign.json}` plus input journal, full continuing world/profile, runtime and screenshots. This is **paid rumour/free journal/exact two-member continuation** evidence only. Outdoor→underground crossing, individual stairs and split-floor paid continuation remain to be performed. The actual reported distance is substantial; travel must use observed paths and ordinary paid movement with checkpoints, never geographic shortcuts, rerolls or actor repositioning. Root owns the shared status/preservation records and final acceptance.
