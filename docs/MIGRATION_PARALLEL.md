# Parallel migration work

The user explicitly authorized this worktree/worker arrangement on 2026-11-06. The shared objective remains `/home/gongahkia/.codex/attachments/8ba90282-fe56-4de0-bbf8-a2fd4b85d637/goal-objective.md`; every worker must read it, applicable AGENTS instructions and its referenced migration documents. Assignment boundaries divide implementation, not the final acceptance requirements. The root coordinator owns the active goal and its completion audit. Workers must not mark the full migration complete or change the goal's scope/status.

## Ownership

| Stream | Checkout / branch | Responsibility |
| --- | --- | --- |
| Main / coordinator | `/home/gongahkia/coding/projects/roag` / `main` | Item 16: physical complexes, original contents, routes, world/party scheduling, generation integration; shared status/preservation/playtest records; review, integration and final acceptance. Existing uncommitted population work remains here. |
| Boss worker | `/home/gongahkia/coding/projects/roag-worktrees/bosses` / `codex/new-bosses` | Item 18: all four approved boss mechanics, normal/final-role variants, persistence, atlas bindings, inspection/feedback and focused verification. Provide generation/registration interfaces for the coordinator; do not take over world layout/final-role selection. |
| Profile worker | `/home/gongahkia/coding/projects/roag-worktrees/profiles` / `codex/native-profile` | Item 20: native permanent achievements/unlocks, original profile preservation, terminal-death/fresh-world lifecycle, storage and focused verification. Provide victory/progression interfaces for the coordinator; do not take over campaign role selection. |

Workers started from committed **9c7c3ee4** (the **0d5cbf3b** implementation plus this coordination document). Main's unfinished population edits are deliberately absent from their branches. Parallel preparation of B is explicitly authorized; the coordinator still verifies the combined world/expedition scope before declaring A complete and performs combined campaign acceptance after integration.

## Shared interfaces and invariants

- Keep one world/actor/health/build authority and the existing action-driven tactical timeline. Free inspection/rendering must remain free. No parallel combat runtime, mana layer, real-time simulation or replacement source catalog.
- Preserve all original 50 controllers, original catalog identities/pools, stored enum values and existing saves. Add new boss/profile state through typed native C# components with explicit validation and exact continuation. Older absent/default state must not silently alter a continuing world or grant progress.
- The boss worker should propose a concrete arena/registration API accepting actual world coordinates, seeded variation and an explicit final-role flag. The coordinator owns placement and which role is final. All four mechanics must work on real tiles with private observation, locked telegraphs, paid movement and solo/party counterplay. Use existing universal-atlas mappings/provenance only.
- The profile worker should propose a concrete permanent-profile API and isolated storage policy, plus hooks for committed achievements/victory, final party death and fresh-world creation. Preserve all original achievements/unlocks under approved adaptations. Record progress only from real committed outcomes, retain terminal safeguards and avoid real user data in tests.
- Each worker must send the coordinator its proposed interfaces and existing shared files it needs to edit before implementing cross-stream hooks. Prefer new partial/type files for owned behavior and minimal, reviewable hook hunks in existing files. Main currently edits `WorldDungeon*.cs`, `DungeonComplexState.cs`, `TacticalDungeonRoutes.cs`, `TacticalEnemyTurns.cs`, `TacticalValidation.cs`, `TacticalServices.cs`, `TacticalVaults.cs` and related population validators; changes to these need coordination.
- Do not edit shared MIGRATION_DECISIONS/PROGRESS/PRESERVATION/TUNING/PLAYTEST or this coordination file in worker branches. Put worker evidence, tuning and handoff notes in `docs/MIGRATION_BOSS_WORKER.md` or `docs/MIGRATION_PROFILE_WORKER.md`; the coordinator reconciles them into the shared records during integration. Approved decisions remain authoritative; ask through the coordinator about genuinely new gameplay ambiguity.

## Work and reporting

