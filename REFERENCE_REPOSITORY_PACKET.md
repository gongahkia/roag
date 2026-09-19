Completed. The packet is saved at DEEPWARD_CONTEXT_PACKET.md (/tmp/cosmonauts-context.QXSDHg/DEEPWARD_CONTEXT_PACKET.md).

The inspected baseline was `aa541332…`; the checkout then changed concurrently to clean commit `4f607699…` containing the Cosmonauts branding edits. I did not issue a commit command. Current snapshot: `0fd6daaebf537d108fe54e293775592498b150ac383e4cb1ba0ca3bdf7d70d84`.

The game title, UI, tools, docs, benchmarks, and test-temp paths now say Cosmonauts. I intentionally retained `deepward_02` and `deepward-map` only as documented legacy save/map wire identifiers, plus historical records and compatibility fixtures. Post-brand syntax, core, mocked GUI, and benchmark-smoke checks passed.

# DEEPWARD CONTEXT PACKET

## Orientation

This packet records the actual local checkout at the source snapshot specified below. It is for a separate web ChatGPT conversation. That assistant cannot open this checkout or run its tests; it receives information only through the owner relaying messages. It must not claim direct access or execution. The inspected snapshot uses the product name DEEPWARD. A separately requested branding change to Cosmonauts follows this context pass and is not folded into the baseline described here.

Evidence labels:

- **OBSERVED**: source read or command executed in this pass.
- **DOCUMENTED**: repository statement or historical report, not independently re-run now unless stated.
- **INFERRED**: interpretation from observed evidence.
- **OWNER INTENT**: requirements supplied by the owner.
- **UNKNOWN**: unavailable or not established.

This is context acquisition and verification, not permission to implement.

## 1. RECEIVING ASSISTANT BRIEF

The owner uses a manual relay: web ChatGPT helps with design, research, trade-offs, and review; local Codex inspects the actual checkout and performs only explicit owner-approved implementation/testing. A relayed suggestion is not an authorization. Do not say that you opened a file, ran a command, or tested a build solely because this packet mentions it.

**OWNER INTENT:** a finite, two-dimensional side-view colony-management game in Lua/LÖVE, using a fine-grid Noita-inspired material simulation. It is not the former top-down terrain laboratory. Engineering and operating a settlement should yield emergent stories through environment, logistics, ecology, and worker systems. Noita, RimWorld, Dwarf Fortress, and Caves of Qud are influences, not feature-parity targets or copied implementation claims. Core algorithms are to be written from scratch in Lua; LÖVE and ordinary runtime facilities are acceptable.

The target is intentionally hard and unforgiving. Permanent deaths, inaccessible resources, irreversible decisions, and settlement collapse are valid outcomes. They do not justify crashes, corrupt saves, nondeterministic replay, broken commands, or unexplained accounting errors. “Souls-like” does not authorize adding bonfires, bosses, respawn, or combat systems. Preserve the core loop. Earlier requests named varied terrain and spatial biomes, reusable serialized maps, individual and proportional crew assignments, fictional demolition charges, ruins, creatures, and an initially unfamiliar ecosystem with consistent discoverable rules.

Your first response should confirm these goals/constraints, identify consequential gaps without claiming full understanding, and give one prioritized batch of questions for local Codex only when needed. Do not start a redesign or feature pass before the owner chooses a task.

## 2. SNAPSHOT AND LOCAL STATE

**OBSERVED, 2026-09-19 Asia/Singapore:** the project root is the Git worktree root. Branch: main tracking origin/main. Full HEAD: aa54133221abc77161665aadbb5e176747d9580d; subject “Revert thing history import”; commit date 2026-09-19T13:20:53+08:00. The worktree was clean before and after inspection: no staged, unstaged, or untracked files. Root AGENTS.md is the only applicable guidance in this checkout; no nested AGENTS.md or AGENTS.override.md exists. It requires evidence-first, minimal diffs and reading a Next guide before changing Next code. No Next documentation directory exists; this is a Lua/LÖVE project rather than Next.

Baseline identifier:

```text
Git HEAD: aa54133221abc77161665aadbb5e176747d9580d
Scoped snapshot: 7e77fcc1140a7bc6cf59c55603ca2d6d09308691a4e88de0caec026d4c84f6e2
File count: 133

```

The scoped snapshot was made by listing all Git-tracked plus non-ignored untracked project paths, C-locale sorting them, SHA-256 hashing every file, then SHA-256 hashing the resulting filename-plus-digest manifest. It includes implementation, configuration, tests, example maps, fixtures, documentation, and historical records. It excludes Git internals, ignored files, and temporary output. The manifest is in `/tmp/cosmonauts-context.QXSDHg/snapshot-aa541332.manifest`.

**OBSERVED:** `config.lua:2-13` sets application/save envelope 0.4.0; base world state 0.2.0; default 256×160 fine cells; four-cell building blocks; challenge mode; 20 ticks/second; checkpoints/autosaves every 200 ticks; and default frontier/living/three-person generation. Map schema is 2, reads 1 (`src/mapfile.lua:10,78-80`). Feature/live-content version is 1 (`src/content.lua:5`). Current terrain generator is frontier-v2 (`src/generation/frontier.lua:9`); preserved frontier-v1 is in `src/generation/frontier_v1.lua:9`.

Runtime probe: Linux 7.2.5-200.fc44.x86_64 on x86_64; the `lua` command is **UNAVAILABLE**; LuaJIT is 2.1.1767980792; LÖVE is 11.5. The observed Fedora 44 kernel conflicts with AGENTS.md’s default Fedora-43 assumption. The real LÖVE window was not launched because save/display isolation was not established.

**OBSERVED mismatch:** `sha256sum` validation of `SOURCE_SHA256.txt` failed only for `.gitignore` and `AGENTS.md`; every other listed entry passed. That repository manifest is stale for this checkout and is not an acceptable current snapshot identifier.

Coverage ledger: all first-party Lua implementation modules were read (about 4,981 Lua lines including tests/tools); current root documentation, architecture/workforce/ecology/map contracts, test entrypoints, and tool bodies were read; current docs plus historical report boundaries were inspected. Example map JSON was sampled through schemas, consumers, and tests, not dumped (each is approximately 16.9–32.6 KiB). The 390–573 KiB legacy serialized fixtures were not dumped; their loaders, expected hashes, and compatibility tests were read. Historical directories `docs/legacy-v030`, `docs/prior-verification`, and `docs/verification-v040` are historical evidence, not claimed as new test results. This packet is broad but not a claim of complete understanding.

## 3. DESIGN INTENT AND NON-NEGOTIABLES

**OWNER INTENT:** retain side view, finite pre-generated maps, systemic engineering/operation, performance/memory awareness, repeatable evaluation, and connected systems rather than decorative features. The unknown ecosystem must follow authored stable rules, not random arbitrary punishments.

**OBSERVED challenge/practice policy:** a world mode is challenge or practice (`src/world.lua:5-20`). History queue rejects commands when viewing a challenge archive; in practice it branches by cloning the viewed state and discarding future commands/checkpoints (`src/history.lua:15-29`). Challenge extinction rejects new commands, though materials continue after the final death (`src/colonists.lua:66-69`; `src/commands.lua:31-35`). Practice material painting is enforced by command validation, not just disabled UI (`src/commands.lua:25-27`).

**OBSERVED deliberate limits:** ecology is bounded authored content, not general chemistry; fauna steer locally instead of using colonist BFS; there is no fog of war, structural stress, pressure/temperature solver, equipment/crafting chain, guard AI, projectile combat, weather, or infinite world extension. These are implementation boundaries, not owner requirements. Exact cross-runtime floating-point replay is not promised (`UPGRADE.md:25-28`).

## 4. WHAT CURRENTLY WORKS

