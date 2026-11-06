# P03 — Physical colony work, hauling and construction

## Identity and outcome

- Requested goal: autonomous finite gathering, physical hauling/storage, worker-built walls,
  doors/storage/workshop, repair/dismantling, job controls, conservation and exact saved work
  continuation. P02 was accepted before this goal. **Result: COMPLETE.**
- Starting and ending HEAD: `88026d076ea2940d244d1f0e38e4644f9dd2ceb1`.
  The reviewed implementation was already uncommitted: 43 source files were snapshotted
  under `artifacts/colony-web/P03/before-source/`, with hashes in `before-source.json`.
  No checkout/reset, commit, push or deployment occurred.
- Ending identity: `artifacts/colony-web/P03/final-source.json` lists every uncommitted
  source file/hash and the P03 changed-file list. `P03.patch` is the incremental change
  against that snapshot; `full-worktree.patch` includes all pre-existing uncommitted work.
  The tested 46-file web/workflow source set has SHA-256
  `84056e9916f9f901790ba977a8c714011c9085c9742e7181d1846413c22f9d5a`
  (`tested-source.json`). Final integrity verification checks those exact files again.
- Players can designate sources, establish an empty stockpile, place a blueprint and
  watch workers claim, approach, extract, carry, deliver and build. The board shows
  actual/named workers, priorities, costs, claims, site supplies, progress and waiting
  reasons. It supports suspension/resumption, reassignment, cancellation, repair,
  dismantling and door controls. Inspector cargo/life/job fields and map marks expose work.
- Hunger, farming, medicine, production recipes, combat, towers and raids remain unavailable.
  A workshop is a physical completed structure, without P04 production behavior.

## Acceptance evidence

All evidence below is under `artifacts/colony-web/P03/`. Final commands were run from
`web/` with `ROAG_EVIDENCE_DIR=../artifacts/colony-web/P03` and the pinned toolchain.

| Gate | Result | Actual scenario / command, exit | Evidence and observed result |
| --- | --- | --- | --- |
| Conservation and exactly-once work | PASS | `fnm exec --using=24.21.0 npm test`, 0; `npm run test:scenarios` through the same fnm invocation, 0 | `local-acceptance.log`: 30 passing tests. `work-scenarios.json`: seed 42, 17 exact due-tick commands, 19 checkpoints. Per-tick assertions cover extraction, physical storage/cargo, construction, repair, cancellation, dismantling, death and reload. Reservations are excluded from ownership totals. |
| Full storage, competing claims, missing inputs, changed source/storage, failed routes, suspend/handoff/death, repeated cancel | PASS | `tests/work.test.mjs` scenarios, 0 | No over-capacity storage/cargo, overlapping occupancy, negative stock, duplicate completion or unaccounted output. Source removal is separate from produced material. Storage teardown releases claims/delivery references. Occupied stockpiles clear by physical sidesteps. |
| Pickup/haul/construction continuation | PASS | Seed 42 saves at ticks 550, 568, 606; each continued 300 ticks, 0 | `save-pickup.json`, `save-haul.json`, `save-construction.json`; equal full-world hashes and material totals in `work-scenarios.json`. Pending commands, worker intent/progress, reservations and RNG are included. |
| Determinism and retained generation/routing | PASS | Both scenario runners, 0 | `scenarios.json`: 103 normal seeds, three forced fallbacks, retained 768 RNG draws/nine UTF-8 labels, six P02 replay checkpoints. `work-scenarios.json`: ten work/RNG/material checkpoints match across `[10,17,33]` and `[250,120,7,850]` ms frame schedules. Obstruction/recovery, traffic, invalidation and no-teleport assertions pass. |
| Ordinary production work journey at both mounts | PASS | `fnm exec --using=24.21.0 npm run test:browser`, 0 | `browser-acceptance.log`, `browser-results.json`: 22 passed, zero skipped/flaky/unexpected. Both `/` and `/roag/` use UI designation/stockpile/blueprint commands, real generated sources and worker-built walls. No inventory or finished structures are injected. |
| Cancellation, repair and dismantling visible | PASS | `tests/browser/work.spec.ts`, root + roag, 0 | Site suspension/cancellation leaves recoverable wood; workers recover it. A lab damage command changes only condition to 50%; real collected stone and worker time repair to 100%. Wall dismantling yields salvage; a second teardown makes an occupying Ivo step out of a stockpile and retains its contents locally. |
| Reload, portable JSON and safe rejection | PASS | Root + roag work and retained world journeys, 0 | Actual reload mid-cargo exactly restores the complete save. Continued browser worlds match pure uninterrupted stepping. JSON round-trip and subsequent reload match; corrupt/version/reference/forged-cargo imports preserve current state and valid slots. Quota/unavailable-storage journeys retain working export. |
| Camera, selection, resize, art/provenance/credits, missing art, observed visibility | PASS | Retained browser journeys on both mounts, 0 | Camera pan/zoom/anchor, unchanged-view resize limits, responsive selection, provenance and credits retained. Missing-art failures are visible/safe. Separate Chromium/CDP native tab transitions freeze ticks with no catch-up and preserve manual pause; no synthetic visibility event/property override. |
| Asset/build/preservation boundary | PASS | Asset check, typecheck, three production builds and preservation, all 0 | Four selected runtime asset files only; 27 unchanged bindings, 18,477 bytes. No research catalog/atlas, archives, legacy runtime or source maps ship. Preservation checks cover all 1,163 protected originals before and after. Old reports, 977 earlier evidence files, manifest and archives match hashes. |

