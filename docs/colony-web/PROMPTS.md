# Copyable goal prompts and the implementation loop

P00 (preservation/planning) is handled in this preparation round. Begin with P01.
Run one phase at a time in the repository. The [plan](PLAN.md), [decisions](DECISIONS.md),
[port map](PORT_MAP.md), [status](STATUS.md), and [report template](REPORT_TEMPLATE.md)
are durable context for the implementation instance.

The official [Codex Goals guide](https://developers.openai.com/cookbook/examples/codex/using_goals_in_codex)
documents `/goal <objective>` and goal lifecycle commands. If the implementation
client lacks `/goal`, paste the same objective as an ordinary implementation request;
do not claim a persistent goal was created. Do not resume an old RPG migration goal.
If an unrelated goal is still active, the user can inspect it with `/goal` and clear
it with `/goal clear` before starting this new direction.

The preparation session does not start any of these implementation goals on the
user's behalf. No token budget is implied by these prompts.

## Common contract referenced by every prompt

1. Read the active plan/decisions/status, latest phase report, and relevant source/tests
   in PORT_MAP.md. Inspect existing changes and preserve unrelated work.
2. Deliver the requested phase's player-visible outcome and all its acceptance gates.
   Make necessary supporting changes and repairs within that scope. Do not expand into
   later phases or the previous C# migration's unfinished scope.
3. Work under `web/`; use pure deterministic TypeScript simulation and Phaser presentation.
   Consult current docs for installed package versions. Keep the old game as reference
   until P09; no runtime dependence on it. No new Next.js application is needed.
4. Run preservation verification before and after changes. Do not edit/delete files in
   preservation-manifest.json or reset that manifest to conceal differences. Put new
   definitions/bindings in the web project. Archive artifacts are local; never imply
   they have been uploaded or are present in a different clone.
5. Make routine engineering choices autonomously; record material assumptions. Ask
   concise questions only when a missing product decision/access detail prevents the
   dependent work. Keep working on independent parts. Follow current user instructions
   over historical project records.
6. Test meaningful behavior, verify rendered changes with real browser interaction,
   and report scope honestly. A skipped required gate prevents phase completion.
7. Update STATUS.md and write the requested report using REPORT_TEMPLATE.md. Finish
   with its paste-back block and exact local run instructions. Stop at this phase's
   boundary. Never mark the whole migration complete from one milestone.
8. No push, publication, paid services, or changes to an existing production destination
   are implied by P01-P08. P09 prepares the release and uses the explicitly selected
   hosting destination. If absent, request that detail after preparing the build.

## P01 — Browser foundation

```text
/goal Implement P01 of docs/colony-web/PLAN.md in this repository: a working TypeScript + Phaser + Vite browser foundation using the preserved sprite artwork. Follow the Common contract in docs/colony-web/PROMPTS.md and read DECISIONS.md, STATUS.md, PRESERVATION.md and PORT_MAP.md first.

Create the web/ project, a compact deterministic runtime asset exporter with original ID/crop provenance, and a rendered colony scene with camera pan/zoom, selection and a readable inspector. Pin compatible dependencies and add meaningful type/build/asset/browser checks. Verify the production build at both / and /roag/, inspect screenshots, and ensure the full research catalog/atlas is excluded from shipped assets. Do not implement later gameplay phases or delete legacy sources.

Continue through necessary fixes until all P01 acceptance gates pass. Write docs/colony-web/reports/P01-report.md using REPORT_TEMPLATE.md, update STATUS.md, and return its paste-back summary with exact run commands, actual test evidence and limitations. Finish at P01; do not automatically start P02.
```

## P02 — Simulation, world, navigation, and saves

```text
/goal Implement P02 of docs/colony-web/PLAN.md: a deterministic fixed-tick colony world with seeded terrain, inhabitants, the colony heart, routing, pause/speed controls and exact browser save/resume. Follow the Common contract in docs/colony-web/PROMPTS.md. Review the completed P01 report and current tree before changes.

Use pure TypeScript simulation with ordered commands, independent seeded RNG, stable IDs and validated save data. Add connected surface generation, occupancy/navigation with bounded work and route invalidation, IndexedDB plus JSON export/import, and pause-on-hidden-tab without catch-up. Prove deterministic replay independent of render cadence, mid-movement save continuation, safe corrupt-save rejection, and actual page-reload persistence. Satisfy every P02 acceptance gate without starting construction/economy scope.

Write docs/colony-web/reports/P02-report.md using REPORT_TEMPLATE.md, update STATUS.md and return the paste-back summary, runnable commands and evidence. Stop after P02.
```

## P03 — Work, hauling, construction

```text
/goal Implement P03 of docs/colony-web/PLAN.md: autonomous resource gathering, physical hauling, storage, construction and repair through a usable colony job interface. Follow the Common contract in docs/colony-web/PROMPTS.md and review the preceding accepted report/current tree.

Use the C# work/supply/output sources and tests in PORT_MAP.md as behavioral references. Implement explicit claims/reservations/cargo, job priorities, cancellation/refunds, full-storage and blocked-route handling, worker handoffs/death, and exact saved work continuation. A real browser journey must collect materials and complete a building through worker actions. Prove resource conservation in all relevant transitions; no inventory duplication or synthetic construction shortcuts count as acceptance.

Complete all P03 gates, write docs/colony-web/reports/P03-report.md using REPORT_TEMPLATE.md, update STATUS.md and return its paste-back summary and evidence. Stop after P03.
```

## P04 — Needs and production

```text
/goal Implement P04 of docs/colony-web/PLAN.md: a manageable colony with hunger, fatigue, treatment, rest, simple production, worker priorities, and downed/dead lifecycle. Follow the Common contract in docs/colony-web/PROMPTS.md and review the preceding report/current tree.

Port the useful resident/occupation rules from PORT_MAP.md while replacing party assumptions with autonomous colony work. Add a small farm/food chain, kitchen, beds, infirmary and practical duties. Needs must interrupt/resume existing jobs without losing reservations or cargo. Make missing food, access, workers and storage understandable in the UI. Demonstrate several sustainable days using ordinary commands, real rescue/medicine costs, all-dead semantics and exact save/reload of interrupted work.

Complete all P04 gates, write docs/colony-web/reports/P04-report.md using REPORT_TEMPLATE.md, update STATUS.md and return its paste-back summary. Stop after P04.
```

## P05 — First playable colony defense loop

```text
/goal Implement P05 of docs/colony-web/PLAN.md: the first complete playable gather-build-defend-recover loop. Follow the Common contract in docs/colony-web/PROMPTS.md and review the preceding report/current tree.

Add coherent tick-driven combat, walls/gates, an ammunition-fed tower, traps and guard posts, melee raiders and a breacher, a useful raid warning, and civilian shelter/defender orders. Enemies must attack sensible obstructions when the heart is enclosed. Defeat occurs when the colony heart is destroyed OR every colonist dies; temporary downing is not death. Persist terminal outcomes and allow an explicit fresh run.

Use real browser actions to build a colony and defenses from collected resources, survive a raid, rescue/treat damage and repair. Also demonstrate both defeat conditions, exact mid-wave reload, finite ammunition and shared damage/death accounting. Complete every P05 gate. Write docs/colony-web/reports/P05-report.md using REPORT_TEMPLATE.md, update STATUS.md and return its paste-back summary. Stop at this playable milestone before expanding content.
```

## P06 — Endless raids, squads, and first boss

```text
/goal Implement P06 of docs/colony-web/PLAN.md: escalating endless survival with announced raids, recovery windows, coordinated squads and an Ossuary Captain boss wave followed by continued play. Follow the Common contract in docs/colony-web/PROMPTS.md and review the preceding report/current tree.

Build a deterministic saved raid director with finite per-wave budgets, legal entries, active-enemy caps and retained delayed spawns. Port two contrasting enemy support squads and adapt the Captain's facing, charge and breakable barricades to the colony. Preserve readable counterplay and exact resource/death/reward accounting. Verify multiple ordinary waves, the boss and a later wave, plus save/resume in every director phase and bounded late-wave stress. Do not turn boss death into campaign victory.

Complete all P06 gates, write docs/colony-web/reports/P06-report.md using REPORT_TEMPLATE.md, update STATUS.md and return its paste-back summary. Stop after P06.
```

## P07 — Remaining bosses and environments

```text
/goal Implement P07 of docs/colony-web/PLAN.md: Bellkeeper, Sluice-Maw and Threadmother colony encounters plus a small biome/environment set that changes defensive choices. Follow the Common contract in docs/colony-web/PROMPTS.md and review the preceding report/current tree.

Read the preserved boss proposals, source implementations and tests from PORT_MAP.md. Retain sound/resonator counterplay, explicit channels/valves, and physical webs/pulls/fire while adapting to real-time colonists. Place required geometry safely without repainting player buildings, preserve escape/counter routes, and validate art against original catalog IDs. Use bounded hazard rules and continued survival after boss rewards.

Demonstrate all three bosses with real actions, counters, reload during their distinctive effects and post-boss colony continuation. Validate multiple seeds and selected biome resources. Complete every P07 gate; write docs/colony-web/reports/P07-report.md using REPORT_TEMPLATE.md, update STATUS.md and return its paste-back summary. Stop after P07.
```

## P08 — Living world and recovery economy

```text
/goal Implement P08 of docs/colony-web/PLAN.md: a bounded living-colony layer with traits/memories, actual-effect consequences, seasons/weather, finite forage/regrowth, limited wildlife and visiting trade/recruitment. Follow the Common contract in docs/colony-web/PROMPTS.md and review the preceding report/current tree.

Port selected C# mechanics from PORT_MAP.md without recreating the huge overworld or complex party politics. Climate must change actual work/rest/food behavior; shelter requires arrival. Memories must follow real aid/harm. Trade, ecological recovery and recruitment preserve quantities, identities and one-time rewards. Keep history and scheduling bounded. Demonstrate recovery after adverse events and exact saves, plus a documented longer colony run with food/population/history measurements.

Complete all P08 gates, write docs/colony-web/reports/P08-report.md using REPORT_TEMPLATE.md, update STATUS.md and return its paste-back summary. Stop after P08.
```

## P09 — Release and cutover

```text
/goal Complete P09 of docs/colony-web/PLAN.md: a verified static web release and retirement of the archived legacy runtime from the active checkout. Follow the Common contract in docs/colony-web/PROMPTS.md, read PRESERVATION.md, and review all previous acceptance reports/current tree.

Finish onboarding/readability/accessibility and browser-save failure handling. Run the complete ordinary browser journey, required regression checks, root/subpath production-build checks, and representative performance/save-growth measurements. Prepare GitHub Pages and document equivalent Vercel deployment. Use the user's selected authorized hosting destination; if project/account details are missing, finish the local release artifact first and ask only for the missing publication detail. Do not claim a deployment without checking the real URL.

Before deleting any old runtime/build files, verify the source tag/portable archives, the preservation manifest, and an explicit deletion list. Retain all protected assets/docs/content/catalog tools/tests and the new web app. Preserve unrelated changes. Re-run web and catalog checks after cleanup, document recovery and primary web commands, and record exact tested source identity and release artifact hashes.

Write docs/colony-web/reports/P09-report.md using REPORT_TEMPLATE.md, update STATUS.md and return its paste-back summary, launch/deployment instructions and remaining limitations. Mark release complete only when its required gates, including configured publication, actually pass.
```

## Review prompt after each implementation report

Paste this into this planning conversation, or into a planning turn of the same
implementation instance. When sharing a different instance, append its paste-back
block. If both can read the same checkout, a report path is sufficient.

```text
Review the latest Roag colony-web implementation report against docs/colony-web/PLAN.md, DECISIONS.md, PORT_MAP.md, STATUS.md and REPORT_TEMPLATE.md. Inspect the current repository and referenced evidence where accessible. Treat this as a review/planning turn, not authorization to start the next implementation phase.

Identify which acceptance gates actually passed, missing evidence, defects, changed assumptions and any preservation risk. Distinguish playable behavior from backend-only or fixture-only results. Do not accept a reported COMPLETE status without supporting evidence, and do not rerun unrelated expensive checks without reason.

Generate exactly one copyable /goal prompt: either the smallest repair goal needed for the current phase, or the next phase if the current gates are satisfied. Include concrete completion checks, preserved-file constraints, and the requirement to write a report and return the paste-back block. Ask only necessary product/access questions and continue independent review work. Do not execute the generated goal yet.

Implementation report or report path:
<paste the ROAG MIGRATION REPORT block or its repository path here>
```

## Repair prompt pattern

The reviewer should fill in actual findings; do not send placeholders to an implementer.

```text
/goal Repair PXX of the Roag colony-web migration: <specific failing behavior/gate and reproduction>. Follow docs/colony-web/PROMPTS.md's Common contract and preserve the already working phase scope. Read <prior report> and the relevant plan/port references.

Fix <concrete defects>, add or update meaningful regression coverage for those defects, and rerun <affected checks and actual browser journey>. Completion requires <observable outcomes and preservation/save invariants>. Do not advance to PYY or conceal missing checks by weakening assertions or updating expected output without a justified behavior change.

Write docs/colony-web/reports/PXX-repair-NN-report.md using REPORT_TEMPLATE.md, update STATUS.md to the evidenced result and return the paste-back summary. Keep earlier reports. Stop at the repaired phase boundary.
```