| SubsystemImplemented behaviorLimitations/statusSource and verification |                                                                                           |                                                  |                                                                |
| ---------------------------------------------------------------------- | ----------------------------------------------------------------------------------------- | ------------------------------------------------ | -------------------------------------------------------------- |
| Runtime/world                                                          | Deterministic tick world; fine cells; 4×4 build grid; 2×3 worker body                     | Single-threaded; whole map active                | `src/world.lua:5-20`; `src/sim.lua:7-29`; core PASS            |
| Materials                                                              | Falling soil/sand, water/lava, steam rise/condense, lava-water, ice-lava reactions        | Local rules, no pressure/heat/support simulation | `src/materials.lua:1-13`; `src/particles.lua:21-72`; core PASS |
| Workers/jobs                                                           | Body-aware BFS, reachable faces, reservations, fetch/deliver/build/dig/haul/eat/rest/pump | No collision avoidance/global scheduler          | `src/nav.lua:4-108`; `src/jobs.lua:38-372`; core PASS          |
| Labour                                                                 | Priorities, pinned role, AUTO quotas by largest remainder, named owner, rally/hold        | Quota is headcount, no borrowing/training/shifts | `src/labor.lua:6-132`; expansion PASS                          |
| Settlement                                                             | Finite piles, construction escrow, food/needs/beds, irrigation/harvest, pumps, stockpiles | No industry chain/capacity economy               | `src/structures.lua:5-90`; headless/soak PASS                  |
| Charge/ward                                                            | Build, field-worker arming, 80-tick fuse, radius-10 attenuated blast/chains; water ward   | Fictional stylized rules; no disarm after arming | `src/blasts.lua:7-102`; expansion PASS                         |
| Ecology/ruins                                                          | 3 flora, 4 fauna, 4 sites, 5 ruin kinds; sight/survey/salvage/cull/signals                | Caps 128/64/48/32; local fauna steering          | `src/catalog.lua:3-24`; `src/ecology.lua`; soak PASS           |
| Generation/maps                                                        | 11 layouts, 12 biomes, 7 profiles, 3/6/9 crew; exact RLE templates                        | Openness does not guarantee route/survival       | `src/generation`; `src/mapfile.lua`; map/maximum PASS          |
| Persistence/history                                                    | Canonical data-only full saves, replay/checkpoints, live/archive separation               | 32 MiB bound; no fsync database transaction      | `src/history.lua:9-77`; `src/codec.lua:5-63`; replay PASS      |
| UI/tooling                                                             | Controls, inspector, crew draft, map lab, notes, exports/benchmarks                       | UI tests mock adapter only                       | `main.lua`; `src/render.lua`; `src/ui`; mock PASS              |

These counts are direct registry/constant counts, not a promise every map meets all generation targets. Ruin placement is bounded and can produce fewer than its target (`src/generation/wonders.lua:41-79`).

## 5. REPOSITORY AND ARCHITECTURE MAP

The entry path is `conf.lua` then `main.lua`. `love.load` creates a renderer, attempts `Store.load`, otherwise makes a default world/history (`main.lua:109-118`). The main scheduler caps catch-up at eight ticks/frame (`main.lua:144-178`). UI/input should queue mutations; renderer should not mutate simulation.

Exact `Sim.step` order, from `src/sim.lua:7-29`:

```text
increment integer tick
apply queued commands in recorded order
refresh labour allocation
advance fuses/blasts
step materials
settle loose item piles
step structures
step ecology if content exists
step colonists
periodically clean items/jobs

```

Wall clock is scheduler/measurement input only. Particle scans alternate direction and use cell stamps to prevent repeated moves in one tick (`src/particles.lua:21-72`). Geometry mutation increments `world.navRevision` (`src/world.lua:40-44`; `src/particles.lua:5-9`), but **OBSERVED** no Lua source reads it. Navigation instead recomputes flood maps during planning/rerouting and revalidates every next path edge (`src/jobs.lua:247-258`). `navRevision` is not an incremental cache invalidator.

```text
UI/input draft -> History.queue -> commands[tick+1]
History.advance -> Sim.step -> authoritative world table
world -> Metrics / renderer / checkpoint clone / Codec full save
map RLE JSON -> Map.toWorld -> Expedition.populate -> optional Content.install

```

