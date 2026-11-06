# Roag colony web — P07

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
sends that founder to a physical watch position near the heart; on a raid, guards rally at guard posts and can fight nearby attackers.
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
| Ammunition | Workshop | 1 wood + 1 stone → 4 rounds | 24 | 36 rounds outside magazines |

Farms pause at a 20-food reserve. Targets count existing storage, bundles and work
cargo; an in-progress batch finishes. Tools are stored products, with no
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
finite source depletion - development removal + farm/recipe production + enemy loot
  = stores + defense magazines + bundles + work/care cargo + sites + embodied buildings
    + salvage/repair spent + recipe/eating/medicine/ammunition consumed + explicitly lost
```

Reservations are subsets of real inventories. Recipes, harvests, meals, treatment,
rescue and death have validated counters. Dismantling retains storage contents and
returns 75% of embodied costs, rounded down; the remainder is spent. Dead founder
records remain saved; their cargo is recoverable. No resource duplication/rewards
are attached to repeated cancellation or death.

## Defend and recover

The first warning arrives at **tick 10800** and the raid at **tick 12000** (day
four). The warning names the approach. Pause to inspect it. Four melee raiders and
one breacher enter at legal map-edge cells; they seek the heart or nearby founders
and can break player-built obstructions. Gates are doors: open admits everyone,
closed blocks everyone. Building a blueprint does not create an invulnerable barrier.

Towers cost **4 wood + 4 stone**, hold 12 rounds, and shoot within Manhattan range
10. Traps cost **2 wood + 1 stone**, hold one round and trigger under a raider.
Guard posts cost **1 wood** and are passable rally locations. Make ammunition at a
workshop after cancelling its previous recipe order. Use each defense card's
**Reload** button: a worker physically collects and delivers four rounds to a
tower or one to a trap. Reloading needs that much free magazine space. Three jobs
fill a tower. Magazine ammunition is separate from the workshop reserve target.

Use the **Colony defense** panel for a selected founder's rally, retreat or shelter
order. Group shelter uses nearby distinct destinations. Urgent eating, resting and
care take precedence. Work and cargo remain attached during emergency orders;
release the order to resume them. Raids initially shelter civilians and rally guards;
you can change or release those orders. Rallied founders strike adjacent attackers.

Attacks warn on locked cells and never follow a moving target. Walls and friendly
bodies block tower shots; friendly bodies take no damage from colony shots. A round
is consumed at launch even if its source is destroyed and the unresolved attack is
cancelled. Each ordinary defeated attacker leaves one recoverable wood unit exactly once; the Captain leaves five.

After the raid, rescue and treatment use the same physical medicine chain. Repair
surviving damaged defenses with actual supplies, reload, and resume production.
Heart destruction or actual death of every founder ends the run; downing alone does
not. Save/export retains pending attacks, cooldowns, emergency orders and ammunition.

For the current seed-42 preparation scenario, add stockpiles **59/58/57/56,66** and
designate wood **#12** and stone **#18** as the first sources deplete. Build towers at
**71,60**, **70,62**, **64,69**, **60,63**, a trap at **64,58**, a guard post at
**62,62**, a wall at **65,60**, and a gate at **64,60**. Physically load the tower at **71,60**, the two rear towers and the trap before
the warning. The forward tower at **70,62** can draw breacher attacks while the
**71,60** tower covers it; repair the surviving damage after combat. This scenario
leaves the forward tower unloaded to exercise recovery from imperfect preparation. The headless ordinary-command probe is
`node scripts/defense-probe.mjs`; its outputs are integration evidence, not a
replacement for production browser acceptance.

## Endless raids and the Captain

After a wave is defeated, the next warning begins **1,200 ticks** later and arrival
follows **600 ticks** after that. Wave budgets rise from seven by three per wave,
capped at 60. Composition, approach, squad identities and entry offsets commit at
announcement; saving cannot reroll them. Blocked entry or a full battlefield retains
the pending queue. The outermost approach lane cannot receive colony blueprints.

Shield guards reduce damage from their front until they lower their shield to
strike. Archers commit cardinal shots that can be blocked or dodged. Healers spend
12 ticks on a locked patient tile; wounded rushers retreat and hold near their
living healer unless threatened. Interrupting the healer changes that support.
The defense panel and map show roles, facing, support and committed impact cells.

Every fourth wave brings one of four bosses, rotating from a seed-specific first boss.
Seed 42 starts with the **Ossuary Captain**. Its front reduces damage by 80%
while protected; turning and recovery expose it to 50% additional damage. A visible
four-cell charge commits before moving and cannot pass through a new obstruction.
The alternating rampart leaves a walking gap. Its barricades have 36 health and
expire after 600 ticks; select one and order a founder or the group to **Clear**.
Workers must physically approach and strike it. The Captain's death removes its
remaining barricades. No arena is painted over the colony.

For the ordinary five-wave seed-42 example, add storage at **54/55,66/67**, designate
wood **#8** and stone **#14/#16**, add an ore workshop at **72,70**, and cover other
approaches with towers at **60,59**, **69,58**, **59,69**. Keep magazines supplied.
Ivo and Nell can guard while civilians are released to work under cover. Avoid
chasing enemies out to the map edge; withdraw wounded guards for real treatment.
The scenario in `npm run test:raids` records every ordinary command, exact reload
checkpoints and five completed waves followed by six fully healed founders. This
is one demonstrated strategy/seed, not a claim that all placements are balanced.

The last 16 wave summaries retain losses, ammunition use and repairs. Live enemies
and queued entries each cap at 150; committed attacks at 256 and barricades at eight.
The artificial cap workload tests bounded behavior, not ordinary late-wave balance.

## Environmental bosses and biomes

Seed **43** begins in marshland and meets **Bellkeeper** on wave 4; seed **44** begins
in ashlands and meets **Sluice-Maw**; seed **45** begins in woodland and meets
**Threadmother**. Bosses rotate every fourth wave thereafter. Woodland has balanced
resources. Marsh has richer finite forage and less wood; shallow tiles take 6 ticks
per step. Ash has richer stone and less wood/forage; exposed ember tiles deal 2 damage
every 50 ticks. A completed structure covers its tile. Workers and enemies obey the
same hazard and movement rules. Webs take 8 ticks; ordinary ground takes 4. A committed
step retains its duration even if water drains or a web burns midway through it.

- **Bellkeeper:** actual work, footsteps and colony attacks produce bounded sound.
  Its pulse locks the last heard cell; leave that cell before impact. Resonators
  announce three expanding rings. Select a founder and **Strike fixture** to approach
  and work beside a resonator. Destroying it cancels its rings and exposes the boss
  to 50% additional damage for 40 ticks.
- **Sluice-Maw:** channels have fixed saved cells, eastward flow, a pool and a valve.
  Surges hurt and push either faction. A feast heals only after the boss physically
  reaches its connected wet pool. **Drain channel** takes 20 ticks of adjacent work,
  permanently cancels that channel's surge/feast and exposes the boss. Draining both
  channels leaves it ordinary melee attacks.
- **Threadmother:** anchors hold a web partition with a walking gap. Its reel locks
  a cell and visible line; moving, placing an obstruction or **Cut thread** can stop
  the pull. A downed target instead loses 120 ticks from its rescue deadline. Strike
  an anchor to clear its connected partition. **Ignite web** takes 24 ticks of primitive
  spark work, with no fuel recipe; connected web/anchor cells burn after visible
  delays. Fire hurts both factions and nearby structures, and committed fire remains
  dangerous after its owner or igniter dies. Empty gaps stop spread.

The **Environmental counters** panel shows actual cells, work progress, deadlines,
exposure and permanent valve state. Choose a living founder before using its action
buttons. Counters require arrival and work; urgent food/rest/care can interrupt them.
Boss geometry searches at most 64 candidate sites without repainting buildings,
sources, bodies or previously accessible routes. If none is safe, its reserved
identity waits in the raid queue. Killing a boss yields five physical wood once and
continues the endless run. Sound/rings/reels target colonists; surges, fire and ash
are neutral hazards. Geometry, noise memory, work claims, pending effects and locked
movement durations are saved exactly.

The ordinary `npm run test:bosses` command starts each of seeds 43/44/45 with no
inventory or buildings, gathers/builds defenses, survives four waves, uses the boss
counter and recovers all six founders. It exports the complete command journal,
replays from a fresh seed and checks exact save continuations. Keep only useful
wood/stone reserves: suspend excess gathering so civilians can farm, cook, reload
and make medicine. Stage an unwounded worker near a valve before the feast; keep a
second guard ready to attack during the exposed window. These are demonstrated
preparations, not a guarantee for every seed or colony layout.

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
Schema **6**, content **colony-p07-1**, rejects earlier schemas and legacy saves without replacing
them. No migration is claimed. Export older slots with their older build before an
explicit fresh run. The 2 MB validation limit covers all needs, crop stages, claims,
cargo, deadlines, job/movement progress, IDs, RNG and pending commands.

From `web/`:

```sh
export ROAG_EVIDENCE_DIR=../artifacts/colony-web/P07
fnm exec --using=24.21.0 npm run assets:check
fnm exec --using=24.21.0 npm run typecheck
fnm exec --using=24.21.0 npm test
fnm exec --using=24.21.0 npm run test:scenarios
fnm exec --using=24.21.0 npm run test:raids
fnm exec --using=24.21.0 npm run test:bosses
fnm exec --using=24.21.0 npm run build
fnm exec --using=24.21.0 npm run test:browser
python3 -B ../tools/verify_preservation.py
```

Install Chromium if absent with `fnm exec --using=24.21.0 npx playwright install chromium`
(or `--with-deps chromium` on minimal Linux). Native visibility tests use full Chromium
through CDP, actual tab transitions, and no synthetic visibility override. Browser
checks generate ordinary raid and boss fixtures unless `ROAG_RAID_FIXTURE_DIR` and
`ROAG_BOSS_FIXTURE_DIR` point to validated scenario directories from the same source, then build and serve both `/` and `/roag/` on ports 4173/4174. To inspect those outputs:

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

The original 27 selected sprite crops/provenance/credits remain intact. Sixteen additional selected crops supply boss, hostile and environmental roles; see `content/boss-art.json` for identity uncertainty and casting decisions. Only four
compact generated runtime asset files ship; no full research catalog/atlas, archives,
legacy runtime or source maps. Original artwork and legacy sources are untouched.
P07 adds three environmental bosses and three biome preparations. Recruitment and seasonal/social simulation follow in P08; the dedicated game HUD follows in P09-HUD.
Six founders/one-tile structures are supported; large-colony performance, Firefox,
Safari, touch work journeys and long-run survival balance are not claimed.
