# Ithica: project state and agent reference

**Last reconciled: 2026-10-20.** This is the master source of truth for the implemented game, its rules and limits, data ownership and verification. Read it before changing the project. Update this document alongside affected behavior, content, formats or tooling. Source code and authored configuration implement behavior; catalog ideas remain proposals.

**Ithica is one playable C# / MonoGame lunar salvage colony game.** Start with one colonist, rescue people through controlled expeditions, bring the same residents home, recover useful components and build improvements that change later trips. The Moon remains recognizable; names and explanations stay sparse. The old fantasy RPG and Python prototypes remain explicit compatibility/regression references.

## Contents

- [Current scope](#current-scope)
- [Run and inspect](#run-and-inspect)
- [The integrated loop](#the-integrated-loop)
- [Time contract](#time-contract)
- [People supplies and recovery](#people-supplies-and-recovery)
- [Colony systems](#colony-systems)
- [Sites generation and hazards](#sites-generation-and-hazards)
- [Field roles and encounters](#field-roles-and-encounters)
- [Controls](#controls)
- [Ownership and architecture](#ownership-and-architecture)
- [Saves and compatibility](#saves-and-compatibility)
- [Sprites and authoring](#sprites-and-authoring)
- [Verification and performance](#verification-and-performance)
- [Boundaries and future work](#boundaries-and-future-work)
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
| Colony modules | 12 definitions, 11 buildable; RTG is starter-only; suit service needs a recovered component |
| Generation | CA terrain, WFC room connections, cellular damage/exposure, protected approaches and global seal validation |
| Assets | 7,115 occupied regions / 11,887 total records, across 3 original packs |
| Current sprite selection | 166 IDs including font/UI; 14 explicit field aliases/IDs; 75 additional preview-only IDs |
| Catalog | Schema 5: generic artwork metadata/measured colors for every region; separate current/preview usage and preserved source evidence; neutral visual authoring in progress |
| Focused Urizen review | 104 preserved individual crop observations; no claim of a new manual identification of every sprite |
| Tutorial / viewer | 6 sectioned lessons; full atlas in the same window with current-use filtering |
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

The default window is 1440×900, minimum 1024×720. `--window-size 1024x720`, `--screenshot PATH --frames 12`, `--skip-intro`, `--save PATH` and `--help` are supported. Named preview scenes execute ordinary campaign commands to reach their state, use a fresh campaign and do not autosave on exit. F5 remains an explicit save. The normal new game starts paused with its tutorial.

[tools/dotnet.sh](../tools/dotnet.sh) finds a system SDK, an Ithica per-user installation, or the existing historical Regolith installation. [tools/setup-dotnet.sh](../tools/setup-dotnet.sh) installs SDK 10.0.402 under `~/.local/share/ithica/dotnet`. Existing SDK installations are not moved or deleted. [global.json](../global.json) requests 10.0.100 with `latestFeature` roll-forward; project packages are pinned and locked. Python is a catalog development tool, not a game runtime dependency.

## The integrated loop

1. Maintain pressure, power, food, water and useful work while colony time runs.
2. Click a distress contact or discovered site directly on the map. Preparation pauses time; select residents and supplies, with home staffing/stock consequences visible.
3. Depart as those residents. They leave local work, consume their own needs and explore under player control. Valid moves, interactions and combat actions advance both site and colony conditions.
4. Share supplies with the stranded person, recover salvage or restore useful equipment. Return through the site's return approach.
5. Deposit cargo and unused supplies once, recruit a living escort only in breathable shelter, and retain injuries/fatigue. Read the paused arrival report, then resume the previous colony pause/speed state.
6. Use an actual recovered component to construct powered suit service. It equips departing residents to use 35% less suit oxygen and reveals the next signal. Below, a paid pump repair opens a seal and supplies home with water.

The first rescue supports Ada alone and has no mandatory battle. The first salvage encounter and component are a separate outing. Residents away cannot also work or tend farms at home. An empty shelter still runs machines; a living person outside counts toward survival.

The game uses observed labels such as surface, shelter, movement and sealed passage. It does not establish a named alien civilization or supernatural origin. The old Hearth/Ashwood/Abbey story is confined to the explicit preview.

## Time contract

[CampaignClock.cs](../src/Ithica.Core/Campaign/CampaignClock.cs) defines clock policy and centralized `ActionCosts`; [Campaign.cs](../src/Ithica.Core/Campaign/Campaign.cs) is its gameplay owner. `CampaignTick` is the colony's authoritative integer tick. Clients request advancement; rendering and animation do not independently advance either simulation.

| Situation | Advancement |
| --- | --- |
| Colony unpaused | Elapsed-time accumulator, initially 8 ticks/second; speed 1× / 3× / 8× |
| Colony paused, tutorial, construction, preparation, atlas or arrival report | No simulation ticks |
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

The optional retained charter requires 3 rescues, cycle 3, 4 living crew, 20 food and a powered pressurized uplink. It is not the combined game's first-loop objective or a required ending. Stock includes metal, water, food, crystal and components. Delivery is abstract; there is no hauling or collision reservation, and people may share cells.

## Sites generation and hazards

[LunarGenerator](../src/Ithica.Core/Generation/LunarGenerator.cs) reuses the tested CA/WFC algorithms, then explicitly changes ground/collision, art, objects and progression. Old forest, pond, shrine and relic objects do not enter the lunar site.

| Family | Layout | Content |
| --- | --- | --- |
| Surface | 48×32; cellular rubble with protected approaches | Distress rescue or salvage encounter/component; explicit return point |
| Compartments | 51×37; 7×5 chunks of 7×7 rooms; WFC and cellular damage | Pump, sealed passage, heavier encounter, component and return point |

The WFC solver uses authored opposing sockets, bounded attempts and a valid structural fallback. Global validation checks reachability and ensures the seal cannot be bypassed while its repair prerequisite remains reachable. Room variants are presented as intact, service and damaged geometry; atlas indices never supply sockets automatically.

Each discovered site's seed/version is fixed. Generation happens once when first prepared; generated arrays, seen cells, cleared encounters, recovered loot, repairs, seal state and exposure are saved. Revisits do not reroll. Seeds for layout/dressing, site combat and colony simulation remain independent, preserving the legacy stream algorithms where required.

Home and the current site are active. Inactive site cellular fields freeze; known distress survival still follows campaign time through the home contact schedule. The current site advances its exposure automaton every four logical ticks, including combat, rest and repair costs. Protected approaches remain excluded. The symbolic exposure/interference overlay can affect health/stress and be suppressed nearby with a kit; source removal persists. No ordinary outdoor fire or spores are initialized in lunar sites. Colony fire consumes oxygen. Vegetation remains a future enclosed-habitat casting idea.

A secured pump provides one home water per 32 campaign ticks. Remote autonomous colonies, streamed regions and a fully simulated distant ecology are not implemented.

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
| Colony inspection | Arrows/HJKL cursor, Shift stride; C manifest, E log, O overlays, G home, F follow |
| Expedition movement | WASD/arrows; click seen terrain or an object to route/approach; Esc stops |
| Expedition interactions | E/Enter interact, Space/period wait, R return at the return approach |
| Field supplies | O refill, H treat selected resident, C rest, T suppress nearby exposure |
| Combat | 1–6 select action; click map unit/enemy entry/party card; Enter uses the current action on the default appropriate target |
| Both maps | Wheel zoom at pointer; +/- or Page Up/Down at cursor/party; 0 reset; right/middle drag pan |
| Preparation | Click residents, scroll list, +/- supplies; Enter depart, Esc cancel |
| Arrival | Enter/Space/Esc or button dismisses report |
| Whole campaign | F5 save, F9 load; colony also S / Shift+L; loading pauses colony time |
| Guide / atlas | T at home, F1 outside; F2 atlas, Esc closes; atlas F3 pack, F4 palette, F6 family, F7 usage, type search |
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

Unified snapshots include colony arrays/work/power/contacts/roster, clock/pause/speed/remainder, phase, registry and generated site maps, active party IDs, conditions, packed stock/cargo, encounter order/status, completion state and independent RNG. Writes flush a temporary file then replace atomically. Validation checks phase/location/roster/projection consistency and map/encounter integrity. Loading does not regenerate saved maps.

`./ithica.sh --import-colony PATH` imports an inactive legacy colony into a new campaign; finish its automated legacy rescues first. It preserves source world, people, resources, clock and RNG, reads without changing the source, and writes only the selected Ithica save. It does not merge an unrelated fantasy campaign. Original save identifiers, historical SDK lookup and `LegacyWorldBuilder` remain intentional compatibility code.

## Sprites and authoring

Read [the schema-5 guide](../assets/reference/CATALOG_V2.md) and [current field casting](../assets/reference/ithica-sprites.md). The canonical catalog's `sprites[].asset` describes artwork independently of gameplay: generic type, subjects with confidence/evidence, suggested layers, composition, exact foreground palettes and color coverage. Neutral visual authoring is continuing; regional/category inference is not an individual verified identification. Source identities, visual interpretations and game casting remain separate. Original sheets, IDs, rectangles, inventory/archive hashes, credits and uncertain labels are preserved.

| Authoritative input | Purpose |
| --- | --- |
| [sprites.json](../assets/sprites.json) | Frozen import audit and stable source/crop/evidence records |
| [asset-semantics.json](../assets/reference/asset-semantics.json) | Game-independent visual descriptions and subject evidence |
| [asset.schema.json](../assets/reference/asset.schema.json) | Version-1 generic artwork metadata contract |
| [urizen-silhouette-matches.json](../assets/reference/urizen-silhouette-matches.json) | Pinned historical exact-mask evidence; conservative medium-confidence subject support, separate from original identity |
| [ithica-expedition-bindings.json](../assets/ithica-expedition-bindings.json) | Field/retained-preview alias-to-ID selection |
| [ithica-casting.json](../assets/ithica-casting.json) | Current lunar roles/limits, placement categories and future lunar ideas |
| [ithica-generation.json](../assets/ithica-generation.json) | Retained preview/intermediate CA/WFC material and room rules |
| [urizen-generation-review.json](../assets/reference/urizen-generation-review.json) | 104 focused crop observations and uncertainty |
| [atlas-template.html](../assets/reference/atlas-template.html) | Offline atlas UI/template |
| Existing [catalog.json](../assets/catalog.json) inventory | Also an input: retain original file/archive inventory when regenerating |

Derived outputs are the colony/field manifests, schema-5 catalog, viewer index, offline HTML and generated Markdown tables. Edit inputs and regenerate:

```sh
python3 tools/build_runtime_assets.py
python3 tools/build_game_catalog.py
python3 tools/build_game_catalog.py --check
python3 -m unittest discover -s tests/catalog -v
```

The catalog covers 664 PNGs, 3 archives, 11,887 regions / 7,115 occupied crops. Per-region `ithica` distinguishes bound, preview-only, unused and non-gameplay status, with current phase aliases and lunar proposals. Current selection is 166 IDs including shared glyphs/UI; the field/preview manifest selects 191; their historical selection union is 241 and 6,874 occupied sprites remain unbound. There are 14 current field aliases/IDs and 104 preserved focused visual reviews. General proposals do not mean each crop was newly manually identified.

`python3 tools/query_assets.py --type actor --color green --limit 10` searches generic metadata without game interpretations. Subjects, minimum subject confidence, exact RGB, layer suitability, composition, sort order, facets and JSON/CSV/ID export are available. Full foreground color measurement and crop-hash validation use Python's standard library; no imaging package is required for regeneration or queries.

Neutral visual authoring currently records 36 body series, 98 prop/stance series, 1,224 individual observations and 32 inspected coordinate sheets covering 5,080 occupied crops. Every uncertain one-bit crop in columns 0–76 (excluding guide columns) and all 125 occupied two-bit crops have individual descriptions. The review-sheet generator covers all 7,115 crops, but generated pages count as inspection only after explicit recording. Exact historical foreground-mask comparison supports 81 previously unconfirmed crops while preserving their original source identities. Medium-confidence silhouette claims, compositional series observations and individual descriptions remain distinguishable in `asset.evidence`; systematic authoring of the remaining art and neutral atlas controls is ongoing.

Colony and field binding contexts intentionally differ, for example `wall`; they use separate sprite libraries rather than dictionary overwrite. Texture masks/black removal are in-memory copies, with nearest-neighbor rendering. Native atlas registration is lazy and is not gameplay use. Offline atlas scene exports use `ithica-sprite-scene`, version 1; runtime scene-recipe import remains absent.

## Verification and performance

The acceptance record and commands are maintained in [VERIFICATION.md](VERIFICATION.md). The current change preserves the original 97 core cases and adds tests for the shared clock, all saved phases, actor identity, paid supplies, departure/return, failed cargo recovery, deeper-site progression and legacy import. Consult that record for final totals and actual native/published results; a plan or configured CI job is not verification evidence.

```sh
./tools/dotnet.sh restore Ithica.sln --locked-mode
./tools/dotnet.sh build Ithica.sln --configuration Release --no-restore
./tools/dotnet.sh test Ithica.sln --configuration Release --no-build
./ithica.sh --headless
./ithica.sh --validate-seeds 1000 --seed -500
./ithica.sh --campaign-benchmark --samples 32
./ithica.sh --benchmark --width 256 --height 256 --agents 1000 --ticks 120
./ithica.sh --smoke-test --screenshot /tmp/ithica-smoke.png
./rpg.sh --headless
```

`--headless` without a tick count completes the real integrated loop. `--headless N` advances the unified colony; `--demo` first completes the loop. `--legacy-colony --headless N --demo` preserves the old automated-rescue regression. Tools never write player saves. Generation validation covers 1–10,000 consecutive signed seeds, wrapping deterministically.

CI runs locked restore/build/tests, the legacy colony check, integrated loop, retained RPG quest, 256 lunar seeds and catalog freshness/hashes across its configured OS scope. It does not certify native rendering on all platforms. Actual graphical checks cover macOS Intel and Fedora 44 x64 under WSLg. The Fedora local setup passed locked restore, a clean Release build, all 131 tests, the integrated headless loop and all 18 graphical smoke checks at 1024×720 using SDK 10.0.402 / runtime 10.0.13. Native Windows and standalone Linux desktop sessions still need smoke checks; see the scoped record in [VERIFICATION.md](VERIFICATION.md).

Current and historical measurements are distinguished in [PERFORMANCE.md](PERFORMANCE.md). The historical dense-world samples do not prove Dwarf Fortress/Songs of Syx scale. Combined samples measure the actual small home-plus-expedition workload and useful completed work. Root arrays/caches remain a foundation, not a promise of arbitrary simulation scale.

## Boundaries and future work

| Area | Present boundary |
| --- | --- |
| World | Finite persistent sites and authored opportunity types; no streamed endless world or autonomous distant colonies |
| Time | Atomic synchronous commands; inactive site fields freeze; important deadlines follow campaign time |
| Tactics | Intent/ability combat in a top-down view; no tactical walking, range, cover or flanking |
| Economy | Aggregate stock/cargo and fixed recipes; no hauling, per-item gear swapping, detailed crafting or shops in the lunar loop |
| People | Needs, jobs, field roles, stress and recovery; no rich relationships, diplomacy or generational simulation |
| Navigation | Overlapping people, request-limited BFS; no crowd reservations or hierarchical regions |
| Assets | Original crops and masks; no inferred animation/direction sets, automatic fragment assembly or sprite-derived sockets |
| Input | Mouse/keyboard; controller artwork exists but gamepad input is absent |
| Authoring | Atlas scene experiments export JSON/PNG; no runtime recipe importer, general map editor or mod API |

Balance values are current implementation choices. New biomes/enemies, permanent-death settings and larger simulation changes need actual implementation, updated counts and appropriate validation. Keep mechanics readable while leaving the setting unexplained.

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
