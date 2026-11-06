# Roag colony web migration plan

Prepared 2026-11-06 against legacy commit
`4d89585c13cfcb6874e48a0d8ef64c3a2a1001df`.

This is the active migration plan for a new TypeScript/Phaser/Vite game. The old
RPG plans are preserved reference material, not instructions to resume the previous
C# migration. [Decisions](DECISIONS.md) distinguish user requirements from tunable
defaults. [Status](STATUS.md) records actual delivery; this plan describes future work.

## Product and first release

The player designates work, provisions a small colony, builds defenses, manages
inhabitants, and survives raids for as long as possible. The same colonists who
build and haul must eat, rest, fight, flee, recover, and repair the damage. The
colony heart and living population are both essential: losing either ends the run.

The core loop is gather -> haul -> build -> provision -> defend -> recover -> expand.
Pause and speed controls make planning manageable. Defensive placement matters,
but enemies can attack obstructions instead of being defeated by an impossible path.
Boss victories create recovery/reward opportunities and continued survival.

P05 is the first complete playable loop. P06 makes it an endless survival game.
P07 and P08 add the reusable boss and living-world identity. P09 is the release gate.
Each phase has a visible outcome; a passing isolated test is not a playable milestone.

The first release excludes multiplayer, server simulation, accounts, mobile-first
controls, a huge streamed overworld, underground floors, fluid physics, politics,
full dungeon exploration, all 103 boons, all 48 legacy bosses, and migration of old
saves. Preserve those references. Reconsider features only after the initial game
is stable and fun. A phase cannot quietly expand to implement the whole old game.

## Preservation and transition

P00 preserves original artwork, all original docs/content, catalog tooling, and a
complete source snapshot. See [PRESERVATION.md](PRESERVATION.md) for exact archives,
recovery instructions, and verification commands. The immutable manifest checks
original files; new web mappings, recipes, and rules live separately.

Start the web implementation under `web/`. Keep the old sources readable during
porting. The application must never import or invoke C#, Lua, MonoGame, a sibling
checkout, or the generated reference runtime. Python is acceptable for build-time
asset tooling and preservation checks; it is not a browser dependency.

At P09, use an explicit reviewed deletion list to retire old runtime/build files
from the active checkout. The source tag/archive and port map retain their history.
Keep assets, docs, content, the asset tools/tests, and the new web code. Preserve
original paths covered by the manifest; moving a protected file breaks that check.
Do not repoint or rewrite the global AGENTS.md symlink. This repository has no
installed Next.js package; the web target is Vite, not Next.js.

## Architecture contract

Suggested structure (create only what the current phase needs):

```text
web/
  package.json, package-lock.json, index.html, vite.config.ts
  src/
    simulation/       # pure deterministic TypeScript; no browser or Phaser imports
    content/          # typed runtime definitions and validation
    presentation/     # Phaser scenes, camera, sprites, effects, interpolation
    ui/               # DOM/CSS management panels and accessible controls
    persistence/      # browser storage, import/export, schema validation
    platform/         # time adapter, input and visibility handling
  content/            # colony-specific authored definitions and stable sprite aliases
  scripts/            # minimal asset export, scenario and benchmark runners
  tests/              # simulation/integration tests and browser journeys
  public/game-assets/ # generated selected artwork only
  dist/               # ignored static build output
docs/colony-web/
  reports/            # milestone reports and an accumulated decisions/status trail
artifacts/colony-web/ # ignored screenshots, traces, logs, benchmarks
```

### Simulation ownership

- `step(state, orderedCommands)` advances exactly one fixed tick. Commands have
  explicit sequence/tick identity; order cannot depend on asynchronous UI callbacks.
- Use a single authoritative ID and state record per person, enemy, building, job,
  item stack, reservation, and projectile/effect. Render objects hold references/IDs.
- Keep simulation randomness seeded with independent streams for map, resources,
  combat, raids, incidents, and loot. Do not call Math.random or read wall-clock time
  in simulation. Port C# unsigned arithmetic deliberately, with fixed known vectors.
