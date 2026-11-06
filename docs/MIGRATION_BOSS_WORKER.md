# Item 18 worker evidence

This isolated `codex/new-bosses` stream starts from `9c7c3ee4` (shared implementation baseline `0d5cbf3b`). The coordinator owns the active full migration objective, placement/final-role selection, special-site scheduling, integration and all eight acceptance gates. None of those gates is closed by this worker record. The sibling Vagabonds checkout remains read-only.

## Implemented checkpoint

Four additional typed boss roles use the existing persistent actors, canonical damage/rewards, paid tactical actions and action timeline. `EnemyCatalog.Default` remains the original fifty-definition catalog; original stored enums retain their values and the new action/pulse kinds are appended. There is no alternate battle runtime, mana layer or duplicate actor HP/position.

- Bellkeeper: recent committed loud party actions are heard through the existing attenuated physical sound query; Soundseek locks that tile. Funeral Chime schedules expanding rings from breakable resonators. Breaking one cancels its rings, interrupts a pending chime and creates extended recovery/exposure.
- Sluice-Maw: explicit finite local channel geometry, marked surge lanes and physical pushes; a connected-pool feast locks its announced pool, moves the actual actor and heals at impact. Adjacent valve operation costs one action, permanently drains its channel, preserves unrelated tile changes and prevents that pool's healing. No general fluid simulator is introduced.
- Threadmother: warned cuttable physical partitions retain central walking gaps and clear when their anchor is cut. Reel In locks a tile, draws a visible cuttable line and pulls the actual occupant subject to physical blockers. Connected web fire schedules visible delayed area impacts with canonical friendly fire and clears strands.
- Ossuary Captain: front/front-diagonal protection, warned forward charge/knockback, long exposed charge recovery and breakable bone ramparts with a permanent central gap/open ends. Turning exposes it, so solo positioning has the same counterplay as party flanking.

All roles have three seeded authored orientations and an explicit stronger final-role version. Final Bellkeeper adds a resonator, a fourth ring and wider Soundseek; final Sluice-Maw adds a channel, stronger feast and double push; final Threadmother adds a second partition, longer pull and stronger burning web; final Captain has longer charges, stronger shield and tougher ramparts. Stats never depend on party size or level.

## Integration contract

`WorldSession.CreateCampaignBossArena(kind, center, seed, finalRole, variation = -1, readyAt = null)` returns `CampaignBossArena` with the actual actor ID, fixed center, seed, variation, role, fixtures and channels. It paints one 15×15 world-space footprint with four physical exits. It rejects observed, edited, occupied or already registered ground before painting; reentry must never call this constructor. The caller owns world placement and final-role selection. `TacticalCombat.RegisterCampaignBoss(actor, arena, readyAt)` registers a persistent unacted actor in a caller-registered typed arena if custom generation supplies geometry instead.

`WorldSession.CampaignBossRoomAt(p)` and public static `SnapshotCampaignBossRoomAt(world,p)` return `WorldRoomId(RoomArea.Dungeon,1000+arena.Id,p.Floor,1)` for arena ownership. The coordinator must prioritize this lookup before complex/generated room lookup. The coordinator also owns the new-identity exclusion in its `WorldDungeonEncounters` original-regular qualification. Other direct original-catalog Get callers were audited: targeting/navigation/inspection dispatch through the new role; phase/revival use existing Contains guards; original contracts and malformed pursuit-boundary validation explicitly exclude new roles.

New files own catalog/state, arena materialization, primary planning/resolution, objects/pulses, validation and client valve interaction. Existing files contain small dispatch/state/collision/inspection/visibility hooks. The profile worker has disjoint `Emit` and end-of-`DrainBoons` hooks; both must be preserved during coordinator integration.

