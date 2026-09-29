# ROAG — Consolidated Game Design and Architecture Brief

**Project:** ROAG  
**Pronunciation:** “Rogue”  
**Engine:** LÖVE2D  
**Language:** Lua

This document is the current design baseline for ROAG.

It is intended to guide future implementation work incrementally. Individual mechanics may be tuned through playtesting, but future development should preserve the core principles and decisions below unless explicitly revised.

---

# 1. Product Identity

ROAG is a **turn-based, combat-heavy, build-heavy roguelite** centered on:

- tactical movement and positioning,
- destructible and systemic environments,
- scavenging enemy bodies,
- physically rebuilding the player's own body,
- temporary, deteriorating power,
- highly variable builds,
- compact procedural levels,
- branching route selection,
- and permanent progression across runs.

The game should be:

> **easy to understand moment-to-moment, difficult to master systemically.**

Depth should emerge from interactions between relatively understandable mechanics rather than from puzzle-like complexity.

ROAG is not intended to resemble a logic puzzle game.

Combat and movement should be immediately playable, closer in cognitive character to classic top-down action/adventure games, while the underlying build and simulation systems become increasingly deep.

---

# 2. Core Player Fantasy

Every run begins with a **procedurally generated nobody**.

The player's body is an unstable reconstruction assembled from:

- biological remnants,
- machine components,
- cybernetics,
- scavenged organs,
- derelict robotic systems,
- and eventually increasingly strange anatomy.

The player enters hostile environments, destroys or disables enemies, scavenges their bodies, and gradually transforms themselves into an increasingly powerful and unusual organism/machine.

The defining question of a run should become:

> **What did my body become this time?**

Late-run builds should be capable of becoming extremely powerful, strange, and mechanically expressive.

However, power is not permanent.

Parts:

- suffer damage,
- degrade through use,
- break,
- become critical,
- may be severed or destroyed,
- and eventually need replacement.

A powerful build is therefore something the player continually maintains and evolves rather than permanently completes.

---

# 3. Design Pillars

## 3.1 Body as build

The player's physical body is the primary equipment/build system.

Traditional abstract equipment should be minimized when an effect can instead be represented physically.

Instead of:

> +20% ranged damage accessory

prefer:

> a targeting optic, neural processor, stabilizer arm, recoil-dampening shoulder assembly, etc.

---

## 3.2 Enemies obey the same physical rules

Enemy abilities should generally originate from their actual installed anatomy/components.

If an enemy:

- fires missiles,
- sees through darkness,
- produces electricity,
- jumps long distances,
- projects shields,
- emits gas,

there should normally be some physical component responsible.

That component can potentially:

- be damaged,
- disabled,
- destroyed,
- preserved through careful combat,
- salvaged,
- researched,
- and installed on the player.

Enemy design and player build design should therefore share a common underlying body/component model.

---

## 3.3 Power is temporary

The game should continuously create tension between:

- obtaining powerful parts,
- damaging those parts through use,
- protecting important systems,
- replacing failing systems,
- inventory limitations,
- weight,
- resource consumption,
- repair cost,
- and new opportunities.

Maintenance should create decisions, not repetitive chores.

---

## 3.4 Systemic environments

ROAG should favor a relatively small collection of reusable environmental primitives that interact broadly.

Final-game targets include:

- destructible terrain,
- material-dependent destruction,
- fire,
- spreading fire,
- liquids,
- electricity,
- conductive water/metal,
- gases,
- pressure/contained gas behavior where useful,
- knockback,
- hazards,
- doors that can be hacked/broken/powered,
- machinery that can be powered/depowered,
- destructible cover,
- structural collapse,
- environmental damage to corpses/salvage.

Enemies should be capable of participating in these systems too.

Avoid implementing every interaction as an isolated scripted exception.

---

## 3.5 Deep systems, readable interface

The simulation may become complicated.

The moment-to-moment interface should not.

Information should be surfaced when relevant instead of permanently covering the screen with numbers.

---

# 4. Setting and Atmosphere

The current direction is **post-apocalyptic high science fiction** with a wide range of environments.

Atmospheric inspirations include aspects of:

- Caves of Qud,
- Dune,
- Night City / Cyberpunk,
- Blade Runner,
- Neo-Tokyo,
- Ghost in the Shell.