Module ownership: `world.lua` owns cells/IDs/events/validation; `materials.lua` registry; `particles.lua` motion; `nav.lua` occupancy/BFS/reach; `jobs.lua` task/reservation lifecycle; `colonists.lua` needs/death; `structures.lua` buildings; `labor.lua` policy; `commands.lua` plus `colony_commands.lua` input contracts; `fieldwork.lua` encounter-work adapter; `blasts.lua` demolition; `catalog.lua`/`content.lua` registries/state; `ecology.lua` executable ecology; `history.lua` replay; `codec.lua`/`json.lua` codecs; `mapfile.lua` templates; `mapstore.lua` LÖVE map file adapter; generation creates terrain/content; `metrics.lua` accounting. Tests are core `suite.lua`, `maps.lua`, `expansion.lua`, plus mocked GUI adapters.

Glossary: fine cell = material coordinate; block = 4×4 construction cell; `gx,gy` = block coordinate; slot = `(gy-1)*cols+gx`; work pose = reachable standing cell with hand reach; live = current frontier world; view = live or reconstructed history; template = starting terrain/content, not full colony; owner = optional worker ID on a job.

### Material, navigation, and presentation detail

The material registry is deliberately small: air, bedrock, rock, soil, sand, water, ore, lava, steam, ice (`src/materials.lua:1-13`). Solids/powders/liquids scan bottom-up through the inner two-cell bedrock border. Powder may displace water; fluids move down, diagonally, then sideways; lava moves less often. Steam runs in a separate top-down pass, rises into air, and has deterministic periodic condensation. Lava-water turns lava to rock and water to steam; adjacent lava melts ice. Structures only block movement when a wall, or at the top row of a platform (`src/world.lua:34-39`). There is no pressure equalization, heat field, granular cohesion, structural integrity, true gas flow, or rigid body model. A structure can therefore make a physically stylized barrier but not a load-bearing simulation.

Navigation is body-aware. A worker needs a two-wide, three-high empty body, with solid/ladder support; safe routing rejects lava, steam, and upper-body water (`src/nav.lua:4-21`). Neighbors are one-cell horizontal moves, small rises, controlled drops of up to four cells, and ladder vertical moves (`22-43`). Flood is a deterministic BFS from each worker’s standing cell; returnability is separately computed for supply trips so a one-way drop does not trap a fetcher (`45-90`). A hand reaches within Manhattan range four only through a clear line segment (`92-108`). It follows that visually connected air is not a guarantee of workforce pathability. Generation report air-component metrics are explicitly point-cell metrics, not this body graph (`src/generation/report.lua:19-30`).

Input/modal precedence is concrete. Crew draft, field notes, help, map browser, and new-run lab short-circuit ordinary controls in `main.lua:185-237` and mouse paths in `309-357`. H opens crew, F4 notes, N lab, F2 template export, F3 map browser, F5 full save, F6 diagnostics plus a live replay, F8 benchmarks, F10 core suite inside LÖVE, and F12 screenshot. Space pauses/runs, arrows inspect/step, Home/End seek history, and map hotkeys queue designations. The renderer maintains a material image per world/tick/view and overlays structures, jobs, items, workers, encounters, chronicle/timeline, inspector and side controls (`src/render.lua:60-155` and `360-416`). **UNVERIFIED:** actual font/scale/GPU behavior; only command/raster mocks ran.

### Ecology rules, observation, and accounting detail

Flora records store food biomass 0–12, root water 0–6, phase, position, alive state. Every phased interval they can absorb adjacent water, convert stored water to biomass while recording `waterUsed`/`foodGrown`, reproduce by transferring their own stores, or die to burial/lost support/lava and drop their stores (`src/ecology.lua:86-124`). Veil bloom emits exposure/signals, glass reed turns adjacent steam into water, and iron thorn turns adjacent sand into rock. Fauna energy is 0–24; grazers consume wild biomass/ripe farms, leeches consume physical water, stalkers hunt grazers/follow recent signals, and sentinels wake from nearby signals and stay near home. Hostile contact damages both settler and creature in elementary counterattack logic; it is not a weapon/equipment combat system (`197-255`).

