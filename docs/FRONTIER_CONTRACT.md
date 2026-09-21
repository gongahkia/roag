# Frontier campaign contract

COS-P01 introduced a headless campaign root above the established local world.
COS-P02 adds a feature-gated, three-site regional campaign. COS-P03 adds a
feature-gated parked craft and physical expedition preparation. Neither tranche
implements travel, automatic lunar founding, a galaxy simulation, or cross-site
inventory.

COS-G01 adds two explicit current-frontier versions: `features.body=1` means
`settler-2x4-v1` (two fine cells wide by four high, left-foot anchor), and
`features.visibility=1` means fog/illumination. Visibility requires body. Each
new-feature campaign world carries matching `frontier.body=1`,
`frontier.visibility=1`, `body=1`, and compact per-row explored-memory state.
Campaigns without those markers retain 2×3 geometry, omniscient old map rendering,
and the P05 visibility behavior they recorded. Unknown/mismatched versions reject;
there is no migration.

G01 ordinary panels are live: Crew, F4 notes, Region, expedition, School,
inspector, help and block selection never implicitly pause or resume the campaign.
Space remains the deliberate normal clock control. Visibility has unseen,
remembered and current states. Current visibility is the union of living local
settlers' 20-cell shadowcast sight from their 2×4 eyes, subject to 3-cell dark
vision or source illumination. Solid terrain and walls are opaque; boundary walls
remain visible. A torch is a normal transparent one-block structure costing one
metal and lighting radius 16, capped at 128 installed torches per site. It may
stand on solid floor or mount to a solid terrain face/completed wall behind its
build block; removing that backing support disables its normal structure/light
support. A docked
shuttle lights radius 20; a travelling shuttle lights no site. Fog memory is
campaign/history state but templates exclude it. New spatial commands and P05
environmental witnessing use this same local visibility boundary.

COS-G02 adds `features.equipment=1` and `features.safe_excavation=1` to current
frontier campaigns. Both require G01 body and visibility; safe excavation also
requires equipment. Worlds in those campaigns carry matching frontier markers,
bounded local rope arrays, and persistent worker `stress`/`panic` state. Older
recorded histories carry none of those fields and retain their former digging,
fall, starter and visibility behavior.

Equipment is campaign-owned, stable-ID physical custody: a pickaxe or rope coil
is loose at one site, carried/equipped by one persistent person, in one craft,
or (for a coil) embodied in one local rope. There are at most 128 site items, 64
craft/transit items and 128 ropes per site. New frontiers begin with two loose
pickaxes and four loose rope coils, with their metal value in the initial
accounting baseline. One equipped pickaxe follows its owner during actual travel;
each loose tool consumes one of the craft's ordinary 24 shared cargo slots.

A rope coil creates one two-cell-wide, 1–24-cell-long local climb lane. It needs
an ordinary reachable anchor and physical fetch/deploy work, grants no light or
map information, and returns its same coil only when intact and deliberately
removed. A current frontier's Delegate panel and `L`/`Shift+L` controls unfurl
the lane downward/upward respectively; both use the same physical coil and
climb behavior. New ladders are no longer offered in current-frontier controls,
while existing ladders and historical command replay remain valid. Ropes
destroyed by ordinary blasts are lost. The Tool bench is a
one-block normal construction costing four stone and two metal. It fabricates a
pickaxe from two metal in 120 work actions or a rope coil from one metal in 60;
inputs remain real escrow until output placement.