The native mouse/Enter path operates adjacent valves through the existing paid SubmitDungeon transaction. Boss/fixture inspection describes counterplay; visible hazard cells and exact impact times remain private to the controlled member. New mappings are selected from the existing universal atlas; the full catalog generator updates runtime bindings and derived usage projections, preserving source art, IDs and crop rectangles. Crop review is retained in worker artifacts; initial fixture art is being refined to exact web/bell/valve crops before final handoff.

## Delegated initial tuning

Normal health: Bellkeeper 52, Sluice-Maw 66, Threadmother 56, Captain 72; power 5, speed 85, windup 160, recovery 140. Final roles double health, use power 9, speed 100, windup 120 and recovery 100. Captain armor is 1; all others 0. Ordinary movement/valve actions cost 100. Major/final rewards are 40/100 XP and 15/30 gold through the existing one-time receipt path.

Normal/final resonators have 5/8 HP; rings expand to radius 3/4, spaced 120/90 units after the initial warned band. Broken chorus exposes for 450/350 units and extends readiness. Exposed bosses take 50% additional raw damage. Feast windup is 240 and restores at most 14/24 HP. Valve exposure lasts 450/350 units; all channels dry leaves persistent dry exposure. Partitions/ramparts have 3/5 HP; burning impacts deal 2/4 damage after 100 units plus 75 per connection. Reel pulls at most 2/3 legal steps. Charges reach 4/5 tiles, recover for 240 units and expose for 300. Captain frontal reduction is 80/90 percent. These numbers are initial tuning, not final solo/four-party balance acceptance.

## Verification checkpoint

All commands run in `/home/gongahkia/coding/projects/roag-worktrees/bosses`, using `./tools/dotnet.sh`, `TMPDIR=/home/gongahkia/.cache/roag-tests-tmp`, `ALSOFT_DRIVERS=null`, `SDL_AUDIODRIVER=dummy`, `LIBGL_ALWAYS_SOFTWARE=1`.

- `dotnet test tests/Roag.Core.Tests/Roag.Core.Tests.csproj --filter FullyQualifiedName~CampaignBossTests`: first compile built every runtime project cleanly, then rejected two test-only `Assert.Empty(filtered)` uses under xUnit2029. Both were corrected to `Assert.DoesNotContain`; no runtime result was waived. Retained `artifacts/migration/verification/campaign-bosses/focused-initial.log`.
- The corrected `--no-restore` focused rerun **passed all 30 cases**, zero failed/skipped, 14 seconds. Evidence: `focused-runtime.log` / `focused-runtime.trx` in that directory. Includes every primary move through an ordinary actual world save and exact five-decision four-member continuation, all four roles × three variations in normal/final forms, free previews/invalid repeated valves, locked noise, chime interruption/private warnings, real valve drainage/feast denial, connected feast, physical blocked push, anchor cuts, cut reel, connected friendly-fire burns, directional shield/charge/ramparts and malformed-state rejection.
- `python3 tools/build_game_catalog.py` completed and `git diff --check` passes. Atlas candidates were inspected as enlarged exact lossless source crops; this establishes crop identity, not final rendered encounter readability.

Remaining worker review: add explicit charge-obstruction and wet-channel push edge checks, tighten commitment validation, finish exact fixture atlas selection, then run affected original-controller/tactical/client checks in the coordinator-assigned heavy slot. No rendered natural boss journey or refreshed Linux package is claimed. The coordinator still must place all four arenas, select one final role at generation, integrate room precedence/victory/profile hooks and verify actual rendered solo/four-member campaign journeys and the final combined save/release lifecycle.

## Physical edge and presentation follow-up

The second compiled batch passed **41 focused cases**, zero failures/skips, 28 seconds (`focused-edges.log` / `focused-edges.trx`). Added concrete wet-channel double-push without ordinary swimming, a wall constructed after charge commitment stopping both damage and movement before bodies beyond it, actual Reel In pull versus paid evasion, free foreign/malformed commitment rejection, and five exact-state refusal cases for registered structure/work/cache/corpse/decoy positions. Arena validation now happens before creating a tactical timeline and checks original typed fixtures/hazards/loot, persistent bodies/equipment, work stations/supplies, class constructs and pending attack cells. Charge wall clipping and specialized forced water landings use the existing canonical displacement/events; ordinary forced movement remains unchanged.

