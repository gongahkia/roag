# Colony web migration status

Updated 2026-11-06. Source baseline:
`4d89585c13cfcb6874e48a0d8ef64c3a2a1001df`.

The preservation/planning round is complete. The web game has not started. All
existing gameplay source and original protected artwork/content/docs remain intact.
New planning/checker files are local working-tree additions; no implementation goal
has been started, no changes committed/pushed, and no site deployed in this round.

| Phase | State | Evidence / next action |
| --- | --- | --- |
| P00 Preservation and planning | COMPLETE | Source tag; two verified archives; 1,163-file integrity check; 38 catalog tests; clean-snapshot generator check; [report](reports/P00-report.md). |
| P01 Browser foundation | READY | Use the first goal in [PROMPTS.md](PROMPTS.md). |
| P02 Simulation/world/saves | NOT STARTED | Depends on accepted P01. |
| P03 Jobs/construction/logistics | NOT STARTED | Depends on accepted P02. |
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
see [PRESERVATION.md](PRESERVATION.md) for the exact scope. No outstanding question
blocks P01. Hosting account/project details can be resolved when release is prepared.

When updating this file, link the actual latest report, preserve failed/partial
attempts, and move a phase to COMPLETE only after its acceptance gates pass. Do not
translate an old C# migration status into completion of a web phase.