Safe-feature miners recheck post-dig body support at the actual terrain mutation.
They prefer a safe alternate pose, then a controlled existing/physically deployed
rope route, otherwise retain the player's order with `Unsafe descent — rope
required`. A pickless dig contributes one work unit; an equipped pick contributes
two against soil/sand/ice and three against rock/ore. Feature-on falls of at most
four cells are safe; each begun additional four cells deals 15% of maximum HP,
capped at 90% for a fall. Existing demolition charges remain the sole explosive
system and need normal construction, field-work arming, an escape route and fuse.

Stress is deterministic and bounded 0–100. Damage, a transition to critical
breath, an unsafe-task abort, and established blast danger add stress; safe living
conditions recover one every 20 ticks. Panic begins at 80 and clears only at 50
outside an immediate emergency. A panicked person releases ordinary work and seeks
safe lit footing; only when no such escape exists may they take one emergency drop
up to eight cells. Panic never authorizes a deliberate support-removing dig or
charge arming.

## State and identity

P01 campaigns remain valid exactly as before:

```lua
{format='cosmonauts-campaign-state', version=1, ruleset='frontier-campaign-v1',
 features={core=1}, tick=0, mode='challenge', seed=1,
 society={id=1,origin='abandoned_convicts',independent=true}, nextPersonId=1,
 sites={{id=1,ownerSocietyId=1,world=localWorld}}}
```

P02 new campaigns add `features.region=1`, three sites, and this validated region
record:

```lua
region={version=1,bodies={
 {id=1,kind='planet',name='Home Planet',siteId=1,terrainSeed=...,recipe=...},
 {id=2,kind='moon',parentBodyId=1,name='Moon I',siteId=2,terrainSeed=...,recipe=...},
 {id=3,kind='moon',parentBodyId=1,name='Moon II',siteId=3,terrainSeed=...,recipe=...},
},notices={}}
```

Regional sites add `bodyId`. Site 1 is owned by society 1; sites 2 and 3 begin
unowned. IDs, never record positions or labels, are identity. Labels are persisted
provisional UI text and use no randomness. Each campaign world has
`frontier={version=1,siteId=...}`. `personId` is globally unique across actual
workers and assigned in ascending local `worker.id`; `worker.id` remains local job
and navigation identity. There is no authoritative global people table or global
inventory. Legacy worlds carry neither field, and `deepward_02`, `deepward-map`,
local map schemas 1/2, and local save/replay behavior remain compatibility paths.

`Campaign.newRegion(masterSeed, options)` uses the frozen version-1 plain-Lua
Park--Miller helper in `src/campaign_random.lua`. Terrain seeds derive from
`region/body/<id>/terrain/v1`; moon recipe streams derive from
`region/body/<id>/recipe/v1`. The moon stream chooses from sorted existing layout
keys first, then sorted existing climate keys. Resolved recipes and terrain seeds
are persisted. The home uses the lab option snapshot and its derived terrain seed.
Cosmetic streams are independent. A P01 campaign never silently gains a region.

All sites are generated at tick zero and persist as worlds; selecting/loading never
regenerates them. Moons are 192x112 current-Frontier worlds with valid landing
geometry but no workers, jobs, structures, loose starter supplies, or arrival event.
They retain current material/ecology rules. P02 implements no low gravity, vacuum,
pressure, oxygen, temperature, radiation, shuttle, flight, or founding mechanics.
Three full-detail maps are a provisional verification/performance bound, not a
permanent one-world or three-world design limit.

P03 new campaigns additionally have `features.logistics=1`, which requires
`region=1`. P01/P02 histories do not gain it at load time. Its bounded data-only
state is campaign-owned, never a world back-reference:

```lua
logistics={version=1,
 rules={version=1,seats=3,cargoCapacity=24,maintenanceMetal=1,assemblyRadius=8},
 nextCraftId=2,nextManifestId=1,nextOperationId=1,
 crafts={{id=1,ownerSocietyId=1,seats=3,capacity=24,dockedSiteId=1,
          anchor={x=...,y=...},cargo={},activeManifestId=nil}},
 manifests={},operations={}}
