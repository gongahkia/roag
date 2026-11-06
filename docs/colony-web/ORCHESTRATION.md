# Migration orchestration

The user handed implementation to the coordinating Codex instance after P04 and
authorized parallel agents in isolated worktrees, conflict resolution,
deduplication, and integration into local and remote main. The remaining scope is
P05–P09 from PLAN.md; acceptance gates remain required. A phase is never complete
merely because its feature branch exists.

The root workspace uses `codex/colony-integration`. P04 was already committed as
`f1c49d4f`; all 53 source files matched the tested report. Repair 01 was reviewed,
tested in isolation, and merged before defense integration.

## P05 ownership

| Worktree branch | Owner | Files/responsibility |
| --- | --- | --- |
| codex/colony-integration | Coordinator | Shared types, world/step, logistics/lifecycle, controller, integrated verification and evidence |
| codex/colony-combat | Combat agent | Combat rules/content/geometry/navigation and focused combat tests |
| codex/colony-ui | Presentation agent | Scene, UI panels, HTML/CSS and defense controls |
| codex/colony-schema | Persistence agent | Strict save schema/combat validation and save regressions |

All worktrees are under `../roag-worktrees/colony-*`. Existing older worktrees are
unrelated and are retained. Agents commit only their owned files. Shared API
changes are communicated before implementation. Root merges commits, checks
cross-system behavior, and owns the final validation. Browser production servers
and integrated evidence writes are serialized; each worktree's unit evidence
uses its own ignored artifact directory.

## Shared P05 rules

The first normal warning begins at tick 10800; the first raid begins at tick
12000 (day four), giving the existing three-day survival foundation time to
establish. Development labs do not start automatic raids. Scenario fixtures must
be labeled; acceptance colonies must use real collection, construction and
ammunition production.

Tower magazines hold 12 ammunition and traps hold one. A physical reload job
carries four rounds to a tower or one to a trap. Generic hauling never fills a
magazine with unrelated resources. Ammunition is consumed once at attack launch,
including if the source is destroyed before impact. Unresolved attacks owned by
a dead or destroyed source are cancelled. Locked target cells never follow a
moving target. Friendly bodies block colony projectiles without taking damage.

Structures use shared destruction/dismantling cleanup for material ownership,
job/care claims and navigation. Raiders use global stable identities; each death
awards one recoverable wood unit once. Ledger validation accounts for this
explicit loot source. Actual all-dead or heart destruction is terminal; downing
alone remains nonterminal.

Later workstreams will be assigned after shared interfaces stabilize. Final
cutover requires verified archives and an explicit deletion list. Final remote
synchronization uses an ordinary push, preserving unrelated commits and branch
protection. Publication requires the chosen hosting destination and actual
verification of its deployed URL.

## P06 ownership and accepted checkpoint

P05 is accepted at `60155b42`; see its report for the complete browser coverage
boundary. P06 preparation was kept isolated while the P05 production build was
frozen. The dedicated P09-HUD tranche is required before release.

| Worktree branch | Responsibility |
| --- | --- |
| codex/colony-p06-integration | Coordinator-owned shared lifecycle, ordinary multiwave scenario and final integration |
| codex/colony-p06-director | Pure deterministic wave budgets, tickets, deadlines and bounded summaries |
| codex/colony-p06-encounters | Squads, Captain geometry, fair path budget, collision/lifecycle regressions and actor stress |
| codex/colony-p06-schema | Strict schema 5, malformed imports and encounter continuation |
| codex/colony-p06-ui | Visible roles, facing/windows, telegraphs, barricade counters and raid history |

P06 keeps the first warning/arrival and budget-seven roster. Later waves warn
1200 ticks after completion and arrive 600 ticks later; budgets grow by three to
60. Every fourth wave includes the Ossuary Captain. Announced IDs and entry choices
are saved before spawning. Actor/pending caps are 150 each, attacks 256,
barricades eight and history sixteen. Artificial cap stress is distinguished from
ordinary colony balance. Captain loot is five wood; other attacker loot remains
one. The outer entry ring is reserved from construction so entry cannot be sealed
with free blueprints. These are implementation defaults subject to measured tuning.
