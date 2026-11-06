# Roag colony web — P03

From the repository root:

```sh
cd web
fnm use 24.21.0
npm ci
npm run dev
```

Open http://127.0.0.1:5173/. Node **24.21.0**, npm **11.19.0**, TypeScript **5.9.3**,
Phaser **3.90.0**, Vite **8.3.6**, Playwright **1.65.0** are pinned. With fnm installed,
`fnm exec --using=24.21.0 npm run dev` also selects the exact toolchain.

## Play and reproduce physical work

Start paused at seed 42 with the heart and six founders. Expand **Work & materials**
below the map. Place a **Stockpile zone** at **63,66** (empty, capacity 30), designate
**source #11 wood at 45,56** and **#15 stone at 56,38**, then place a **Wall** at
**72,64**. Choose **4×** and **Resume**. Ada and Bram approach the sources, spend
work ticks, extract up to five units, carry them to storage and deliver only what
fits. A free builder claims existing supplies, physically collects them, delivers
them to the site and spends 16 ticks building. No shared/global stock or starting
inventory is provided. Find buttons center sources, sites or structures on the map.

The job board shows priorities, actual workers, requested assignments, costs,
material reservations, site stock, progress and waiting reasons. Suspend/resume,
reassign or cancel a job. Cargo becomes a recoverable bundle at the worker's last
committed tile; delivered site supplies remain at the site during suspension and
become bundles on cancellation. Free workers automatically recover bundles to storage.
Stored reservations remain in the source inventory until physical pickup, and are
excluded from other claims. They are never a second copy of material.

Walls, doors, built storage and workshops have one-tile footprints and data-defined
costs/work times in `src/simulation/work-content.ts`. Blueprint footprints block
navigation immediately and cannot overlap the heart, workers/reserved movement
cells, resources, bundles, structures or another site. Deliberate enclosures are
allowed. Doors start open and passable for every worker; closing blocks everyone
and is refused while a worker occupies/reserves the doorway. Open/close invalidates
routes. A workshop is a completed structure only; production is outside P03.

Stockpiles are passable ground zones requiring no material. Built storage has
capacity 60. Full storage retains cargo visibly; add another zone, build storage,
use materials, or cancel to drop a bundle. Missing ingredients release the worker
for other jobs and retry every ten ticks. Blocked work keeps its claims and can be
cancelled, reassigned or resumed after access changes. Idle/work traffic yields
by real adjacent steps and retries with bounded backoff. An enclosure without an
exit or a one-wide corridor without a passing space can remain blocked; open access
or redirect a worker. No movement swaps or teleportation occur.

**Repair** uses one stone for a wall or one wood for another building and 12 work
ticks; that material is recorded as spent. **Dismantle** takes 12 ticks, drops all
storage contents and returns 75% of each embodied material (rounded down). The
remainder is recorded as spent. This also releases outstanding storage claims.
The material ledger counts extracted material once across storage/bundles, cargo,
sites, buildings, spent and explicitly lost categories. Finite source depletion is
recorded separately; no sources regrow in P03.

Select a founder on the map or roster. The inspector shows life, position, movement,
job/status, cargo and blocked reason. Right-click a destination or use **Go to tile**.
A direct order releases that worker's job/cargo safely before movement. Drag to pan;
scroll/+/- zoom in integer steps; Home centers and Escape clears selection. Focus
the map for WASD/arrows. Credits and original crop provenance remain available.

## Time and saves

Pause/1×/2×/4× use fixed 10 Hz ticks. Hidden tabs pause with no accumulated catch-up,
retaining manual pause intent. Foreground processing services at most eight ticks
per frame while retaining/shown backlog. Rendering interpolates without mutating
simulation state. Per tick, routing allows two searches of at most 4,096 expansions,
plus bounded local yielding probes; failed searches back off to 100 ticks. Valid
interaction goals remain stable during an in-flight movement edge.

**Save** records the current tick; **Export JSON** downloads a portable snapshot;
**Import JSON** validates and transactionally stores before installing it. **New run**
saves a paused colony with the entered seed. Reload restores the last successful
save. There is no autosave or offline progress. Storage failures remain visible;
in-memory play and JSON export work. Invalid imports and aborted writes preserve
both the current world and valid saved slot.

