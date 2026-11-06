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
