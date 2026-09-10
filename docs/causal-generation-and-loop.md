# Causal generation and playable loop

Jomon derives a bounded set of consequential facts from named SHA-256 seed
stages:

```text
seed and world parameters
→ watershed, coast, elevation and climate exposure
→ geology, soil and water behaviour
→ plant and animal ecology
→ resources, production and settlement work
→ institutions, dependencies and material disputes
→ five-event regional histories with physical and social evidence
→ routes, seasonal hazards and trade pressure
→ caches and history-derived working objects
→ quests, inhabitants and encounter ecology
```

The same readable seed reproduces the six-adult household, relationships,
starting equipment, all eight maps, 24 regional contacts, histories, treasure, visitors,
encounter compositions, regional processes, weather and voyage checks. Random
derivation uses named `stage_rng` streams backed by SHA-256, never Python's
process hash. Geography is generated lazily where appropriate, while the
manifest, version and sparse mutations make revisits and save reloads exact.

## Eight regional families

Hearthford combines smoothed elevation/wetness fields, constrained river
carving, seeded roads, and authored settlement, ruin, cave, watch, and stacked
mill templates in a 96×54 footprint. Greywash (104×56) carves a moving
shoreline, tidal channel, dunes, and three parallel coast routes around anchored
salt, wreck, cave, mast, and chain-house structures. Greenwold (100×58) smooths
cellular canopy clusters into open clearings and recuts three authored forest
trails around resin, root, burn, and watch-tree work. Whitecairn (98×60) lays
seeded limestone terrace bands, switchbacks, a quarry loop, sink cave, kiln,
ridge bridge, and bell tower.

Dunmire Peat Isles jitters walkable bog islands around raised causeways, fuel
racks and buried drains. Rillscar Iron Gorge folds mineral cuts around two
bridges, industrial spans and a connecting undercut. Marlbank Clay Terraces
shifts irrigation bands through fields, potters' courts, kilnworks and buried
water routes. Frostmere Braided Estuary winds three seasonal channels around
gravel islands, net work, a winter loft and sheltered crossings. These four
families are generated on first visit and thereafter retain their exact sparse
changes.

Each footprint has aligned levels `-1`, `0`, `1`, and `2`. Explicit links join
caves/cellars, ground, upper works, canopy/gantries, and roofs. Validation checks
the contact, objective, underground entrance, elevated landmark, every
container, every vertical link, and the physical path back to the landing.
Placed actors are moved only to the nearest reachable tile when a seeded
obstruction covers an authored encounter coordinate. Structure passes establish
function and entrances, place compatible authored pieces and supports, apply
historical damage, then validate access, loops, sightlines, seasonal returns,
evidence and populations. These are eight small feature-specific generators,
not a universal biome, Wang-tile or WFC toolkit.

## Facts that change current decisions

Every generated regional fact is visible and actionable. Greywash's tide can
cover the wreck road while leaving dune and chain routes. Greenwold wind carries
burn smoke through upper openings. Whitecairn rockfall covers a direct stair
while leaving the sink loop. Hearthford fog, rain, and water change mill and
floodplain travel. A material control can alter each process or its elite
encounter. Pressure combines visible elapsed actions, geographic/elevation
depth, noise, and carried valuables; higher pressure expands awareness and
pursuit and wakes a stronger recorded threat.

Each region has named persistent containers. Their positions teach a
closed `C` and opened `o` language through ordinary stores before optional
cave, height, rope, light, or key routes. Contents are seed-deterministic and
physical: one build item, one armour item, and one finite supply. Pack cells and
weight can force the player to rotate, rearrange, leave, drop, or surrender a
reward.

Each region also has an authored marked cache; Hearthford has nine stores, the
other established regions seven each, and the four frontier regions eight each,
for 62 containers overall. Contact testimony, elevation, regional material
signs and five-event histories offer independent ways to mark optional treasure
in exploration memory. Each region also forges one finite working object from
its actual crisis, repair, witness and institutional claim. Marks persist, but
they do not open or move the physical container.

The causal layers have multiple downstream consumers. Geology changes terrain,
structure damage, work goods, armour and encounter equipment. Flood and fire
history change current tiles, controls, testimony, stock, duties and cache
clues. Production changes markets and institutional services; disruption changes
contracts, patrol pressure, voyage risk and prices. Historic disputes produce
opposed accounts, obligations, named physical evidence and route consequences.
The eight local work accounts are joined by four travelling material interests
with two embodied witnesses each. Their daily production uses the same bounded
market reducer. Delivering a real dependency lot earns separate network trust;
that trust can open one safer connected route and shelter at the cost of a
persistent obligation. No distant individual is simulated between actions.

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

The eight primary regional lines apply that chain differently: Hearthford changes
sluice access and mill obligations; Greywash chooses delayed safety or a tide
window around salvage; Greenwold changes the burn's smoke and patrol ecology;
Whitecairn changes warning bells, quarry exposure, and vertical pursuit;
Dunmire assigns ground between water and peat work; Rillscar settles a bridge
claim; Marlbank balances kiln and seed bed; Frostmere marks a winter channel. A
finite existing actor is assigned to the opened material dispute rather than a
new enemy being spawned. Four second undertakings deepen the original regions,
and eight later-visit aftermath lines consume the actual ending configuration,
for 20 substantial regional lines. Five cross-region arcs compare Working
Marks, Banks That Hold, Soundings and Spans, Scars Kept in Use, and Refuges at
Low Water through physical records. Their eleven endings persistently allocate
route risk, household surety, market pressure, institutional obligation or
local authority.

The one atomic format-7 JSON save contains the current household, physical
items and orientations, regional exploration and changes, contacts, markets,
enemies, objectives, travel, sparse reactions, institutions, rivals, vessel
damage and bounded significant history. Format-6 saves migrate deterministically
without repainting their geography, moving items, restoring losses or changing
settled quests. A returned region therefore changes later expeditions without
any offline simulation.