```

The parked craft has three seats, 24 abstract resource slots, and an empty hold.
It does not add people, starter resources, hull physics, construction, damage,
boarding, stasis, fuel, launch, flight, or a remotely usable stockpile. Its cargo
is one exclusive owner. A local `load`/`unload` job and an assembly directive hold
only validated references (`craftId`, manifest/revision or operation ID); workers
remain their source world's bodies, identified locally by `worker.id` and globally
by `personId`.

An active manifest has a monotonic `id` and `revision`, source/destination site
IDs, craft ID, sorted unique passenger `personId`s, positive target cargo map, and
reserved assembly poses. Target cargo is a desired final amount, not inventory or
an additive request. Actual cargo persists through edits/cancellation; a lower
target creates physical surplus that must be unloaded. One active manifest per
craft and one unload operation per craft/resource are bounded and validated.

## Tick, commands, and notices

`Sim.step(world, commands)` preserves the local order: `Sim.begin` increments the
world tick, existing local commands apply, then `Sim.body` runs labour, blasts,
materials, loose items, structures, ecology, colonists, and cleanup.

`Campaign.step` increments campaign tick, begins sites by ascending site ID, applies
the one global command array in recorded order, runs logistics reconciliation once
when the feature is present, then runs each site body in that same order. Local
legacy envelopes remain:

```lua
{scope='site',siteId=1,payload=existingLocalCommand}
```

P03 adds source-bound campaign envelopes:

```lua
{scope='campaign',type='prepare_expedition',sourceSiteId=1,craftId=1,
 destinationSiteId=2,passengers={personId},cargo={food=2,metal=1}}
{scope='campaign',type='assemble_expedition',sourceSiteId=1,craftId=1,manifestId=1}
{scope='campaign',type='cancel_expedition',sourceSiteId=1,craftId=1,manifestId=1}
{scope='campaign',type='unload_cargo',sourceSiteId=1,craftId=1,resource='food',amount=1}
```

Queueing checks structure, ownership and current state without mutation;
application rechecks current preconditions and returns a deterministic rejection
when an earlier same-tick command made one inapplicable. Repeated identical
preparation and unload requests are idempotent. A changed plan validates before it
cancels stale jobs/directives, then increments its revision. Cargo jobs are offered
after commands and before local worker actions, use the existing `haul` duty and
body-aware navigation, and move only through pile -> carry -> craft or craft ->
carry -> local pile. Need/hazard interruptions and task release use the normal
drop path. Requested amounts and reservations are never a second resource count.

`assemble_expedition` is deliberately separate from preparation. It only succeeds
with exact target cargo, one metal unit for a future flight, no cargo operations,
and living source passengers. Poses are selected in ascending `personId` within an
eight-cell Manhattan radius, then distance/y/x order, through non-mutating
body-aware reachability checks. Hunger, fatigue and hazards still interrupt them.
The latest explicit movement directive wins: a later rally replaces assembly;
cancellation clears only a matching assembly directive. Readiness is a pure
calculation, not launch permission or a latched state.

Queueing and application reject non-owned/unknown sites and malformed payloads;
application rechecks local state. P02 runs all three worlds every tick whether
unvisited, empty, or not rendered. There are no per-site pause states, dormant
catch-up, lazy generation, or camera-dependent simulation. An empty owned site may
retain unstaffed orders while another site has living crew; challenge input stops
only at campaign-wide extinction.

Region campaigns retain at most 128 canonical typed notices ordered by
tick/site/event ordinal. They are simulation outputs, not parsed English text. UI
dismissal is presentation-only.

## History, saves, and maps

`src.campaign_history` keeps one initial campaign, live campaign, global command
stream, and at most eight in-memory checkpoints at the configured 200-tick
interval. Seek/branch/restore apply to every site together. Challenge archives
reject mutation; practice branching clones the viewed campaign and drops all future
commands/checkpoints. Structural load does not run an unbounded replay.
`beginVerify`/`updateVerify(budget)` and `verifyReplay(budget)` are explicit
diagnostic replay checks.

Campaign files retain the distinct bounded 32 MiB envelope:

```lua
{format='cosmonauts-campaign',version=1,initial=...,live=...,commands=...}
```

`Store.saveCampaign` writes `campaign.run.tmp`, then same-directory-renames to
`campaign.run.dat`; `run.dat` is untouched. This is replacement, not an fsync-backed
database transaction. Region records, sites, ownership, people, notices, and
recipes participate in initial/live state, checkpoints, saves, seeks, branches, and
explicit replay verification. When present, P03 rules, crafts, actual cargo,
manifests/revisions, operations, cargo jobs and assembly directives participate too.
Map export is still only the selected local map; it does not export campaign
ownership, people, history, craft cargo, manifests, directives, or transport state.

## UI and current boundary

`N` remains the generation lab. Its default action and ordinary Enter behavior stay
local. Press `C` in that lab to select **New frontier campaign** or **Continue
frontier campaign**. The first Enter for a new campaign generates/validates a
candidate; the second confirms it, replaces the active session, and writes only the
campaign slot. Continue reads only
that slot: a missing slot reports `No campaign save`; corrupt/unsupported data is
left in place and does not replace the active session. Local previews are explicitly
local and are not exact previews of derived campaign homes.

Campaign mode has a visible Region button and uses `Shift+F7`, because `F7` retains
the existing biome-view binding. The Region overlay consumes input; Escape/toggle
closes it, while Space controls the one campaign clock. It shows body relationships,
fixed labels, owned-site crew/alerts, and `Unvisited — Transport pending`; it does
not display an unvisited moon's underground terrain, inventory, resources, ruins,
or species. The persistent selector lists owned sites by stable ID, including empty
ones. Switching cancels unfinished drags, changes UI view only, and never retargets
queued orders. Archive views use the viewed campaign's ownership/notices rather
than the live frontier.

P03 new frontier campaigns initialize logistics only after their three sites have
generated and validated. The Region home card exposes **Prepare craft**. Its modal
has an editable UI-only draft for destination, passengers and target cargo, shows
actual/target cargo, physical-operation progress and pure readiness reasons, and
submits source-bound commands only after a click. Escape returns to Region; Space
remains the global clock; clicks and focused draft input do not leak into world
tools. It says `Departure becomes available in the next implementation tranche`.
Older campaigns explicitly report logistics as unavailable rather than receiving a
shuttle. A parked craft marker is presentation only.

P04 adds travel only to campaigns created with `features.travel=1`; it requires
both `region=1` and `logistics=1`. P01/P02/P03 and legacy histories retain their
old feature sets when loaded. There is no in-place activation or migration editor.
New frontier campaigns created through the lab choose all currently implemented
features, including travel.

## P04 travel, landing, and accounting

Travel campaigns add this data-only record and extend each craft with
`passengers={}` and `journey=nil` while docked:

```lua
travel={version=1,
 rules={version=1,routes={
   {from=1,to=2,duration=400}, {from=1,to=3,duration=600},
   {from=2,to=3,duration=800},
 }},
 nextJourneyId=1,nextReceiptId=1,receipts={},
 accounts={sites={...},transit={imports=...,exports=...,consumed=...}}}
