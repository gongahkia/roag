# COS-P04 implementation handoff

## Status and checkout

**Status: completed for the headless and mocked-UI P04 scope.** Real-window
interaction remains unverified.

The P04 pass began from the current checkout after the P03 prerequisite gate. The
branch advanced concurrently from `4bf9918c300fe1c80084a51bd891871453d266c7`
through owner-managed commits while the pass was running; this report is evidence
about the checked-out source and commands, not a claim that this agent committed
anything. I did not run `git commit`, `push`, `reset`, `clean`, install a package,
or access a player save. Recheck `git status` and HEAD before a follow-on tranche.

Runtime used for the checks: LuaJIT 2.1.1767980792 on Linux 7.2.5 x86_64. LÖVE
11.5 is installed, but no real game window was launched. Mocked storage used fresh
`/tmp/cos-p04-*` directories.

## P03 gate rerun

Before P04 integration, the normal registered suite and the independent mock
adapters were run. `tests/logistics.lua` P03-B/C/E/F/G/L exercises physical
fetch/carry/deposit/unload/cancel custody, interrupted work, target reduction and
reservation limits; P03-D/H/J/K/M covers manifest revisions, all-crew loading,
assembly/needs/directive precedence and labour OFF; P03-G/I/O covers persistence,
cross-site local IDs and feature/template isolation. `tests/region.lua` P02-D/E/G/H
retains all-site simulation, site routing, empty-home survival and whole-campaign
history. The feature-off P01/P02/P03 assertions run through `tests/run.lua`.

Fresh command outcomes are listed below; a mock is not a window pass.

## Implemented contract

`src/campaign.lua` now accepts `features.travel=1` only with `region=1` and
`logistics=1`, and creates a version-1 `travel` record only when a new region is
explicitly requested with `travel=true`. The N-lab uses that option for new
frontier campaigns; earlier campaign histories retain their existing feature sets.

`src/travel.lua` owns campaign-level travel state. Its version-1 rules contain the
fixed symmetric routes Home Planet/Moon I = 400 ticks, Home Planet/Moon II = 600,
and Moon I/Moon II = 800. It validates a bounded (128) receipt ring, monotonic
journey/receipt IDs, exact account vectors, strict journey states, and exclusive
local-versus-transit `personId` custody. A craft is either docked with no
passengers, or travelling/holding with a journey and one canonical cargo map.

`launch_expedition` is bound to `sourceSiteId`, `craftId`, `manifestId`, and
`expectedManifestRevision`; `return_to_origin` is bound to `craftId`, `journeyId`,
and expected outbound leg. Both validate at queue time and again at application.
Launch consumes one actual cargo metal, closes/releases the P03 manifest safely,
detaches source workers, starts an outbound journey, and records a source-to-transit
receipt. A duplicate/stale launch has no second effect. A holding outbound craft
may spend one actual cargo metal for its one permitted reverse leg; a blocked return
remains holding.

Portable worker fields are explicitly adapted in `src/travel.lua`: `personId`,
name, life/HP/needs, mine/build values and currently implemented status counters
move with the person. Local worker ID, cell, task/path, carry, job ownership,
directive and labour roster do not. Arrival allocates fresh local IDs in ascending
person order and default AUTO labour entries. Existing local `worker.id` remains
the navigation/job identity. Living transit passengers are included in
`Campaign.extinct`.

`Campaign.step` retains the P01/P02/P03 beginning/command/logistics/local-body
order, then invokes `Travel.step`: active crafts and passengers update in stable
craft/person order, then due landing/holding resolution is processed. A traveller
gets exactly one cabin physiology update per campaign tick; a successful arrival
does not run local physiology/work until the next tick. `Colonists.transitStep`
uses the established hunger rate, fatigue rest decrement of 0.035, breath recovery
of 1.6, starvation-before-eating order and normal food threshold/reduction without
terrain exposure or hidden ground food.

Landing uses the existing anchor/body-safe pose predicate within the eight-cell
Manhattan radius. It does not repair terrain or inspect hidden destination content.
An unowned moon needs a living party to become owned. Flooded/buried/capacity-blocked
landing and all-dead unowned arrival are valid holding states. Docked cargo remains
in its craft until existing P03 unload jobs move it to local physical storage.

`Campaign.metrics` extends the existing accounting view for site import/export/
consumption accounts and transit cargo. `src/travel.lua` records per-resource
departure, arrival, maintenance-part and cabin-meal receipts; only the newest 128
are retained, while cumulative accounting and next IDs remain monotonic. No target,
reservation, global inventory, or duplicate cargo container is introduced.

`main.lua` and `src/render.lua` extend the existing Region/expedition interface:
new campaigns show Launch only for a concrete applied manifest/revision; transit
and holding panels show endpoint, ticks, passenger count, cargo and eligible return.
The Region view can open a travelling craft without switching to an unowned map.
All new labels use the shared 13px Cozette renderer instance; headings use graphics
scaling. No new font asset or fallback path was introduced.

## Acceptance coverage

