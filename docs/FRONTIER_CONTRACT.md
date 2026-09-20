# Frontier campaign contract

COS-P01 introduced a headless campaign root above the established local world.
COS-P02 adds a feature-gated, three-site regional campaign. COS-P03 adds a
feature-gated parked craft and physical expedition preparation. Neither tranche
implements travel, automatic lunar founding, a galaxy simulation, or cross-site
inventory.

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