### Exact continuation evidence

| Saved phase | Saved tick | Advance | Both uninterrupted/restored SHA-256 |
| --- | ---: | ---: | --- |
| Reserved pickup | 550 | 300 | `0881ba3d9de71e8b69ffcfc0cb5d5364fe056b783a3106ccc8f4ae4ec1727a5e` |
| Cargo in flight | 568 | 300 | `db68abcae7cb88cca8455377ab27de538f7a303c821b99e323c927b15a3fb464` |
| Construction in progress | 606 | 300 | `fa54dd14774c705015c0cc61329603cea635bddae2951589b8f544b78323003d` |

The browser root journey saved/reloaded tick **111**, then matched uninterrupted
execution at tick **365** (254 more ticks), SHA-256
`59b5d499c3db769b20356b930d4fc2d8e2f38c331c07d63d2d0aed46e45aba6b`.
The subpath journey saved/reloaded tick **129**, then matched tick **378** (249 ticks),
SHA-256 `08060f703e969175ec9f60ab074c9a06cd1fbde36fa10c99c8aa500873208b21`.
These are separate real-time UI runs, not an assertion that different UI click times
produce identical worlds. `browser-summary.json` includes all seven work checkpoints
per mount, quantities, hashes, browser version, observed native visibility and errors.

## Port and design decisions

| Legacy reference read | Retained invariant | Intentional colony adaptation | Verification |
| --- | --- | --- | --- |
| `WorldResources.cs`, `WorldWorkOutputs.cs`; `WorkOutputTests.cs` | Finite source, extract once, actual cargo, capacity-limited delivery; changed source cannot repeat extraction | Source quantities are material units; batches up to five require eight ticks. No regrowth, shared pack, rally radius or passive stock increase | Extraction/storage/full-capacity/changed-source tests; both production journeys |
| `WorldWorkSupplies.cs`; `WorkSupplyTests.cs` | Explicit supply ownership, physical pickup/delivery, no double claim; local recoverable cancellation and handoff | Reservations stay in actual stores/bundles until pickup. Suspension/reassignment/death drops cargo at the last committed tile for automatic recovery rather than remote party transfer | Conservation each tick, reservation/forgery rejection, physical handoff/death, three save phases |
| `WorldWork.cs`, `WorldDesignations.cs`, `WorldNativeWork.cs`; `NativeWorkTests.cs`, `WorldDesignationTests.cs` | Timed work, one worker/job, validated target, suspend/cancel/repair, exactly-once completion and safe invalid orders | Six autonomous workers; fixed integer ticks replace paid tactical turns/readiness, party sight/rally and pack assumptions. All founders currently qualify for every P03 task | Priority/assignment UI, route/access/cancel tests; worker-built browser wall |
| `content/recipe/wall.json` (`recipe.wall`), door/storage/camp recipes | Data-defined costs/work and embodied structure cost | Locally authored colony values below replace legacy quantities/times. Workshop is a new colony structure role, not ported camp crafting | Validated `work-content.ts`, complete all four types headlessly; UI type/cost controls |
| Work supply/output validation and existing world persistence | Save complete ownership/progress and reject invalid references without replacing valid state | Schema **2**, content **colony-p03-1**; P02 schema 1 is safely rejected, not migrated. Combined live/future structure budget is checked on import | Strict malformed work/claims/cargo/cost/ID/version tests; production invalid-import + reload |

All pure simulation modules remain independent of Phaser, DOM, wall-clock time and
legacy runtime. Stable numeric IDs and `(due, sequence)` commands are retained;
work dispatch uses descending priority then job ID and stable founder order. One
worker performs at most one job per tick. No new RNG consumption/stream changes.

Colony content is validated at import and tested for invalid numeric values:

| Type | Cost | Work ticks | Storage capacity |
| --- | --- | ---: | ---: |
| Stockpile ground zone | none; designation only | immediate designation | 30 |
| Wall | 3 stone | 16 | 0 |
| Door | 3 wood + 1 stone | 16 | 0 |
| Built storage | 4 wood + 2 stone | 24 | 60 |
| Workshop | 6 wood + 4 stone | 30 | 0 |

Repair costs one stone for a wall, one wood otherwise, with 12 work ticks; inputs
become spent. Dismantling takes 12 ticks, returns all stored contents and 75% of each
embodied material rounded down; the remainder becomes spent (legacy owned structures
returned full cost). Cargo capacity is five. No starting inventory is supplied.
These values establish observed behavior, not universal balance/fun.

Conservation is checked independently of claims:

```text
sum(source.initial - source.remaining) - sourceRemoved
  = storage + ground bundles + carried + site + embodied + spent + explicitly lost
reservations are a subset of existing storage/bundles, never an extra category
```

Missing materials release workers and retry after ten ticks. Full storage retains
cargo with a reason. Cancel/suspend/reassign release claims correctly; site progress
and delivered supplies survive suspension, while cancellation produces local bundles.
Dead/downed workers release work/cargo for recovery; actual death alone counts toward
the retained heart-destroyed OR all-dead rule. No new needs/combat lifecycle was added.

Blueprints block their tile immediately; placement rejects the heart, occupied or
reserved movement cells, live sources, bundles, structures and conflicting sites.
Exhausted source plots become buildable while retaining depletion records. Deliberate
enclosures are legal. Doors start open; closed doors block every worker and cannot
close on occupied/reserved cells. Construction, teardown, exhaustion and door changes
invalidate routes. Dismantling a passable occupied zone asks the occupant to sidestep;
work waits for the physical exit. Reserved movement edges always finish before replan.

There are at most two 4,096-expansion route searches per tick, plus bounded 256-expansion
local traffic probes; adjacent yielding uses real reserved movement. Failed searches
back off to 100 ticks, with immediate invalidation after navigation changes. Valid
interaction goals stay stable during an in-flight edge. Active jobs cap at 128;
completed/cancelled history is pruned within a 256-job bound; structures plus active
blueprints cap at 256. Stable IDs are never reused. Commands targeting removed
structures/pruned job history are retired deterministically because they cannot
become valid again. `DECISIONS.md` was not changed; 128×128, six founders and 10 Hz remain.

## Preservation and scope

- `python3 -B tools/verify_preservation.py`: PASS before and after, **1,163 files**, zero
  failures. The final repeat/receipt is `preservation-after.log` / `validation.json`.
- `integrity-after.json`: verifies **977** earlier P01/P01-repair/P02 evidence files,
  all three preceding reports, the unchanged preservation manifest, both archive
  sizes/hashes and unchanged legacy source paths. Archives remain local, not uploaded.
- No protected originals, legacy sources, original artwork/content, archives or prior
  reports/evidence were edited/deleted. The original 43-file worktree snapshot is retained.
- No new art bindings: wall=`rock` / `urizen-onebit-r02-c000`, door=`cottage` /
  `urizen-onebit-r01-c030`, storage=`wood` / `urizen-onebit-r23-c000`, workshop=`hearth` /
  `urizen-onebit-r42-c002` reuse existing derived crops as local roles. Neutral original
  names, IDs, crop coordinates/hashes and source provenance remain intact. Blueprint,
  zone, cargo and bundle marks are drawn primitives; no animation is inferred.
- Changes outside `web/`: only scoped web CI evidence destination and these phase
  report/status records. Existing .NET CI is unchanged. No P04 work, push or deployment.

## Tests, browser, saves and performance

Pinned installed toolchain: Node **24.21.0**, npm **11.19.0**, TypeScript **5.9.3**,
Phaser **3.90.0**, Vite **8.3.6**, Playwright **1.65.0**. Dependencies/lockfile were
unchanged; `npm ci` was not rerun in P03. No new legacy .NET/catalog-suite run is claimed.
Final commands, each exit **0**, from `web/`:

```sh
export ROAG_EVIDENCE_DIR=../artifacts/colony-web/P03
fnm exec --using=24.21.0 npm run assets:check
fnm exec --using=24.21.0 npm run typecheck
fnm exec --using=24.21.0 npm test
fnm exec --using=24.21.0 npm run test:scenarios
fnm exec --using=24.21.0 npm run build
fnm exec --using=24.21.0 npm run test:browser
```

