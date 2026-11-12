# Historical colony migration — superseded

The user paused this direction on 2026-11-12 and explicitly chose to restore the
classic Lua/LÖVE roguelike, dropping web support. The current game and instructions
are in the repository [README](../../README.md). This document and the reports below
remain historical records; they are not the current product plan. P06/P07 browser
verification was interrupted at the user's request. P08 was incomplete. Paused
worktrees and uncommitted P08 source were retained; the committed checkpoint is
`archive/colony-paused-2026-11-12` (`f7589d81`).

---

# Colony web migration status

Updated 2026-11-07. Source baseline:
`4d89585c13cfcb6874e48a0d8ef64c3a2a1001df`.

P05 is accepted with [its report](reports/P05-report.md), including the automatic
guard-post save fix and an explicit full-suite/targeted browser coverage boundary.
P06 is in progress in isolated director, encounter, persistence and presentation
worktrees. The user authorized internal implementation/review through P09-HUD and
P09, then synchronization of local and remote main; see [orchestration](ORCHESTRATION.md).
Nothing from this orchestration has been pushed or deployed. Vercel is selected;
account/project details can be resolved when the release is ready.

| Phase | State | Evidence / next action |
| --- | --- | --- |
| P00 Preservation and planning | COMPLETE | Source tag; two verified archives; 1,163-file integrity check; 38 catalog tests; clean-snapshot generator check; [report](reports/P00-report.md). |
| P01 Browser foundation | COMPLETE (repair 01 verified) | Latest repair: type/build, 8 local tests, 10 production browser checks at `/` and `/roag/`, inspected resize screenshots and 1,163-file preservation pass. [Repair report](reports/P01-repair-01-report.md). Original [P01 report](reports/P01-report.md) retained, including earlier clean-install and 38 catalog-test evidence. |
| P02 Simulation/world/saves | COMPLETE | 17 local tests; 103 seeds plus 3 forced fallbacks; reference RNG vectors and exact replay/save hashes; 20 production browser checks at `/` and `/roag/`, including real reload/JSON and observed native visibility; inspected screenshots; preservation passes. [Report](reports/P02-report.md). |
| P03 Jobs/construction/logistics | COMPLETE | 30 local tests; 19 work checkpoints and three exact mid-work save continuations; 103 seeds plus three fallbacks; 22 production browser checks at `/` and `/roag/`, ordinary extraction/cargo/delivery/construction, reload/JSON, cancellation, repair, salvage and occupied-stockpile recovery; inspected screenshots; preservation passes. [Report](reports/P03-report.md). |
| P04 Needs/production | COMPLETE (repair 01 verified) | 55 local tests; ordinary three-day survival, 13 exact care/work continuations, material accounting and starvation/overwork recovery; 24 production browser journeys at `/` and `/roag/`, including ordinary food/rest infrastructure, real medicine rescue, all-dead reload and native visibility; inspected screenshots; preservation passes. [Report](reports/P04-report.md). |
| P05 First playable defense loop | COMPLETE | 84 local tests; ordinary prepared/losing colonies; real raid, repair, rescue and reload at both mounts; 25/26 frozen browser cases then all four affected cases passed on patched builds; inspected visuals and preservation. [Report](reports/P05-report.md). |
| P06 Endless raids/squads/first boss | IN PROGRESS | Recurring director, support squads, Captain, schema 5 and presentation in isolated worktrees; ordinary five-wave and bounded-stress verification underway. |
| P07 Bosses/biomes/hazards | NOT STARTED | Depends on accepted P06. |
| P08 Living world/recovery economy | NOT STARTED | Depends on accepted P07. |
| P09-HUD Game interface | NOT STARTED | Explicit user-requested tranche: full-screen contextual/diegetic HUD, sprite pack and bitmap font; [plan](HUD_PLAN.md). |
| P09 Release/performance/cutover | NOT STARTED | Depends on accepted P08 and all required earlier gates. |

Confirmed defeat rule: heart destroyed OR all colonists dead. Temporary downing is
not death. Other product defaults and hosting assumptions are in [DECISIONS.md](DECISIONS.md).

Known preparation detail: ignored `assets/catalog.json` is stale and unchanged. The
tracked compressed catalog and recovered tracked asset workbench pass validation;
see [PRESERVATION.md](PRESERVATION.md) for the exact scope. No outstanding product question
blocks implementation. Hosting account/project details can be resolved when release is prepared.

When updating this file, link the actual latest report, preserve failed/partial
attempts, and move a phase to COMPLETE only after its acceptance gates pass. Do not
translate an old C# migration status into completion of a web phase.
