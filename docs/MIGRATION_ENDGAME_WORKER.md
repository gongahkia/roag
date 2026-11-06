# Generated campaign and final-role worker

Worktree `/home/gongahkia/coding/projects/roag-worktrees/endgame`, branch `codex/campaign-endgame`, base main `d2dccacd`. The completed item-18 worktree remains intact at `ad794080`. The coordinator explicitly delegated item 19 after reviewing that audit. Shared migration records, discovery/entrance/journal presentation, special sites and aggregate acceptance remain coordinator-owned.

## First implementation checkpoint

Fresh `CreateOpenCampaign` now constructs all four original complexes, consolidates all 48 imported guardian families into 24 seeded strongholds at separate depths 4/6, installs all four approved arenas in four other existing sites with physical approaches from their original paired stairs, and selects exactly one of five fixed final roles. Twenty full generated sites remain without a major boss. The existing imported actor IDs, variants, minion links, stats, controllers, reward/source IDs, themed gear, every source-site cache and ordinary discovery guard condition remain. Generation receipts describe historical homes; later actor positions, HP, objects and terrain remain authoritative. Loading/reentry never invokes construction or relocation.

The four added final roles use their reviewed item-18 variants. When Hollow Regent is selected, one existing original complex finale receives a fixed stronger role: 124 HP, damage 8, defense 1, speed 100, windup 90/recovery 115, 100 XP and 30 gold. Its original identity, three attack patterns and half-health speed phase remain; the other original Regents and immutable fifty-definition catalog remain unchanged. No party/level scaling or cross-boss weakening is used. Original branches remain physically diggable, and no other major death is required by final selection or settlement.

`NativeCampaignState.Finale` is optional/null-omitted. Its typed generation receipts validate the selected actor/role, arena roles/seed/physical location, imported families and cache opportunities, Regent stats, and one-time victory. Absent earlier saves keep their previous objective and placements. Canonical settled selected-actor death with a standing survivor records `CampaignVictory` once, after damage/rewards, without pausing or resetting the world. The existing profile wrapper observes this result; `regents_fall` remains specific to the original Regent. Native keyboard, pointer, hover and frozen presentation predicates permit continued paid exploration while legacy endings remain intact.

Stable public metadata for coordinator journal/rumours/special-site exclusion:

```csharp
GeneratedCampaignFinale? CampaignFinale { get; }
CampaignFinalRole? FinalCampaignRole { get; }
WorldPosition? FinalCampaignPosition { get; }
IReadOnlyList<GeneratedBossSite> GeneratedBossSites { get; }
RetainedGuardianHome? RetainedGuardianLocation(int sourceSite);
IReadOnlyList<RetainedGuardianHome> RetainedGuardiansAt(int siteId);
// GeneratedBossSite(CampaignBossKind Kind, long Arena, int SiteId, WorldPosition Entrance)
// RetainedGuardianHome(long Actor, int SourceSite, int SiteId, WorldPosition Spawn)
```

`Spawn` is historical; current actor position comes from `ActorById(home.Actor)`. Metadata does not reveal terrain. `FinalObjective` now supplies the actual native final location and victory-continuation wording. The coordinator owns the remaining old `REGENT DEFEATED` presentation branch and the source-site rumour adaptation in `WorldDiscoveries`.

Shared hunks, approved before modification: `WorldOpenCampaign` factory call; `NativeCampaignState` optional field/validation; `WorldEndgame` objective branch; `WorldPersistence` narrow native final identity; `RetainedEnemyCatalog` and `TacticalEnemyTurns` selected-Regent definition resolver; `TacticalBoonEvents` settlement before final profile progress; `TacticalValidation` optional content propagation; `WorldSession.Frozen` victory predicate only; client victory guards in `WorldRoagCombat`, `WorldGame`, `WorldInteractionFeedback`; feedback label/sound in `WorldDungeonFeedback`. New partial/type files contain generation, state, validation and settlement. No visibility hook, discovery, `DungeonComplexState`, `WorldDungeonPresentation` or shared migration document is changed by this worker.

## Evidence and limits

Muted Release wrapper, worktree-local separate runner, `TMPDIR=/home/gongahkia/.cache/roag-tests-tmp`, `ALSOFT_DRIVERS=null SDL_AUDIODRIVER=dummy LIBGL_ALWAYS_SOFTWARE=1 DOTNET_PROCESSOR_COUNT=2`:

```sh
./tools/dotnet.sh test tests/Roag.Core.Tests/Roag.Core.Tests.csproj -c Release -m:2 --artifacts-path artifacts/migration/verification/campaign-endgame/runner-v1 --filter 'FullyQualifiedName~SeededSelectionIncludesAllFiveFixedRolesAndAcceptsTheWholeWorldSeedDomain|FullyQualifiedName~EarlierAbsentFinaleStateLoadsAndResavesWithoutAddingGenerationOrChangingTheObjective' --logger 'trx;LogFileName=compile-compatibility-v1.trx' --results-directory artifacts/migration/verification/campaign-endgame
./tools/dotnet.sh test tests/Roag.Core.Tests/Roag.Core.Tests.csproj -c Release -m:2 --no-build --no-restore --artifacts-path artifacts/migration/verification/campaign-endgame/runner-v1 --filter 'FullyQualifiedName~GeneratedCampaignTests|FullyQualifiedName~OpenCampaignConstructionTests' --logger 'trx;LogFileName=generation-v1.trx' --results-directory artifacts/migration/verification/campaign-endgame
```

First compile/compatibility batch **passes 2**, zero failures/skips, 1 second; actual generation batch **passes 5**, zero failures/skips, 37 seconds. Logs/TRX, `compile-compatibility-v1-inputs.json` and `generation-v1-audit.json` are under `artifacts/migration/verification/campaign-endgame`. All **2,374 source/input** and **1,556 runner bin-file** hashes remained unchanged. The full original-factory preservation fixture now compares exact retained identities/definitions/health/tactics/equipment/cargo and every cache/generated-gear opportunity, with explicit relocation receipts. Actual seeded Regent/Captain worlds verify four physical arena approaches, no prior boss receipts, nonfinal death independence, one-time canonical final death/profile, initial/victorious exact ordinary saves, paid continuation and rejection of forged role/actor/home/victory/cache state. The old absent-field manifest resaves byte-identically. Both sessions (62567, 58248) are terminal; no worker build/test/client remains running at this checkpoint. No failed build/test attempt occurred in this checkpoint.

This is backend generation/settlement evidence. Still required: remaining selected-role worlds, stronger Regent pending attack/phase continuation, co-hosted retained payout/guard checks, actual post-victory input checks, appropriate affected regression, coordinator combined integration, natural placed solo/four-member rendered boss/victory journeys and Linux delivery. All eight aggregate gates remain open. No debug scene, injected natural-play state, unearned profile grant or full-game acceptance claim is introduced.