`validation.json` records exact argv/cwd/environment/exits/durations. Final logs use
`*-acceptance.log`; the asset check is also recorded separately. The browser command
regenerates assets, typechecks, builds explicit `/` and `/roag/` distributions, checks
both distributions and runs the suite. Default relative-base `npm run build` also passes.

| Production output | Raw bytes | gzip level 9 | Brotli quality 11 |
| --- | ---: | ---: | ---: |
| Relative default | 1,292,743 | 347,420 | 280,445 |
| `/` | 1,292,738 | 347,420 | 280,420 |
| `/roag/` | 1,292,763 | 347,428 | 280,462 |

Sizes sum individually compressed files; actual hosting transfer encoding was not
measured. `root-sizes.json` / `roag-sizes.json` include every shipped file/hash. Runtime
art is still 18,477 bytes, including the unchanged 3,288-byte PNG.

Final browser: Linux Chromium **158.0.8127.18**, two local strict production mounts,
22 passed in **164.846 s**, no skips/flakiness/unexpected errors. Desktop is 1440×1000;
retained layout checks include 960×720 and 390×844, plus large/unchanged-view zoom
resize checks. Missing-asset request failures are intentional test inputs; ordinary
journeys have zero unexpected console/page/network errors. Node's `NO_COLOR` /
`FORCE_COLOR` harness notices are not application console failures.

There are **42 final screenshots**, JSON exports and detailed reporter attachments.
Six representative final screenshots were opened and inspected; exact paths, hashes
and observations are in `visual-review.json`: root extraction/cargo, root 390px layout,
root cancellation bundle, subpath finished wall, subpath repaired wall and subpath
occupied-stockpile removal. Pixel artwork, cargo/bundle marks, inspector/job details,
costs and waiting states are readable, without layout overlap. Browser source-pixel
terrain assertions and provenance/credits checks also pass. Mid-cargo exports are
37,461 bytes (root) and 37,912 bytes (subpath).

Capacity/FPS/p95 benchmarking was not performed. This is active six-worker browser
behavior and bounded deterministic work, not a 24-colonist throughput claim. Firefox,
Safari, touch work journeys, deployment and long-run survival balance were not tested.

### Failed attempts retained and resolved

- Initial local cases still treated version 2 as unsupported, chose an occupied build
  tile, or assumed a dropped bundle could not already be physically picked up. Assertions
  were corrected to the new schema/legal placement and conserved bundle-or-cargo ownership.
  Two early scenario attempts chose a random solid tile at 76,68; actual placement rejection
  was correct, and the scenario was moved to clear ground. Logs remain in this directory.
- `browser-attempt-01.log`: **20 passed / 2 failed**. Both new journeys exposed an actual
  dismantle approach oscillation. Nearest-neighbor retargeting during a reserved edge
  kept a worker moving between two cells. Keeping a valid approach stable and adding a
  focused obstruction/approach regression fixed it. Failed traces/screenshots/builds
  and source hashes are retained under `attempt-01-*` / `browser-attempt-01-source.json`.
- `footprint-regression-before-fix.log`: reproduced passable-stockpile self/idle-occupant
  deadlock (exit 1). Physical bounded yielding fixes it, with headless and ordinary UI
  coverage at both mounts. Later checks also close combined future structure budgets and
  permit exhausted plots without losing depletion accounting.
- Intermediate passing browser runs/builds/source sets remain as `attempt-02-*`,
  `attempt-03-*`, `attempt-04-*`; final source was rechecked with all 30 local and 22
  browser tests. No failed evidence or prior phase evidence was removed to obtain PASS.

## Resume and recommendation

From the repository root:

```sh
cd web
export ROAG_EVIDENCE_DIR=../artifacts/colony-web/P03
fnm exec --using=24.21.0 npm run dev
```

Open `http://127.0.0.1:5173/`. At seed 42, below the map place a stockpile at **63,66**,
designate wood **#11 (45,56)** and stone **#15 (56,38)**, place a wall at **72,64**, then
choose **4× / Resume**. Find Ada to inspect cargo and watch delivery/construction.
Use board priorities/assignments, suspend/resume/cancel, Save/reload and Export/Import.
For repair, open `?scenario=work-lab` (separate slot, ordinary generated resources,
no supplied stock/structures), build the wall normally, use its **Lab: damage to 50%**
button, then **Repair** and **Dismantle**. `?scenario=routing-lab` remains a separate
terrain-edit fixture; it is not browser economy evidence.

To inspect the exact production mounts after the suite, from `web/` in separate terminals:

```sh
export ROAG_EVIDENCE_DIR=../artifacts/colony-web/P03
fnm exec --using=24.21.0 node scripts/serve-production.mjs root
# http://127.0.0.1:4173/
```