```

Routes are symmetric. A craft is either `DOCKED` with no passengers and a site ID,
or has one `journey` and an off-map passenger array. A journey holds its monotonic
ID, immutable origin/destination site IDs, `outbound` or `return` leg,
`travelling` or `holding` status, start tick, route duration, remaining ticks,
safe-cabin physiology parameters, and a bounded holding reason. A holding craft
may reverse only an outbound leg, once, to its original site; it consumes an
actual metal unit held in cargo. A blocked return stays holding rather than
ping-ponging or receiving terrain repair.

`launch_expedition` is source-, craft-, manifest- and revision-bound:

```lua
{scope='campaign',type='launch_expedition',sourceSiteId=1,craftId=1,
 manifestId=1,expectedManifestRevision=1}
{scope='campaign',type='return_to_origin',craftId=1,journeyId=1,
 expectedLeg='outbound'}
```

Launch rechecks P03 readiness, exact cargo, assembly, route, ownership, active
operation absence, and a metal part. It then consumes one metal in source custody,
closes the manifest, releases site-local task/job/labour references, transfers
people to the craft, and records the remaining cargo as a transit export/import.
A stale or duplicate command has no partial effect. `personId`, name, life state,
HP, needs, and current implemented aptitude/status fields are portable; local worker
ID, coordinates, path/task/job/carry/directive and local labour roster state are
not. Arrival allocates new destination-local IDs in person-ID order and normal
AUTO labour entries. Departure is not death; a retained dead passenger can arrive
once as an inert local dead worker without a second death event.

Every campaign tick still begins every local world, applies ordered commands,
runs logistics offering and all local bodies in stable site-ID order, then updates
off-map crafts in craft-ID/person-ID order and resolves due arrivals. A departure
at tick `t` with duration `D` receives its first cabin update/decrement at `t` and
can arrive after the local bodies at `t + D - 1`; an arrival first runs local work
on the following tick. Cabin use is the ground resting-rate subset in
`Colonists.transitStep`: hunger increases by the world rule, fatigue falls by
`0.035`, breath recovers by `1.6`, starvation damage is applied before eating, and
one craft-food unit may reduce hunger by `48` at the existing threshold. It is a
safe resting cabin, not stasis, healing, a remote ground stockpile, or a new oxygen
model. Living transit passengers count toward campaign survival.

Landing checks the persistent destination anchor and legal full-body standing poses
in the established Manhattan radius of eight. It neither digs, clears liquid,
spawns supplies/people nor exposes an unvisited underground map. An unowned moon
becomes owned only after a successful landing with a living passenger. Unsafe
geometry, capacity exhaustion, or an all-dead party at an unowned destination is a
valid serialized holding state. Docked cargo remains in the craft and must be
unloaded through the existing physical P03 jobs.

Travel accounting extends `Campaign.metrics` rather than creating a second ledger.
Docked cargo counts with its site, and travelling/holding cargo counts in transit.
Departure, arrival, maintenance parts, and cabin meals produce one monotonic
receipt with a per-resource vector and matching account debit/credit; at most 128
recent receipts are retained while cumulative accounts and next IDs persist.
Targets/reservations remain nonphysical. Existing local production/consumption is
unchanged, so resource reconciliation distinguishes local ledgers, docked cargo,
transit cargo, and transfer/consumption accounts.

## P04 presentation and current boundary

The existing Region and expedition modal now expose Launch for an applied,
ready manifest and show travelling/holding route, remaining ticks, passenger count,
cargo, landing obstruction, and the one eligible Return action. A successful first
landing adds the owned settlement to the normal selector without forcing a camera
switch. The modal uses the viewed history state; archive mutations still reject or
practice-branch at the campaign layer. Travel status does not reveal unvisited
terrain, resources, species, or ruins.

All renderer text continues to use the bundled Cozette bitmap font through the
shared 13px font instance in `src/render.lua`; headings are drawn with graphics
scaling rather than requesting unsupported bitmap strike sizes. The fallback font
is only for a missing/corrupt bundled asset. This is not a broad typography or
real-window usability guarantee.

P04 supports one craft, the fixed local planet--moon routes, actual passenger and
cargo custody, founding, physical unloading, return, and resupply. It does not add
mid-flight aborts/diversions, hull damage, orbital physics, extra spacecraft,
vacuum/gravity/temperature systems, long-range relic travel, new cultures,
discoveries, schools, or a remote simulation abstraction. Finite local maps remain
a prototype boundary, not a permanent one-world restriction.

## P05 personal field knowledge

P05 adds `features.knowledge=1`, requiring campaign core identity. New frontier
campaigns choose it alongside the already implemented region/logistics/travel
features. Older local and P01--P04 campaigns retain their original field-note and
shared-discovery behavior; loading does not populate personal facts or reinterpret
old observations.

Knowledge-enabled campaigns carry a bounded versioned root:

```lua
knowledge={version=1,rulesVersion=1,registryVersion=1,nextHistoryId=1,history={}}
```

Each campaign world marks `frontier.knowledge=1`. Every worker and transit
passenger has the portable data-only extension
`frontier={version=1,knowledge={version=1,observations={},facts={},studies={},lastStudyActionTick=...}}`.
It is copied by the explicit P04 portable-person adapter, not inferred from local
labour policy. Observations, facts and studies are capped at 64 each; an observation
retains at most four effect samples and facts/studies retain at most four provenance
samples. The campaign's acquisition-history ring holds at most 128 typed entries.

Stable identification fact IDs are `identify/<flora|fauna|sites>/<catalog-key>/v1`
for the existing authored encounter registries. The only P05 operational facts are:

```text
operational/flora/filter/steam-to-water/v1
operational/flora/thorn/sand-to-rock/v1
```

The ecology stage runs a knowledge-only passive-sighting scan before it mutates
ecology. It orders living local workers by `personId` and encounter categories/IDs
stably, uses an eight-cell Manhattan range and the existing clear-line helper, and
never changes materials or random streams. The actual glass-reed `steam -> water`
and iron-thorn `sand -> rock` mutation branches synchronously capture eligible
witnesses before changing a cell. Successful effects alone receive a typed event
identity `(siteId,tick,ordinal)` and immutable before/after sample; failed attempts,
later arrivals, cameras and repeated inspection add nothing.

`field` adds the feature-gated generic kind `study`. A normal site-bound field
command names only its target and optional local worker; it does not accept a hidden
fact ID. Survey grants its acting worker's identification fact. Study requires that
same living worker's identification plus two distinct-tick successful observations
of that exact current specimen. It gains one unit only on an eligible fieldwork
action, at most once per person per campaign tick, and completes at 120 units.
Progress and qualifying evidence belong to the person/fact record, so another
worker cannot take over progress. A matching new specimen requires new firsthand
evidence before a suspended study can resume; local target/jobs/paths never travel.

The F4 field-notes modal is observer-specific in knowledge campaigns. Left/Right
selects a living local observer; the visible **Study** tool submits the generic
field command. Unknown encounters use neutral physical descriptions until that
observer surveys them. A fact may be historically recorded after its expert leaves
or dies, but history is not a capability, evidence source, or teaching system. New
text uses the existing shared Cozette font instances. This remains mocked-interface
evidence, not a real LÖVE-window layout claim.

P05 deliberately does not add schools, records that teach, XP, global research,
automatic knowledge sharing, generated languages/cultures, new ecological
mechanisms, or P06 functionality.

## P06 field schools and communicated knowledge

P06 adds `features.education=1`, which requires `knowledge=1`. New frontier
campaigns opt into it with the previously implemented core, region, logistics,
travel and knowledge features; earlier histories keep their recorded feature set
and have neither `campaign.education`, `world.education`, personal expertise nor
school structures.

An education-enabled world has a monotonic school/session allocator and each
person has this portable extension beside P05 knowledge:

```lua
education={version=1,fieldworkXP=0..400,teachingXP=0..400,
           tuition={},lastLearningActionTick=...}