The world should feel:

- immersive,
- strange,
- technologically layered,
- partially ruined,
- biologically and mechanically hybrid,
- and geographically coherent.

Narrative/lore is secondary during early development.

Gameplay systems, consistency, extensibility, content tooling, and systemic interaction take priority.

---

# 5. Run Structure

A successful base-game run should take approximately:

> **up to ~90 minutes**

excluding New Game Plus.

Runs contain many compact levels, typically around:

> **5–15 minutes each**

The overall structure should combine ideas from:

- Dead Cells,
- Slay the Spire,
- Binding of Isaac,
- Spelunky.

The player advances through a **branching world graph**.

Example conceptual structure:

```text
Start
  ↓
Biome
 ↙   ↘
A     B
↓ ↘ ↙ ↓
C  D  E
 \ | /
 Boss
   ↓
Next region...
```

The player cannot visit every route.

Route choice should matter based on:

- current body condition,
- desired salvage,
- shops/services,
- biome hazards,
- available research,
- known rewards,
- permanent traversal abilities,
- and risk.

---

# 6. Boss Structure

A normal run should contain roughly:

> **3–4 major bosses**

with possible elites and minibosses between them.

Bosses should primarily follow the same body/component rules as normal entities.

Bosses can contain:

- locomotion systems,
- weapon systems,
- sensors,
- defensive systems,
- reactors,
- cooling,
- organs,
- redundant systems,
- support components.

Destroying specific systems should meaningfully alter the fight.

Killing the core/body eventually ends the fight without necessarily requiring every subsystem to be destroyed.

Boss salvage should create difficult inventory decisions.

Boss components may be particularly run-defining.

---

# 7. New Game Plus

After defeating the final boss, the run does **not** immediately end.

The player continues into New Game Plus with:

> **the same current body and build.**

This allows extreme builds to continue evolving beyond the intended main-run power curve.

NG+ difficulty should increase primarily through:

- harder combinations,
- additional system interactions,
- hostile environmental combinations,
- stronger enemy configurations,
- harder boss configurations,
- unusual materials,
- expanded route possibilities,
- additional attacks,
- and more difficult encounter composition.

Avoid relying mainly on enormous HP/damage inflation.

---

# 8. Player Anatomy

The normal body begins with a comprehensible fixed anatomy.

Baseline conceptual slots:

```text
Head
Torso / Core
Left Arm
Right Arm
Left Leg
Right Leg
Internal systems
```

This base structure provides readability.

However, exceptional components may rewrite the body's topology.

Possible late-game bodies may include:

- additional arms,
- tank treads,
- spider-like lower bodies,
- multiple hearts,
- multiple internal cores,
- no conventional head,
- distributed camera/sensor systems,
- enormous weapon limbs,
- robotic/organic hybrids,
- exotic locomotion,
- strange boss-derived components.

The fixed body is therefore the **default anatomical grammar**, not a permanent restriction.

---

# 9. Component Properties

A component may define properties such as:

- slot/topology requirements,
- integrity,
- condition,
- weight,
- resource usage,
- power requirements,
- ammo requirements,
- biological requirements,
- effects,
- abilities,
- compatibility,
- salvage value,
- research state,
- degradation characteristics,
- material composition.

Not every component needs every property.

Avoid meaningless stat complexity.

---

# 10. Component Condition and Injury

Body systems should support several damage outcomes.

Condition may conceptually progress through states such as:

```text
Healthy
↓
Damaged
↓
Critical
↓
Broken / Destroyed / Severed
```

Specific behavior depends on the component.

Examples:

- damaged leg → reduced mobility,
- destroyed optic → reduced information/aim capability,
- broken arm → weapon unavailable,
- reactor failure → installed systems shut down,
- catastrophic reactor failure → explosion,
- organic injury → bleeding or other biological consequences.

The game may use:

- general health damage,
- component degradation,
- localized injury,
- catastrophic destruction.

Catastrophic injury should be memorable but not so frequent that every encounter becomes tedious reconstruction management.

---

# 11. Degradation

Primary degradation sources:

1. **Usage**
2. **Incoming damage**

Background passive decay should be relatively minor if used at all.

Examples:

- firing a weapon degrades it,
- dashing stresses locomotion,
- armor degrades when absorbing damage,
- biological systems may deteriorate differently.

Powerful components should not necessarily be permanent possessions.

---

# 12. Repair Philosophy

Replacement should remain central.

## Field repair

Primarily:

- stabilize,
- prevent worsening,
- restore limited functionality,
- temporarily patch.

## Reconstruction facility

May provide:

- repair,
- stabilization,
- diagnostics,
- component replacement,
- modification.

Repair should have meaningful:

- cost,
- limitations,
- diminishing feasibility,
- or maximum recoverable condition.

Rare components may possess self-repair capabilities.

---

# 13. Combat Model

ROAG remains **turn-based**.

Fundamental rule:

> **Player performs one meaningful action → world responds.**

Avoid introducing an unnecessarily complicated action-point scheduler unless future testing demonstrates a need.

Typical actions:

- move,
- attack,
- use installed ability,
- use consumable,
- interact,
- wait,
- perform contextual action.

---

# 14. Movement and Aiming

Movement:

> **8-directional**

Aiming:

> **mouse-controlled**

The player's facing/direction should generally be inferred from:

- aim,
- movement,
- attack direction.

Turning the cursor should not consume turns.

Actually firing/acting does.

Mouse aiming is important because ROAG should support a wide variety of weapons whose effectiveness may depend on:

- line of sight,
- range,
- angle,
- cover,
- target body region,
- terrain,
- projectile behavior.

No click-to-move requirement for the initial design.

---

# 15. Body-Part Targeting

Normal attacks should remain quick.

Default attacks may resolve body-region impact based on:

- angle,
- side,
- exposure,
- weapon,
- accuracy,
- target posture/configuration.

The player should additionally have an **optional precision targeting mode** allowing intentional targeting of components such as:

- weapon arm,
- leg,
- optic,
- organ,
- power source,
- armor plate.

Position should also matter.

Attacking from a particular side may naturally increase the chance of damaging parts exposed on that side.

---

# 16. Enemy Injury

Enemies obey the same physical damage principles as the player.

An enemy does not necessarily die because one important component is destroyed.

Examples:

- gun arm destroyed → changes strategy,
- locomotion damaged → becomes slower,
- sensor destroyed → perception worsens,
- shield generator destroyed → loses defense,
- core compromised → loses powered abilities,
- limb severed → changes available attacks.

This is a major source of systemic enemy reactions.

---

# 17. Weapon Diversity

Melee and ranged builds should both be viable.

Different weapon/body systems should have mechanically distinct interactions.

Possible future examples:

- claws,
- hydraulic fists,
- blades,
- tails,
- electrical contact weapons,
- cannons,
- rifles,
- beam emitters,
- grenade systems,
- biological acid projection,
- area-control organs,
- shields.

Avoid weapon diversity that is merely:

> same attack + different damage number.

---

# 18. Resource Families

Components may consume resources differently.

Examples:

- conventional ammo,
- stored power,
- biological matter,
- self-integrity,
- charges,
- heat/cooling constraints.

Do not create one unique meter per item.

Prefer a small, understandable family of reusable resource concepts.

---

# 19. Enemy AI Priorities

In descending importance:

1. **Systemic reaction**
2. **Normal AI behavior**
3. **Build/tactic adaptation**
4. **Procedural individual variation**

Systemic state should naturally alter behavior.

AI may also:

- flank,
- seek cover,
- retreat,
- cooperate,
- protect damaged systems,
- flee hazards,
- call reinforcements,
- respond to alarms.

Adaptation should be meaningful but should not feel like omniscient anti-player counter-building.

---

# 20. Enemy Bestiary

Enemies should primarily be **authored recognizable archetypes**, not randomly generated blobs.

A particular enemy type should have an understandable identity.

Individual instances may still vary in:

- condition,
- installed parts,
- equipment configuration,
- elite modifications,
- damage,
- rare variants.

Encounter density can vary substantially.

Some biomes may contain:

- small tactical encounters,

while others may support:

- hordes,
- swarms,
- numerous weaker enemies.

---

# 21. Factions and Ecology

ROAG should eventually include lightweight mechanical factions/ecologies.

Possible categories include:

- machines,
- scavengers,
- wildlife,
- mutants,
- corporate remnants,
- other future groups.

They do not all need to be allied against the player.

They may:

- fight each other,
- hunt each other,
- respond differently to hazards,
- have different salvage,
- use biome-specific behaviors.

This should add systemic life without requiring a full diplomacy simulation.

---

# 22. Enemy Spawning and Activation

Use a hybrid model.

Some enemies:

- exist as generated occupants.

Others may:

- arrive after alarms,
- emerge from nests,
- be deployed by machines,
- come from elevators,
- arrive through vents,
- appear as reinforcements.

Hidden implementation-level spawn/activation regions may be used for:

- optimization,
- population control,
- believable reinforcement.

They should not normally feel like arbitrary magical spawning to the player.

Cleared areas generally remain cleared unless a world/systemic reason creates new enemies.

---

# 23. Elites

Elite enemies should normally be:

> **interesting body configurations**

rather than simply normal enemies with huge HP multipliers.

Examples:

- redundant hearts,
- shield limbs,
- reinforced plating,
- unusual optics,
- additional weapons,
- cloaking systems,
- exotic locomotion.

This simultaneously makes them:

- harder to fight,
- visually distinct,
- mechanically different,
- desirable salvage targets.

---

# 24. Inventory

ROAG uses **one shared inventory**.

Body salvage and ordinary consumables compete for space.

This is intentional.

Inventory pressure is part of the game.

---

# 25. Spatial Inventory

The target design is Resident Evil-like spatial packing.

Items occupy different shapes/sizes.

Examples:

- ammo → small,
- medical item → small/medium,
- severed arm → long,
- reactor → large square,
- large weapon component → long/heavy.

Rotation may be allowed where sensible.

Initial capacity should be around the conceptual equivalent of:

> **~10 normal items**

but the actual grid dimensions should be determined through prototyping rather than treating “10” as a rigid slot count.

---

# 26. Weight and Encumbrance

Spatial fit and total mass are separate constraints.

Encumbrance should use understandable discrete states.

Example:

```text
Light
Burdened
Heavy
Overloaded
```

Avoid tiny continuous percentage penalties.

Legs, chassis, body configuration, research, and upgrades may affect carrying thresholds.

---

# 27. Corpse Salvage

Enemy corpses physically persist where practical.

Looting should remain quick enough not to interrupt combat flow excessively.

The state of a corpse/component should matter.

Examples:

- fire ruins organic salvage,
- explosions damage fragile components,
- acid contaminates components,
- precision killing preserves desired anatomy.

This connects combat decisions directly to loot quality.

---

# 28. Reconstruction

Major body reconstruction occurs primarily:

> **between levels**

rather than constantly during combat.

This creates the loop:

```text
Fight
↓
Take damage
↓
Kill / salvage
↓
Choose what to carry
↓
Survive the remainder of the floor
↓
Reach reconstruction
↓
Rebuild body
↓
Choose next route
```

At reconstruction locations the player can freely:

- remove parts,
- install parts,
- compare parts,
- reorganize inventory,
- discard parts,
- repair/stabilize,
- use relevant services.

Do not impose an arbitrary limited number of swaps.

Constraints come from:

- inventory,
- compatibility,
- money,
- condition,
- research,
- component requirements.

---

# 29. Run Economy

Use **one primary run currency**.

It may be spent on:

- repair,
- body parts,
- ammunition,
- consumables,
- services,
- kiosk upgrades,
- augmentation,
- research services where applicable.

Avoid unnecessary currency proliferation.

Separate persistent research/progression resources may exist when clearly justified.

---

# 30. Shops and Services

Possible service types:

## Salvager

Buys/sells body components.

## Repair kiosk

Repairs/stabilizes parts.

## Augmentation kiosk

Improves or modifies parts.

## Supply vendor

Provides ammo, consumables, general supplies.

## Research terminal

Analyzes unknown technology/components.

## Rare / black-market merchant

Provides unusual, dangerous, cursed, experimental, or rare components.

Routes may communicate likely services ahead.

---

# 31. Charms, Boons, and Curses

Body construction is not the only build layer.

The run also includes **charms**.

Purchasing/equipping charms provides:

> **boons**