```sh
export ROAG_EVIDENCE_DIR=../artifacts/colony-web/P03
fnm exec --using=24.21.0 node scripts/serve-production.mjs roag
# http://127.0.0.1:4174/roag/
```

From the root, `python3 artifacts/colony-web/P03/verify-evidence.py` verifies the
recorded source, old evidence/reports, archive/manifest integrity, originals and final
source/diff receipts. It does not reset/rebaseline preservation data.

No known required P03 repair remains in the verified journeys. Limitations: one-tile
buildings, six current founders, no P02 save migration, and closed enclosures/narrow
corridors without a passing space can still require opening access or redirecting a
worker; their work remains visible/cancellable. Schema 1 restore/import rejection
leaves the old slot intact. A fresh run explicitly replaces its slot. No autosave.

Recommendation: review P03, then authorize P04 needs/production as a separate goal.
Keep conservation and saved interruption/ownership tests when adding needs. No user
decision blocks this phase. A fresh session should read PLAN, DECISIONS, STATUS,
PRESERVATION, PORT_MAP, this report and `web/README.md`. P04 has not started.

## Paste-back block

```text
ROAG MIGRATION REPORT
Phase: P03
Status: COMPLETE
Source: HEAD 88026d076ea2940d244d1f0e38e4644f9dd2ceb1 plus uncommitted artifacts/colony-web/P03/P03.patch. Full worktree and file hashes are retained separately; tested source fingerprint: 84056e9916f9f901790ba977a8c714011c9085c9742e7181d1846413c22f9d5a.
Report: docs/colony-web/reports/P03-report.md
Player-visible result: Designate finite sources, establish stockpiles, place blueprints and watch autonomous workers extract, carry, deliver and build. Walls, doors, storage and workshops have validated costs/work times. The job board exposes priorities, assignments, material claims, progress and waits; cancellation, suspension, reassignment, repair and dismantling preserve ownership. Cargo and recoverable bundles are visible.
Acceptance: Conservation passes through extraction, storage, transport, construction, repair, cancellation, dismantling, death and reload. Full storage, competing claims, missing materials, changed targets, blocked paths, handoffs and repeated cancellation are covered. Three saves at pickup/haul/construction match uninterrupted execution after 300 ticks. Ten work replay checkpoints match across rendering cadences; retained RNG vectors and 103 seeds plus three fallbacks pass.
At both / and /roag/, ordinary production UI actions gathered actual materials and worker-built a wall, reloaded mid-cargo, cancelled a supplied site, repaired with collected stone and dismantled for salvage. Occupied stockpile removal makes the occupant step aside and retains contents. No inventory or finished buildings were injected. All four building types completed in headless work scenarios. Native tab visibility, camera/selection/resize, credits/provenance, missing-art and storage-failure journeys remain passing. Six representative final screenshots were opened and inspected.
Checks executed: From web/, set ROAG_EVIDENCE_DIR=../artifacts/colony-web/P03 and prefix npm commands with fnm exec --using=24.21.0. assets:check, typecheck, test, test:scenarios, build and test:browser all exited 0. Final results: 30 local tests, 22 browser checks, zero skipped/flaky/unexpected. Exact commands/exits, checkpoint hashes, 42 screenshots, exports, previous failed traces and builds are under that evidence directory. The initial approach-oscillation failure and stockpile occupancy regression were fixed and rerun.
Preservation: Before/after verification passes all 1,163 originals. Manifest, archives, legacy sources, three preceding reports and 977 earlier evidence files remain unchanged. The same 27 sprite crops ship; research assets are excluded.
Ported/changed rules: Physical supply/output ownership and exactly-once work are retained. Autonomous integer ticks replace party readiness/shared-pack access. Cargo capacity is five; zones/store buildings hold 30/60; salvage is 75%, rounded down, with remainder recorded as spent. Schema 2 safely rejects P02 saves without replacing valid state/slots.
Known gaps: Six-worker scope, one-tile structures, no P02 migration; sealed access may need player intervention. Firefox/Safari, touch work journeys and large-colony performance were not measured. Needs, production recipes, combat and raids are absent.
Run/reproduce: cd web; export ROAG_EVIDENCE_DIR=../artifacts/colony-web/P03; fnm exec --using=24.21.0 npm run dev. Open http://127.0.0.1:5173/. Seed 42: stockpile 63,66; designate sources 11 and 15; wall 72,64; 4× then Resume. Use ?scenario=work-lab for damage-only repair checks after ordinary construction.
Recommendation: Review P03, then authorize P04 separately. No next phase, push or deployment was started.
Questions: none
END ROAG MIGRATION REPORT
```