| Evidence | Registered implementation test |
| --- | --- |
| P04-A | `tests/travel.lua`: actual command/job load, assembly, launch, first landing, physical unload, return preparation/landing. `tools/travel_soak.lua` adds production-duration founding, return and resupply. |
| P04-B | `tests/travel.lua`: one update per ordinary tick, deferred first local update, and controlled D=1/D=2 calls through the same `Travel.step` engine. |
| P04-C | `tests/travel.lua`: living traveller prevents extinction; empty owned home remains commandable; total extinction rejects challenge input. |
| P04-D | `tests/travel.lua`: source detachment, portable aptitude/needs preservation, fresh destination-local state. |
| P04-E | `tests/travel.lua`: maintenance/departure/arrival receipt vectors and per-resource custody totals. |
| P04-F / L | `tests/travel.lua`: malformed, stale, unassembled and duplicate launch identities reject without a second part/person movement. |
| P04-G / H / N | `tests/travel.lua`: blocked outbound/return holding, all-dead unowned holding, no false ownership, and strict malformed-state validation. |
| P04-I | `tests/travel.lua`: active-transit save/load continuation, seek/practice branch and explicit replay verification. |
| P04-J / K | `tests/travel.lua`: read-only site inspection independence; return preserves existing world generation/terrain and does not grant replacement crew. |
| P04-M / O | `tests/travel.lua`: actual cargo food use/no hidden stockpile; real `Travel.step` receipt pruning with a controlled four-craft fixture. |
| P04-P | `tests/travel_gui.lua`: mocked N-lab -> Region -> prepare -> assemble -> launch -> transit -> landing flow, modal isolation and pure inspection. |
| P04-Q | `tests/travel.lua` feature/template boundary plus retained P01/P03 storage and codec checks from `tests/run.lua`. |

The P04 headless module contains 16 groups / 88 assertions. P04-P is intentionally
a separately runnable mock because it initializes a mock LÖVE process.

## Numerical production-duration trace

`luajit tools/travel_soak.lua 73421 10000` used a 128x80 home, two 192x112 moons,
`core/region/logistics/travel`, and quiet deterministic ecology only to isolate the
journey. It performed real loading jobs and the fixed 400-tick Home/Moon I route:

```text
outbound: depart t=661, personId=1, craft food=4 metal=2
          land/found t=1060, personId=1, craft food=4 metal=2
          physical food unload completed t=1280, craft food=2 metal=2
return:   depart t=1941, personId=1, craft food=2 metal=1
          land t=2340, personId=1, craft food=1 metal=1
resupply: depart t=3001, personId=2, craft food=2 metal=0
          land t=3400, craft food=2 metal=0
```

The return's food decreased by one through transit consumption; the two outbound
departures and return each consumed their actual maintenance metal. At tick 10,000
the craft was docked at site 2, site 2 remained owned, transit cargo was food 0 /
metal 0, the receipt ring held 10 records, encoded history was 3,157,886 bytes,
and eight in-memory checkpoints were retained. The soak saved/restored and ran
explicit replay verification before its final extension to tick 10,000.

## Verification and measurements

```text
luajit tests/syntax.lua                         PASS, 83 Lua files
luajit tests/run.lua                            PASS, 115 groups; 120,974 assertions
luajit tests/region_gui.lua <fresh /tmp>        PASS P02-I, mocked UI/storage
luajit tests/logistics_gui.lua <fresh /tmp>     PASS P03-N, mocked UI/storage
luajit tests/travel_gui.lua <fresh /tmp>        PASS P04-P, mocked UI/storage
luajit tests/benchmark_smoke.lua                PASS, mocked cooperative benchmark adapter
luajit tools/headless.lua 12345 frontier 2000   PASS, legacy CPU/accounting trace
luajit tools/expansion_soak.lua                 PASS, six legacy replay/accounting traces
luajit tests/maximum_size.lua                   PASS, 11 maximum-dimension smoke cases
luajit tools/travel_soak.lua 73421 10000        PASS, production-duration trace above
luajit tools/travel_benchmark.lua 200 3         PASS, CPU-only samples below
git diff --check                                PASS when run before this report update
```

An initial seeded P04 campaign encoded to 3,154,737 bytes (0 checkpoints). The
three 200-tick CPU-only diagnostic rows were:

```text
sample  generation  3-map local/tick  prepare+assemble  transit/tick  landing  serialize  bytes    heap MiB
1       0.073628 s  0.001155585 s      0.814656 s        0.001121680  0.002810 0.168189   3177856 36.566
2       0.085707 s  0.001214100 s      1.063288 s        0.001495370  0.001678 0.287779   3177856 39.287
3       0.113757 s  0.001998000 s      1.202739 s        0.001167905  0.001263 0.182395   3177856 38.341
```

These are `os.clock` CPU seconds on this host. The local row is a three-map
feature-off P03 trace; the transit row is three maps plus an active craft. Variation
is substantial, so there is no portable or stable P03/P04 overhead percentage.
They do not measure FPS, GPU time, wall time, or OS resident memory.

## Isolated manual checklist and limits

With confirmed disposable LÖVE save isolation: open N, use C to choose New frontier
campaign, confirm generation then creation, open Region (button or Shift+F7), select
Prepare craft, select a settler and food/metal target, Apply, let real haulers load,
Assemble, then Launch. Verify home continues running while the craft panel reports
transit. At Moon I, verify first landing makes the site selectable without a forced
camera change, unload cargo physically, prepare a return, and inspect a blocked
landing only as holding rather than repaired terrain. Never point this workflow at a
real player save.

No real LÖVE window/manual journey was run; mocked UI and an earlier isolated
Cozette load check do not prove input delivery, bitmap scaling, click alignment or
GPU rendering. No transport combat, cabin interior, stasis, additional craft,
orbital physics, new planetary physics, discovery, school, culture, faction, or P05
work was started.