Characters may also acquire:

> **curses**

Body parts and charms/boons should both be meaningful parts of build construction.

They should not collapse into the same system.

---

# 32. Permanent Research Progression

ROAG is a roguelite with a **large, deep permanent research/progression tree**.

Research should create a strong feeling of long-term progress.

It may permanently unlock:

- player abilities,
- traversal capabilities,
- new reconstruction techniques,
- better identification,
- compatibility information,
- salvage capabilities,
- access methods,
- routes,
- world interactions,
- other future systems.

Research is not merely an encyclopedia.

It materially expands what future characters can do.

---

# 33. Permanent Traversal Abilities

Major traversal abilities belong permanently to the player/account.

For example, after permanently acquiring reinforced-wall breaching capability, future characters can use that interaction without needing to reacquire a specific tool every run.

These permanent abilities may unlock:

- new biome branches,
- secret shortcuts,
- hidden areas,
- valuable augmentation opportunities,
- unusual rewards,
- optional difficult routes.

---

# 34. Meta-Progression Philosophy

The body itself resets after death.

Permanent progression exists outside the body.

A new run begins as a new disposable nobody but benefits from the player's accumulated account-level progression.

Enemies do not become new species simply because research progressed.

The stable bestiary/world should remain independently authored.

---

# 35. Dead Previous Characters

Dead player characters may appear in later runs.

The game should retain enough information about old characters to recreate meaningful aspects of their final body.

They may appear as:

- remains,
- salvageable corpses,
- corrupted forms,
- hostile former characters.

Where possible, the encountered body should contain actual components from that historical build.

This should be mechanically meaningful, not merely cosmetic.

---

# 36. Permanent Unlock Sources

Permanent progression may come from:

- bosses,
- milestones,
- hidden areas,
- secrets,
- unusual discoveries,
- rare research objects,
- special encounters,
- difficult optional routes.

Exploration should therefore matter beyond immediate loot.

---

# 37. Normal Level Objective

The default objective of most floors is:

> **find and reach the exit alive.**

The player is not generally required to clear every enemy.

Combat is often optional risk/reward.

Reasons to fight include:

- salvage,
- currency,
- access,
- secrets,
- objectives,
- elite rewards,
- tactical necessity.

Some special floors may have alternate objectives.

---

# 38. Backtracking

Backtracking within a level is fully allowed.

Examples:

- see locked facility,
- discover power source elsewhere,
- activate power,
- return,
- enter facility,
- collect reward,
- leave.

Shortcuts can be created through:

- doors,
- wall destruction,
- elevators,
- machinery,
- collapses,
- hacking.

---

# 39. Secrets

Secrets are important.

Potential secret types:

- breakable walls,
- hidden tunnels,
- maintenance shafts,
- rare rooms,
- buried caches,
- research areas,
- previous-character remains,
- special merchants,
- hidden biome transitions,
- unusual traversal challenges.

Secrets should primarily reward:

- observation,
- experimentation,
- systemic knowledge.

Avoid relying heavily on pixel-hunting.

---

# 40. Biomes

ROAG should eventually contain a **deep corpus of mechanically distinct biomes**.

Biome distinction should not merely be visual.

Examples:

## Flooded area

- conductive water,
- electrical risk,
- corrosion,
- aquatic enemies.

## Industrial complex

- power systems,
- machines,
- gas,
- heavy salvage,
- reinforced materials.

## Organic overgrowth

- fire,
- toxins,
- regenerative biology,
- organic salvage.

## Megacity ruins

- hacking,
- buildings,
- vertical/structured spaces,
- ranged combat,
- cybernetic salvage.

Shared environmental primitives should produce biome identity naturally.

---

# 41. Level Generation

Different biome families may use different generation algorithms.

## Structured room-based biomes

Use a **Spelunky-like corpus of authored room templates**.

Pools may include:

- entrances,
- normal rooms,
- vertical connectors,
- special rooms,
- treasure rooms,
- merchant candidates,
- secret rooms,
- elites,
- landmarks,
- exits.

## Open biomes

May use:

- noise functions,
- Wave Function Collapse,
- cellular techniques,
- graph-constrained generation,
- other appropriate algorithms.

Generation approach should be chosen per biome.

---

# 42. World Coherence