- Separate decision cadence (slower jobs/routines) from movement/combat ticks and
  rendering. Preserve scheduled intent and stable tie-breaking in saves.
- Keep quantities and tick times integer where practical; constrain IDs/counters to
  safe integer ranges. Prefer arrays and small spatial indexes to an ECS framework
  until evidence justifies extra machinery.
- Draw interpolation never changes collision, damage, resources, discovery, or RNG.
  DOM/Phaser input produces commands; drawing and inspection are side-effect free.
- Validate command legality. Invalid orders cannot consume supplies or random draws.
  Pause stops ticks; planning may enqueue validated designations without doing work.
- Bound per-frame tick catch-up and route-search work. A slow frame must not drop
  authoritative ticks silently. Discard wall-time accumulation when explicitly paused
  or hidden, and report prolonged simulation overload during profiling.

### Assets and content

- Read canonical `assets/catalog.json.gz`; use stable source IDs and explicit crops.
  Existing game aliases are references, not neutral identities or animation metadata.
- Export only selected sheets/crops and a compact colony manifest to the web build.
  Never copy the full atlas browser, archives, catalog, old saves, or research docs
  into public game assets. Include credits for the actual shipped assets/fonts.
- Keep reproducible generated web artwork ignored and regenerate it in development/CI
  from tracked selections and preserved sources. The old catalog tests inventory all
  tracked media; adding derived PNGs to Git without classifying them would break that
  contract. Do not weaken the original catalog tests to accommodate build outputs.
- Keep original pixels untouched. Any black-background handling or tinting happens
  in derived runtime output/rendering, with a source mapping retained for review.
- Pixel art uses nearest-neighbor sampling; verify crop margins/gutters at several
  zoom levels. Do not infer autotiling, collisions, species, or frame sequences.
- Each content definition uses an explicit ID. Validate cross-references, counts,
  costs, duration bounds, and sprite bindings at build/load time.
- Start with explicit colony recipes and entity roles. Port meanings and invariants;
  do not mechanically translate the large `WorldSession` or generated C# rule layer.

### Save and browser boundaries

- Save schema version, content version, seed/RNG streams, clock, entities, jobs,
  reservations, cargo, terrain edits, raid schedules, pending impacts, and terminal state.
- Browser storage uses a transactional IndexedDB adapter, with a validated portable
  JSON export/import path. A failed/invalid import leaves the current save intact.
- Test after a real page reload, including a save made during hauling or an attack.
  Do not use unload events as the only save mechanism. Handle unavailable/quota-limited
  storage with visible failure and a working download path.
- Render-only camera/selection can be stored separately; it is not simulation authority.
- No offline progression. Visibility loss pauses the game and prevents accumulated
  background elapsed time from generating a surprise attack on return.
- Use relative/configurable base paths. GitHub Pages may serve under `/roag/`, Vercel
  under `/`. The same source supports both; root-absolute asset URLs are forbidden.

## Implementation phases

### P00 — Preservation and planning (this preparation round)

Deliverables: a legacy Git tag, complete source archive, portable asset workbench,
file hashes, a read-only verification tool, this plan, port map, goal prompts, and
report template. Test archive content and recoverability before future deletions.
Keep the original game and original protected files unchanged. A stale ignored
catalog export is historical data, not the canonical catalog.

### P01 — Browser foundation and artwork pipeline

Dependencies: P00.

Outcome: open the browser, see a small colony map using real preserved artwork,
pan/zoom/select a person, and inspect its name/role. This is a rendered foundation,
not yet a working colony simulation.

Work:

- Create `web/` with strict TypeScript, Phaser, Vite, a pinned Node version and npm
  lockfile. Check current official docs and pin compatible versions. Avoid unrelated
  frameworks, procedural placeholder art, server features, and renderer rewrites.
- Establish a small asset-selection input and deterministic exporter with source
  ID/crop provenance. Load terrain, people, structures, food, and useful UI symbols.
