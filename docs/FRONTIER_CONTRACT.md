# Frontier campaign contract

COS-P01 introduced a headless campaign root above the established local world.
COS-P02 adds a feature-gated, three-site regional campaign. It is not travel,
automatic lunar founding, a galaxy simulation, or cross-site inventory.

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

## Tick, commands, and notices

`Sim.step(world, commands)` preserves the local order: `Sim.begin` increments the
world tick, existing local commands apply, then `Sim.body` runs labour, blasts,
materials, loose items, structures, ecology, colonists, and cleanup.

`Campaign.step` increments campaign tick, begins sites by ascending site ID, applies
the one global command array in recorded order, then runs each site body in that
same order. The envelope remains:

```lua
{scope='site',siteId=1,payload=existingLocalCommand}
```

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
explicit replay verification. Map export is still only the selected local map; it
does not export campaign ownership, people, history, or future transport state.

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

P02 therefore supports simultaneous generated local maps and site-scoped management
fixtures, not transport, loading, flight, lunar founding, global stockpiles,
factions, or a remote simulation abstraction. Finite local maps remain a prototype
boundary, not a permanent one-world restriction.