High biome diversity is desirable.

However, transitions should feel as though areas belong to one physical world.

Prefer:

```text
Rooftop settlement
↓
Transit infrastructure
↓
Abandoned station
↓
Cooling complex
↓
Subterranean facility
```

over arbitrary disconnected biome roulette.

---

# 43. Materials

Materials should be first-class systemic content.

Potential properties include:

- hardness,
- structural strength,
- conductivity,
- flammability,
- permeability,
- corrosion resistance,
- fracture behavior,
- liquid interaction.

Terrain destruction depends on material.

Examples:

- glass breaks easily,
- wood burns,
- steel conducts,
- concrete requires heavy breaching,
- reinforced structures require stronger methods.

---

# 44. Map Destruction

Much of the environment should be alterable.

Potential interactions:

- breach weak walls,
- destroy cover,
- smash doors,
- collapse selected structures,
- destroy machines,
- burn vegetation,
- break glass.

The generator must preserve critical run progression.

Players should not casually make the level permanently unwinnable.

---

# 45. Automap and Information

Use an automap.

Principles:

- unexplored areas remain unknown,
- explored geometry remains mapped,
- discovered points of interest can appear,
- enemies/items are not automatically permanently revealed,
- sensors/research may improve information,
- special effects may interfere with information.

Free-look/panning should be available over appropriate explored/visible territory.

---

# 46. Route Information

Meta progression/research may improve information shown on route nodes.

Early:

```text
Unknown Industrial Sector
Possible service
Unknown threats
```

Later:

```text
Reclamation District

Likely:
• Mechanical salvage
• Repair services

Threats:
• Automated security
• Conductive coolant

Routes:
• Lower Arcology
• Waste Processing
```

Permanent progression can therefore improve strategic information in addition to direct power.

---

# 47. Visual Presentation

Preserve the current broad visual identity initially.

Priorities:

- pixel-art / low-resolution readability,
- existing sprites initially,
- strong silhouettes,
- limited controlled color,
- readable biome/material/status differentiation,
- dark sci-fi atmosphere,
- strong combat readability.

Do not prioritize replacing art during early system development.

Sprites can be replaced gradually later.

---

# 48. Animation and Juice

**Preserve the existing smooth movement, animation feel, responsiveness, and juice.**

The simulation remains turn-based.

Presentation can interpolate actions smoothly.

Potential effects:

- movement interpolation,
- responsive projectile travel,
- hit flashes,
- recoil,
- severed-part effects,
- explosions,
- environmental propagation,
- knockback,
- telegraph animation.

Animations must not make fast play feel sluggish.

Visual feedback may accelerate/catch up if the player inputs quickly.

---

# 49. HUD

Keep the normal HUD relatively restrained.

Immediately relevant information may include:

- health/general condition,
- critical component warnings,
- current weapon,
- relevant ammo/power/resource,
- selected ability,
- encumbrance state,
- currency,
- map,
- major statuses.

Do not permanently display every component's integrity.

Detailed information belongs in dedicated screens.

---

# 50. Body Interface

Create a dedicated full-screen body interface.

It should show the player's current physical configuration.

It must eventually handle altered anatomy/topology.

For each component, the player should be able to inspect:

- function,
- integrity,
- degradation,
- material,
- active ability,
- resource consumption,
- compatibility,
- positive effects,
- negative effects,
- research certainty.

---

# 51. Inventory Interface

Create a spatial inventory UI.

Capabilities should eventually include:

- drag/reposition,
- rotation where valid,
- size visualization,
- weight information,
- compare,
- discard,
- relevant contextual actions.

Installed body components are shown in the body interface, not merely as inventory clutter.

---

# 52. Precision Targeting Interface

Precision targeting should be optional.

The player can quickly inspect/select enemy body regions when needed.

Normal combat remains fast.

Avoid forcing a body-target menu before every attack.

---

# 53. Platform Scope

Initial target:

> **Desktop keyboard + mouse**

Gamepad/mobile support is not an early requirement.

Remappable controls can be added later.

---

# 54. Content Architecture

Content should be highly data-driven and separate from core game logic.

Potential categories:

```text
content/
  body_parts/
  weapons/
  enemies/
  charms/
  curses/
  materials/
  biomes/
  rooms/
  shops/
  research/
```

