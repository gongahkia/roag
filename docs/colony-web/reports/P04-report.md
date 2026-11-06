# P04 — Colony survival and production

## Identity and outcome

**COMPLETE.** Implemented the requested deterministic hunger, fatigue, injury, simple mood, farming/recipes, physical eating/rest/care, five duties, safe work interruption, downing/death and exact versioned persistence. An ordinary six-founder colony now builds its own food/rest/care infrastructure, with visible shortage and access explanations. P05 has not started; no combat, recruitment or broader seasonal/social simulation was introduced.

Starting HEAD was `88026d076ea2940d244d1f0e38e4644f9dd2ceb1`, with the preceding implementation, workflow, reports and STATUS changes already uncommitted (`before-status.txt`, `before-source.json`). HEAD advanced during this session; the final tested HEAD is `67509c0602452a486b8e0108823d2b0a31281765`. No commit, push or deployment was performed by this implementation work. Exact source identity takes precedence over an assumption that HEAD stayed fixed.

All evidence paths below are relative to `artifacts/colony-web/P04/`. `tested-source.json` records 53 web/workflow file hashes, fingerprint **`7789ac88faf4b8722706955e8ad078fc0e8185c24be302198ad0802a7c0cac76`**. `final-source.json` and `final-status.txt` identify the remaining uncommitted files; `P04.patch` is the incremental diff against the preserved start-of-phase snapshot, and `full-worktree.patch` is the remaining diff against final HEAD. Existing implementation and earlier reports/evidence were retained.

## Acceptance evidence

All commands in this table were executed with exit **0** on the final implementation. npm commands ran from `web/`, with `ROAG_EVIDENCE_DIR=../artifacts/colony-web/P04` and `fnm exec --using=24.21.0`.

| Requested gate / plan gate | Result | Actual command/scenario | Evidence and observed outcome |
| --- | --- | --- | --- |
| Three complete ordinary days; plan gate 1 | PASS | `npm run test:scenarios`, seed 42, 21 recorded ordinary commands | `survival-scenarios.json`: checkpoints 3,600/7,200/10,800 ticks; six living founders at 100 health, five meals and four rests each; no injected stock or disabled needs. |
| Starvation/overwork consequences and recovery; plan gate 1 | PASS | `npm test`, natural starvation and focused deprivation/overwork scenarios | `needs-checkpoints-86016.json`, `edge-scenarios-86015.json`: visible reasons/health loss; fed overwork alone damages workers, rebuilt beds and physically produced medicine restore all six to 100 health by tick 9,318. |
| Hauling/construction interrupted by eating/rest/treatment; plan gate 2 | PASS | `npm test`, six combinations, save during interruption, advance 650 ticks | `needs-checkpoints-86016.json`: exact restored/uninterrupted hashes, original work resumes, all material categories balanced. Thirteen saved continuation comparisons total. |
| Accounting, repeated cancellation, competing claims, blocked care, full storage, worker death; plan gates 2–3 | PASS | `npm test`, survival and edge suites plus retained work suite | 55 tests pass; separate care/work ownership, scarce-food claims, production output retention, medic death, sealed patient, suspension/reassignment and dead-footprint saves covered. |
| Rescue/death/all-downed/terminal saves; plan gate 3 | PASS | `npm test`; `npm run test:browser` on both bases | Physical medicine pickup, 40-tick stabilization, 60-tick clinic treatment, recovery; browser consumes exactly two doses. All six downed remains nonterminal; 900-tick deadline produces actual deaths. Terminal reload and explicit new run verified. |
| Ordinary browser food chain, beds, priorities, blockage, saves; plan gate 4 | PASS | `npm run test:browser`, seed 42, UI commands only | `browser-summary.json`, `browser-results/`: farm/kitchen/three beds/infirmary/workshop built; Ada cook 3, Ivo healer 3; medicine shortage visibly resolved with ore production. Real page reload and JSON round-trip exact at both `/` and `/roag/`. |
| Retained deterministic/work/presentation/storage checks | PASS | `npm test`, `npm run test:scenarios`, `npm run test:browser`, `npm run assets:check` | 103 seeds + three forced generation fallbacks, reference RNG vectors, work conservation and continuation, camera/selection/resize regression, credits/provenance/missing art, quota/unavailable storage, invalid imports and observed native visibility. 24/24 browser journeys, zero skips/flakes/unexpected failures. |