1. Run every command and edit in the assigned worktree using its absolute path; never write into another checkout. The sibling Vagabonds repository remains read-only. Do not spawn further workers.
2. Inspect actual branch/status before edits. Make regular coherent atomic commits on the assigned branch; never reset/discard changes, publish, merge into main or cherry-pick another stream on your own. Send milestone updates and commit IDs to the coordinator as work progresses.
3. Use `./tools/dotnet.sh`, ample `TMPDIR=/home/gongahkia/.cache/roag-tests-tmp`, and null audio/software rendering. Keep build/test outputs inside the assigned worktree. A live main population test and a separate frozen 72-hour calendar probe already exist; do not terminate/restart them or mutate their evidence/runtime.
4. Coordinate expensive builds/regressions/rendered journeys through the coordinator. Read/code review and light checks may proceed independently. The coordinator schedules heavy tests and the final combined regression; a focused pass must not be reported as full acceptance.
5. Reports must identify commit(s), changed files/APIs, exact commands/results and source/runtime identities, failed/incomplete evidence, verification limits, unresolved dependencies and remaining work. Distinguish implemented code, backend checks, actual rendered gameplay and delivered package. Do not stop at stubs or compilation; finish the assigned approved feature and its appropriate checks, then report for integration.

## Integration

Only the coordinator integrates reviewed worker commits into main, after committing/stabilizing its own relevant work. Preserve commit identity/history where practical and resolve conflicts according to the authoritative decisions and the combined intended behavior; never use an unconditional “ours/theirs” resolution to drop a stream. Inspect merged code and run relevant combined checks before continuing. Keep worker branches/worktrees until their work and evidence are safely integrated. Final solo/four-member rendered journeys, profile lifecycle, regression, preservation audit and refreshed reproducible Linux package remain mandatory; worktree completion alone closes no full-game gate.

## Current state

Both isolated workers were started, and are now **paused by explicit user request**: `/root/bosses` on `codex/new-bosses`, and `/root/profiles` on `codex/native-profile`. Neither has been integrated or passed its assigned acceptance yet.

Accepted boss contract: additive `CampaignBossCatalog` and typed arena/memory state preserve the original 50-definition catalog. `WorldSession.CreateCampaignBossArena(kind, center, seed, finalRole, variation)` paints new, unobserved/unmodified/uninhabited ground once and returns the actual arena/boss/fixtures; `TacticalCombat.RegisterCampaignBoss` supports coordinator placement. Live/snapshot arena-room lookup will use a separate place namespace; the coordinator adds precedence in shared room lookup. Main owns generation placement, final selection and special deadlines. All mechanics remain on the existing timeline and real tiles.

Accepted profile contract: independent native profile storage defaults to the selected continuing slot plus `.profile`, with explicit `--profile` sharing and isolated automation paths. Original achievements/unlocks and the campaign-creation reward-pool snapshot are preserved. Explicit attachment initializes eligibility; loading an older world alone grants nothing and preserves absent state. The coordinator calls committed physical-entry milestones and `RecordCampaignVictory(bossId)` for the actual selected final role. Canonical Hollow Regent achievements retain their specific trigger. The two workers agreed on disjoint shared combat/event hooks.

Heavy verification is coordinated: the profile worker released its initial slot while authoring its files; the coordinator will run the broader population/bounded regression next. The separate frozen 72-hour calendar journey continues independently. No worker commits have been merged; root retains responsibility for code review, conflict resolution, combined verification and all eight final gates.


Pause checkpoint (2026-11-06): all three streams are preserved; no worker implementation is merged. Boss HEAD d92ef3e7 retains implementation506f0a49/b78c69ae (last41 focused passing) plus twelve unverified compatibility edits. Profile HEAD9d3f81e3 retains c9b0d654/c3a5ff11/88a8649b (last156 affected passing) plus seven tracked/untracked follow-up edits. Main population8fcb7bea passes167; physical route edits remain uncommitted and the live-route test was cancelled for the pause. The calendar probe saved after47 completed hours plus a partial rest. [MIGRATION_PAUSE.md](MIGRATION_PAUSE.md) is the coordinator's exact resume handoff; `/goal resume` in the same conversation resumes the existing objective and worker arrangement. No further feature work should run until explicit resume.
