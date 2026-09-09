# Jomon Product Constraints

Jomon is a turn-based roguelike about sending one member of a persistent vessel-household into dangerous medieval settlements and wilderness. A courier trades, investigates, negotiates, and fights under pressure, then returns—or fails to return—with consequences that reshape Jomon, its people, and the local world.

The current product is a fullscreen Python 3.11 terminal game using the standard-library `curses` module. Its eight destinations retain Hearthford, Greywash's tidal coast, Greenwold's open forest, and Whitecairn's limestone uplands, alongside the four frontier families described below. Travel begins from physical Jomon, whose tavern contains visible household members and bounded recruitable visitors. Couriers organise physically shaped items in a rotatable pack grid, wear armour across six readable body locations, and face terrain, weather, ranged fire, and goal-directed enemies whose plans depend on what they can see and hear.

The preceding four-region milestone deepened that established world in place.
The active systemic expansion adds Dunmire, Rillscar, Marlbank and Frostmere
without replacing existing geography. Jomon has three aligned functional
decks plus a dedicated walkable tavern; its chart is now a persistent 16-node
connected route map; and
named adults follow deterministic action-clock schedules. Packing has live
placement previews, a spatial paper doll, optional mouse input, pinning,
transactional auto-packing, and bulk actions. A visible calendar and seasonal
effects alter existing weather, routes, work, stock, and exposure. All of
these features are direct additions to the existing state and curses
interface, not generic UI, schedule, travel, or social-simulation frameworks.

The preceding content milestone deepened the original four regions in place. Each has
one directly authored, persistent regional questline; progress in at least two
regions opens one grounded cross-region material dispute. Retained equipment,
discoveries, recruits, terrain, enemies, controls, and physical items have
observable action hooks. Production encounters use the existing bounded
regional composer, while authored quest duties and seeded elite alternatives
remain explicit. The expansion retains these lines and adds frontier material
disputes without a generic quest, dialogue, planning, faction, or encounter
language. Additional story depth and content targets remain acceptance gates,
not completed claims.

The permanent constraints are:

- seeded, inspectable determinism and action-driven time;
- one directly controlled courier at a time, with a persistent household and physical Jomon;
- material cargo/resources and bounded local markets;
- injury, permanent death, succession, and contextual defeat;
- compact causal feedback and local offline persistence;
- keyboard-first ASCII play with accessible non-colour cues;
- original low-mysticism medieval content, with no copied lore, assets, or mechanics; and
- no sexual violence, slavery, torture, or harm/endangerment of children in authored or generated content.

Technical boundaries are equally permanent: new gameplay must remain local, deterministic, testable without initializing `curses`, and proportionate to its exercised state. The game targets Linux, macOS terminals, and Windows through WSL; uses Python's standard library only; and runs with `python -m jomon`. Do not add a web runtime, server, account, telemetry, database, multiplayer authority, wall-clock simulation, plugin framework, or public modding promise.

Prefer one primary game state, small dataclasses with actual gameplay state, direct action functions, one turn-resolution path, one atomic JSON save, authored content tables, small causal generators, and bounded significant history. Do not rebuild the browser-era event, replay, fidelity, provenance, dialogue, economy, or frontier frameworks in Python.

Depth should continue to come from coherent geography, sight and sound,
readable enemy intent, elevation, finite supplies, spatial packing,
armour/terrain/equipment/passive/relic interactions, tempting visible treasure,
and persistent material consequences. Each region has a dedicated bounded
generator and authored encounter compositions; these are product features,
not a universal biome, planning, physics, fluid, economy, or content framework.

Items carried by a courier occupy a readied/body slot or a cell in the 10×6
pack; Jomon storage is a bounded 18×10 locker. Shape and weight remain separate
constraints. Regional urgency comes from visible action-clock processes—tide,
weather, burn wind, quarry instability, patrol movement, alarm, and material
timing—rather than one hidden or real-time deadline. Severe ranged attacks must
provide a readable aim, lane, cover, or setup opportunity before impact.

New worlds must not make system discovery a departure prerequisite. One
deterministic adult begins on courier watch at the gangplank, and every
eligible courier receives a one-time, role-appropriate basic weapon, secondary
item, and modest working armour. Issued equipment remains ordinary physical
state subject to weight, damage, deliberate replacement, dropping, and loss;
it is not silently recreated after issue.

The immediate first departure remains a product constraint: a new-world
bargemaster can interact at the gangplank and enter Hearthford without first
opening the tavern, inventory, support, chart, or equipment interfaces.

The active systemic-world milestone expands to eight regional destinations,
bounded causal histories, sparse material reactions, institutions, physical
equipment fittings, regional stories and tactical voyage encounters. Existing
content and immediate departure remain intact; optional vessel work must not
become a preparation checklist. The acceptance ledger and measured status are
in `docs/systemic-world-milestone.md`; targets are not completed features.

This milestone explicitly excludes infinite or offline world
simulation, exhaustive needs or anatomy, unrestricted autonomous death,
generic schedule/GOAP/encounter/quest/narrative languages, real-time travel,
graphical tiles, generic spellcasting, and unbounded regional or onboard
content. Voyage events continue to reuse ordinary people, cargo, inventory,
and combat rules in twelve sporadic authored families, eight offering physical
deck play. Counted repairs, meals, pumping and workshop fittings are optional;
their normal-play depth and balance remain under assessment.

Action-clock social life is intentionally bounded: named adults travel between
authored work, berth, watch, treatment, and tavern positions; schedule
boundaries may produce a shared meal, assistance, or an argument grounded in a
recorded relationship or memory. Routine catch-up cannot kill anyone. A
permanent off-screen death still requires an explicit, previously signalled
crisis accepted by the player.

`LORE.md` owns setting, tone, and content canon. `TODO.md` owns current scope and frozen work. The causal chain is documented in [`docs/causal-generation-and-loop.md`](docs/causal-generation-and-loop.md); browser history is recorded under [`docs/archive/`](docs/archive/).