The ordinary three-day run established its infrastructure by tick 2,916. Eight harvests produced **40 raw food**; 14 meal batches produced **56 meals**, with **30 eaten** and 26 remaining. Finite forage depletion was zero. Three ore batches supported two medicine batches (four doses) and one tool. All six survived at full health; no rescue/treatment was needed in that ordinary run. `workTicks=12163` counts work-service dispatches, not necessarily productive labor. These are measured results for this seed/layout, not a claim of universal balance.

At day three the ledger records wood `61 = 24 stored + 22 embodied + 15 consumed`; stone `29 = 15 stored + 8 embodied + 6 consumed`; farm food `40 = 22 stored + 18 recipe inputs`; meals `56 = 26 stored + 30 eaten`. All three ore were consumed, four medicines and one tool remained. Full per-checkpoint material and job records are in the scenario JSON. Two render cadences match 18 checkpoints over 10,800 ticks. The day-three world hash is `d84ded0dd29529f3f5514e261c1dc115e837ddd751cd761f1f987e6206125d84`; saved/restored execution for another 500 ticks matches `cb646439520310cb17d14fcaa1465da588a6b517e1e14da28dbc9a7f7828a94a`.

Failed attempts remain in `validation.json` and their named logs. Initial fixture assumptions used the old stock/job shape or sealed an occupied tile; these were corrected. Early continuous production filled storage, motivating explicit reserve targets. Edge tests exposed guard-priority preemption, deferred destinations during care, a hauling retry cancelling physical yielding, and a healer retaining a patient who left for urgent rest. These were fixed with targeted regressions. A valid blueprint on a deceased founder's tile initially failed save validation (`dead-footprint-before-fix.log`, exit 1); validation now consistently ignores dead occupancy. An intermediate typecheck found an extra parenthesis (exit 2), corrected before final checks. A recording wrapper initially lacked its JSON file; its failed run is explicitly retained. Final local/build/scenario/browser checks all pass. Earlier successful browser attempts are retained under `attempt-01-*` and `attempt-02-*`.

## Port and design decisions

| Legacy sources/tests read | Retained invariant | Intentional adaptation | Verification |
| --- | --- | --- | --- |
| `WorldOccupations`, `WorldOccupationValidation`, `WorldNativeResidents`; `OccupationTests`, `NativeResidentWorkTests` | Physical arrival, finite inputs, delivered supplies, one output, retained interrupted work | Six independently assignable workers; no aggregate town production or four-person party cap. Explicit planting/tending/growing/harvest is new. | Ordinary chain; cancellation/output/full-storage tests; three-day accounting. |
| `WorldSimulation`, `WorldRoutines`, `WorldResidentJourneys`, `NeedTimeline`; `ResidentRoutineTests`, `ResidentJourneyTests` | Food/rest priorities, arrival, penalties, blocked reasons, resumed work | Integer colony ticks, exclusive bed/clinic claims, simple derived mood. Workers waiting for unavailable facilities can still build/provision them. | Six interruption/save combinations; scarce food, blocked clinic and overwork recovery. |
| `TacticalResidentRescue`, combat down/revive/die paths, `WorldNativeLife`; `NativeNpcRescueTests`; `TacticalRevival` reference | Real medicine and time, limited window, stable deceased records, cargo recovery and exactly-once cleanup | 900 simulation ticks instead of personal turns; field stabilization permits walking to a clinic. No enemy resurrection or combat added. | Two-dose browser rescue, sealed/unrescued patient, medic death, all-downed/all-dead reload. |

Validated content lives in `survival-content.ts`/`work-content.ts`; deterministic transitions in `needs.ts`, `jobs.ts`, `step.ts`, model/world and save validation. Rendering/UI only reads state and queues commands. Original independent RNG streams and ordered command semantics remain intact. Installed Phaser 3.90 Graphics/tint documentation was consulted for the care visuals. DECISIONS.md is unchanged; phase-specific tuning is recorded here and in `web/README.md`.

