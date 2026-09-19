# Living world rules — developer spoilers

The game intentionally withholds names/descriptions until observation and survey.
The engine does not invent rules per encounter or alter them to punish a player.
`src/catalog.lua` contains the authored descriptions; the implementation is in
`content.lua`, `ecology.lua`, `signals.lua`, `fieldwork.lua` and `blasts.lua`.

## Geometry and actor limits

Three growth archetypes, four fauna archetypes, four site archetypes and five ruin
archetypes currently exist. Generated positions, stock, phase and room geometry vary
by seed. Caps are 128 living/dead-pending growth records, 64 fauna, 48 sites, 32 ruin
records, 48 recent signals, and 12 recent blast effects. Generation uses lower targets.
Dead growth/creature records are periodically retired after a short delay; already
recorded notes remain. New offspring are not updated again in the same fauna loop.

Creatures are small rectangular entities with local deterministic steering, not
full settler BFS. They cannot move through solid terrain; leeches require water.
Ground animals can fall and attempt small steps. They can fail to find an existing
long route, become stranded or starve. Sentinel movement stays near its origin.
There is no species-scale population optimiser or rescue spawner.

## Growth and food web

All growth stores integer edible biomass and root water. Adjacent liquid may be
absorbed up to capacity six, on a phased 120-tick schedule. Water converts to biomass
on a phased 240-tick schedule, declared as a crop-production recipe in the ledgers.
It is not a scientific atom-by-atom conversion. Mature supported plants can transfer
some stored food/water into offspring on phased 960-tick checks if space and capacity
permit. Burial, lost support or lava kills plants and releases their stored contents
as physical piles. Salvaging does likewise.

- **Veil bloom:** established growth periodically emits harmful short-range exposure
  and a bloom disturbance signal. Barriers and wards can interrupt exposure.
- **Glass reed:** condenses adjacent steam into water. Growth still competes for
  water and biomass with the rest of the food web.
- **Iron thorn:** periodically hardens adjacent sand into rock; contact injures people.
- **Lantern grazer:** eats growth biomass and ripe colony crops; supplied animals
  can reproduce by transferring food to offspring. They are not automatically hostile.
- **Silt leech:** needs actual water, converts some to food through an explicit recipe,
  and can attack nearby settlers. Draining strands it; it cannot swim through rock.
- **Hollow stalker:** hunts grazers, notices nearby settlers and follows disturbance
  signals. Food is transferred or consumed, not created by predation.
- **Vault sentinel:** dormant until disturbance, territorial after waking. It does not
  require the organic food loop and does not reproduce. It remains confined locally.

Wild fauna metabolise stored food; starved animals lose health. Actual inventory food
is distinct from potential wild biomass in metrics. Animals killed with biomass
remaining drop that biomass as ration piles: this prototype has no butchery, toxicity,
cooking or equipment chain. Hostile contact includes a simple settler counterattack;
there are no projectiles, weapons, armour, limbs or sophisticated combat tactics.

## Sites and ruins

Caches contain finite resources; salvage drops them for hauling. Nurseries may use
stored biomass to release a grazer and a growth when opened; they cannot manufacture
extra energy for offspring. Resonators activate on disturbance and emit later calls.
Vents change nearby liquid to steam without creating water. Salvaging a site ends
its subsequent activity. A blast releases stock and can destroy its activity.

Cistern, ossuary, archive, forge and nursery compounds are baked into the actual
material grid. They have interior divisions, shelves/pillars or pockets, finite loot
and possible occupants. They need not be connected or accessible without excavation.
The starting chamber is protected from placement overlap, not from subsequent fluids.

## Signals, knowledge and wards

Mining, blasts, blooms and resonators emit location/tick/strength signals. A bounded
recent history drives wake-up/attraction. This is not acoustic wave propagation or a
line-of-sight sound solver. A sealed wall can block a creature's movement even when a
nearby signal alerts it. Site/creature observation requires proximity and clear view.

Completed surveys unlock authored facts by archetype, not every individual actor.
F4 shows only observed/surveyed records. The map and debug biome view remain visible;
this is not fog of war or a claim that the ecosystem is mathematically unknowable.
The journal records actual transitions, not a fabricated overarching narrative.

Wards need supported, dry-enough footprints, enablement and delivered water. Within
12 Manhattan cells they discourage hostile steering and suppress hostile contact /
growth exposure. They consume water every 120 ticks. Their radius is an abstract
field and is not blocked by intervening walls. They do not stop environmental deaths.

## Accounting and persistence

Water includes free water/ice/steam, held/piled water, farm/ward tanks, root stores and
site stores. Food accounting includes rations, wild biomass, fauna reserves and cache
stock, plus declared production/consumption. Mineral accounting includes materials,
items, delivered supplies, constructed costs and ruin stock. Blast/demolition losses
are explicit. No ecological rule updates these values off-tick.

Full saves/checkpoints retain health, positions, phases, fuse deadlines, signal
history, notes, work policies, owners and directives. Map templates retain a starting
placement/stock/crew specification, not live colony state. Current-template export
resets health, activation and knowledge on adoption as a new expedition.

## Explicit limitations

No genetics, weather, universal chemistry, gas pressure, temperature diffusion,
realistic blast impulse, structural integrity, corpse physics, migration director,
procedural narrative text, diplomacy or complete industrial production graph. The
challenge comes from interactions already implemented, not hidden random punishments.