Recent signals are capped at 48 and pruned at 240 ticks (`src/signals.lua:3-13`). Sites have finite explicit stock. Cache salvage spills it; nursery transfers its own stock to possible grazer/flora creation; resonators emit later signals after disturbance; vents turn existing water into steam (`src/ecology.lua:257-284`). Field notes are knowledge gating only: visual map remains visible, observation is proximity plus clear segment, and survey unlocks archetype facts. World content is serialized in full saves/checkpoints, while templates retain only placements/stock/phases, not live knowledge or activation (`src/content.lua:57-84`).

The ledger does not record every movement because ownership moves between mutually exclusive representations. It records mined material, construction costs, food eaten/grown, water used/made, cooled rock, demolition waste, and practice-created minerals (`src/world.lua:13-19`; `src/commands.lua:45-51`). Thus an accounting residual detects a mismatch against baseline for the measured categories; it does not by itself validate every semantic rule, worker order, or UI explanation.

## 6. STATE AND CONTRACTS

`W.new` establishes plain authoritative world state at `src/world.lua:5-20`: dimensions/cells, structure slots, jobs/workers/items, bounded events, monotonic IDs, ledger, rules, optional biomes/labor/content. Underscore keys are dropped by deep clone/codec (`src/util.lua:10-16`; `src/codec.lua:14-24`), so they cannot be authoritative.

A worker has stable id, name, left-foot x/y, alive/HP/needs/body/task/carry data (`src/expedition.lua:38-42`). A job has id, kind, gx/gy, build, priority, state, delivered, progress, reason, and optional owner/assigned/target (`src/jobs.lua:38-43`). A structure has id/gx/gy/kind/enabled/growth/tank/status; a pump adds intake/outlet (`src/structures.lua:35-43`). Content shape is version, flora, fauna, sites, ruins, signals, discoveries, observed (`src/content.lua:45-55`) with strict caps/keys (`18-43;85-118`).

Queued command shapes include:

```lua
{type='order', kind='dig'|'build'|'remove', gx, gy, build?, priority?, worker?}
{type='labor', plan={quotas, weights, people}}
{type='field', kind='survey'|'salvage'|'cull', target, worker?, priority?}
{type='arm', slot, worker?, priority?}
{type='rally'|'releaserally', worker, x?, y?}
{type='target_order', gx, gy, worker}

```

Validation is centralized by `src/commands.lua:7-29` and `src/colony_commands.lua:7-31`. Labour requires all seven weights total 100 and one person record per worker (`src/labor.lua:20-35`). A named job overrides quota/pinned role but not priority zero/OFF (`src/labor.lua:101-111`). Precedence is concrete: evacuation/unsafe condition, hunger, fatigue, rally directive, then ordinary scored work (`src/jobs.lua:96-136`); worker update can interrupt for hazards/high needs (`src/colonists.lua:53-61`).

Accounting is conservation bookkeeping. Metrics includes water cells, item/carry water, tanks, root/site stores; minerals in terrain/items/job escrow/structure costs/site stock; food items plus wild reserves (`src/metrics.lua:5-41`). Residuals compare those totals with baseline and ledger changes. Policy release calls `J.release` with `drop=true` and cancellation returns delivered materials (`src/labor.lua:113-118`; `src/jobs.lua:29-36`). Passing residual tests prove their assertions, not a formal universal proof.

New authoritative state/commands need validators, initial/default state, clone/codec support, replay validation/application, resource ownership/ledger treatment, map-template versus full-save choice, UI/render integration, archive rules, and tests. A map field additionally needs `Map.validate`, JSON-array treatment, export/import contract, schema compatibility tests, and docs. Do not store serializable state in render caches.

## 7. END-TO-END TRACES