A day is **3,600 ticks at 10 Hz**. Hunger/fatigue each accrue one per living tick. Food is sought at hunger 1,800, beds at fatigue 2,400; hunger 3,600 or fatigue 4,800 costs one health every 20 ticks. Hungry/tired/seriously injured workers service work on alternate ticks. Mood is a bounded derived display, without social memories. Founders start fed/rested with zero inventory.

One physically collected meal reduces hunger 1,800, raw food 900, after 20 eating ticks. Beds reduce fatigue 12/tick (11 net) until 200. One infirmary admits one patient; its production bench is independent. A healer reserves and collects actual medicine, reaches the patient, treats for 60 ticks, then permits 100 recovery ticks. Downing releases work safely and starts a 900-tick deadline. Field aid takes one dose/40 ticks and restores 40 health without resetting hunger/fatigue; the patient must walk to a clinic for a second dose. No capable rescuer means eventual death, not immediate all-downed defeat.

Farm work is 20 planting + 20 tending + 240 growing + 20 harvest ticks for five food; growth releases the worker. Recipes are `food + wood → 4 meals` (30 ticks), `2 stone → ore` (30), `2 food + ore → 2 medicine` (40), `ore + wood → tool` (30). Reserves are 20 raw food, 24 meals, four ore, four medicine and two tools; started batches finish. Tools are stored products without an equipment bonus. New buildings cost farm 2 wood/20 ticks, kitchen 4 wood+2 stone/30, bed 2 wood/16, infirmary 4 wood+2 stone/30. Existing P03 costs remain.

Five duties use 0 disabled / 1 normal / 2 preferred / 3 first. Gatherers farm/haul, builders construct and use workshops, cooks make meals, healers make medicine and aid others. Dominant guard priority assigns a physical noncombat watch. Existing other-worker claims remain stable. Higher-duty reassignment releases lower-duty work safely.

Needs retain the interrupted job, progress, input reservation, site supplies and work cargo, while using a separate one-unit care pouch. Suspension/reassignment/cancellation settles work ownership independently of care. Cancellation drops delivered site supplies; interrupted unused medicine drops locally. Manual destination orders during care are deferred. Death clears all claims/cargo once; dead identities remain in saves. Per-material accounting includes generation, consumption, work/care cargo, sites, embodied buildings, salvage losses and explicitly lost stock. Reservations are subsets, never extra inventory.

Save **schema 3**, content **`colony-p04-1`**, persists bodies, routines, claims, deadlines, crop/recipe stages, movement/jobs, pending commands, IDs, RNG and accounting. P02 schema 1 and P03 schema 2 are rejected safely; no migration is claimed. Validation enforces references, ownership, counters and integer bounds within the existing 2 MB limit. Invalid imports do not replace current state or the valid transactional slot.

## Preservation and scope

Before and after `python3 -B tools/verify_preservation.py`: **PASS, all 1,163 originals**, baseline `4d89585c13cfcb6874e48a0d8ef64c3a2a1001df`. `integrity-after.json` additionally verifies 1,653 earlier-evidence/manifest/archive hashes, five prior reports, all 53 tested-source hashes and every shipped file in both builds. Manifest, archives, protected originals and legacy sources remain unchanged. `verify-evidence.py` never rebaselines preservation data.

No artwork or bindings were added: all **27** selected original crops remain. Farm uses `urizen-onebit-r24-c000`, bed `urizen-onebit-r36-c000`, hearth/kitchen `urizen-onebit-r42-c002`, infirmary cottage `urizen-onebit-r01-c030` plus a drawn cross. Care dots/rings are primitives. Four runtime asset files total **18,477 bytes**; no research catalog/full atlas, archives, legacy runtime or source maps ship. The ignored stale catalog remains untouched. Workflow/script evidence destinations and presentation fixtures were updated only to support this phase.

## Tests, browser, saves, and performance

Executed from `web/`, with `export ROAG_EVIDENCE_DIR=../artifacts/colony-web/P04`:

```sh
fnm exec --using=24.21.0 npm run assets:check
fnm exec --using=24.21.0 npm run typecheck
fnm exec --using=24.21.0 npm test
fnm exec --using=24.21.0 npm run test:scenarios
fnm exec --using=24.21.0 npm run build
fnm exec --using=24.21.0 npm run test:browser
```