```

The two initial expertise values use independent P02 RNG namespaces
`person/<personId>/expertise/fieldworkXP/v1` and
`person/<personId>/expertise/teachingXP/v1`, each in 0..149. They persist through
save/load and the P04 portable-person adapter. P05 operational analysis retains
its evidence requirements; under education it uses pre-action
`1 + floor(fieldworkXP / 100)` work, then awards one capped fieldwork XP. In a
feature-off history it remains one unit with no expertise state.

`field_school` is a normal nonblocking one-block construction. Its real recipe is
four `stone` and two `metal`; construction still uses physical hauling/escrow and
the usual destruction path. A newly installed school has a lifetime-stable local
school ID, revision 1, disabled record policy, no topic, no draft, no active
session and no records. It holds at most 16 distinct immutable records, one draft
and one active session. School/session/record IDs are monotonic and are never
reused for a rebuilt structure.

The site-bound, revision-bound `school_policy` command selects enabled state,
record/teach/study mode, fact/version and normal priority. The command checks the
actual school ID and policy revision when queued and applied. A topic must have a
living local knowledgeable person or a completed record at that school; disabled
policies remain valid even if their former source later disappears. A real policy
change releases the complete previous session. Changing topic or mode discards an
unfinished local recording draft; disabling with the same topic/mode and changing
only priority retains it.

Recording requires 120 ordinary Field-duty attendance actions by a current local
expert. It creates one school-local record with fact/version/subject, copy tick,
bounded contributor summaries and flat copied provenance. It does not create a
portable book or grant a nearby worker knowledge. A repeated topic record performs
no additional copy work.

Teaching and record study share a learner-owned 200-unit tuition record; its
separate teaching and record-study unit totals always sum to progress. Teaching
forms an atomic teacher/learner pair only after both Field-eligible people have
reachable distinct school poses. Record study reserves one eligible learner and a
surviving record at that exact school. An operational topic additionally requires
the learner's corresponding personal identification. Sessions report attendance
during ordinary work and finalize once after worker updates; live teaching and
record study evaluate only at positive ticks divisible by ten. A policy revision,
task release, need/hazard precedence or departure releases the entire session,
while personal earned tuition and expertise remain with the person.

At an eligible evaluation, live teaching adds
`2 * (1 + floor(teacherTeachingXP / 100))` normalized units, grants the teacher
one teaching XP and learner one fieldwork XP; record study adds
`1 + floor(learnerFieldworkXP / 100)` and grants the learner one fieldwork XP.
The calculation uses pre-reward XP and all values cap at 400. Recording earns no
XP. A person has at most one P05/P06 learning-progress action per campaign tick.
At 200 units one knowledge acquisition is made with `taught`, `record`, or
`mixed` method, and the tuition entry is removed. Communicated facts carry bounded
flat contributor/provenance summaries but do not copy another person's eyewitness
observation array.

The build selector and selected-structure inspector expose **Field school** and a
revision-bound School panel with policy controls, available local topics, record
count, expertise and local expert information. This extends the shared Cozette
13px renderer path. The P06 presentation test is mock-only; it is not evidence of
real-window font layout, hitboxes or input delivery.

P06 does not add remote instruction, portable books, a general research graph,
new facts/reactions, automatic teaching, schools in transit, social simulation,
language/culture systems, relic travel or a P07 tranche.

## G03 minds, memories, and relationships

`features.psychology=1` is a new-frontier-only feature that requires knowledge
and safe excavation. It preserves G02's serialized `stress` and `panic` fields,
but adds a bounded portable personal state: six stable facets, six mutable
values, duty dispositions, one background and ambition, up to 32 memories (eight
core), and up to 64 directional relationships. Every generated value is derived
from campaign seed plus persistent person ID; no camera, UI draw, local worker ID
or mutable terrain RNG influences personality.

Player authority remains absolute. Psychology adds no generic order refusal.
Reluctance can make ordinary disfavoured or strained work slower and occasionally
lose a work beat to a harmless correction. It cannot change the job target,
consume extra matter, bypass an OFF duty, outrank an existing higher priority, or
weaken G02 support/fall safety.

Newly created worlds set `rules.moveEvery=1` and `rules.planEvery=8`, making
ordinary movement and idle route selection more responsive. Those values remain
serialized per world, so existing campaigns retain their recorded pacing. The
body-aware navigation graph additionally permits a level horizontal jump over
one or two unsupported fine cells only when its full raised flight footprint is
clear and its landing body has normal support. It does not jump shafts, climb
height changes, pass low ceilings or hazards, or replace ropes.

Typed danger, knowledge, school, arrival and death seams create explainable
personal memories. Local, eligible idle pairs can form a bounded positive
conversation or argument. Memory reaction, relationships and slow value
adaptation affect stress and social outlook. Psychology maintenance runs once per
campaign tick after site work and travel processing boundaries are resolved;
rendering and ordinary live panels never mutate it.

The Crew panel exposes the state in plain language: mental condition, current
work explanation, personal style, recent/lasting memories, ambition, and
relationship tone. Raw values and score calculations remain backend state.
Existing feature-off histories do not gain psychology fields or reinterpret their
old stress timeline. Industry and automation are the next planned content
direction; factions and cultures follow that work.

## G04 industry, power, machines, and conveyors

`features.industry=1` is a new-frontier-only extension requiring the current
equipment and safe-excavation features. It creates no starter components or
industrial structures, and feature-off campaigns retain their existing mining,
tool, map, and accounting behavior. A **Machine Component** is a stackable,
physical resource worth two units of mineral-equivalent metal. The existing Tool
bench can turn two metal into one component in 120 real work actions; a powered
Fabricator can make components, pickaxes, and rope coils from physical buffers.

All industrial ownership is site-local: a component can be loose, carried,
in normal build escrow, craft cargo, a machine/bin/belt buffer, or its one
ordinary physical owner. Components use one existing craft cargo slot. Industry
does not add a shared stockpile, cross-site electrical network, remote drawing,
or instant manufacture.

Small Solar Arrays generate three integer power units for each of two
sky-exposed block columns. Power Poles connect within six build blocks; a
producer, battery, Fabricator, Mining Rig, or Electric Lamp connects to a pole
within three. Derived topology is a revisioned disposable cache, never saved
truth. A Battery stores 0–200 charge and changes by at most six per tick.
Consumers receive full power or none in priority then stable structure-ID order:
lamp 1, rig 3, Fabricator 2. Solar serves consumers first, batteries discharge
only to make a complete grant, and leftover solar charges batteries.

Fabricators have 16-unit input/output buffers, one recipe, an in-process escrow,
productive-tick progress, priority, and wear. They consume a complete recipe at
cycle start and commit output only when final output space exists. A Mining Rig
works only an existing legal player dig designation within twelve fine cells of
its outward drill mouth; it contributes four work units per powered tick and
cannot mine its own floor support. It never creates designations or reveals fog.

Conveyors are supported, non-body-blocking one-block structures with an
eight-unit directional buffer. On even ticks, a unit advances at most one
automatic conveyor segment. They can feed a compatible Fabricator input or an
Industrial Bin. Bins hold 32 units and use receive/supply mode, a direction, and
an optional item filter. Fabricator/Rig output may enter an adjacent outward
conveyor. Transfer ordering is stable and source/destination capacities retain
blocked cargo rather than deleting it.

Fabricators and Mining Rigs gain one wear on each committed productive tick and
block after their 600th. Ordinary maintenance fetches and consumes one Machine
Component through 60 real work actions, resetting wear. Solar arrays, poles,
batteries, bins, belts, and lamps do not wear in G04. An enabled, fully powered
Electric Lamp supplies G01-compatible light radius 18, but still needs a living
local observer for current visibility and P05 witnessing.

Industrial construction is ordinary Build work with these costs: Solar Array
2 rock/4 metal/1 component; Pole 1 metal; Battery 2 rock/4 metal/2 components;
Fabricator and Rig 4 rock/4 metal/2 components; Bin 2 rock/2 metal/1 component;
Conveyor 1 metal; Lamp 1 metal/1 component. Existing removal/blast lifecycle
releases buffers under normal drop rules and makes mid-cycle Fabricator escrow
demolition waste rather than duplicate input/output. Battery charge is not a
mineral, food, or water ledger value; components, tools, industrial buffers, and
construction material are counted exactly once.

## COS-G05 — factions and contact

`factions=1` is a new-campaign-only campaign feature requiring industry, psychology, and personal knowledge. It generates exactly four bounded external societies using isolated campaign-random namespaces. Their culture values, norms, prehistory, coarse stocks, relations, offers, and incidents are authoritative campaign state; they never run local material simulation. Signal Relay scanning, protocol contact, representative familiarity, Trade Depot custody, and courier shipments are deterministic campaign stages. A foreign shipment is outside player inventory until atomically delivered to its bound local Trade Depot. Older feature-off histories do not generate or receive faction state.
# G06 security extension

Security is an opt-in `security=1` campaign feature requiring factions,
industry, and psychology.  It serializes local policy and people security
state, plus bounded raid/loot state.  Feature-off histories must not receive
weapons, Guard designation, grievance, or raid scheduling on load.