1. **Excavation:** mouse drag queues an order in `main.lua:343-355`; `commands.apply` calls `J.add` (`src/commands.lua:36-42`). `J.plan` runs BFS and finds a reachable work face. `J.digCell` checks hand reach. On sufficient progress `J.act` converts non-ice material to air, creates one physical resource stack at the worker, increments `ledger.mined`, and emits mining signal (`src/jobs.lua:314-329`). `W.put` changes `navRevision`; future path edges are rechecked. Weak point: no nav cache uses the revision.
2. **Quota plus named assignment:** crew UI queues a complete labour draft, not direct mutation; next tick refresh allocates living AUTO workers with largest remainder, preference/skill and stable ID (`src/labor.lua:61-92`). `Labor.score` allows owner override of quota/pinned role but not OFF. Evacuation, hazards, hunger, fatigue, and rally take precedence. Expansion tests cover arithmetic, vacancies, OFF, named jobs, policy release and need override (`tests/expansion.lua:31-84`).
3. **Reservoir breach:** digging/blasting changes material, and particles run before colonists. A stored worker path must pass `N.edge` at its next step or job becomes blocked with “Route changed or flooded” (`src/jobs.lua:252-258`). Water lowers breath; steam/lava/burial damage can kill (`src/colonists.lua:22-40`). **UNKNOWN:** no named regression drives the generated reservoir breach end-to-end; material, reroute, drowning, and blast paths are separately tested.
4. **Farm loop:** a build job fetches soil into `job.delivered` and installs only on a clear supported footprint. Food/crop workers fetch water piles or actual cells and transfer it to tank (`src/jobs.lua:152-180,272-304`). Every ten ticks a dry-enough farm grows; every 100 growth consumes tank water/records `waterUsed`; mature crop harvest becomes food pile/`foodGrown` (`src/structures.lua:62-75`; `src/jobs.lua:356-363`). A hungry worker consumes an item ration and records `foodEaten`. The executed 2,000-tick smoke ended with zero residuals, not universal survival proof.
5. **Charge:** construction consumes four metal; arm queues fieldwork; after 30 progress `Blasts.arm` sets `fuseAt=tick+80` and evacuation flags (`src/structures.lua:13-14`; `src/fieldwork.lua:32-52`; `src/blasts.lua:7-15`). The expiry wave is computed before mutations, attenuates through solids/structures, never crosses bedrock, changes water/ice, accounts lost/recovered mineral, destroys structures/cancels jobs, hurts actors, chains charges next tick, emits a signal, and is persisted. Expansion tests cover construction, arming, fuse, lethal blast, water changes, bedrock, chain and ledger.
6. **Ruin/ecology:** `wonders.place` embeds room geometry, stock/sites/occupants before starter stamping while excluding starter region (`src/generation/wonders.lua:7-102`). Nearby clear sight creates observation; reachable fieldwork surveys registry facts, salvages finite stock into piles, or culls a moving creature (`src/ecology.lua:233-285`; `src/fieldwork.lua:21-52`). Ecology consumes/transfers food/water, changes terrain, damages workers, and signals. Spoiler descriptions are `docs/ECOLOGY_RULES.md`.
7. **Template/save:** `Map.fromWorld` serializes exact terrain/biome RLE plus optional starting features, never live jobs/workers/inventory/history (`src/mapfile.lua:62-76`). `Map.toWorld` validates, creates a fresh world, adds standard crew/supplies, initializes health/knowledge (`116-123`). `History.bundle` instead includes initial/live worlds and command history (`src/history.lua:58-77`). Map schema and save envelope are separate gates.
8. **History:** seek clones initial/checkpoint and re-simulates commands (`src/history.lua:41-56`). Challenge queue rejects commands in archive; practice clones past state then truncates future (`15-23`). Main Home/End/arrow control calls it (`main.lua:264-268`). Tests cover challenge lock and practice branching.

## 8. GENERATION, CONTENT, AND EXTENSION POINTS

Add a layout through `Layouts.names`/descriptions, an open-mask function, `L.make`/hybrid selection, lab/docs/map/generation tests (`src/generation/layouts.lua:5-13,196-204`). Add a biome via stable ID/key in `src/biomes.lua:6-24`, placement/geology/render effects, map palette compatibility and exact round-trip tests. Layout creates open geometry; biome field is immutable spatial metadata; materials move; ruins/features are template state baked after geology and before starter stamp.

Add an archetype through catalog registry, content template validation, wonder placement, ecology execution, rendering/field notes, map/full-save support, metrics/ledger and tests. Add a duty via labour role/duty lists, crew UI, `roleForTask`, and task offer. Add job via validation, planning offer, action/reservations/cancellation, UI, accounting/replay/archive tests. Add a structure via `S.def`, site validity/install/step/render and build flow. Add command via both command layers when appropriate plus queue/replay/UI/archive tests. These are observed current seams, not a rewrite prescription.