All exit 0. Final logs: `assets-acceptance.log`, `typecheck-acceptance.log`, `local-acceptance-02.log` (**55 pass**, 11.50 seconds reported by runner), `scenarios-acceptance-02.log`, `build-acceptance-02.log` (also repeats typecheck), `browser-acceptance.log` (**24 pass**, 533.07-second browser duration). Browser command builds both explicit base paths. Exact argv/cwd/environment/exits/durations are in `validation.json`. Node 24.21.0/npm 11.19.0, TypeScript 5.9.3, Phaser 3.90.0, Vite 8.3.6 and Playwright 1.65.0 were verified; dependency pins/lockfile are unchanged. `npm ci`, the .NET suite and full research-catalog suite were **not rerun in P04**.

Linux Chromium **158.0.8127.18** tested 1440×1000, 960×720 and 390×844. Production URLs were local `http://127.0.0.1:4173/` and `http://127.0.0.1:4174/roag/`, not deployed sites. No unexpected console/page/request/HTTP errors occurred. Deliberately missing assets produced the expected safe error UI.

The ordinary UI-built saves occurred at root tick 3,048 and roag tick 3,020, each with six meals eaten and all six healthy/rested. Actual page reload preserved the whole save. Continued worlds match pure simulation after 107/105 ticks respectively: root `ace150dd28c7de633160eaa8c07cc3da6e5f80bbad29e76306329125c7ff7d38`, roag `15774cbcaa876fa0d9a0312a19aedb72ed78a7d26c81858c067840bae2d4ca8e`. Invalid version/bed reference/negative need imports preserve world and slot. JSON round-trip also matches. Browser interaction timings differ naturally; each actual command stream is checked against its own exact continuation.

The focused browser care journey imports that same ordinarily built colony into a separate lab slot, changing **only its scenario flag**, then issues visible injury controls. No stock/building is injected. Both bases prove two-dose rescue/treatment, all-downed nonterminal reload, all-dead terminal reload and a fresh six-founder new run. Native visibility uses a separate Chromium process, CDP and actual tab focus transitions: hidden tick 6 stays fixed; manual-pause transitions retain root tick 10/roag tick 12. No synthetic visibility override is substituted.

`browser-summary.json` extracts all attachments and command/continuation/lifecycle evidence. There are **54 browser screenshots**, plus six curated element captures. `visual-review.json` lists the **11 actually opened and inspected** images with hashes and observations: ordinary colony, missing medicine inputs, downed deadline, successful care, terminal state, both maps and both narrow needs/inspectors. Narrow controls wrap, needs remain readable, and distinct farm/bed/clinic visuals preserve sprite crispness. DOM measurements confirm inspector text fits. Capturing other screenshots is not claimed as visual inspection.

Root production totals are **1,315,361 raw / 353,903 gzip / 285,921 Brotli bytes**; roag **1,315,386 / 353,911 / 285,940**. These sum independent file compression, not measured HTTP transfer. `sustainable-colony.json` is **48,101 bytes**. Six actors and bounded production reserves were exercised over three days; no p95/max frame-time, 24-worker throughput or indefinite job/history growth benchmark was measured.

## Resume and recommendation

Start locally:

```sh
cd web
fnm exec --using=24.21.0 npm run dev
# http://127.0.0.1:5173/
```

For a fresh checkout only, first run `fnm exec --using=24.21.0 npm ci` and install Chromium as documented in `web/README.md`; these are setup instructions, not additional executed P04 checks. For reviewed production outputs, set the evidence environment above and run `fnm exec --using=24.21.0 node scripts/serve-production.mjs root` or `roag` in separate terminals.

Seed 42 starts paused. Place stockpiles at 63/62/61/60,66; gather wood #11 and stone #15. Farm 68,64 → Plant/tend/harvest; kitchen 70,64 → Produce meal; beds 66/68/70,68; infirmary 72,68; workshop 74,68. Resume at 4×, pausing for orders. Medicine before ore visibly waits; workshop Produce ore resolves it. Set Ada cook 3 and Ivo healer 3. Save, reload and export/import. Full steps and costs are in `web/README.md`; exact automated command logs are in scenario/browser receipts.

