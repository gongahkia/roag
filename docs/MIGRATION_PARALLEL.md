# Parallel migration work

The user explicitly authorized this worktree/worker arrangement on 2026-11-06. The shared objective remains `/home/gongahkia/.codex/attachments/8ba90282-fe56-4de0-bbf8-a2fd4b85d637/goal-objective.md`; every worker must read it, applicable AGENTS instructions and its referenced migration documents. Assignment boundaries divide implementation, not the final acceptance requirements. The root coordinator owns the active goal and its completion audit. Workers must not mark the full migration complete or change the goal's scope/status.

## Ownership

| Stream | Checkout / branch | Responsibility |
| --- | --- | --- |
| Main / coordinator | `/home/gongahkia/coding/projects/roag` / `main` | Item 16: physical complexes, original contents, routes, world/party scheduling, generation integration; shared status/preservation/playtest records; review, integration and final acceptance. Population/routes and integration work remain under this stream; inspect current HEAD/status. |
| Boss/endgame worker | Original completed `/home/gongahkia/coding/projects/roag-worktrees/bosses` / `codex/new-bosses`; active fresh `/home/gongahkia/coding/projects/roag-worktrees/endgame` / `codex/campaign-endgame` | Item18 backend complete/integrated. Root explicitly reassigned the same worker to19 generation/placement/final-role/continued-victory integration, with reviewed typed interfaces and coordinated shared hunks. Old boss checkout remains intact. |
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

## Current state — resumed, 2026-11-06

Goal remains active. Both original workers use their existing worktrees. Initial implementations and all verified follow-ups are integrated; no branches/WIP were reset. Latest boss mappings: f605e641→cac1e109, d1a67b6b→72143e85, c001960c→2d428319, d5e0601b→5006f592,041567d1→3e7ce40b,06ce814b→a4daa888. Profile lifecycle/audit mappings include118126bd→d580a7a0, ebcd2c10→71e47872 and corrected30f5db73→6984b04e. The worker-document conflict retained both historical pause and resumed evidence; earlier sound conflict retained both effect families.

| Stream | Current work | Verification boundary |
| --- | --- | --- |
| Main | Explicit full-world client launch/fresh integrationbfdea9b3; entrance discovery, physical journeys, #17/#19 and final acceptance | Route10 and merged131 pass in pinned earlier snapshots. Factory1/selected28/support-waits12 and later startup10 pass; source/runner hashes reviewed. Full pre-startup-wiring regression passes2,386, zero failures/skips; all2,220 inputs/839 runner files independently matched to6fc3f8c9. |
| Bosses | Backend/controller/input complete; read-only audit merged57845da9. Now implementing approved19 in freshcodex/campaign-endgame basedd2dccacd; initial19 merged20892bb3 with5 generation passes; follow-up/acceptance open | Final worker89 focused/1288 affected pass, all2228 source/838 runner hashes independently matched. Earlier allocation failure retained/repaired. No actual rendered new-boss encounter yet. |
| Profiles | Completed rendered town/complex-save check (45072bfa→878165a1). Rumour/journal merged147684eb; physical split-travel continuation on root frozen entrances-v2 runtime | Earlier bounded profile terminal/fresh lifecycle passes with11 retained badges and future-only unlock. Read-only performance audit confirms all4 required no-hostile support IDs. Merged bfdea9b3 rendered town/private member/profile/exact save passes; all699 runtime hashes independently matched. No underground journey yet. |

Calendar v4 completed72 real paid hours alive; session96168 exited0 and all1567 runtime files match v3. Its frozen old runtime does not establish later accounting/complex/ecology acceptance. Completed root full regression and active worker rendered slot remain isolated/coordinated with null audio and bounded CPU. [MIGRATION_PAUSE.md](MIGRATION_PAUSE.md) is historical; no current pause is active. All eight full-game gates remain OPEN.

Root owns integration, conflict resolution, geography/role/deadline state, preservation, natural solo/four-member campaign journeys, final current regression and reproducible Linux delivery. Neither worker backend completion nor scoped town/profile checks close aggregate gates.

Current live work: root special-dungeon backend-v5 is terminal19/19 green; boss/endgame worker corrected followup-v2 and profile worker travel-world-v2 remain active. The first travel segment hit the1,024-request automation cap; exact fixed-input/public-boundary recovery and its actual limits are preserved in the worker record, and only its recovered final save is continued. No failed run was relabelled as a pass.