Exact format may vary by content type.

---

# 55. Stable Semantic IDs

Game content must use stable semantic identifiers.

Example pattern:

```text
part.arm.hydraulic_mk1
enemy.scavenger.rifleman
material.reinforced_concrete
biome.lower_arcology
charm.ballistic_feedback
```

Display names must not be used as internal identifiers.

---

# 56. Shared Action / Effect Vocabulary

Prefer reusable primitives.

Examples:

```text
spawn_projectile
trace_line
deal_damage
apply_force
ignite
emit_gas
conduct_electricity
consume_ammo
consume_power
damage_component
apply_status
modify_material
telegraph_area
spawn_entity
```

Content composes these mechanics.

Avoid:

```lua
if item.name == "Special Gun X" then
    ...
end
```

whenever a reusable rule can express the behavior.

Bespoke mechanics remain allowed when genuinely necessary.

---

# 57. Enemy Content Model

Enemies should be authored primarily from:

```text
identity
body topology
installed parts / part pools
AI package
faction
spawn constraints
salvage rules
```

Stats should preferably emerge from actual body/component configuration.

If an enemy has armor, there should generally be something physically providing it.

---

# 58. Save / Load

ROAG requires save/resume because a run may approach 90 minutes.

Initial serialization:

> **JSON**

Maintain separate conceptual data for:

1. current active run,
2. persistent account/meta progression,
3. retained dead-character records.

Default behavior:

- one authoritative active run,
- autosave/checkpoint behavior,
- quit and resume,
- death ends that run,
- avoid intentional manual save-scumming mechanics.

---

# 59. Determinism

Deterministic simulation/generation is a hard requirement.

Use:

- root run seed,
- deterministic derived RNG streams where appropriate,
- reproducible floors,
- reproducible content placements,
- reproducible loot,
- reproducible enemy configuration,
- reproducible relevant random decisions.

Benefits include:

- debugging,
- regression tests,
- seed sharing,
- challenge runs,
- daily runs,
- speedrunning,
- bad-generation reproduction.

---

# 60. Speedrunning Support

Deterministic runs make later speedrunning support practical.

A run timer should eventually be supportable.

Timing rules can be specified later.

Do not allow early architectural decisions to make deterministic challenge/speedrun modes impossible.

---

# 61. Off-Screen Simulation

Prioritize responsiveness.

Nearby/important entities may receive full simulation.

Distant entities may:

- sleep,
- receive simplified updates,
- use abstract state,
- activate upon relevant events.

Remote systems may still react to:

- alarms,
- faction reinforcement,
- world triggers.

Further optimization should only be added when needed.

---

# 62. Generation Inspector

Generation tooling is a first-class development feature.

Build upon the existing sprite editor/view tooling where practical.

Potential features:

- choose biome,
- set/randomize seed,
- regenerate,
- zoom,
- pan,
- inspect tiles,
- inspect materials,
- inspect room/template IDs,
- show connectivity,
- show inaccessible regions,
- display enemy placement,
- display invisible spawn/activation regions,
- display exits,
- display kiosks,
- display shops,
- display charms/boons,
- display loot,
- display hazards,
- display secrets,
- display traversal locks,
- display reward distribution.

Use switchable overlays.

---

# 63. Bespoke Level Editor

A bespoke level/room editor is desirable.

Especially useful for:

- authored room chunks,
- Spelunky-style template corpora,
- landmarks,
- secret rooms,
- encounter spaces,
- boss arenas.

Do not require users to manually edit cumbersome raw coordinates where good editing tools can make authoring safer.

---

# 64. Batch Generation Analysis

Developer tooling should eventually support generating:

- hundreds,
- thousands,
- or more maps

headlessly.

Report useful statistics such as:

- reachability failures,
- exit distance,
- path length,
- reward density,
- enemy density,
- kiosk distribution,
- secret frequency,
- room frequency,
- inaccessible areas.

This allows generator tuning based on actual distributions.

---

# 65. Testing

Strong automated testing should be introduced early.

Important contracts include:

- content IDs resolve,
- references resolve,
- component topology is valid,
- body mutations preserve invariants,
- effects reference valid actions,
- materials validate,
- biome definitions resolve,
- room templates validate,
- generated required paths remain reachable,
- services/rewards spawn legally,
- deterministic seeds reproduce behavior,
- save/load round-trips preserve state,
- damage/injury rules remain valid,
- inventory rules remain valid,
- simulation does not depend upon rendering.

---

# 66. Headless Mode

Core simulation/generation should be usable without graphical rendering where practical.

This supports:

- tests,
- generation analysis,
- deterministic simulations,
- automated balance experiments,
- debugging.

Rendering should observe simulation state rather than secretly define gameplay rules.

---

# 67. Modding

Public third-party mod support is **not a requirement**.

Data-driven architecture exists to:

- make first-party development easier,
- improve maintainability,
- support validation,
- enable rapid content expansion.

Do not spend meaningful early development effort guaranteeing a stable mod API.

---

# 68. Migration Policy

The existing prototype may be changed aggressively.

Current game-specific systems are **not sacred**.

Systems that may be replaced include:

- existing classes,
- current enemies,
- current objective scoring,
- existing boon/curse implementation,
- current shop,
- current boss,
- existing content assumptions,
- monolithic state architecture.

However:

> **The game must remain LÖVE2D + Lua.**

---

# 69. Presentation Preservation

During migration:

**Do not casually destroy the current game's feel.**

Preserve:

- smooth movement,
- interpolation,
- stylized animation,
- responsiveness,
- visual juice,
- existing usable sprite presentation,
- useful rendering behavior.

Gameplay architecture may be replaced underneath this presentation.

---

# 70. README Constraint

For current development:

> **Do not edit `README.md`.**

The sole exception is:

> the `## Controls` section, and only when controls genuinely need updating.

Do not rewrite:

- project description,
- screenshots,
- lore,
- badges,
- installation text,
- roadmap,
- other README sections

unless this restriction is explicitly changed later.

---

# 71. Implementation Philosophy

Implementation must proceed in **small, auditable tranches**.

Each tranche should:

1. inspect the current repository state,
2. preserve unrelated user changes,
3. define bounded goals,
4. implement only that scope,
5. add/adjust relevant tests,
6. run validation,
7. verify the game still boots,
8. report exactly what changed,
9. identify remaining work without automatically beginning it.

Avoid giant rewrites.

Avoid prematurely implementing the complete design.

---

# 72. First Vertical-Slice Target

The first major architectural/playable milestone should prove:

> **An enemy's ability comes from its real body component; the player can kill the enemy, salvage that component, carry it, reach the exit, install it, and then use that same capability themselves.**

That is the smallest meaningful demonstration of ROAG's defining idea.

---

# 73. Foundation V1 Target

Foundation work should ultimately establish:

- modular Lua architecture,
- deterministic RNG,
- semantic content IDs,
- external data-driven content,
- content validation,
- automated tests,
- headless testable simulation,
- JSON save/load foundation,
- material definitions,
- component/body model,
- normal fixed anatomy,
- integrity/damage model,
- spatial inventory foundation,
- one generated biome,
- generation inspector foundation,
- room/template infrastructure where applicable,
- one enemy built using the common body system,
- one usable ranged body component,
- corpse salvage,
- between-floor reconstruction,
- exit-based progression.

Do not attempt the entire final game during Foundation V1.

---

# 74. Features Deliberately Deferred From Foundation V1

Examples:

- enormous research tree,
- complete biome corpus,
- full fire simulation,
- full liquid simulation,
- full electrical propagation,
- full gas system,
- advanced faction simulation,
- sophisticated adaptive AI,
- dozens of enemies,
- dozens of body parts,
- finished art replacement,
- complete bosses,
- full NG+,
- advanced speedrunning UI.

Architecture should anticipate these without implementing them prematurely.

---

# 75. Success Criterion

ROAG succeeds if players eventually describe it along the lines of:

> **“The combat and builds are addictive and every run turns my character into something completely different.”**

and:

> **“Enemies behave according to what they physically are and what has happened to them, rather than just following canned RPG rules.”**

The game's complexity should emerge through combinations between understandable systems.

The player should be able to become extremely powerful.

That power should always feel:

- earned,
- assembled,
- temporary,
- vulnerable,
- and unique to that run.