## 9. SAVES, MAPS, REPLAY, AND COMPATIBILITY

| ArtifactRetainsDeliberately resets/omits |                                                                                                  |                                                                                       |
| ---------------------------------------- | ------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------- |
| Full save/replay `.dw`                   | Initial/live worlds and tick-indexed commands; workers/jobs/items/fuses/ecology/knowledge/policy | Checkpoints reconstructed; only known envelopes accepted                              |
| Initial `.dwmap.json`                    | Exact terrain/biomes/arrival/recipe/features/crew                                                | Live IDs/health/activity/jobs/structures/items/workers/orders/fuses/knowledge/history |
| Current-terrain map export               | Current terrain/biome/surviving encounter template                                               | Buildings, piles, bodies, farm growth, signals, knowledge/jobs                        |
| Archive view                             | Reconstructed chosen historical tick                                                             | Challenge is non-mutating; practice branches                                          |

Full codec is data-only and bounded (`src/codec.lua:5-56`). Map parser rejects unknown fields and validates RLE/bounds/known registries (`src/mapfile.lua:13-47,78-112`); it is defensive parsing, **not** an audited hostile-input guarantee. Save writing is `run.tmp` then same-directory rename to `run.dat`; failure intends to leave previous run, but there is no fsync transaction guarantee (`src/storage.lua:4-22`; `UPGRADE.md:29-34`). Never use a live save as a test target.

History restores save envelopes 0.4.0 or 0.2.0 (`src/history.lua:61-74`); worlds use base version 0.2.0 (`src/world.lua:73-112`); maps read schemas 1/2; v1 generation remains explicit. Tests advance genuine 0.2 and 0.3 fixtures. The product branding name should not be confused with these persisted protocol contracts.

## 10. VERIFICATION AND PERFORMANCE

All commands below ran from project root with LuaJIT 2.1.1767980792. Logs and disposable mock save directories are in `/tmp/cosmonauts-context.QXSDHg`, outside the checkout.

| ResultCommandOutcome |                                                   |                                                                  |
| -------------------- | ------------------------------------------------- | ---------------------------------------------------------------- |
| PASS                 | `luajit tests/syntax.lua`                         | 64 manifest Lua files compile                                    |
| PASS                 | `luajit tests/run.lua`                            | 66 groups; 113,663 assertions                                    |
| PASS                 | Three `luajit tests/*gui*.lua` temporary-dir runs | GUI/map/expansion mock-contract tests                            |
| PASS                 | `luajit tests/benchmark_smoke.lua`                | Two cases; six cooperative yields                                |
| PASS                 | `luajit tools/headless.lua 12345 frontier 2000`   | 3 alive, farm/3 structures, all residuals zero, 2.220497 CPU sec |
| PASS                 | `luajit tools/expansion_soak.lua`                 | Six 600-tick cases; replay/save/residuals pass                   |
| PASS                 | `luajit tests/maximum_size.lua`                   | 11 layouts at 512×256/crew 9; exact map round-trip/two ticks     |
| FAIL finding         | `sha256sum -c SOURCE_SHA256.txt`                  | Stale `.gitignore` and `AGENTS.md` entries                       |
| SKIPPED              | Real `love .`, F10/screenshot/file drop           | No confirmed isolated save/display session                       |
| UNAVAILABLE          | Makefile/`lua` default commands                   | No `lua` executable; no runtime installed                        |

The soak reports mean whole-tick CPU time 0.5766–0.8423 ms and ecology-only 0.0347–0.0627 ms across its six fixed 192×112 scenarios. They are `os.clock` CPU figures, include checkpoint work, exclude renderer/GPU, measure evolving worlds, and are not FPS/RSS/peak/GPU results. The maximum map test is a boundary smoke, not a long-lived populated performance guarantee.

`TEST_REPORT.md` and `docs/verification-v040` contain historical native-Lua-5.4/mock results. They remain historical even where counts agree with this pass. Existing tests exercise extension fields, fuses, ecology, ownership, schema and archive locks; they do not establish real LÖVE/SDL/GPU correctness, balance, universal reachability/survival, multi-runtime bit identity, or long-duration maximum-size play.

