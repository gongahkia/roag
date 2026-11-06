# Roag colony web — P04

From the repository root:

```sh
cd web
fnm exec --using=24.21.0 npm ci
fnm exec --using=24.21.0 npm run dev
```

Open http://127.0.0.1:5173/. Node **24.21.0**, npm **11.19.0**, TypeScript **5.9.3**,
Phaser **3.90.0**, Vite **8.3.6** and Playwright **1.65.0** are pinned.

## Establish a colony

Seed **42** starts paused with six founders and no inventory. Use **Work & materials**
below the map. Place stockpile zones at **63,66**, **62,66**, **61,66**, **60,66**;
designate wood **#11 (45,56)** and stone **#15 (56,38)**. Build a farm at **68,64**;
choose **4× / Resume**. From its completed structure card choose **Plant / tend /
harvest**. Build a kitchen at **70,64**, then choose **Produce meal**. Build beds at
**66,68**, **68,68**, **70,68**, an infirmary at **72,68**, and a workshop at **74,68**.
Start **Produce ore** at the workshop and **Produce medicine** at the infirmary.
Production works from physical storage or bundles; it never creates inputs remotely.
Pause freely while placing orders; queued commands take effect when resumed.

A medicine order without ore explains its missing ingredients. Add the workshop
ore order to resolve that shortage. The material ledger, job cards and founder
inspector show cargo, claims, site supplies, work/stage progress, needs and access
problems. Find buttons center the relevant founder/source/site/building.

All five duties have per-founder priorities: **0 off**, **1 normal**, **2 preferred**,
**3 first**. Farming and hauling use gatherer; construction, repair, dismantling and
workshop production use builder; kitchen work uses cook; medicine and aid use healer.
Higher priority changes release lower-priority work safely. At a new claim, a worker prefers ready jobs of their higher duty priority when global job priority is equal or higher; existing claims of other workers are retained. A dominant guard duty
sends that founder to a physical watch position near the heart, without combat.
Explicit job assignments still require an enabled applicable duty. All six founders
remain independently assignable; no party cap or aggregate background production exists.

## Needs, care and timing

Simulation uses **10 Hz** integer ticks. A day is **3,600 ticks**: six minutes at 1×,
90 seconds at 4×. Founders start fed/rested; hunger first requests food at tick 1,800
and fatigue first requests a bed at 2,400. One tick adds one to each need while alive.
At hunger 3,600 or fatigue 4,800, health falls by one every 20 ticks. Hungry, tired
or seriously injured workers perform work on alternate ticks. Mood is a simple
0–100 indicator derived from hunger, fatigue and health, without social simulation.

Eating requires arrival, one claimed meal/raw food, and 20 work ticks. A meal reduces
hunger by 1,800; raw food by 900. A founder claims a bed and reaches it before fatigue
falls by 12 per tick (11 net after accrual), until 200. Bed claims are exclusive.
When beds/clinics are occupied or absent, workers can continue at reduced efficiency
with a visible explanation; this leaves someone able to build/provision care.

Injured founders seek a free infirmary. A healthy-enough healer physically collects
one reserved medicine, reaches the patient, treats for 60 ticks and restores health,
followed by 100 recovery ticks. The infirmary has one patient claim; its medicine
production bench is independent. A patient leaving for urgent rest releases their
clinic claim; the healer drops unused medicine locally and can help another patient.

Zero health causes downing: work/cargo/claims settle once, and a **900-tick** rescue
deadline starts. A healer must collect one real medicine, reach the downed founder
and spend **40 ticks** stabilizing them. Stabilization restores 40 health, allowing
them to walk to the infirmary for a second dose and full treatment; it does not
teleport them or reset hunger/fatigue. With no capable rescuer/access/medicine,
the deadline causes actual death. All-downed remains nonterminal until actual deaths.
Defeat is heart destroyed OR all six actually dead. Terminal saves remain terminal;
**New run** explicitly replaces the selected slot with a fresh paused colony.

## Production and accounting

| Building | Cost | Work ticks |
| --- | --- | ---: |
| Farm plot | 2 wood | 20 |
| Kitchen | 4 wood + 2 stone | 30 |
| Bed/rest area | 2 wood | 16 |
| Infirmary | 4 wood + 2 stone | 30 |
| Workshop | 6 wood + 4 stone | 30 |
| Wall / door / storage | 3 stone / 3 wood + 1 stone / 4 wood + 2 stone | 16 / 16 / 24 |

A farm repeats **20 planting**, **20 tending**, **240 growing**, and **20 harvest**
ticks, producing five food in actual cargo. Growth releases the worker; work stages
require arrival. This is new farming behavior, independent of finite forage nodes.

