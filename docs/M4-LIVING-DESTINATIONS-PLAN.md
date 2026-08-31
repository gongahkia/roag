# M4 living destinations implementation plan

## Scope and boundary

M4 adds a compact, persistent simulation for the five Route Board destinations: Kestrel Landing, Orison Relay, Halcyon Dock, Nerida Pressure Chain, and Borealis Glassworks. It does not stream tile maps, simulate populations, or replace the M1–M3 transit, package, or landing systems.

`advanceGalaxyRouteReckoning` remains the one domain boundary that changes canonical world time. The active-session clock and M3 transit already call this function; no menu, renderer, report lookup, visual-mode change, reload, or wall-clock time may call the partition simulator directly.

## Durable model

`GalaxyState` will move from v3 to v4 and gain one `destinationWorld` value. It will contain:

- a stable, ordered record of five destination partitions;
- a current Jomon-known report for each destination; and
- no renderer-only or wall-clock state.

Each partition will retain a stable ID, content revision, last processed Route Reckoning mark, condition, one named bounded pressure, scheduled developments, bounded resolved-development IDs, bounded persistent consequences, bounded interventions, and bounded local history. Scheduled IDs and intervention IDs use stable machine identifiers, not presentation text.

Reports remain separate from partition truth. A report carries destination ID, reported condition, source, observation mark, received mark, confidence, and an optional known consequence. Initial charts describe only the initial condition. A report becomes visibly aged as Route Reckoning advances; it is refreshed by arrival or explicit local inspection, never by viewing the Route Board.

## Deterministic advancement

Destination advancement receives a target canonical mark and processes each partition independently in stable ID order. It resolves only scheduled developments whose due mark lies between the saved processed mark and target mark, in `(due mark, development ID)` order. Each development removes itself and records its stable resolved ID, so a save/load or repeated advancement cannot resolve it twice.

Any random authored variation is derived from `rngFor(seed, 'destination-partition', partitionId, developmentId)`; there is no shared mutable stream. Partition order and unrelated developments therefore cannot perturb another destination. The queues, resolved IDs, consequences, interventions, and local history have explicit small limits. The model does not run a per-tile or per-resident loop while a destination is unloaded.

The existing generic sector tick remains inside this same boundary, but it will exclude the five Route Board sites so it cannot overwrite their authored partition conditions.

## Authored pressures and developments

| Destination | Initial condition and pressure | Development path |
| --- | --- | --- |
| Kestrel Landing | Calibration queue; instrumentation backlog | A later inspection audit can put approach work under review. |
| Orison Relay | Balanced relay; thermal load | Thermal load becomes relay overheating, then a throttled transfer window. |
| Halcyon Dock | Tender cycle; dock congestion | A salvage-tender backlog constrains dock work. |
| Nerida Pressure Chain | Pump watch; pump wear | Cavitation restricts transfer approaches. A physical bypass intervention becomes a later pump stabilization check. |
| Borealis Glassworks | Kiln nominal; thermal debt | Kiln backlog progresses to a controlled cooldown. |

Nerida is the end-to-end causal arc. Its initial report warns of pump wear. At a scheduled mark, cavitation changes the authoritative condition and adds a transfer restriction. This remains hidden behind the old report until an arrival or local inspection refreshes Jomon knowledge. A known cavitation report increases the displayed duration and risk of incoming Nerida routes. While physically docked at Nerida, the player can explicitly confirm a sixty-mark bypass installation. That operation uses the canonical boundary, records an intervention, and schedules a later stabilization verification rather than solving the problem immediately.

## Player surfaces and records

The existing Route Board keeps its topology, selection, confirmation, and cancellation flow. It will receive a derived connection view based only on Jomon-known reports: condition, report age/confidence, and known route modifiers. A committed transit saves its derived duration so reloads cannot recalculate it differently.

A compact `D` destination-status action at the hub will show the docked destination’s confirmed condition, pressure, report source, and available intervention. `I` performs a local inspection (refreshing only knowledge); `B` begins the Nerida bypass confirmation; `Enter` confirms it. The operation has a stated sixty-mark cost and later stated outcome. Status viewing itself is non-mutating.

Arrival and significant local inspection changes append one General Manifest entry only when the Jomon learns a changed condition. The bypass and its later confirmed resolution append one entry each. Unseen developments never append omniscient Manifest entries.

## Migration and regression preservation

`migrateGalaxy` will accept v1 through v4. v1–v3 saves receive deterministic initial partitions at their exact existing Route Reckoning mark; their developments are scheduled after that mark, so migration does not retrospectively simulate elapsed or offline time. v4 values are normalized, bounded, and cloned without rerolling. Existing packages, contracts, transit history, route caches, courier state, site snapshots, and Manifest records remain untouched.

The two stale hub assertions will use `Composite Cap` and `Vital Gel`. The runtime registry applies the documented retheme mapping before hub strings are generated, which makes those names authoritative.

## Verification plan

Focused tests will cover active and remote advancement, no offline advancement, large/chunked equality, partition-order and RNG isolation, report inspection non-mutation, stale/refresh behavior, one-time development and Manifest entries, known route modifiers, the Nerida intervention arc, v3 migration, existing M3 transit and Kestrel package settlement, and bounded durable state. A deterministic soak will compare repeated travel/intervention/save-load fingerprints. The autoplay catalogue and browser flow will exercise report inspection, stale knowledge, Nerida arrival, intervention, later consequence, Manifest history, and explicit Kestrel package settlement.