- Implement resize, camera bounds, pixel scaling, keyboard/mouse input, selection,
  readable DOM panels, and a small credits view. Keep UI commands separate from state.
- Add typecheck, build, test and browser-test commands. Wire preservation checks into
  a scoped web CI job; do not claim old .NET CI has been replaced yet.

Acceptance:

1. `npm ci`, typecheck, tests and production build succeed in `web/`.
2. Browser tests visit a served production build at `/` and `/roag/`, exercise camera
   and selection, and detect missing assets/console errors. Inspect screenshots.
3. Runtime manifest validates every source ID/rectangle and the final distribution
   excludes the full catalog/atlas/archive. Record raw and compressed build size.
4. Preservation verification passes. Document exact start/build commands and versions.

### P02 — Deterministic world, movement, and saved continuation

Dependencies: P01.

Outcome: a seeded terrain map with the heart and moving inhabitants; pause/speed and
save/reload work before complex management systems are introduced.

Work:

- Implement fixed ticks, command ordering, independent RNG streams, entity IDs,
  integer resource records, terminal-state scaffolding, and headless scenario runner.
- Generate one connected surface map with terrain, wood/stone/food sources and clear
  starting approaches. Validate reachability; bounded attempts use a known fallback.
- Add tile occupancy, deterministic routing, route invalidation after terrain edits,
  and spatial lookup. Set finite search budgets and expose blocked reasons.
- Add pause/speed and tab-visibility behavior, save schema validation, IndexedDB,
  JSON export/import, and replayable simulation commands.

Acceptance:

1. Same seed + command sequence yields identical state/RNG hashes at checkpoints;
   different render frame cadences leave those hashes unchanged.
2. Save midway through movement, reload, continue N ticks, and match an uninterrupted
   run. Reject corrupt/unknown-schema saves without overwriting the valid slot.
3. A multi-seed generation check verifies spawn/resources/heart reachability. A trapped
   route fails visibly and retries after access changes; no teleporting through walls.
4. Browser reload restores actual state. Hidden tab/resume causes no catch-up progress.

### P03 — Physical work, hauling, and construction

Dependencies: P02.

Outcome: designate trees/rock, deliver actual materials to a stockpile, and construct
walls, doors, storage and a workshop through autonomous workers.

Work:

- Port resource stocks, task claims, worker selection, reservations, pickup, carrying,
  delivery, timed construction, repair, dismantling, and suspension/resumption.
- Make job priorities and named assignments visible. Avoid multiple workers claiming
  the same output or one worker performing two jobs in the same tick.
- Place costs in colony content data. Reserve once; transfer actual cargo; refund or
  leave a recoverable bundle on cancellation/death. Make full storage/blockage legible.
- Invalidate routes when buildings/doors change. Protect the heart/spawn and validate
  placement. Colony walls can enclose areas; enemy breaching arrives in P05.

Acceptance:

1. Conservation equation holds through extraction, transport, construction, cancellation,
   dismantling, death and reload: initial + produced = stored + carried + reserved/site
   material + embodied/spent + explicitly lost, with each unit in one category only.
2. Real scenarios cover full storage, two claimants, missing ingredients, changed target,
   blocked path, worker handoff/death, and save during pickup/hauling/building.
3. A browser journey designates a source and observes the worker collect, carry, and
   finish construction. Directly spawning a finished wall does not establish this gate.

### P04 — Colony survival and production

Dependencies: P03.

Outcome: a small self-sustaining settlement with food, rest, treatment, workshops,
work priorities, and clear explanations when a resident cannot complete a task.

Work:

- Port hunger, fatigue, injuries, simple mood, and danger/food/rest/work priorities.
  Needs interrupt work without deleting its progress or duplicating reservations.
- Add beds/rest areas, food storage, a small plant/harvest cycle, a kitchen and infirmary.
  Explicit farming is new behavior: old grain gathering is a reference, not full farming.
- Start with gatherer, builder, cook, healer, and guard duties; allow one person to
  change duties. Build a small recipe chain using food, wood, stone, ore, and medicine.