| Recipe | Facility | Inputs → output | Work ticks | Reserve target |
| --- | --- | --- | ---: | ---: |
| Meals | Kitchen | 1 food + 1 wood → 4 meals | 30 | 24 meals |
| Ore processing | Workshop | 2 stone → 1 ore | 30 | 4 ore |
| Medicine | Infirmary | 2 food + 1 ore → 2 medicine | 40 | 4 medicine |
| Simple tools | Workshop | 1 ore + 1 wood → 1 tool | 30 | 2 tools |

Farms pause at a 20-food reserve. Targets count existing storage, bundles and work
cargo; an in-progress batch finishes. Tools are stored products in P04, with no
equipment bonus yet. Ore is processed from finite stone, not an invented free stock.
Recipes take delivered inputs once, generate one output batch, and physically deliver
it before starting another. Full storage retains cargo and explains the wait.

Needs retain the original job, worker, work progress, input reservation, site stock
and work cargo while using a separate one-unit care pouch. Returning resumes that
job. Suspend/reassign/cancel/death release work claims and drop work cargo locally;
site progress/supplies survive suspension, while cancellation drops site supplies.
Care claims remain independent of job cancellation. A manual destination during care
is retained until care ends. Unused care medicine drops on interruption/death.

The ledger proves, per material:

```text
finite source depletion - development removal + farm/recipe production
  = stores + bundles + work/care cargo + sites + embodied buildings
    + salvage/repair spent + recipe/eating/medicine consumed + explicitly lost
```

Reservations are subsets of real inventories. Recipes, harvests, meals, treatment,
rescue and death have validated counters. Dismantling retains storage contents and
returns 75% of embodied costs, rounded down; the remainder is spent. Dead founder
records remain saved; their cargo is recoverable. No resource duplication/rewards
are attached to repeated cancellation or death.

## Movement, saves and checks

Select via map or roster; right-click a destination or use **Go to tile**. Drag to
pan, scroll/+/- for integer zoom, Home to center, Escape to clear. Focus the map for
WASD/arrows. Routing is bounded with deterministic ties, occupancy reservations,
physical yielding and navigation invalidation. Closed enclosures/no-passing corridors
can require opening access or redirecting a worker. No teleportation is used.

Pause/1×/2×/4× retain fixed ticks and render interpolation. Hidden tabs pause without
catch-up, preserving manual pause. Foreground frames process at most eight ticks
while retaining backlog. Render/UI reads never advance the simulation themselves.

**Save**, **Export JSON**, **Import JSON**, **New run** remain available. Saves use
transactional IndexedDB; invalid imports and failed writes preserve the current world
and valid slot. Unavailable storage is visible and JSON export still works. Reload
restores the last successful save; there is no autosave or offline simulation.
Schema **3**, content **colony-p04-1**, rejects P02/P03 and legacy saves without replacing
them. No migration is claimed. Export older slots with their older build before an
explicit fresh run. The 2 MB validation limit covers all needs, crop stages, claims,
cargo, deadlines, job/movement progress, IDs, RNG and pending commands.

From `web/`:

```sh
export ROAG_EVIDENCE_DIR=../artifacts/colony-web/P04
fnm exec --using=24.21.0 npm run assets:check
fnm exec --using=24.21.0 npm run typecheck
fnm exec --using=24.21.0 npm test
fnm exec --using=24.21.0 npm run test:scenarios
fnm exec --using=24.21.0 npm run build
fnm exec --using=24.21.0 npm run test:browser
python3 -B ../tools/verify_preservation.py
```

Install Chromium if absent with `fnm exec --using=24.21.0 npx playwright install chromium`
(or `--with-deps chromium` on minimal Linux). Native visibility tests use full Chromium
through CDP, actual tab transitions, and no synthetic visibility override. Browser
checks build and serve both `/` and `/roag/` on ports 4173/4174. To inspect those outputs:

```sh
fnm exec --using=24.21.0 node scripts/serve-production.mjs root
# http://127.0.0.1:4173/
# Separate terminal, with the same ROAG_EVIDENCE_DIR:
fnm exec --using=24.21.0 node scripts/serve-production.mjs roag
# http://127.0.0.1:4174/roag/
```

`?scenario=work-lab` has ordinary terrain/resources, no supplied inventory or buildings,
and a separate slot. It exposes injury/downing/hunger/fatigue controls for focused care
checks and building damage for repair. `?scenario=routing-lab` remains a separate
terrain-edit corridor. Neither is the three-day sustainable-colony evidence.

The original 27 selected sprite crops/provenance/credits remain intact. Only four
compact generated runtime asset files ship; no full research catalog/atlas, archives,
legacy runtime or source maps. Original artwork and legacy sources are untouched.
No combat, towers, raids, bosses, recruitment or seasonal/social simulation is added.
Six founders/one-tile structures are supported; large-colony performance, Firefox,
Safari, touch work journeys and long-run survival balance are not claimed.