No known failing P04 acceptance gate or required repair remains. Limits: six founders/one-tile buildings; only seed 42 demonstrated for three-day survival balance; finite wood/stone need further designation; single-patient clinics; field stabilization followed by walking, not body carrying; tools have no equipment benefit. Closed/no-passing paths may need player intervention. No prior-schema migration, autosave or offline simulation. Firefox/Safari, touch work journeys and large-colony performance remain untested. Combat, towers, raids, bosses, recruitment and broader seasonal/social systems are absent.

Recommend review, then separately authorize P05 (first defense loop) with its plan gates; no P05 work was started. Fresh sessions should read PLAN, DECISIONS, STATUS, PORT_MAP, this report and `web/README.md`, then inspect the exact current source/evidence. No user decision blocks P04.

## Paste-back block

```text
ROAG MIGRATION REPORT
Phase: P04
Status: COMPLETE
Source: Tested HEAD 67509c0602452a486b8e0108823d2b0a31281765 plus uncommitted changes; artifacts/colony-web/P04/tested-source.json identifies 53 tested files. Incremental changes: P04.patch; remaining HEAD diff: full-worktree.patch. Starting HEAD and existing work are recorded in before-source.json.
Report: docs/colony-web/reports/P04-report.md
Player-visible result: Six founders physically gather, farm, cook, eat, rest, make medicine and rescue/treat others. Build farms, kitchens, beds, infirmaries and workshops; set five duty priorities; inspect needs, health, claims and blocked reasons. Save/reload and JSON import/export preserve production, care and interrupted work exactly.
Acceptance: All P04 gates pass. Seed 42 survives 10,800 ticks (three days) without injected stock or disabled needs: six founders at full health, 40 farm food, 56 meals produced/30 eaten, five meals and four rests per founder. Starvation and fed overwork cause damage; restored supplies/beds/medicine enable recovery. Six haul/build × eat/rest/treatment save interruptions match uninterrupted execution after 650 ticks. Accounting covers cancellation, scarce claims, full storage, blocked facilities and worker death. Real rescue consumes two medicine doses across field stabilization and clinic treatment. All-downed remains nonterminal; actual all-dead defeat survives reload.
Checks executed: From web/, export ROAG_EVIDENCE_DIR=../artifacts/colony-web/P04, then run each with prefix “fnm exec --using=24.21.0”: npm run assets:check; npm run typecheck; npm test; npm run test:scenarios; npm run build; npm run test:browser. All exit 0. 55 local tests and 24 production browser journeys pass at / and /roag/. Retained checks include 103 seeds/three fallbacks, RNG/replay, P03 work conservation, actual reload/JSON, safe invalid imports, quota/unavailable storage, native hidden-tab transitions, camera/selection/resize, provenance/credits and missing art. No unexpected browser errors. Eleven screenshots were inspected; logs, 54 browser captures and exact hashes are under P04. Three-day save plus 500 ticks matches cb646439520310cb17d14fcaa1465da588a6b517e1e14da28dbc9a7f7828a94a.
Preservation: python3 -B tools/verify_preservation.py passes before/after for all 1,163 originals. Earlier reports/evidence, manifest, archives and legacy sources remain unchanged. Only four compact runtime asset files ship.
Ported/changed rules: Physical arrival, finite inputs, interrupted jobs and one-time cleanup retained. New deterministic farming, exclusive care claims, 3,600-tick days and 900-tick downing deadlines replace town/party/personal-turn assumptions. Save schema 3 safely rejects older schemas.
Known gaps: Three-day balance demonstrated on seed 42; finite resources require further designation. Tools are stored products without bonuses. No old-save migration/autosave/offline simulation, Firefox/Safari or large-colony performance claim.
Run/reproduce: cd web; fnm exec --using=24.21.0 npm run dev; open http://127.0.0.1:5173/. Follow web/README.md seed-42 coordinates: stockpiles and sources #11/#15, farm, kitchen, three beds, infirmary and ore workshop. Pause for orders; resume at 4×. Save/reload and export/import.
Recommendation: Review P04, then separately authorize P05. No P05 implementation, legacy deletion, push or deployment occurred.
Questions: none
END ROAG MIGRATION REPORT
```