- Show population, food reserve, jobs/blocked reasons, health, and priorities. Make all
  common actions usable with visible controls. Do not impose the old four-person cap.
- Implement downed/dead lifecycle and rescue work sufficient for P05. All-dead defeat
  is real; temporary downing alone does not end the run.

Acceptance:

1. Several simulated days through ordinary commands sustain the test colony without
   synthetic stock injections. Deliberate starvation and overwork have visible costs.
2. Needs interrupt and resume hauling/construction across saves with exact accounting.
3. Downed rescue consumes real time/medicine; a worker's death releases/settles claims.
4. Browser journey builds a food chain and beds, adjusts priorities, and diagnoses an
   intentionally blocked job from UI explanations. Save/reload the resulting colony.

### P05 — Defenses and first complete survival loop

Dependencies: P04.

Outcome: build a settlement and defenses, survive one announced raid, repair, and
continue playing. This is the first complete playable milestone.

Work:

- Introduce shared combat resolution, targets, range/line of sight, cooldowns, delayed
  locked impacts, damage/death, and visual attack warnings. Fixed simulation owns combat.
- Build a small defense set: wall/gate, projectile tower, trap, and guard post. Towers
  consume defined ammunition; hauling replenishes it and workers repair damaged defenses.
- Add melee raiders and a breacher. Target the heart or reachable defenders according
  to explicit policy. If enclosed, attack a sensible obstruction and recalculate after
  it breaks. Do not spawn inside the colony to bypass a blocked route.
- Schedule one announced wave using a separate seed stream. Give the player useful
  warning, defender rally/retreat controls, and emergency shelter behavior for civilians.
- Resolve defeat once on heart destruction or all actual colonist deaths. Provide
  run statistics, a terminal save, and an explicit fresh-run action.

Acceptance:

1. Browser journey starts a fresh colony, collects costs, builds/reloads defenses,
   survives a raid, rescues/treats damage, repairs, and resumes production.
2. Test enclosed-heart breaching, shot blockers, target death before impact, destroyed
   tower mid-shot, finite ammo, and exactly-once death/loot. Save mid-wave and compare.
3. Demonstrate both defeat paths and terminal reload. An all-downed colony remains
   consistent with the rescue rules rather than falsely triggering all-dead defeat.
4. Balance evidence includes a viable prepared defense and a losing unprepared case;
   invincible defenders or debug stock do not count as the playable acceptance run.

### P06 — Endless pressure, enemy squads, and first boss

Dependencies: P05.

Outcome: repeated raids with recovery windows, varied tactical problems, and an
Ossuary Captain boss wave, followed by continued play.

Work:

- Implement a deterministic raid director: announced timing, finite per-wave budget,
  composition/entry rules, capped active attackers, recovery periods, and retained
  pending spawns. Start with elapsed days/waves as the readable difficulty basis.
- Never resurrect a dead enemy identity or repay its rewards. Persist the wave and
  director state so reload cannot reroll difficulty, spawn positions, or loot.
- Port two contrasting squads, such as shield bearer + archer and healer + rushers.
  Add artillery only when readable impact/counterplay is established.
- Adapt the Ossuary Captain's facing protection, charge and breakable barricades to
  colony lanes. Preserve openings/counterplay and avoid permanent map deadlocks.
- Record waves survived, losses, resource pressure and repair burden. Tune bounded
  growth; endless play does not require infinitely accumulating entities/events.

Acceptance:

1. A seeded ordinary run survives multiple waves, one boss, and a later wave; saves
   restore warning/spawn/combat/recovery phases exactly.
2. Support removal changes enemy behavior; walls and flank routes matter; the boss
   has visible exploitable windows rather than only additional health.
3. Late-wave stress tests respect actor/event bounds without deleting living attackers,
   inventing rewards, or claiming natural balance from artificially accelerated fixtures.

### P07 — Bosses, biomes, and environmental defenses

Dependencies: P06.

Outcome: the colony encounters three more distinct boss problems and multiple
landscape themes that change preparation and defense.

