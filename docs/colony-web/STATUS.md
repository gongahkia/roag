# Colony web migration status

Updated 2026-11-06. Source baseline:
`4d89585c13cfcb6874e48a0d8ef64c3a2a1001df`.

P03 is complete: autonomous gathering, physical hauling/storage, worker-built
walls/doors/storage/workshop, repair/dismantling and a job board with priorities,
assignments, suspension and cancellation. Exact work continuation and conservation
are verified. P01, repair 01 and P02 passed user review; their reports/evidence remain
intact. All legacy sources and original protected artwork/content/docs remain unchanged.
Work is local and uncommitted; nothing was pushed or deployed. P04 has not started.
Review the [P03 report](reports/P03-report.md) before authorizing the next phase.

| Phase | State | Evidence / next action |
| --- | --- | --- |
| P00 Preservation and planning | COMPLETE | Source tag; two verified archives; 1,163-file integrity check; 38 catalog tests; clean-snapshot generator check; [report](reports/P00-report.md). |
| P01 Browser foundation | COMPLETE (repair 01 verified) | Latest repair: type/build, 8 local tests, 10 production browser checks at `/` and `/roag/`, inspected resize screenshots and 1,163-file preservation pass. [Repair report](reports/P01-repair-01-report.md). Original [P01 report](reports/P01-report.md) retained, including earlier clean-install and 38 catalog-test evidence. |
| P02 Simulation/world/saves | COMPLETE | 17 local tests; 103 seeds plus 3 forced fallbacks; reference RNG vectors and exact replay/save hashes; 20 production browser checks at `/` and `/roag/`, including real reload/JSON and observed native visibility; inspected screenshots; preservation passes. [Report](reports/P02-report.md). |
| P03 Jobs/construction/logistics | COMPLETE | 30 local tests; 19 work checkpoints and three exact mid-work save continuations; 103 seeds plus three fallbacks; 22 production browser checks at `/` and `/roag/`, ordinary extraction/cargo/delivery/construction, reload/JSON, cancellation, repair, salvage and occupied-stockpile recovery; inspected screenshots; preservation passes. [Report](reports/P03-report.md). |
| P04 Needs/production | NOT STARTED | Depends on accepted P03. |
| P05 First playable defense loop | NOT STARTED | Depends on accepted P04. |
| P06 Endless raids/squads/first boss | NOT STARTED | Depends on accepted P05. |
| P07 Bosses/biomes/hazards | NOT STARTED | Depends on accepted P06. |
| P08 Living world/recovery economy | NOT STARTED | Depends on accepted P07. |
| P09 Release/performance/cutover | NOT STARTED | Depends on accepted P08 and all required earlier gates. |

Confirmed defeat rule: heart destroyed OR all colonists dead. Temporary downing is
not death. Other product defaults and hosting assumptions are in [DECISIONS.md](DECISIONS.md).

Known preparation detail: ignored `assets/catalog.json` is stale and unchanged. The
tracked compressed catalog and recovered tracked asset workbench pass validation;
see [PRESERVATION.md](PRESERVATION.md) for the exact scope. No outstanding product question
blocks P03. Hosting account/project details can be resolved when release is prepared.

When updating this file, link the actual latest report, preserve failed/partial
attempts, and move a phase to COMPLETE only after its acceptance gates pass. Do not
translate an old C# migration status into completion of a web phase.
