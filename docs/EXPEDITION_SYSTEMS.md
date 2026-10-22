# Expedition systems expansion

Approved 2026-10-22 from the five-point attachment and the preceding recommendation. This is the implementation and completion checklist, not a claim that the features below already exist. Preserve mouse-first delegation, four generated companions, shared needs, turn-based party combat, open exploration, solid collisions, dungeon privacy and safe save rejection.

## Required outcomes

- [x] Actual harvestable fruit trees, fishing banks and mineral deposits, varied by region; visible depletion and explicit recovery rules; right-click and drag-selection work distributed among companions. Use the unused apple/cherry/meat/egg and tool artwork where applicable.
- [x] Digging uses work, fatigue and ordinary hunger instead of upfront food per tile. Tools visibly affect relevant work. Resource depletion, jobs and recovery continue exactly across saves and cache eviction.
- [x] Existing camps support assigned cooking, treatment and watch duty; meals prepare the party for specific threats; companions react to recent events or share actionable discoveries. Existing rest/crafting remains useful.
- [ ] Equipment gains bounded, readable effects: protection of wounded allies, improved burning attacks and faster excavation. Generated rewards combine authored, validated effects and offer meaningful choices.
- [ ] Rabbits flee, spiders defend nests, bees defend valuable gathering places and bats inhabit caves. Local collision, visibility, danger feedback and actual encounters respect these behaviours.
- [ ] Generated trapped storerooms, poisoned springs and guarded crossings offer appropriate disarm/repair/negotiate/fight/tunnel approaches. Reuse trap/lever/fountain art. All outcomes and rewards are persistent; no repeated reward exploitation or mandatory global unlocks.
- [ ] Cooperative abilities work between generated companions, with explicit participant/cost rules and the existing paused turn-based combat.
- [ ] Each new content family has stable IDs, strict validation/references, schema/scaffold/removal support, real runtime previews and authoring documentation. Review/dogfood tooling at each milestone.
- [ ] Reproducible seed/scenario checks, gameplay/save/collision regressions, native mouse acceptance and inspected screenshots cover the full expansion. Audit each requirement against actual evidence before completion.

## Implementation stages

1. Resource/tool content, deterministic world resources, work/depletion/regrowth and digging rebalance; mouse interaction and visual feedback; focused tests and developer preview.
2. Camp roles, recipes, food effects and party conversations; meaningful equipment effects and generated variants; combat and persistence checks.
3. Wildlife ecology and multi-approach discoveries; world generation audits and executable previews.
4. Cooperative abilities, integrated expedition acceptance, native playthroughs, documentation and final requirement audit.

Use cohesive atomic local commits throughout. New mechanics/content may require a new campaign because the game pins the gameplay content hash; old/incompatible saves must fail safely without overwrite. No migration or player-save modification is authorized or needed for verification. Do not replace source artwork or historical IDs.

## Evidence and remaining work

Planning checkpoint: current baseline `ff4489d`; 308 .NET tests and 56 native mouse checks were recorded for the prior UI milestone. Those results do not verify this expansion. All implementation requirements above remain pending.

Stage one evidence: 318 Release .NET tests passed; nine resource cases plus a multi-companion batch cover normal dispatch, exact saves, cache eviction, finite ore, read-only inspection, invalid content, cancellation and optional tool effects. Native acceptance passed 59 checks including real mouse gathering, depleted-stock loading, ingredient sprites and recovery feedback. Inspected the native full/depleted/pack captures and improved resource markers to retain tree/rock/water silhouettes; the revised marker capture is also inspected. `build_game_catalog.py --check`, resource previews for apple trees/fishing/deep iron, schema generation and tooling review passed (176 definitions, 17 content families). Later-stage checkboxes remain pending.

Camp milestone: 329 Release .NET tests passed, including concurrent cooking/treatment/watch with exact save continuation, rest while workers serve, armour/burn/bleed effects, cooldown-safe stories revealing real sites, malformed-save rejection and two-resolution mouse-control bounds. Native camp acceptance passed nine checks, with inspected duty/voiced-story screenshots. The resource and baseline portions of the earlier combined run passed before a camp verifier enum-format mistake; corrected that verifier and reran the camp scenario. Tooling now includes camp validation/references/scaffold/schema/runtime previews and selectable native scenarios (183 definitions, 18 families). Equipment effects and generated variants are the next part of stage two.