Presentation now names the committed move and its countdown above a visible boss, numbers valves/channels/pools, traces the visible selected channel on valve hover, draws visible surge flow arrows and marks the Captain's guarded front. Hover inspection starts with the actual warning/counterplay. Exact reviewed fixture selections: web `urizen-onebit-r39-c004` `[53,508,12,12]`, valve control `urizen-onebit-r00-c068` `[885,1,12,12]`, funeral bell `urizen-onebit-r25-c036` `[469,326,12,12]`, all from `urizen/urizen_onebit_tileset__v2d0.png`. Earlier humanoid/anvil/urn candidates were replaced after crop inspection; no source pixels or neutral identities were changed. The catalog generator completed after those selections. This is backend/crop evidence; a natural rendered boss playthrough remains integration work.

Current coordinator-requested follow-up: omit absent boss state/null metadata when loading and resaving older worlds, initialize only on actual boss registration, and verify exact absent-state serialization. The 41-case runner precedes that compatibility change.

## User-requested pause checkpoint — 2026-11-06

The coordinator relayed the user's explicit temporary pause. Feature work stopped immediately. Preserve this worktree and branch for the same goal's later resumption; no builds, gameplay tests or rendered runs were started after that request.

Completed implementation commits:

- `506f0a49`: four typed native bosses, normal/final variants, world arena API, exact state, counterplay, inspection/atlas bindings and initial 30-case passing batch.
- `b78c69ae`: physical surge/charge corrections, pre-paint placement refusal/atomicity, stricter commitment validation, readable numbered previews and reviewed fixture art. All 41 focused cases passed, zero failures/skips, 28 seconds. `edge-build.json` records 2,193 source/input hashes and 838 runner hashes. This is the last compiled gameplay checkpoint.

**Uncommitted compatibility edits are saved, uncompiled and unverified.** They make `TacticalWorldState.CampaignBosses` nullable and null-omitted, add null omission to `EnemyMemory.CampaignBoss` and `LockedAttack.Boss`, initialize the store only during accepted arena construction, guard empty/no-boss runtime/client/snapshot/reference paths and update the absent-state serialization check. The modified files are:

- `src/Roag.Core/Tactics/TacticalState.cs`, `EnemyCatalog.cs`, `CampaignBossValidation.cs`, `TacticalCampaignBosses.cs`, `TacticalCampaignBossObjects.cs`, `TacticalCombat.cs`, `TacticalReferences.cs`.
- `src/Roag.Core/World/WorldCampaignBosses.cs`, `WorldCollision.cs`.
- `src/Roag.Client/WorldCampaignBossPresentation.cs`, `WorldInteractionFeedback.cs`.
- `tests/Roag.Core.Tests/Tactics/CampaignBossTests.cs`.

A read-only checkpoint `git diff --check` passed. No worker build/test/render/catalog process remains running: the 41-case test process completed successfully; the catalog regeneration process was polled to terminal success at this pause. The coordinator's processes and other worktrees were not touched.

On explicit resume, finish reviewing the optional-state null guards, add an actual ordinary pre-boss manifest load/resave byte-identity test (including original enemy memory and a committed original windup), compile/run that compatibility batch, then run appropriate affected original-controller/tactical/client checks in the coordinator-assigned slot. Finish the requested review of all four visible targeting tendencies and canonical damage/proc/contribution ownership, preserving any findings/tests. No broader current-source regression or rendered boss journey has yet been performed. Root still owns room precedence, original population's additive-ID exclusion, geography/placement/final selection, victory/profile integration, natural rendered solo/four-member journeys and the refreshed Linux delivery. All eight aggregate acceptance gates remain open.
