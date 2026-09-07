# Causal generation and playable loop

Jomon derives a bounded set of consequential facts from named SHA-256 seed
stages:

```text
terrain, water, and climate
→ local work and settlement form
→ useful resources, surplus, and shortage
→ named contacts and material objective
→ hostile interests, patrols, and environmental timing
→ visible route choices and optional treasure
→ market, relationship, geographic, injury, death, and succession consequences
```

The same readable seed reproduces the six-adult household, relationships,
starting equipment, all four maps, contacts, treasure, visitors, encounter
compositions, regional processes, and voyage checks. Random derivation uses
named `stage_rng` streams backed by SHA-256, never Python's process hash.

## Four direct regional generators

Hearthford combines smoothed elevation/wetness fields, constrained river
carving, seeded roads, and authored settlement, ruin, cave, watch, and stacked
mill templates in a 96×54 footprint. Greywash (104×56) carves a moving
shoreline, tidal channel, dunes, and three parallel coast routes around anchored
salt, wreck, cave, mast, and chain-house structures. Greenwold (100×58) smooths
cellular canopy clusters into open clearings and recuts three authored forest
trails around resin, root, burn, and watch-tree work. Whitecairn (98×60) lays
seeded limestone terrace bands, switchbacks, a quarry loop, sink cave, kiln,
ridge bridge, and bell tower.

Each footprint has aligned levels `-1`, `0`, `1`, and `2`. Explicit links join
caves/cellars, ground, upper works, canopy/gantries, and roofs. Validation checks
the contact, objective, underground entrance, elevated landmark, every
container, every vertical link, and the physical path back to the landing.
Placed actors are moved only to the nearest same-level reachable tile when a
seeded obstruction covers an authored encounter coordinate. These are four
small feature-specific generators, not a biome or world-generation toolkit.

## Facts that change current decisions

Every generated regional fact is visible and actionable. Greywash's tide can
cover the wreck road while leaving dune and chain routes. Greenwold wind carries
burn smoke through upper openings. Whitecairn rockfall covers a direct stair
while leaving the sink loop. Hearthford fog, rain, and water change mill and
floodplain travel. A material control can alter each process or its elite
encounter. Pressure combines visible elapsed actions, geographic/elevation
depth, noise, and carried valuables; higher pressure expands awareness and
pursuit and wakes a stronger recorded threat.

Each added region has six named persistent containers. Their positions teach a
closed `C` and opened `o` language through ordinary stores before optional
cave, height, rope, light, or key routes. Contents are seed-deterministic and
physical: one build item, one armour item, and one finite supply. Pack cells and
weight can force the player to rotate, rearrange, leave, drop, or surrender a
reward.

Enemies use limited current sight, sound origins, last-known positions, group
alerts, morale, ammunition, allies, home, and duty. Those facts select one
concrete action such as patrol, investigate, aim, reload, intercept, flank,
mark a net cell, steal, escape, or retreat. Ranged attacks telegraph a lane or
setup before severe harm. Nothing advances while the terminal waits.

## Playable loop

Walk through Jomon's physical tavern, speak beside a visible adult to switch or
recruit, arrange shaped equipment between the locker, 10×6 pack, readied slot,
and six armour locations, select one crew support at the bar, and choose a
destination at the chart. Travel costs six action-clock measures and can
occasionally present one bounded voyage event. Cross the physical gangplank.

In the active seamless region, meet the primary contact and accept, refuse, or
materially alter the objective. Observe weather and patrols, choose quiet or
dangerous routes, enter structures/caves/upper works without room screens, open
or leave treasure, fight or negotiate, manipulate local controls, and manage
finite treatment, light, rope, smoke, ammunition, armour condition, load, and
terrain exposure. Then walk back through the same geography and gangplank—or
suffer cargo loss, forced injured return, or permanent death with succession.

The one atomic format-6 JSON save contains the current household, physical
items and orientations, regional exploration and changes, contacts, markets,
enemies, objectives, travel, and bounded significant history. A returned region
therefore changes later expeditions without any offline simulation.