Work:

- Port Bellkeeper noise/resonators, Sluice-Maw explicit channels/valves, and Threadmother
  anchors/webs/pulls/fire. Reuse their concepts and source tests, not the party-turn UI.
- Give each boss valid terrain and accessible counters. If required geometry cannot
  be placed safely, omit/defer that encounter; never overwrite occupied colony buildings.
- Add a small selected biome set (for example ruins/woodland, marsh/aqueduct, ash/forge)
  with explicit resources, hazards and raid mixes. All bosses must be reachable across
  documented seeds/scenarios; all seven old dungeon themes need not ship initially.
- Make hazards mechanically consistent for workers and enemies. Fire/spores/slowing
  terrain are bounded local rules; do not introduce a full fluid/atmosphere simulator.
- Store new art roles separately and validate each boss's exact selected crops.

Acceptance:

1. For every boss, demonstrate ordinary encounter actions, counters, defeat/reward,
   continued colony play, and reload during its distinctive pending effect.
2. Check valve state, resonator destruction, web cancellation/spread, blocked pulls,
   friendly-fire policy, and absence of permanently sealed mandatory routes.
3. A multi-seed map/encounter matrix validates safe placement and biome resources.
   Save files retain actual edits; returning/reloading cannot repaint the settlement.

### P08 — Living colony, climate, and recovery economy

Dependencies: P07.

Outcome: surviving between raids has consequences: weather affects work/food,
inhabitants remember events, supplies recover under finite rules, and trade assists recovery.

Work:

- Port a bounded subset of traits, mood/memories, work/aid consequences and preferences.
  Tie memories to actual witnessed effects, not just queued/cancelled designations.
- Add seeded seasons/weather with readable production/rest/travel effects and real
  shelter behavior. No new random consumption from UI inspection.
- Add finite wild-food pressure/regrowth and a small wildlife lifecycle where useful.
  Preserve identity and one-time loot; bound historical corpses/receipts safely.
- Add periodic trader visits with finite stock and conserved transactions. First
  release trade is local; a simulated continent of caravans is deferred.
- Add recoverable incidents and recruitment opportunities up to measured capacity.
  Avoid making complicated social simulation a prerequisite to basic survival.

Acceptance:

1. Weather changes actually affect production/shelter and survive exact reload.
2. A consequence scenario changes a resident's behavior and can recover through real aid.
3. Trade/forage/recruitment cannot duplicate resources or identities through reload.
4. Run a longer colony scenario with food/stock/population/event-history measurements;
   distinguish fixture isolation from natural balance and disclose unresolved issues.

### P09 — Release, performance, and retirement of old runtime

Dependencies: P08 and all earlier acceptance gates; P05/P06 may have local or explicitly
requested preview deployments sooner without being called the full release.

Outcome: a clean static web release with a documented URL (once its destination is
configured), robust browser saves, credits, and an active tree focused on the new game.

Work:

- Finish onboarding, legible need/job/raid explanations, pause/speed, sound controls,
  reduced motion, keyboard focus, contrast/readable text, and error recovery.
- Profile 24 colonists + 150 attackers + representative buildings/jobs on a 128x128
  map. Initial targets on documented desktop hardware: sustained 60 FPS, p95 pure
  simulation tick under 10 ms, startup compressed transfer under 8 MiB. These are
  provisional measured budgets; record hardware/browser and explicitly revise any
  justified budget rather than silently weakening acceptance.
- Run Chromium and Firefox journeys; check WebKit if tooling is available. Report
  untested browsers honestly. Test real production output and storage failures.
- Verify root/subpath deployments, compressed sizes, missing-resource errors and
  absence of old runtime dependencies. Configure a web-only CI pipeline.
- Prepare deployment to the user-selected Vercel destination; retain static subpath compatibility for Pages.
  Publish only to the user-selected/authorized destination; missing access blocks
  publication, not packaging or local release tests. Save a release manifest and URL.
