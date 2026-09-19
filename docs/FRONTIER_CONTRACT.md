# Frontier campaign contract

COS-P01 introduces a headless campaign root above the established local world. It is
not travel, a galaxy simulation, or a multi-settlement UI. P01 accepts exactly one
site so later work can add explicit, versioned campaign features without changing
the current local simulation contract.

## State and identity

`src.campaign` validates this data-only, canonical state:

```lua
{
  format = 'cosmonauts-campaign-state', version = 1,
  ruleset = 'frontier-campaign-v1', features = {core = 1},
  tick = 0, mode = 'challenge', seed = 1,
  society = {id = 1, origin = 'abandoned_convicts', independent = true},
  nextPersonId = 1,
  sites = {{id = 1, ownerSocietyId = 1, world = localWorld}},
}
```

P01 requires one site owned by society 1. The embedded world has
`frontier={version=1,siteId=1}`. Every worker in such a world has a positive,
campaign-wide `personId`, assigned by ascending existing local `worker.id` during
`Campaign.new`. `worker.id` remains the local navigation/job identifier. There is
no global authoritative people table, global inventory, or aliasing of a caller's
source world. Campaign state validates dense arrays, stable IDs, counters, finite
numbers, world/campaign tick equality, ownership, feature/ruleset versions and the
embedded local world.

Legacy worlds have neither marker nor `personId`; their existing save/history path
is unchanged. `deepward_02`, `deepward-map`, old local envelopes, and map schemas
1/2 remain compatibility contracts. A map template is still not a campaign save.

## Tick and command order

Legacy `Sim.step(world, commands)` still performs exactly one local tick. It now
uses exposed stages: `Sim.begin` increments the local tick, command application
uses `src.commands`, and `Sim.body` retains the established labour, blasts,
materials, item, structure, ecology, colonist, and cleanup order.

`Campaign.step` increments campaign tick; begins sites in ascending site ID; applies
the global tick command array in its recorded order; then runs each site body in
ascending site ID. A campaign command is:

```lua
{scope='site', siteId=1, payload=existingLocalCommand}
```

`src.campaign_commands` rejects unknown scopes/sites/types/fields at queue time and
rechecks local state at application. Rejection is deterministic; state-dependent
local rejection records the existing local rejected-event convention without partial
gameplay mutation. Camera/render/inspection never route or execute commands.

Campaign extinction is the campaign-level count of living workers across owned
sites. It is not a single-world flag, although P01 has one site.

## History and saves

`src.campaign_history` holds initial/live campaign state, one tick-indexed command
stream, and at most eight in-memory checkpoints at the configured 200-tick interval.
Seeking reconstructs the entire campaign. Challenge archive input is rejected.
Practice archive input clones the viewed campaign and drops all future commands and
checkpoints together. Checkpoint eviction never removes commands.

Normal restore structurally validates the tagged envelope and does not replay an
unbounded history. `beginVerify`/`updateVerify(budget)` and `verifyReplay(budget)`
are explicit bounded diagnostic replay operations; successful verification means a
reconstructed live campaign canonically equals encoded live state.

Campaign persistence uses the distinct envelope:

```lua
{format='cosmonauts-campaign', version=1, initial=..., live=..., commands=...}
```

`src.campaign_codec` reuses the data-only canonical codec and its 32 MiB cap.
`Store.saveCampaign` writes `campaign.run.tmp` then renames it to
`campaign.run.dat`; `run.dat` is untouched. This is same-directory replacement,
not an fsync-backed database transaction. Campaigns are new or explicit test
constructions only: P01 does not auto-import old local runs.

## Current boundary

The current game still presents one local settlement through its existing UI. P01
does not implement travel, spacecraft, additional sites, global stockpiles,
factions, or a campaign renderer. Finite local maps are the current local-world
implementation boundary, not a permanent one-world design decision.