## 11. RISKS, MISMATCHES, AND OPEN QUESTIONS

| CategoryEvidence, consequence, confidenceSmallest next check |                                                                                                                                                                          |                                                                             |
| ------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------- |
| Observed documentation drift                                 | `SOURCE_SHA256` fails exactly two current files; cannot verify current package. High.                                                                                    | Regenerate only in approved packaging work.                                 |
| Observed architecture                                        | `navRevision` is written but never read; geometry safety relies on replan/edge checks. High.                                                                             | Decide whether it is diagnostic/dead before caching.                        |
| Intentional simplification                                   | Materials/fauna can strand actors; no recovery guarantee, consistent with harsh intent but feedback may be insufficient. High.                                           | Playtest flooded/blocked fieldwork with navigation explanation.             |
| Replay risk                                                  | Clone/codec canonicalize, but some order-insensitive scans use `pairs`; docs do not promise cross-runtime bit identity. Medium.                                          | Run fixed replay corpus under target runtimes before promising portability. |
| Compatibility constraint                                     | LÖVE identity `deepward_02` and map format `deepward-map` are persisted (`conf.lua:3`; `src/mapfile.lua:10`). Blind renaming breaks old save discovery/map import. High. | Owner chooses compatibility policy before changing either.                  |
| Unknown balance                                              | No real-window measured food/water throughput for 3/6/9 crew. High.                                                                                                      | Isolated manual test with seed/layout/actions/metrics.                      |
| Unverified presentation                                      | Mocks do not validate fonts, scaling, drivers, or physical file drop. High.                                                                                              | Run LÖVE with an isolated identity/save path.                               |
| Document orientation                                         | Docs say 0.4.0 release, but Git HEAD is later and source manifest stale. Medium.                                                                                         | Reconcile release/docs/manifest in approved maintenance work.               |

## 12. CONTINUING THROUGH THE RELAY

### WEB -> LOCAL CODEX

```text
Request ID:
Mode: INSPECT | PLAN | IMPLEMENT
Based on snapshot:
Owner's objective:
Questions or explicitly approved scope:
Files/symbols to inspect, if known:
Constraints and invariants:
Acceptance checks, when relevant:
Required response/evidence:

```

### LOCAL CODEX -> WEB

```text
Request ID:
Mode performed:
Source snapshot before/after:
Answers by question ID:
Evidence: paths, symbols, excerpts, command results:
Changes made: none, or exact approved changes:
Checks executed and outcomes:
Compatibility/accounting/replay impact:
Uncertainty, blockers, and unverified behaviour:
Updated context since the previous packet:
Decisions still required from the owner:

```

INSPECT and PLAN never authorize implementation. A relayed suggestion is not approval. IMPLEMENT applies only to owner-authorized scope. Never commit, push, migrate live saves, or expand scope because a remote assistant suggested it. Before later relay work, local Codex compares requested baseline with current Git/diff state and reports relevant delta; materially stale implementation instructions need clarification. Approved work reports changed files, public/schema effects, checks, remaining limits, and a small incremental update, not a regenerated full packet.

Where to look: startup/control routing `main.lua`; fixed tick `src/sim.lua`; world/coordinates `src/world.lua`; materials/nav/jobs `src/particles.lua`, `src/nav.lua`, `src/jobs.lua`; needs `src/colonists.lua`; labour `src/labor.lua`, `src/colony_commands.lua`, `src/ui/crew.lua`; economy `src/structures.lua`, `src/metrics.lua`; content `src/catalog.lua`, `src/content.lua`, `src/ecology.lua`, `src/fieldwork.lua`, `src/blasts.lua`; generation `src/generation`, `src/biomes.lua`; saves/maps/history `src/history.lua`, `src/codec.lua`, `src/json.lua`, `src/mapfile.lua`, `src/storage.lua`; UI `src/render.lua`, `src/ui`; tests `tests/suite.lua`, `tests/maps.lua`, `tests/expansion.lua`; tools `tools`; contracts `docs`.

END OF DEEPWARD CONTEXT PACKET