- Re-verify archives and original files before deleting old runtime sources/projects,
  Lua entry points, generated translation code and superseded build workflows. Use
  an explicit path list, preserve unrelated work, and retain the baseline tag/archive.
  Keep catalog tools/tests and protected content/docs in place. Do not remove .git.
- Make the new web commands the primary README instructions. No old executable is
  needed for development, tests, build, or runtime after cutover.

Acceptance:

1. Fresh install -> build -> ordinary play -> multiple raids -> boss -> recovery ->
   save/reload -> both defeat paths -> new run is reproduced with actual browser evidence.
2. Required simulation/integration/browser/preservation checks pass on final sources;
   screenshots and benchmarks identify their exact commit or uncommitted diff.
3. Record memory/save/history growth during long runs; avoid unbounded logs or receipts.
4. A real static host serves the game when publication is configured. Otherwise report
   exactly which release checks passed and mark publication incomplete.
5. Recovery archive and selected legacy references remain accessible after cutover;
   the web app and catalog tooling still work without the retired runtime.

## Per-phase evidence and operating loop

Use [PROMPTS.md](PROMPTS.md) to run one phase at a time. Before changes, read the
current plan/status, the previous report, and the relevant source/tests in PORT_MAP.
Record starting commit and existing local changes. Do not reset unrelated work.

Use Vitest (or an equally small test runner with a documented reason) for meaningful
simulation contracts, and Playwright for browser interactions. Tests should exercise
conservation, ordering, continuation and user outcomes, rather than mirror private
implementation details. Prefer small captured C# behavior fixtures when semantics
are retained; use new tests for deliberately changed real-time/colony rules. Do not
port the complete legacy regression suite or require .NET to run the web suite.

Every report uses [REPORT_TEMPLATE.md](REPORT_TEMPLATE.md) and identifies completed,
partial and deferred work; actual commands/exit codes; scenario seeds and paths;
rendered evidence; user-facing changes; known risks; and the next smallest useful goal.
Do not mark a phase complete because source exists or unit tests alone pass. Do not
mark the entire game complete because an early phase passes. If a required check
cannot run, repair the environment if feasible and record the specific remaining gap.

The next planning pass reviews the report and current tree, then generates either a
repair goal for this phase or the next phase's goal. Routine implementation decisions
do not require repeated approval. Product changes and missing deployment destinations
are concise clarification questions. Existing goal lifecycle/budget rules still apply.

## Principal risks and responses

| Risk | Response |
| --- | --- |
| Repeating the previous long migration without a playable result | Finish P05's visible full loop before more content. Phase goals stop at explicit gates. |
| Erasing asset interpretation work during cleanup | Immutable file manifest, complete workbench, source archive, and preservation check in every phase. |
| Coupled C# party logic becomes coupled TypeScript | Port one invariant/use case at a time into small systems; keep rendering separate. |
| Workers duplicate stock or become stuck | Reservation/cargo ownership, deterministic claims, blocked reasons, conservation and reload scenarios. |
| Tower mazes make enemies inert | Explicit obstacle attack/breaching policy and dynamic route invalidation. |
| Simulation stalls at colony/raid scale | Spatial queries, bounded planning, cadence separation; profile representative active actors before adding workers/threads. |
| Browser saves break between milestones | Versioned schema/content, exact continuation, controlled migration or explicit rejection; never silent overwrite. |
| Content scope overwhelms the first release | Port map separates selected first-release mechanics from archived later expansion candidates. |

## Technical sources checked during planning

- [Phaser installation and TypeScript support](https://docs.phaser.io/phaser/getting-started/installation).
- [Vite static deployment, including GitHub Pages](https://vite.dev/guide/static-deploy.html).
- [Vite on Vercel](https://vercel.com/docs/frameworks/frontend/vite).
- [Official Codex Goals guide](https://developers.openai.com/cookbook/examples/codex/using_goals_in_codex).

Resolve and lock current compatible package versions during P01. Vercel is a hosting
choice for the Vite output, not a replacement for TypeScript, Phaser, or Vite.
