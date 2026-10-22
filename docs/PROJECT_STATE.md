# Ithica: project state and agent reference

**Last reconciled: 2026-10-22.** This is the master source of truth for the implemented game, its rules and limits, data ownership and verification. Read it before changing the project. Update this document alongside affected behavior, content, formats or tooling. Source code and authored configuration implement behavior; catalog ideas remain proposals.

**Ithica is one playable C# / MonoGame lunar salvage colony game.** Start with one colonist, rescue people through controlled expeditions, bring the same residents home, recover useful components and build improvements that change later trips. The Moon remains recognizable; names and explanations stay sparse. The old fantasy RPG and Python prototypes remain explicit compatibility/regression references.

**The standalone fantasy RPG is now Vagabonds**, developed in the independent sibling repository `../vagabonds` ([repository](https://github.com/gongahkia/vagabonds)). It was extracted from Ithica commit `c87d480` with its own RPG application, projects/tests, complete original assets, catalog, mappings and authoring tools. Its normal launcher is `vagabonds.sh`, and its default save is `~/.vagabonds/campaign.json`. Ithica's lunar gameplay, save and existing `rpg.sh` compatibility path are preserved. These repositories do not reference or automatically synchronize each other's code/assets; historical `ithica-*` filenames and original IDs in the copied artwork remain intentional provenance.

## Contents

- [Current scope](#current-scope)
- [Run and inspect](#run-and-inspect)
- [The integrated loop](#the-integrated-loop)
- [Time contract](#time-contract)
- [People supplies and recovery](#people-supplies-and-recovery)
- [Colony systems](#colony-systems)
- [Extraction and work assignments](#extraction-and-work-assignments)
- [Station pressure and community](#station-pressure-and-community)
- [Sites generation and hazards](#sites-generation-and-hazards)
- [Field roles and encounters](#field-roles-and-encounters)
- [Controls](#controls)
- [Ownership and architecture](#ownership-and-architecture)
- [Saves and compatibility](#saves-and-compatibility)
- [Sprites and authoring](#sprites-and-authoring)
- [Verification and performance](#verification-and-performance)
- [Boundaries and future work](#boundaries-and-future-work)
- [Design direction (planned)](#design-direction-planned)
- [Maintaining this reference](#maintaining-this-reference)
- [Detailed references](#detailed-references)

## Current scope

| Item | Implemented state |
| --- | --- |
| Product / runtime | Ithica; C# / .NET 10, MonoGame DesktopGL 3.8.6 |
| Normal executable | One window and campaign, with colony, preparation, expedition and arrival phases |
| Population | One starting colonist; rescue-driven recruitment; parties of 1–4 actual residents |
| Clock | Continuous unpaused colony time; both sides wait at field decisions; accepted actions advance shared ticks |
| Shared state | Home stock, explicit carried supplies/cargo, resident conditions, progression, persistent sites and one save |
| Environments | Built colony plus 2 generated site families: lunar surface and compartments below it |
| Site registry | Initial distress site; salvage after a rescue; deeper signal after powered suit service; additional distress sites arrive over time |
| Enemies | 3 lunar stat/name variants using 2 appearances; 2 encounter templates |
| Field abilities | 4 roles × 3 skills, plus guard, medical kit and retreat |
| Colony modules | 14 definitions, 13 buildable; RTG is starter-only; suit service/fabricator each need a recovered component |
| Extraction | Pump discovery → 3 surveyable station deposits → powered pressure/refuge extensions → 3 finite layers per deposit → deep ore, reinforcement and component fabrication |
| Work assignments | Six priorities per resident, five shift options, protected rest and cautious/worn drilling policies; actual worker recovery/carrying |
| Station pressure | Usage/exposure wear, three repairable fault types, paid resident service jobs and prevention deadlines |
| Community | Persistent preferences, bounded memories, directional trust, unequal-work disputes, protected breaks and reconciliation |
| Incidents | 12 authored definitions / 24 responses across 8 families; hidden saved pacing, warnings, cooldowns and follow-ups |
| Generation | Version 3 lunar gradient-noise/crater geology and seams; BSP chamber networks, protected approaches and validated seals; legacy CA/WFC generation retained |
| Assets | 7,115 occupied regions / 11,887 total records, across 3 original packs |
| Current sprite selection | 166 IDs including font/UI; 14 explicit field aliases/IDs; 75 additional preview-only IDs |
| Catalog | Schema 5: generic artwork metadata/measured colors for every region; separate current/preview usage and preserved source evidence; all occupied artwork inspected; neutral offline/native queries verified |
| Focused Urizen review | 104 preserved individual crop observations; no claim of a new manual identification of every sprite |
| Tutorial / viewer | 7 sectioned lessons; full atlas with neutral search, filters/sort, evidence and optional game notes |
| Verification | See the dated verification record below; historical prototype measurements remain separately identified |

The combined loop is implemented. Larger armies, an endless world, physical hauling, z-levels and a rich social simulation are not part of this delivery.

## Run and inspect

From the repository root:

```sh
./ithica.sh
./ithica.sh --load
./ithica.sh --seed 1337
./ithica.sh --scene preparation
./ithica.sh --scene surface
./ithica.sh --scene battle
./ithica.sh --scene structure
./ithica.sh --scene arrival
./ithica.sh --scene atlas
```

`play.sh` forwards to Ithica. `rpg.sh` deliberately opens the retained fantasy preview/regression path, equivalent to `ithica.sh --preview`. It does not open a second gameplay phase of the normal campaign.

The shell launchers resolve their own directory. On Windows or with a system SDK:

```sh
dotnet run --project src/Ithica.Client --configuration Release
```

The default window is 1440×900, minimum 1024×720. `--window-size 1024x720`, `--screenshot PATH --frames 12`, `--skip-intro`, `--save PATH` and `--help` are supported. Named preview scenes execute ordinary campaign commands to reach their state, use a fresh campaign and do not autosave on exit. F5 remains an explicit save. The normal new game starts paused with its tutorial and chooses a fresh campaign seed. Explicit `--seed` pins initial generation and retries; headless tools, diagnostic scenes, frame captures and smoke runs retain reproducible default seed 42. Loads use the saved world/seed; normal N-after-loss starts a fresh seed unless explicitly pinned.

[tools/dotnet.sh](../tools/dotnet.sh) finds a system SDK, an Ithica per-user installation, or the existing historical Regolith installation. [tools/setup-dotnet.sh](../tools/setup-dotnet.sh) installs SDK 10.0.402 under `~/.local/share/ithica/dotnet`. Existing SDK installations are not moved or deleted. [global.json](../global.json) requests 10.0.100 with `latestFeature` roll-forward; project packages are pinned and locked. Python is a catalog development tool, not a game runtime dependency.

## The integrated loop

1. Maintain pressure, power, food, water and useful work while colony time runs.
2. Click a distress contact or discovered site directly on the map. Preparation pauses time; select residents and supplies, with home staffing/stock consequences visible.
3. Depart as those residents. They leave local work, consume their own needs and explore under player control. Valid moves, interactions and combat actions advance both site and colony conditions.
4. Share supplies with the stranded person, recover salvage or restore useful equipment. Return through the site's return approach.
5. Deposit cargo and unused supplies once, recruit a living escort only in breathable shelter, and retain injuries/fatigue. Read the paused arrival report, then resume the previous colony pause/speed state.
6. Use an actual recovered component to construct powered suit service. It equips departing residents to use 35% less suit oxygen and reveals the next signal. Below, a paid pump repair opens a seal and supplies home with water.
7. Restoring that pump reveals three deep readings near the station. Survey a reading, extend connected deck and build an enclosed refuge, life support and an extraction rig.
8. Assign mining, maintenance and industry priorities and shifts. Extract finite deep ore, reinforce station systems, and fabricate replacement components; lower exhausted rigs for greater power/wear commitments.
9. Preventive service and cautious drilling avoid operator injuries. Allowing worn drilling can strand a tired resident; another resident must stabilize and carry them to breathable shelter.

The first rescue supports Ada alone and has no mandatory battle. The first salvage encounter and component are a separate outing. Residents away cannot also work or tend farms at home. An empty shelter still runs machines; a living person outside counts toward survival.

The game uses observed labels such as surface, shelter, movement and sealed passage. It does not establish a named alien civilization or supernatural origin. The old Hearth/Ashwood/Abbey story is confined to the explicit preview.

## Time contract

[CampaignClock.cs](../src/Ithica.Core/Campaign/CampaignClock.cs) defines clock policy and centralized `ActionCosts`; [Campaign.cs](../src/Ithica.Core/Campaign/Campaign.cs) is its gameplay owner. `CampaignTick` is the colony's authoritative integer tick. Clients request advancement; rendering and animation do not independently advance either simulation.

| Situation | Advancement |
| --- | --- |
| Colony unpaused | Elapsed-time accumulator, initially 8 ticks/second; speed 1× / 3× / 8× |
| Colony paused, tutorial, construction, station/personal reports, preparation, atlas or arrival report | No simulation ticks |
| Expedition awaiting player choice, inspecting, targeting or reading | Both site and home wait |
| Accepted field command | Stated logical cost; home and active site advance together |
| Hostile turn | Deterministically resolved as a paid action until the next player decision |
| Invalid/canceled action | No time, resource deduction or RNG consumption |

| Accepted operation | Campaign ticks |
| --- | ---: |
| Move whole party / wait / each combat actor action | 1 |
| Approach / return travel | 8 each |
| Rescue / salvage / refill / suppress exposure | 4 each |
| Field treatment outside combat | 8 |
| Pump repair / cycle seal | 12 each |
| Field rest | 32 |

Combat medical kits cost one combat action; outside-combat treatment costs eight ticks. Multi-target attacks are one action. One abstract colony cycle is 480 ticks; this is gameplay pacing, not an astronomical lunar day.

Mode changes discard the real-time accumulator. Returning adds no fixed shift or real-time catch-up. Colony frame pacing starts ticks for up to roughly 12 ms and retains at most eight pending ticks; an individual tick remains synchronous and can exceed that budget. Core callers can request a smaller tick budget without losing pending ticks. Under load the simulation may slow relative to wall time.

Click-to-walk commits successive validated steps. It stops on encounters, relevant exposure, blocked movement, transitions or a newly reported dangerous home incident. Esc cancels uncommitted steps. Waiting to think at the next decision costs nothing. Field home conditions remain visible, and new danger notices stop automated travel.

Hostile resolution and travel settle atomically before the public command returns. Saves occur at settled command boundaries, including an ongoing encounter's next player turn. Animation timers and uncommitted UI routes are not saved as pending gameplay actions.

## People supplies and recovery

[Colonist](../src/Ithica.Core/Colony/Models.cs) is the authoritative resident record: stable ID, home/field location, profession, field role, health, hunger, energy, morale, suit oxygen, charge, level and experience. Party and battle slots reference IDs, including nonsequential IDs. Encounter heroes/units are controlled projections; changes commit after each action, preserving fractional resident health/morale. They are not independent residents synchronized only on return.

A contact has a survivor record until recruitment; new rescues record the recruited resident ID after reaching pressure and becomes historical contact data. Its health and oxygen transfer into the new resident. Escorted survivors still use oxygen on logical ticks, can be included in a paid refill, and remain frozen while the player thinks. Their old contact record does not continue simulating as a second colonist.

Away crew receive needs updates once and no home movement, jobs, bed treatment or local farming bonus. Home sleep restores energy, health, morale and equipment charge. Powered nearby medical care improves sleeping recovery. Injured arrivals may rest before returning to work or becoming available for another outing.

Default departure pack: **4 food, 4 water, 2 medical kits, 1 filter kit**. Packing spends home stock once: each medical kit costs 2 metal, each filter 1 crystal, plus packed food/water. At least one food/water is required; quantities are adjustable. Unused supplies convert back once on return. Viewing or canceling preparation makes no reservation.

Rescue shares one food and one water. A refill uses one water per person, including escorts. Treatment uses a medical kit; rest uses one food per party member and one water. Field meals use carried food, never remote home food. A salvaged cache gives carried **12 metal, 2 crystal and 1 component**. Those become home stock only on return.

Failure favors injury and recovery. Exhaustion or defeat triggers one injured return and leaves carried salvage behind. A failed component remains recoverable at its persistent site, preventing the first upgrade from becoming unreachable; cleared encounters remain cleared. Downed returnees are stabilized, not fully healed. Colony deaths and distress failures remain possible. Exact difficulty is a tuning boundary, not a promised permanent-death ruleset.

## Colony systems

The home retains seeded geology, mining/salvage/survey and rectangular orders; reserved construction/refunds; four-neighbor double-buffered atmosphere, heat and oxygen-dependent fire; breaches; connected power islands, local batteries and priority consumers; farming, beds, medicine, needs, notices, overlays and map following.

| Module | Function |
| --- | --- |
| Pressure hull / habitat deck / airlock | Enclosure, walking surface, power connectivity and a pressure seal |
| Solar array / battery | Up to 12 kW daylight supply / 240 kWh storage per bank |
| Life support | Water-powered oxygen and warmth; 3 kW |
| Hydroponics | Food subject to water, pressure, temperature and 4 kW; local botanists improve output |
| Sleeping pod / med station | Recovery; powered nearby med station improves healing, drawing 2 kW |
| Earth uplink | Retained optional colony charter module, 60 metal + 20 crystal, 5 kW |
| RTG | Reliable 12 kW starter generator; cannot be manufactured |
| Suit service | 24 metal + 4 crystal + 1 component, 2 kW; improved field oxygen efficiency on departure and deeper-site discovery |
| Extraction rig | 24 metal + 4 crystal, 18 work; surveyed deposit, pressure, refuge and worker; 8/12/16 kW at depth 1/2/3 |
| Component fabricator | 24 metal + 4 crystal + 1 component, 18 work; 5 kW and breathable pressure; a paid worker recipe produces components from deep ore |

The optional retained charter requires 3 rescues, cycle 3, 4 living crew, 20 food and a powered pressurized uplink. It is not the combined game's first-loop objective or a required ending. Stock includes metal, water, food, crystal, components and deep ore. Delivery is abstract; there is no hauling or collision reservation, and people may share cells.

## Extraction and work assignments

Implemented in [Extraction.cs](../src/Ithica.Core/Colony/Extraction.cs), [Workforce.cs](../src/Ithica.Core/Colony/Workforce.cs) and [IndustryPersistence.cs](../src/Ithica.Core/Colony/IndustryPersistence.cs). This is physical expansion on the existing colony map, with finite abstract drilling layers; it does not add walking z-levels or remote autonomous mines.

**Discovery and support.** Restoring the below-site pump resolves three readings at offsets (+12,+6), (-12,-6), (+4,+8) from the home center. These initial prospects are fixed within the original cleared apron, independently of the terrain RNG. They are persisted, never regenerated or used to overwrite player terrain. A completed `Survey` order on the exact reading identifies its deposit. Mine existing surface rock first if necessary; an extraction rig then needs deck on that surveyed cell. No ore is awarded merely for discovery, construction or elapsed machine time.

A working rig needs connected **8/12/16 kW** at depth **1/2/3**, local breathable pressure above .45, temperature 0–46 C, and a sleeping pod reachable within **six steps through breathable safe deck** (pod pressure above .5). Its operator must stand on adjacent breathable deck. This makes enclosure, life support, doors, power routing and actual travel relevant. Rigs/fabricators draw power while enabled even if awaiting a worker; their switch releases the load without canceling work/reservations. Life support and food retain higher electrical priority. Support loss preserves partial work; restoring it lets eligible workers resume.

**Finite production and progression.** Each of the three deposits contains layers of **24, 48 and 72 ore** (144 total per deposit). Extraction takes **36/48/60 work** per batch and yields **4/6/8 deep ore plus the same amount of metal**, clamped to remaining stock. It uses the same resident work, morale, specialization, preferences, needs and pathfinding as construction. Exhaustion ends the extraction order. A paid **24-work** lowering order opens the next layer for **8 metal + 2 crystal**, then **16 metal + 4 crystal**; it requires working support and condition at least 50. Depth three has no fourth layer. This is deliberately bounded progression.

| Use of deep ore | Reserved price and labor | Result |
| --- | --- | --- |
| Component fabrication | 4 ore + 4 metal + 2 crystal; 24 work on a supported fabricator | One component, once per order; none while unstaffed/offline |
| Module reinforcement | 4 ore + 6 metal + 1 crystal; 18 work adjacent to a serviceable module | Half baseline/active drilling wear; does not substitute for a repair |
| Reinforced life support | Same reinforcement order | Also uses 35% less production water |
| Reinforced battery | Same reinforcement order | Also holds 360 instead of 240; mixed banks share charge by capacity |
| Reinforced suit service | Same reinforcement order | Also equips departures for 50% rather than 35% oxygen savings |

Paid jobs reserve exact inputs and refund once on cancellation or destroyed targets. Fabrication is an explicit single-component order. Service interrupts an active extraction order; restart drilling after repair. Industrial commands and staffing changes take no logical time or RNG; actual work advances only through campaign ticks.

**Staffing.** Each living resident has priorities **0 disabled / 1 first / 2 normal / 3 last** for construction, mining, maintenance, survey, industry and rescue. Defaults are normal, with rescue first. Higher enabled priorities are considered before proximity/specialization; every eight ticks an available higher-priority order can interrupt lower-priority work. Changing priorities releases current work while retaining its progress and reservations. An unreachable urgent order receives the existing route retry delay.

Shifts use the same 480-tick cycle: **Flexible** works as fit (historical default); **Day** works ticks 000–239; **Night** 240–479; **Extended** 000–399; **Rest** stays off duty. Off-duty residents release jobs and seek rest. Automatic exhaustion/safety and protected incident/player breaks take precedence. The roster can assign **120 ticks of protected rest** (Core supports 24–480). Changes to scheduling do not erase fatigue or health needs. Off-duty, protected-rest, stranded and carrying residents cannot depart on expeditions. An ongoing carry completes before a scheduled break. Away residents still perform no local work/care; staffing commands are gated to the home phase.

**Preventable injury and recovery.** Active drilling adds **.13 + .04 × depth condition loss per work tick**, halved by reinforcement, and **.08 × depth extra energy loss**. The rig warns when crossing 60% condition. Cautious operators stop below **50%** and can queue paid service. The deliberate worn-equipment override permits continued work: at **25% condition**, or below **50% with operator energy below 35**, the rig jams, stops and injures/strands the operator. The injured resident and colleagues remember the unsafe work. Extended actual work can also create existing overwork memories/disputes. There is no random injury roll or unavoidable catch-up escalation in this chain.

A stranded resident cannot work, receive local healing, travel or rescue themselves, and loses .08 health per tick in addition to normal environmental/need damage. Queue recovery for **2 metal + 1 water**. Another available resident with Rescue enabled must reach them, spend **9 work** stabilizing them, then physically carry them along the home route to safe pressure. Carrying consumes extra energy, preserves resident identity/location and is saved mid-route. Arrival stabilizes health to at least 20, assigns rest, forms rescue memories and improves mutual trust; it does not repair the rig. If the carrier dies, the injured person remains recoverable where they were dropped. Failed/dead-target recovery orders refund once. Thinking in a field decision or an open panel freezes these consequences too.

**Player access.** The **Deep Workings** button opens readings, ore/component stock, locate/survey/build actions and stranded-worker recovery. A selected module's **Operations / Upgrades** panel controls extraction, lowering, fabrication, power, service, reinforcement and cancel/refund. Select a resident → **Work / Shifts / Rest** (also accessible from their personal file). B → **Industrial Blueprints** exposes suit service, rig and fabricator. All these panels hold time; gameplay clicks queue mutations for Update. Original artwork/catalog mappings are unchanged; the rig/fabricator reuse existing module bindings.

`./ithica.sh --extraction-check --seed 42` uses ordinary commands to complete the first expeditions, survey, resource mining, a separate pressure annex, staffed extraction, battery reinforcement and component fabrication, then checks exact saved state. `--scene work`, `--scene mining` and `--scene industry` inspect these controls in fresh, non-autosaving scenes. The walkthrough is deterministic acceptance evidence, not a manual balance/fun assessment.

## Station pressure and community

The first pressure/community milestone is implemented in graphics-free Core: [Maintenance.cs](../src/Ithica.Core/Colony/Maintenance.cs), [Community.cs](../src/Ithica.Core/Colony/Community.cs), [Story.cs](../src/Ithica.Core/Colony/Story.cs) and the embedded authored [event library](../src/Ithica.Core/Story/events.json). This is an initial society and director, not RimWorld/Dwarf Fortress feature parity.

**Maintenance.** Every non-bunk module has condition separate from fire-damaged structural health. Powered use loses .008 condition/tick, idle use .002; vacuum/low pressure, heat above 45 C or a storm doubles wear. Below 50 condition, generator/solar output, oxygen output and crop progress run at 60%; at 15 condition the module stops. Offline faults stop a module; a leaking pump wastes .035 water/tick and loses local air; an overheating relay adds heat and wear and can ignite its module. Existing atmosphere, fire, battery islands and worker safety resolve the consequences.

Service reserves **2 metal** for upkeep or **6 metal + 1 crystal** for a major fault, condition below 50, or structural health below 60. A `Maintain` job takes 9/18 work, requires an adjacent resident, and restores condition/health and clears the module fault/local fire when completed. Engineering specialization and work preferences affect progress. Cancel returns the exact reservation once, including after a target is destroyed. If a fault worsens while cheap upkeep is queued, completion needs the extra 4 metal + 1 crystal and waits when parts are unavailable. Queueing alone does not fix a machine. Neighboring fires remain dangerous after repair.

**Residents.** Stable trait/role profiles determine one preference (craft, discovery, fellowship or security) and one aversion (overwork, confinement, conflict or danger), without consuming simulation RNG. Craft/discovery preferences improve matching work by 8%; fellowship improves shared-meal memories; relevant aversions deepen stress reactions. Actual work increments workload. Danger, hunger, overwork, repairs, surveying, deaths, shared watches, meals and the player's responses create memories (at most 12 per resident). Memories expire in logical time and influence morale; normal duration is 480 ticks, grief 960. Trust is directional, clamped -100..100, and persists. Safe nearby idle residents can build trust at home; absent residents receive no home conversation, treatment, rest or work. Protected 24/72-tick breaks release jobs and hold a resident off duty, trading staffing for recovery. Preferences, memories and trust are modest rules, not detailed schedules, families, factions or autonomous politics.

**Director (internal tooling only).** The event director has no player-facing identity, selector or pacing display. Players see station messages, observed air/power/supplies, physical failures and crew consequences. Strain, exposure, budgets, streaks, recovery windows, eligibility families and event IDs stay in source/tool diagnostics and saves. A separate warmed PCG stream makes adjacent seeds vary their first incident without consuming home/layout/combat randomness. The first consideration is 120 ticks after initialization; later gaps are 144–240 ticks, plus 96 in recovery. Up to 3 decisions can remain open, with one per family. Authored definitions provide family, weight, severity, earliest tick, warning interval, cooldown, free expiry response, response labels/costs and explanation. Typed Core handlers test real eligible targets and resolve effects; adding an arbitrary JSON ID does not add a new mechanic.

A bounded 0–3 budget, at most two consecutive hostile selections, per-definition cooldowns (480–1,000 ticks) and recovery windows constrain pressure. Strain uses air, reserves, injuries/morale, existing faults, low power/storage and away staffing; exposure uses modules, population and queued expansion. New hostile incidents stop during recovery or severe strain; positive opportunities remain eligible. Major debris threats require enough exposure and tick 650. Harmful responses give at least 240 ticks of recovery; wear shutdowns give 180 and resident losses 320. Physical wear, current faults and existing deadlines continue during recovery. Preparedness improves outcomes instead of merely raising an unlimited threat multiplier. These are explicit heuristics, not a learned difficulty model or a completed balance pass.

| Definition | Actual consequence / response |
| --- | --- |
| Pump vibration | 96-tick warning; finish real preventive service or face a water/air leak and major repair |
| Hot relay | Finish direct preventive service during the warning, isolate pending major repair, or accept heat/wear and eventual fire risk |
| Dust front | Spend 1 crystal for a 36-tick storm, or endure 120 ticks of weaker solar and increased EVA oxygen use |
| Debris | Spend 8 metal to shield a marked hull tile, or accept a breach sealed with normal hull construction |
| Unequal shifts | Requires actual unequal work and fatigue/overwork; mediate with a 72-tick break and trust, or retain labor with resentment |
| Rest request | Requires fatigue/overwork; protect rest or dismiss it with a persistent negative memory |
| Shared meal | Spend 4 food and two 24-tick breaks to improve mutual trust and mood; decline without penalty |
| Passing hauler | Limited offer: 12 metal for 24 water, transferred once |
| Seed culture | Spend 3 water; after 72 ticks receive 8 food only if the same farm remains powered and habitable |
| Supply locker | Ordinary reachable survey work recovers 8 metal and 6 water once, within 480 ticks |
| Repeating signal | Queue an ordinary survey at the coordinates or archive the unexplained recording |
| Repair remembered | Requires a real repair memory and two home residents; recognition improves mutual trust |

All deadlines, wear, memories, follow-ups and staffing use the existing shared logical clock. Player deliberation outside freezes both sides. Station decisions and service orders use no immediate ticks; workers, production and deadlines advance on subsequent ticks. Invalid/duplicate/stale/unaffordable commands consume no time, RNG or stock. The normal campaign replaces fixed disaster thresholds with this director while retaining the distress contact schedule, low-supply warnings and charter checks. A bare legacy `Simulation` keeps its original incident behavior until story mode is explicitly enabled.

Open **I** for **station reports**: pending messages, response windows, affected people/locations, observed air/power/supplies, work in progress and recent records. The panel does not expose the event director or its pacing metrics. Historical raw handler IDs and decision-number notices are translated for display without rewriting saved data. The panel holds time; responses require being home, while field inspection is read-only. LOCATE focuses the affected module, survey location or resident at home. New dangerous notices and incident warnings stop field click-to-walk. Select a module and use **M** or **QUEUE SERVICE**; X cancels its work order. Select a resident and open **PERSONAL FILE / MEMORIES** for preferences, morale, work, trust and memories. The existing log remains the physical station timeline.

`./ithica.sh --story-check` replays ordinary commands with seed 2: actual leak, resource loss, paid worker repair, rescue, three useful solar construction orders, later unequal-shift dispute, mediated rest/trust and exact saved continuation. It uses no debug recruits, stat edits, teleports or stock grants. `--scene incidents --seed 2` opens the first real pump warning; `--scene resident` shows a personal file after the real first loop and preventive service. All diagnostic scenes avoid player autosaves.

## Sites generation and hazards

[LunarGenerator](../src/Ithica.Core/Generation/LunarGenerator.cs) now has an independent **version 3 lunar generator**. New normal campaigns use their own lunar geology rather than recasting the fantasy forest/abbey. The current terrain authoring is embedded from [lunar-generation.json](../src/Ithica.Core/Generation/lunar-generation.json); it adds no runtime file or package dependency. The original CA/WFC `WorldBuilder`, version-2 lunar recasting and `LegacyWorldBuilder` remain compatibility/regression paths.

| Area | Current generation |
| --- | --- |
| Colony surroundings | Existing configurable world size (default 96×64); warped four-octave Perlin-style gradient fields, crater bowls/rims and independent elongated ore/ice/crystal fields |
| Surface | Width 48–60, height 32–40; coherent rubble/crater collision, four possible return sides, bounded separated objective anchors, terrain-aware protected approaches and connected exploration pockets |
| Compartments | Width 52–64, height 36–44; bounded BSP partitions, variable chamber bounds, connected spanning graph plus extra links, three-octave damaged edges and one isolated sealed vault |

**Four geological profiles** vary thresholds, warp and crater density: broken plain, crater field, basalt ridge and ice basin. Resource fields produce coherent seams; they do not simulate real mineral chemistry or vertical strata. The starter shelter, airlock, cleared staging ground and three nearby 3×3 supply patches remain stable for a fair opening. Home layout/resource generation consumes no home simulation RNG. The bare legacy `Simulation` still uses its original CA/random-walk geology unless lunar terrain is explicitly requested.

Underground chambers are intact, service or damaged, with a separate sealed vault. The spanning graph connects all ordinary chambers; additional corridors allow alternative routes. The pump is outside the vault and reachable before its gate. Only one opening crosses the vault boundary; salvage and its encounter remain behind that seal. This uses BSP/graph construction, not WFC; WFC remains implemented in the retained preview/version-2 path.

Every new site validates all traversable cells as connected, all objective positions as distinct/reachable/protected, and the seal's prerequisite and lack of bypass. Bounded terrain-aware corridor routing can clear rubble without removing the vault boundary. Damaged isolated corner cells are closed before corridor construction. Generated maps keep the same observed lunar terrain and hazard eligibility; no forest, pond, shrine, vegetation or outdoor fire/spores are introduced. This remains finite 2D tile generation, not streamed terrain, walking elevation or z-levels.

Site layout, terrain/profile, crater/chamber metadata, dressing and hazard sources are deterministic for the recorded seed/version and independent of combat, home simulation and director RNG. The campaign mixes stable separate site seeds; diagnostic export rows compare independent generator inputs using the same specified seed. Normal interactive campaigns choose fresh seeds unless `--seed` is supplied; diagnostics remain reproducible.

**Saved maps remain authoritative.** Each site now records its generator version as well as seed. Historical registry entries without this field default to **version 2**, including places discovered but not yet visited; they still generate with the retained implementation. New sites use version 3. Visiting generates once; revisits and loads retain literal arrays, discoveries, cleared encounters, loot, repairs, seal state and exposure. Colony saves add geology profile/version metadata with deterministic legacy defaults and never regenerate saved geology. Preserve shipped generation implementations/rules when adding a future version; do not silently retune an existing saved version.

Home and the current site are active. Inactive site cellular fields freeze; known distress survival still follows campaign time. The current site advances its exposure automaton every four logical ticks, including combat, rest and repair costs. Protected approaches remain excluded. Interference can affect health/stress and can be suppressed nearby using a carried kit; source removal persists. Colony fire still consumes oxygen. Vegetation remains a future enclosed-habitat idea.

A secured pump provides one home water per 32 campaign ticks. Remote autonomous colonies, streamed regions and a fully simulated distant ecology remain unimplemented.

## Field roles and encounters

| Profession / field role | Three abilities |
| --- | --- |
| Engineer / Engineer | Strike, Disrupt, Brace together |
| Miner / Scout | Shot, Track, Burst |
| Botanist / Surveyor | Pulse, Interrupt, Arc |
| Medic / Medic | Strike, Treat, Reassure |

Shared actions are Guard, Medical kit and Retreat. Charge replaces the preview's mana presentation. Initiative, intent telegraphs, armor, guard, mark, stun, damage/bleed effects, stress and experience remain graphics-free rules. Resident health is on a 100-point scale and charge caps at 40. Battle positioning is top-down presentation; tactical movement, cover and range are absent.

| Variant | Appearance | Present statistics |
| --- | --- | --- |
| Movement | `bandit` crop, source identity kept uncertain | 28 HP for 1–2 residents, 30 for 3–4; power 6, speed 6, armor 1 |
| A still shape | Same appearance, heavier variant | 48 HP, power 8, speed 5, armor 2 |
| Interference | `wisp` crop, no asserted supernatural origin | 18 HP, power 4, speed 6, armor 0; stress-oriented intent |

The salvage template has one Movement, adding Interference for parties above two. The deeper template has A still shape plus Interference. Encounters do not respawn after clearing. Ordinary/heavy victories award 20/45 experience each; the lunar game does not fabricate preview gold or medical kits as rewards.

The retained preview has four fixed fantasy companions, 12 class skills plus 3 shared actions, seven named enemies across six appearances, five groups, three areas and its key → gate → boss → relic → return quest. These are compatibility fixtures, not current lunar counts. See [LEGACY_RPG.md](LEGACY_RPG.md).

## Controls

| Context | Input |
| --- | --- |
| Colony | Click/Enter inspect, order or prepare a map contact/site; R locates distress |
| Colony time | Space pause; 1/2/3 speed; period advances one paused tick |
| Colony work | D mine/salvage, B ordinary construction, U suit service, P survey, X cancel; V rectangle |
| Station pressure | I station reports (also readable outside); M service selected module; personal file button after selecting a resident |
| Expansion / workforce | Deep Workings button; selected module → Operations / Upgrades; selected resident → Work / Shifts / Rest; B → Industrial Blueprints |
| Colony inspection | Arrows/HJKL cursor, Shift stride; C manifest, E log, O overlays, G home, F follow |
| Expedition movement | WASD/arrows; click seen terrain or an object to route/approach; Esc stops |
| Expedition interactions | E/Enter interact, Space/period wait, R return at the return approach |
| Field supplies | O refill, H treat selected resident, C rest, T suppress nearby exposure |
| Combat | 1–6 select action; click map unit/enemy entry/party card; Enter uses the current action on the default appropriate target |
| Both maps | Wheel zoom at pointer; +/- or Page Up/Down at cursor/party; 0 reset; right/middle drag pan |
| Preparation | Click residents, scroll list, +/- supplies; Enter depart, Esc cancel |
| Arrival | Enter/Space/Esc or button dismisses report |
| Whole campaign | F5 save, F9 load; colony also S / Shift+L; loading pauses colony time |
| Guide / atlas | T at home, F1 outside; F2 atlas, Esc closes; atlas F1 query help, F3 pack, F4 palette, F6/Tab type, F7 usage, F8 color, F9 reset, F10 sort; Shift reverses cycling |
| Window | F11 fullscreen; Q save/quit through the guarded normal autosave path |

Explicit preview scenes, frame captures, smoke and headless tools do not autosave player state. Normal live campaigns autosave on exit while living; exiting after colony loss preserves the prior save.

## Ownership and architecture

[Ithica.sln](../Ithica.sln) contains five projects:

| Project | Ownership |
| --- | --- |
| [Ithica.Core](../src/Ithica.Core/) | Shared Campaign/clock/commands; Colony simulation; Expeditions combat/maps; Generation; Persistence; command-driven diagnostics. No MonoGame dependency |
| [Ithica.Client](../src/Ithica.Client/) | One MonoGame game; home and field screens, preparation/arrival, guide, atlas, input and presentation; explicit legacy preview in `Preview/` |
| [Ithica.Graphics](../src/Ithica.Graphics/) | Sprite lookup, lazy texture loading, original/mask copies, bitmap text and camera |
| [Ithica.Tools](../src/Ithica.Tools/) | Headless loop, generation validation/reports, colony and combined benchmarks; no graphics initialization |
| [Ithica.Core.Tests](../tests/Ithica.Core.Tests/) | Retained colony/preview behavior plus cross-phase integration, time, failure and saved continuation |

Start at [Campaign.cs](../src/Ithica.Core/Campaign/Campaign.cs), [CampaignClock.cs](../src/Ithica.Core/Campaign/CampaignClock.cs), [CampaignPersistence.cs](../src/Ithica.Core/Persistence/CampaignPersistence.cs), [IthicaGame.cs](../src/Ithica.Client/IthicaGame.cs) and [ExpeditionScreen.cs](../src/Ithica.Client/ExpeditionScreen.cs). The client's `_sim` is an alias for the campaign's home, not another simulation. UI gameplay callbacks detected while drawing are queued for Update. Core commands settle effects and automatic hostile turns before returning; the renderer owns no simulation RNG.

Worlds use contiguous row-major arrays and reusable diffusion/BFS buffers. Work uses spatial buckets and cached topology/power graphs; routing limits new requests to 64 per tick with rotating crew processing. Simulation is synchronous on one thread. Worker threads would require commands and immutable/double-buffered render state; do not share mutable worlds across threads casually.

## Saves and compatibility

| Data | Default path / format |
| --- | --- |
| Ithica campaign | `~/.ithica/campaign.json`, format `ithica`, version 1 |
| Nested current field session | `ithica-field`, version 1; one site's maps with a variable resident projection |
| Legacy colony compatibility | `~/.regolith/colony-csharp.json`, `regolith-csharp`, version 1 |
| Legacy fantasy preview | `~/.regolith/lantern.json`, `lantern-rpg`, versions 1/2 |
| Archived Python | `~/.regolith/colony.json`, separate Python format |

Unified snapshots include colony arrays/work/power/contacts/roster, clock/pause/speed/remainder, phase, registry and generated site maps, active party IDs, conditions, packed stock/cargo, encounter order/status, completion state and independent RNG. Site records include generator version (missing historical values mean version 2); literal saved maps retain their original terrain and generator report. Additive nested-home fields retain machine condition/faults, exact service reservations, resident disposition/work/breaks/memories/trust, and version-1 director RNG/budget/pacing, pending targets/deadlines, bounded 64-entry history, cooldowns and outstanding follow-ups. Historical saves without these fields initialize healthy condition and deterministic profiles/director without advancing time or consuming the original RNG. Current saves validate all new ranges, references and target shapes. Additive version-1 extraction state saves the three readings, survey/depth/reserves/total production; machine power switches/reinforcement, deep ore stock, industrial/recovery reservations, per-resident priorities/shifts/risk policy, incapacitation and carrying references also persist. Historical missing fields mean no extraction discovery, zero ore, enabled/unreinforced machines and flexible/default work; the restored pump reveals readings on the next shared tick if needed. Job/resident assignments, recipes, layer conservation, support modules and carry targets are validated. No maps or RNG are regenerated. Older application builds are not promised to understand new service/industrial orders. Writes flush a temporary file then replace atomically. Validation checks phase/location/roster/projection consistency and map/encounter integrity. Loading does not regenerate saved maps.

`./ithica.sh --import-colony PATH` imports an inactive legacy colony into a new campaign; finish its automated legacy rescues first. It preserves source world, people, resources, clock and RNG, reads without changing the source, and writes only the selected Ithica save. It does not merge an unrelated fantasy campaign. Original save identifiers, historical SDK lookup and `LegacyWorldBuilder` remain intentional compatibility code.

## Sprites and authoring

Read [the schema-5 guide](../assets/reference/CATALOG_V2.md) and [current field casting](../assets/reference/ithica-sprites.md). Generic catalogue coverage is complete for the current repository artwork; this does not confirm every ambiguous publisher identity. The canonical catalog's `sprites[].asset` describes artwork independently of gameplay: generic type, subjects with confidence/evidence, suggested layers, composition, exact foreground palettes and color coverage. All 7,115 occupied crops have recorded original-pixel inspection; series observations and uncertain identities remain distinct from individual verified publisher identifications. Source identities, visual interpretations and game casting remain separate. Original sheets, IDs, rectangles, inventory/archive hashes, credits and uncertain labels are preserved. Publisher enemy/NPC/player role labels remain in provenance rather than neutral names/subjects.

| Authoritative input | Purpose |
| --- | --- |
| [sprites.json](../assets/sprites.json) | Frozen import audit and stable source/crop/evidence records |
| [asset-semantics.json](../assets/reference/asset-semantics.json) | Game-independent visual descriptions and subject evidence |
| [derived-images.json](../assets/reference/derived-images.json) | Reference-only classification of 14 pack-preview composites and 8 documentation renderings |
| [asset.schema.json](../assets/reference/asset.schema.json) | Version-1 generic artwork metadata contract |
| [urizen-silhouette-matches.json](../assets/reference/urizen-silhouette-matches.json) | Pinned historical exact-mask evidence; conservative medium-confidence subject support, separate from original identity |
| [ithica-expedition-bindings.json](../assets/ithica-expedition-bindings.json) | Field/retained-preview alias-to-ID selection |
| [ithica-casting.json](../assets/ithica-casting.json) | Current lunar roles/limits, placement categories and future lunar ideas |
| [ithica-generation.json](../assets/ithica-generation.json) | Retained preview/intermediate CA/WFC material and room rules |
| [urizen-generation-review.json](../assets/reference/urizen-generation-review.json) | 104 focused crop observations and uncertainty |
| [atlas-template.html](../assets/reference/atlas-template.html) | Offline atlas UI/template |
| Existing [catalog.json.gz](../assets/catalog.json.gz) inventory | Also an input: retain original file/archive inventory when regenerating |

Derived outputs are the colony/field manifests, schema-5 catalog, viewer index, offline HTML and generated Markdown tables. The complete catalog is tracked as `assets/catalog.json.gz` (lossless gzip, about 2.55 MiB instead of 72.46 MiB). Queries, verification and inventory regeneration read the compressed file directly; no Git LFS setup is needed. Regeneration also writes the ignored, readable `assets/catalog.json` local export. `--check` works without that export and checks it for freshness when present. Existing large Git history is retained. Edit inputs and regenerate:

```sh
python3 tools/build_runtime_assets.py
python3 tools/build_game_catalog.py
python3 tools/build_game_catalog.py --check
python3 -m unittest discover -s tests/catalog -v
```

The catalog covers 664 original PNGs, 3 archives, 11,887 regions / 7,115 occupied crops. It separately records 22 derived PNG references (14 pack previews, 8 documentation renderings), accounting for all 686 tracked PNG files without counting composites as new sprites. Per-region `ithica` distinguishes bound, preview-only, unused and non-gameplay status, with current phase aliases and lunar proposals. Current selection is 166 IDs including shared glyphs/UI; the field/preview manifest selects 191; their historical selection union is 241 and 6,874 occupied sprites remain unbound. There are 14 current field aliases/IDs and 104 preserved focused visual reviews. General proposals do not mean each crop was newly manually identified.

`python3 tools/query_assets.py --type actor --color green --limit 10` searches generic metadata without game interpretations. Subjects, minimum subject confidence, exact RGB, layer suitability, composition, sort order, facets and JSON/CSV/ID export are available. Full foreground color measurement and crop-hash validation use Python's standard library; no imaging package is required for regeneration or queries.

Neutral visual authoring currently records 36 body series, 98 prop/stance series, 1,706 individual observations (1,704 Urizen plus 2 color-icon exceptions), 137 icon-family descriptions and 54 inspected sheets covering all 7,115 occupied crops. Every uncertain Urizen crop outside the reviewed main character series has an individual description, including all 125 occupied two-bit crops and exceptional figures. Five tiny/ambiguous motifs retain an explicit unresolved type; all have descriptions, measured colors and evidence. The review-sheet generator covers all 7,115 crops, but generated pages count as inspection only after explicit recording. Exact historical foreground-mask comparison supports 81 previously unconfirmed crops while preserving their original source identities. Medium-confidence silhouette claims, compositional series observations and individual descriptions remain distinguishable in `asset.evidence`. All 786 input prompts retain structured control/appearance/size labels, and 146 publisher-labelled font glyphs expose characters and code points. Icon families have neutral shape descriptions; source labels and visual descriptions remain distinct. The offline atlas supports neutral type/subject/confidence/color/layer/composition filters, sorting, linked selections, metadata JSON and exact crop PNG downloads. Game notes are an optional search scope/detail section. All downloadable crops preserve frozen RGBA, including hidden RGB and partial alpha; scene JSON/PNG exports remain available. Both native viewers share the neutral artwork/query reader and expose pack/type/color/layer/composition/usage controls and sorting. Typed filters include `subject:ghost confidence:high`, `hex:#feffff`, `source:high`, `coverage:0.3` and `sort:name desc:true`; F1 explains the syntax. Game notes are optional and filtering preserves exact campaign state.

Colony and field binding contexts intentionally differ, for example `wall`; they use separate sprite libraries rather than dictionary overwrite. Texture masks/black removal are in-memory copies, with nearest-neighbor rendering. Native atlas registration is lazy and is not gameplay use. Offline atlas scene exports use `ithica-sprite-scene`, version 1; runtime scene-recipe import remains absent.

## Verification and performance

The acceptance record and commands are maintained in [VERIFICATION.md](VERIFICATION.md). The current change preserves the original 97 core cases and adds tests for the shared clock, all saved phases, actor identity, paid supplies, departure/return, failed cargo recovery, deeper-site progression and legacy import. Consult that record for final totals and actual native/published results; a plan or configured CI job is not verification evidence.

```sh
./tools/dotnet.sh restore Ithica.sln --locked-mode
./tools/dotnet.sh build Ithica.sln --configuration Release --no-restore
./tools/dotnet.sh test Ithica.sln --configuration Release --no-build
./ithica.sh --headless
./ithica.sh --extraction-check --seed 42
./ithica.sh --validate-seeds 1000 --seed -500
./ithica.sh --generation-report --seed 42
./ithica.sh --export-generation artifacts/lunar-gallery.svg --seed 42 --samples 4
./ithica.sh --campaign-benchmark --samples 32
./ithica.sh --benchmark --width 256 --height 256 --agents 1000 --ticks 120
./ithica.sh --smoke-test --screenshot /tmp/ithica-smoke.png
./rpg.sh --headless
```

`--headless` without a tick count completes the real integrated loop. `--headless N` advances the unified colony; `--demo` first completes the loop. `--legacy-colony --headless N --demo` preserves the old automated-rescue regression. Tools never write player saves. `--export-generation PATH.svg --samples 1..12` atomically writes a developer SVG comparison of colony geology, surface terrain and compartment layouts. `--generation-report` includes profile/version, dimensions, crater/chamber metadata and terrain counts. Generation validation covers 1–10,000 consecutive signed seeds, wrapping deterministically.

CI configures locked restore/build/tests, the legacy colony check, integrated loop, station fault/repair/social walkthrough, retained RPG quest, 256 lunar seeds and catalog freshness/hashes across its configured OS scope. Linux also configures the generic metadata/export suite and Playwright offline-atlas interaction check; see VERIFICATION.md for executed local scope. It does not certify native rendering on all platforms. Actual graphical checks cover macOS Intel and Fedora 44 x64 under WSLg. The Fedora local setup passed locked restore, a clean Release build, all 131 tests, the integrated headless loop and the initial 18 graphical smoke checks at 1024×720 using SDK 10.0.402 / runtime 10.0.13. Later generic-atlas verification passed 26 current-client / 25 preview graphical checks and complete canonical/native query-order comparisons; the offline browser passed 73 assertions. The compressed-storage follow-up passed 38 catalog tests, 74 browser assertions including the compressed catalog download, catalog freshness with/without the local JSON export, and a clean Release build. Native Windows and standalone Linux desktop sessions still need smoke checks; see the scoped record in [VERIFICATION.md](VERIFICATION.md).

The earlier 2026-10-21 station milestone passed 186 core cases, the ordinary-command station walkthrough, normal and retained-preview headless loops, 256 generated seeds without fallback, and native WSLg UI checks; the dated record below distinguishes new runs from prior platform evidence.

The lunar-generation/player-report follow-up passed **205 Core cases**, **1,000 seeds / 2,000 sites with zero fallback**, 64 complete integrated loops with exact continuation, and **35 native checks at each of 1024×720 and 1440×900** under WSLg. It includes backward generation-version behavior for discovered/unvisited sites. See the dated verification record for exact scope.

Current and historical measurements are distinguished in [PERFORMANCE.md](PERFORMANCE.md). The historical dense-world samples do not prove Dwarf Fortress/Songs of Syx scale. Combined samples measure the actual small home-plus-expedition workload and useful completed work. Root arrays/caches remain a foundation, not a promise of arbitrary simulation scale.

## Boundaries and future work

| Area | Present boundary |
| --- | --- |
| World | Finite persistent sites and authored opportunity types; no streamed endless world or autonomous distant colonies |
| Time | Atomic synchronous commands; inactive site fields freeze; important deadlines follow campaign time |
| Tactics | Intent/ability combat in a top-down view; no tactical walking, range, cover or flanking |
| Economy | Aggregate stock/cargo and fixed recipes, plus one limited hauler event; no hauling, per-item gear swapping, detailed crafting or full shops |
| People | Needs, preferences, jobs, memories, directional trust and initial disputes/reconciliation; no rich society, diplomacy or generations |
| Navigation | Overlapping people, request-limited BFS; no crowd reservations or hierarchical regions |
| Assets | Original crops and masks; no inferred animation/direction sets, automatic fragment assembly or sprite-derived sockets |
| Input | Mouse/keyboard; controller artwork exists but gamepad input is absent |
| Authoring | Atlas scene experiments export JSON/PNG; no runtime recipe importer, general map editor or mod API |

Balance values are current implementation choices. New biomes/enemies, permanent-death settings and larger simulation changes need actual implementation, updated counts and appropriate validation. Keep mechanics readable while leaving the setting unexplained.

## Design direction (planned)

**User direction recorded: 2026-10-20; first milestone implemented 2026-10-21 above. The wider requirements below remain a roadmap.** Ithica should become a lunar colony survival simulation with stronger operational pressure and a detailed internal society. Expansion, deeper mining, drones and attacks should interact with residents' preferences, relationships and disputes. The name's kingdom/community association is an intent to make governing the people matter alongside keeping the station alive. The independent fantasy party RPG belongs to Vagabonds.

The implemented foundation now includes pressure/temperature/fire, power networks, construction/mining, needs, field hazards, usage/exposure wear, paid service jobs, an adaptive incident director and initial persistent preferences/memories/trust/disputes. Finite drilling layers, physical station extensions, crew priorities/shifts, ore-funded improvements and resident recovery are now implemented above. Walking strata/z-levels, deployable drones, colony raids/defenses, detailed relationships and a large event library remain unimplemented.

### Coupled systems to build

| Direction | Intended decisions and consequences |
| --- | --- |
| Station condition and failures | Equipment use, exposure and deferred upkeep affect condition. Inspection, preventive maintenance, repair parts, repair jobs, spare capacity and isolation let the player manage risk. Failures can propagate through actual power, water, pressure, temperature and staffing dependencies. |
| Expansion and deeper extraction | New station areas and deeper deposits offer useful resources while requiring support infrastructure, labor, power and safe access. Depth and industrial activity introduce visible risks. Expansion should create choices about resilience and commitments as well as increasing output. |
| Drones | Dispatch units for survey, extraction, repair or recovery with limited charge, range, payload, condition and communications. Automation competes for station resources and can strand valuable equipment, creating recovery decisions. |
| External attacks | Threats can reach the colony and damage or disrupt meaningful systems. Detection, preparation, defensive layout, drones, sheltering and repair matter. Keep attackers' nature within the sparse lunar setting; no new alien civilization or supernatural explanation is established by this direction. |
| Crew society | Residents retain individual preferences, dislikes, values, risk tolerance and work preferences; shared experiences form memories, trust, friendships, rivalries and disputes. Workload, unsafe orders, unequal treatment, rescue choices and losses can affect cooperation and collective demands. Include positive bonds and reconciliation as well as conflict. |
| Event variety and story generation | RimWorld is an additional reference for a broad, unpredictable event library and storyteller-style pacing. Mix technical/environmental problems, social incidents, arrivals/distress requests, discoveries, opportunities, positive moments and unusual lunar signals. Events should alter the actual colony and support persistent follow-ups. |
| Dynamic difficulty | Pace threats using exposure/depth, active commitments, sustainable capacity, reserves, injuries, stress and recent incidents. Use bounded pressure, internal forecasts, cooldowns and recovery windows; expose physical station conditions rather than the directing machinery. Preparedness and prevention must improve outcomes; expose causes and avoid an automatic escalation that simply cancels every successful improvement. |

Social behavior should affect existing work, care, expedition and resource decisions, with visible causes and responses such as rest, reassignment, safer infrastructure or changed priorities. Do not implement personality as decorative text or replace causal disputes with unrelated random sabotage events. Technical failures and social tensions should be able to reinforce or relieve one another without making every setback an unavoidable cascade.

An illustrative future chain: a deep extraction project consumes the station's power reserve; maintenance is deferred to finish a drone; a neglected system fails and injures a resident; colleagues remember the unsafe assignment and dispute the next emergency shift. The player can invest in redundancy, revise work allocation, repair the system or postpone expansion. This example describes desired interactions, not current events or a scripted quest.

### Event system and story generation

**Additional user direction: RimWorld-like breadth and unpredictability of events.** The aim is for different campaigns to produce different stories from interacting systems. A larger event library must include minor incidents, quiet intervals, benefits and dilemmas alongside major emergencies. [RimWorld's official description](https://rimworldgame.com/) identifies its storyteller's selection of incidents as a central story-generation mechanism; this is a design reference, not a claim of feature parity.

External chance events such as impacts or unexpected signals can introduce surprises; internal events such as disputes and breakdowns should also emerge from actual conditions and history. The director governs eligible incident selection, timing, frequency and severity while the simulation resolves the physical and social consequences. Existing wear, damage or relationship conditions must remain causally meaningful; directing an incident should not erase the benefits of prevention.

Example planned categories are technical/environmental incidents (faults, leaks, storms), crew incidents (arguments, cooperation, grief, reconciliation), contacts (distress calls or aid requests), discoveries/opportunities (a deposit, salvage window or recoverable drone), and unusual signals whose explanation remains unresolved. The implemented table above identifies current examples; recoverable drones and wider contact/trade systems remain future work. These ideas establish no supernatural lore. Outcomes can create injuries, memories, obligations, new opportunities and follow-up events rather than ending at a notification.

Use data-driven event definitions with prerequisites, weighted selection, eligible targets, severity/cost bounds, warnings where appropriate, player responses, cooldowns, repetition limits and follow-up conditions. Save the director's independent RNG state, pending events, targets and history so load resumes the same situation. Follow the existing campaign clock and decision-pause contract. Progression, exposure, colony capacity and recent outcomes should shape pacing without making every event predictable or every campaign uniformly hostile.

The first milestone now includes the director foundation and 12 representative technical, social, environmental and opportunity definitions alongside maintenance and initial crew memories. Its tests cover every response, causal outcomes, cooldowns/streak bounds, recovery pacing and exact continuation; expand from this verified baseline. RimWorld-scale breadth remains a long-term content target.

### First milestone and sequencing

The delivered first playable increment pairs **maintenance and repair with the first persistent crew preferences, memories and disputes**, using the current station, jobs and needs. It adds incident pacing and ways to recover. The ordinary-command walkthrough demonstrates a real equipment fault, physical station consequences, an actual paid repair job, resident workload/recovery consequences, a causally explained social reaction and a player response that changes the outcome. Invalid commands must still cost nothing, and saving/loading must preserve the fault, job, memories, relationships and incident pacing exactly.

The 2026-10-22 increment implements the first deeper extraction/workforce loop above. Follow with drone logistics; then station attacks/defense and broader collective politics. These are incremental milestones, not a promise that every listed system ships together. Dwarf Fortress and Songs of Syx are depth references; comparable individual detail should be pursued for a manageable lunar crew before claiming their population scale or full feature breadth.

Reference reading: [Songs of Syx's fulfillment model](https://www.songsofsyx.com/wiki/index.php/Fulfillment) illustrates how differing preferences and provision affect loyalty; [Bay 12 developer discussions](https://www.bay12games.com/media/df_talk_combined_transcript.html) discuss personalities, memories and relationships. These inform design judgment and do not establish Ithica features. Preserve the current shared-clock/resident/save contracts, source artwork and independent Vagabonds boundary when implementing this direction.

## Maintaining this reference

Verify facts in code/configuration before updating counts. Do not copy old fantasy counts into the lunar roster, or treat catalog proposals as shipped systems. Keep authoritative resident ownership, one clock, no deliberation-time countdown, independent RNG and exact saved continuation intact. Preserve the source art and stable mappings; regenerate derived asset data.

For gameplay changes update current scope, controls, costs, roster/content, saves, limits and verification here, then synchronize specialist guides. Record new performance samples with hardware/workload and useful-work counts. Keep legacy save readers, fixtures and explicit preview paths working until a deliberate replacement migration is tested. Historical names inside provenance or compatibility formats are not accidental current branding.

## Detailed references

| Reference | Use |
| --- | --- |
| [README](../README.md) | Normal launch, quick loop and build commands |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Ownership, time, persistence and scaling boundaries |
| [EXPEDITIONS.md](EXPEDITIONS.md) | Current lunar field controls and progression |
| [PROCEDURAL_GENERATION.md](PROCEDURAL_GENERATION.md) | CA/WFC reuse, lunar recasting, persistence and validation |
| [VERIFICATION.md](VERIFICATION.md) | Dated implementation acceptance, actual checks and rollback commits |
| [PERFORMANCE.md](PERFORMANCE.md) | Workloads, raw measurements and limitations |
| [LEGACY_RPG.md](LEGACY_RPG.md) | Retained fantasy prototype walkthrough and regression commands |
| [Catalog guide](../assets/reference/CATALOG_V2.md) | Schema 5, generic queries, evidence, current usage and regeneration |
| [Current field sprites](../assets/reference/ithica-sprites.md) | Every current lunar field alias, source evidence and adaptation limits |
| [Urizen review](../assets/reference/urizen-generation.md) | Preserved 104-crop focused inspection |
| [CREDITS.txt](../assets/CREDITS.txt) | Original publisher provenance and declarations |
| [Archived Python README](../legacy/python/README.md) | Historical terminal implementation |