Schema **2**, content **colony-p03-1**, intentionally rejects P02 schema 1 saves.
An old slot is left intact on restore rejection; export it with the previous build
if needed. No legacy/P02 migration is attempted. Validation bounds 2 MB input,
terrain, entities, stock, IDs, routes, occupancy, command references, worker/job
bijections, reservations, recipes, capacities and conservation. The snapshot includes
all cargo, claims, site/work progress, traffic/retry state, pending commands and RNG.
Stable IDs are never reused; pending commands whose removed targets can never return
are retired deterministically. IndexedDB remains `roag-colony` / `saves` / `active`.

## Checks and production preview

From `web/`, use this environment to retain all earlier phase evidence:

```sh
export ROAG_EVIDENCE_DIR=../artifacts/colony-web/P03
npm run assets
npm run assets:check
npm run typecheck
npm test
npm run test:scenarios
npm run build
npx playwright install chromium
npm run test:browser
python3 -B ../tools/verify_preservation.py
```

On minimal Linux use `npx playwright install --with-deps chromium`. Every npm command
can be prefixed with `fnm exec --using=24.21.0`. The browser runner owns ports
**4173/4174**, builds explicit `/` and `/roag/` variants and runs strict static servers:

```sh
node scripts/serve-production.mjs root  # http://127.0.0.1:4173/
node scripts/serve-production.mjs roag  # http://127.0.0.1:4174/roag/
```

`npm run preview` serves the default relative-base `dist/`. Evidence paths are resolved
relative to `web/` (absolute values also work). Local/scenario checks cover legacy
RNG vectors (768 draws, nine UTF-8 labels), 103 seeds + three forced fallbacks,
render-cadence independence, exact mid-work continuation, conservation, failure and
traffic scenarios. Browser tests retain camera/selection/resize/art failures, actual
reload/JSON and storage failure journeys. The work journey uses ordinary stocks and
worker construction, with a lab damage command only for repair. Native visibility
uses a separate Chromium/CDP `noDefaults:true` process and actual tab switching;
there is no synthetic visibility event or property override. The full Chromium
browser, as well as the usual headless shell, must be installed.

## Development scenarios and scope

`?scenario=routing-lab` uses a separate saved slot and a focused test corridor. Ada
starts at 58,60. Order 70,60, expand Run diagnostics, close the gate at 64,60 and
resume; reopen to test invalidation/recovery. Only this scenario permits terrain edits.

`?scenario=work-lab` has **ordinary seeded terrain, no supplied inventory and no
spawned structures**, in a separate slot. Structure cards expose **Lab: damage to
50%** for repair checks. Pure development commands also support worker death and
source removal for conservation scenarios. Those commands never appear in a normal
colony. Source removal is recorded separately from extracted/produced material.
No combat, needs, farming, production chains, towers or raids are implemented.
Defeat scaffolding remains heart destroyed OR every colonist actually dead; temporary
downing is not death. Detailed report: `docs/colony-web/reports/P03-report.md`.

## Artwork and runtime boundaries

Simulation modules import no Phaser, DOM, wall clock or legacy runtime. Generation
remains 128×128, six founders, 18 finite sources, independent seeded RNG streams and
three attempts plus validated fallback. Completed/cancelled job history is bounded;
active jobs are capped at 128, structures plus pending builds at 256. This is a
six-worker implementation, not a measured 24-colonist capacity claim.

The 27 original sprite bindings are unchanged. The exporter reads canonical
`../assets/catalog.json.gz` and original PNGs, checks original IDs/crop/source hashes,
and packs the selected pixels into a 160×80 sheet. RGB channels all <=8 become
transparent only in derived output. Only `colony.png`, `colony-atlas.json`,
`manifest.json`, `CREDITS.txt` ship; research catalogs/atlases, archives and legacy
runtime are excluded. Wall/door/storage/workshop roles reuse the existing local
rock/cottage/wood/hearth aliases; neutral source names and crop provenance remain
unchanged. Blueprint outlines, zones, bundles and cargo marks are drawn primitives.
Original artwork, protected files and all preceding reports/evidence are preserved.
