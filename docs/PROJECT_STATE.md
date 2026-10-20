# Ithica project state and agent reference

**Last reconciled: 2026-10-20.** This is the master reference for what is implemented in this repository, where its authoritative data lives, and how to work on it. Read this before extending Ithica. Keep it current when behavior, content, data formats, or tooling changes; update the reconciliation date after checking the affected sources.

**Ithica** is the current product name. The approved target is one mysterious lunar salvage colony/RPG; see the [integration plan](ITHICA_PLAN.md). The implemented repository still contains two playable C# prototypes under legacy names: **Regolith**, a lunar colony simulator, and **Lantern Below**, a fantasy party RPG. Both use MonoGame DesktopGL and the same imported artwork. Their gameplay rules, progression, saves, and clients are currently separate. The original Python terminal game is archived under `legacy/python/`.

Executable behavior comes from the source code and authored configuration linked below. Generated catalogs describe artwork and bindings; they do not implement the mechanics suggested in their future-use notes. When documentation and code diverge, inspect the implementation and tests and reconcile the documentation.

## Contents

- [Approved direction](#approved-direction)
- [Current scope](#current-scope)
- [Run and inspect](#run-and-inspect)
- [Code ownership](#code-ownership)
- [Regolith colony](#regolith-colony)
- [Lantern worlds and progression](#lantern-worlds-and-progression)
- [Party and combat](#party-and-combat)
- [Enemies and encounters](#enemies-and-encounters)
- [Procedural generation](#procedural-generation)
- [Exploration hazards](#exploration-hazards)
- [Sprites and authoring](#sprites-and-authoring)
- [Controls](#controls)
- [Saves](#saves)
- [Performance and platforms](#performance-and-platforms)
- [Verification](#verification)
- [Current gaps](#current-gaps)
- [Keeping this reference current](#keeping-this-reference-current)
- [Detailed references](#detailed-references)

## Approved direction

[ITHICA_PLAN.md](ITHICA_PLAN.md) is the consolidated implementation plan, including feasibility findings, shared residents and inventory, clock semantics, persistent lunar sites, asset reuse, the code/runtime rebrand, compatibility and staged acceptance gates. The design is approved; those integration stages are not yet implemented.

The colony runs continuously while unpaused. Expedition exploration and combat wait at player decisions, with **both the home colony and expedition frozen until a committed action advances their shared logical time**. There is no real-time colony countdown while reading or choosing, and no second fixed-shift charge on return.

Start with one colonist; rescue people who become the same workers and expedition members. Retain the Moon setting and restrained observational labels. Existing fantasy areas, companions and enemy names below describe prototype content, not a final Ithica biome count or lunar bestiary. Existing source paths, commands and save identifiers remain listed accurately until their explicit migration lands.

## Current scope

| Item | Implemented state |
| --- | --- |
| Product name / integration | Ithica; solution, Core, graphics, client and tools consolidated; shared gameplay integration in progress |
| Runtime | C# / .NET 10, MonoGame.Framework.DesktopGL 3.8.6 |
| Playable prototypes | 2: Regolith and Lantern Below |
| RPG environments | 3 areas: 2 procedural exploration biomes and 1 town hub |
| RPG room themes | 3 within the Abbey: cloister, cistern, collapsed vault; these are room variants within one biome |
| RPG party | 4 fixed companions/classes; 12 class skills and 3 shared actions |
| RPG enemies | 7 named enemies using 6 sprite archetypes, including 1 boss |
| RPG encounters | 5 authored groups; clearing an encounter persists |
| RPG progression | 1 finite key → gate → boss → relic → return-home quest |
| RPG environmental hazards | 3: fire, spores, corruption |
| Colony progression | Solo opening, survivor rescues, construction and founding charter |
| Sprite coverage | 7,115 occupied regions; 11,887 total indexed regions across 3 publisher packs |
| Asset catalog | Schema 3; usage, interpretation, generation eligibility, identity evidence and future ideas |
| Focused Urizen review | 104 individually inspected crops; generation rules reference 43 distinct sprite IDs |
| Tutorials | Colony: 6 sections; RPG: 4 pages |
| Automated core tests | Last recorded baseline: 97 passing cases, split 41 colony / 56 RPG |

The RPG generates terrain, room connections/styles, selected objective locations and dressing. Enemy compositions, classes and the quest sequence remain authored. The generator does not create additional biomes or a new campaign storyline for each seed.

## Run and inspect

Run these from the repository root. The shell launchers also resolve the repository when invoked from another directory.

```sh
./play.sh
./play.sh --load
./rpg.sh
./rpg.sh --load
./rpg.sh --seed 1337 --scene forest
./rpg.sh --seed 7 --scene crypt
./rpg.sh --demo
./rpg.sh --scene atlas
```

`play.sh` starts the colony client; `--headless` or `--benchmark` routes it to `Regolith.Tools`. `rpg.sh` starts the RPG client; its headless/report modes exit before graphics initialization. RPG scenes are `town`, `forest`, `crypt`, `battle`, `victory`, and `atlas`; `--demo` selects `battle`.

```sh
./play.sh --headless 700 --demo --seed 42
./rpg.sh --headless --seed 42
./rpg.sh --generation-report --seed 42
./rpg.sh --validate-seeds 1000 --seed -500
```

Headless, generation-report and seed-validation modes write no player save. Seed validation checks 1–10,000 consecutive signed 32-bit seeds and reports fallback counts. Generation reports include terrain counts, room sockets/styles, solver statistics, and quest validation.

Both clients default to 1440 × 900 with a minimum of 1024 × 720. Use `--window-size 1280x800`, `--skip-intro`, or `--screenshot PATH --frames 12` when inspecting UI. These are native desktop games; the HTML atlas is a separate asset-inspection tool.

The SDK targets are defined in [global.json](../global.json) and [Directory.Build.props](../Directory.Build.props). `global.json` requests 10.0.100 with `latestFeature` roll-forward; setup/CI use SDK 10.0.402. [tools/dotnet.sh](../tools/dotnet.sh) locates a system or per-user SDK, and [tools/setup-dotnet.sh](../tools/setup-dotnet.sh) can install the latter. Python is a development/catalog tool, not a C# game runtime dependency.

## Code ownership

| Project | Responsibility and entry points |
| --- | --- |
| [Regolith.Core](../src/Ithica.Core/Colony/) | Graphics-free colony simulation. [Simulation.cs](../src/Ithica.Core/Colony/Simulation.cs) owns state/ticks/orders/power; [World.cs](../src/Ithica.Core/Colony/World.cs) owns terrain/environment; [CrewSystem.cs](../src/Ithica.Core/Colony/CrewSystem.cs) owns crew/rescues/progression; [GridRouter.cs](../src/Ithica.Core/Colony/GridRouter.cs) owns routing. |
| [Regolith.Client](../src/Ithica.Client/) | Colony window, mouse/keyboard input, clock, HUD, tutorial and map rendering. Start at `IthicaGame.cs`. |
| [Regolith.Tools](../src/Ithica.Tools/) | Colony headless scenarios and benchmarks. Start at `Program.cs`. |
| [Regolith.Graphics](../src/Ithica.Graphics/) | Shared `SpriteLibrary` and `MapCamera`: lazy textures, crop lookup, black-background removal, palette masks, bitmap text and camera transforms. |
| [Lantern.Core](../src/Ithica.Core/Expeditions/) | Graphics-free RPG maps, party, exploration, combat, generation and saves. [Campaign.cs](../src/Ithica.Core/Expeditions/Campaign.cs), [Combat.cs](../src/Ithica.Core/Expeditions/Combat.cs), and [Persistence.cs](../src/Ithica.Core/Expeditions/Persistence.cs) are partial definitions of `Campaign`. |
| [Lantern.Client](../src/Ithica.Client/Preview/) | RPG window/input, world and battle presentation, field guide and full native atlas. Start at `PreviewGame.cs`; rendering is in `WorldRenderer.cs` and `Interface.cs`. |
| [Regolith.Core.Tests](../tests/Ithica.Core.Tests/Colony/) | Colony environment, routing, work, power, rescues, charter and save continuation. |
| [Lantern.Core.Tests](../tests/Ithica.Core.Tests/Expeditions/) | RPG campaign/combat, procedural layouts, WFC/fallback, hazards and save continuation. |

[Ithica.sln](../Ithica.sln) now contains five projects: Core, Client, Graphics, Tools and Core.Tests. Colony and expedition rules share one graphics-free assembly but retain separate state at this checkpoint. Keep rules in the respective Core project and presentation in its client until the [target architecture](ITHICA_PLAN.md#target-architecture) is implemented. The gameplay cores have no MonoGame dependency and do not share simulation state. Cross-client graphics changes can affect both prototypes.

## Regolith colony

The player starts with **Ada alone** at Tranquility. A stranded survivor is present from the opening; further distress contacts arrive over time. Clicking a contact dispatches a ready colonist and spends **2 food + 2 water**. Rescues have outbound and escorted-return phases. Recruitment completes after the living survivor reaches pressurized shelter. Unassisted contacts lose suit oxygen and then health; failed rescues release their assigned worker.

Rescue dispatch is a direct map interaction. R locates a distress contact; it does not open a separate expedition tab.

Implemented systems include seeded geology/resource seams; mining, building, salvage and survey orders; rectangular designations; reserved construction costs and cancellation refunds; crew health/hunger/energy/morale/suit needs; local work selection; four-neighbor oxygen/heat/fire updates; breaches and incidents; daylight/storm-dependent solar output; connected deck power islands and local batteries; farming, rest and medicine; visibility, notices, overlays and following crew.

The founding charter requires **3 rescues, cycle 3, 4 living crew, 20 food, and a powered uplink in pressure**. One cycle is 480 logical ticks. A completed charter allows continued play. Colony loss offers a restart; exiting after loss preserves the previous save.

Blueprints are defined in [Regolith.Core/Models.cs](../src/Ithica.Core/Colony/Models.cs):

| Module | Purpose |
| --- | --- |
| Pressure hull / habitat deck / airlock | Enclosure, walking surface, power connectivity and a pressure-sealing entrance |
| Solar array / battery bank | Up to 12 kW daylight supply / 240 kWh storage per connected bank |
| Life support | Water-powered oxygen/warmth production; 3 kW draw |
| Hydroponics | Food production subject to water, pressure, temperature and 4 kW supply |
| Sleeping pod / med station | Rest and treatment; med station draws 2 kW |
| Earth uplink | Charter objective; costs 60 metal + 20 crystal and draws 5 kW |
| Radioisotope generator | Reliable 12 kW starter supply; cannot be manufactured |

There are 11 module definitions, including the starter-only RTG. Resources are shared metal/water/food/crystal stock; items are delivered to inventory abstractly. Crew can occupy the same cell. Colony combat, hauling, z-levels and a detailed social simulation are not implemented.

## Lantern worlds and progression

| Area ID | Environment | Size | Present content |
| --- | --- | --- | --- |
| `hearth` | Hearth, town hub | 36 × 26 | Authored houses/roads/services; seeded furnishings; no environmental hazards |
| `wilds` | Ashwood, forest biome | 48 × 32 | Cellular trees/ponds, meandering river and bridges, road branches, two encounters, key chest, shrine, camp |
| `crypt` | Sunken Abbey, ruin/dungeon biome | 51 × 37 | 35 generated rooms, three room styles, two ordinary encounters and boss group, strongbox, camp, seal and relic |

**Counting convention:** two exploration biomes plus one hub gives three environments. Cloisters, cisterns and collapsed vaults are Abbey room styles. Ponds, decorative cave mouths and hazard patches do not create additional playable areas.

Adding an area also requires updating `WorldBuilder.Validate` and the zone/discovery validation in RPG `Persistence.cs`, which currently expect three areas, plus portal and progression checks.

The quest is: leave Hearth → find `iron-cache` in Ashwood → open `iron-gate` in Abbey → defeat `abbey-abbot` → collect `relic` → return to `elder` in Hearth. The key is consumed on opening the gate; completion persists. The relic checks that the boss has been defeated. Four ordinary encounters are available for XP/supplies; their completion is not a condition for opening the key chest.

Hearth services are Warden Elowen (`elder`, quest), Old Garran (`smith`, advice), the Copper Kettle (`inn`, 12 gold for full recovery), and the provisioner (`merchant`, 10 gold for two potions/two torches/two rations). The smith has dialogue, not a crafting interface.

Starting shared supplies are **36 gold, 5 potions, 4 torches, 6 rations**, with light at 100. A torch restores up to 45 light; marked camps spend two rations to restore 18 HP, 6 mana and 12 composure per companion. The forest shrine restores up to 8 mana and reduces stress by 15 once. The key chest gives 18 gold and a potion; the Abbey strongbox gives 24 gold, two potions and a torch. See [Campaign.cs](../src/Ithica.Core/Expeditions/Campaign.cs) for interaction rules.

Defeat returns the party to Hearth at partial health/mana, increases stress, and removes a quarter of current gold. Cleared encounters, collected chests, opened gates and exploration persist. Returning the relic completes the quest and allows continued exploration.

## Party and combat

The party has four fixed roles, defined in [Models.cs](../src/Ithica.Core/Expeditions/Models.cs). These are game casts of sprite appearances, not publisher-confirmed character identities.

| Companion | Role | Level-one HP / MP | Class skills |
| --- | --- | --- | --- |
| Rowan | Warden | 52 / 12 | Longsword; Shield bash; Stand fast |
| Vesper | Ranger | 38 / 12 | Arrow; Hunter's mark; Volley |
| Iris | Arcanist | 34 / 18 | Wand; Ember; Chain arc |
| Sable | Vestal | 42 / 20 | Smite; Mend; Absolution |

Each class also has **Guard, Potion and Retreat**, giving six available action buttons per hero. Potions restore 24 HP or revive. Classes define equipment; an equipping/gear-swapping system is not implemented.

Battles are turn-based on the exploration map. Initiative uses speed plus seeded tie variation; enemy intent and target are telegraphed. Skills validate mana and targets before spending resources. Guard halves damage until the actor's next turn; burn and bleed damage at turn start; stun skips an action; mark grants bonus damage; stress at 80+ weakens attacks. Mend can heal/revive and clear bleeding. Victory gives XP, gold and a potion; leveling increases health, mana and attack strength.

Positions are for battle presentation: there is no tactical movement, range or flanking. Abbey formations stay within two Manhattan cells of their encounter marker. Exploration uses cardinal movement, collision, line of sight, fog/discovery and click routing that avoids unintended encounters/portals and prefers hazard-free paths.

## Enemies and encounters

There are **seven named enemies, six sprite archetypes, and one boss**. Bell keeper shares the `skeleton` appearance and basic attack behavior with Oathless guard. Repeated individuals and encounter-specific stats do not count as new named types. Rosters/stats are currently inline in `StartEncounter`; intent behavior is keyed by sprite alias in `NewRound`, both in [Combat.cs](../src/Ithica.Core/Expeditions/Combat.cs).

| Enemy | Sprite alias | HP | Base power | Armor | Behavior |
| --- | --- | ---: | ---: | ---: | --- |
| Briar hound | `wolf` | 23 | 6 | 0 | Attacks; bleeding on even rounds |
| Ashwood raider | `bandit` | 34 | 8 | 2 | Attacks; guards every third round |
| Grief wisp | `wisp` | 20–24 | 4–5 | 0 | Damage and high stress; stats vary by group |
| Oathless guard | `skeleton` | 30 | 8 | 2 | Armored melee attack |
| Cistern slime | `slime` | 28 | 6 | 0 | Basic melee attack |
| Bell keeper | `skeleton` | 30 | 7 | 2 | Armored boss escort; guard variant |
| Hollow Abbot | `abbot` | 112 | 12 | 3 | Cycles bleeding attack, enemy healing and party-wide sweep |

Base power is an input to combat resolution; actual damage also depends on attack type, armor, guard, conditions and random variation.

| Encounter ID | Area | Composition |
| --- | --- | --- |
| `wilds-hounds` | Ashwood | Two Briar hounds |
| `wilds-raiders` | Ashwood | Ashwood raider + Grief wisp |
| `abbey-hollows` | Abbey | Two Oathless guards + Grief wisp |
| `abbey-slimes` | Abbey | Two Cistern slimes |
| `abbey-abbot` | Abbey | Hollow Abbot + Bell keeper + Grief wisp |

The five groups retain these compositions across seeds. Names, variants and AI are game data; sprite rows/colors are not inferred species or animation sequences. Fire/spores/corruption belong to the environment and are not additional enemies.

## Procedural generation

Current generation rules are [assets/ithica-generation.json](../assets/ithica-generation.json), with rules schema 1 and generator version 2. [GenerationRules.cs](../src/Ithica.Core/Expeditions/GenerationRules.cs) loads an **embedded resource**, so changing the JSON requires rebuilding the executable.

1. **Hearth:** retain authored streets, buildings, services and exit; seed furnishings and appearance choices.
2. **Ashwood:** seed trees at 47% and smooth through four synchronous eight-neighbor passes; seed pond masks at 42% and smooth three passes. Tree birth needs five occupied neighbors; survival needs four. Add a continuous winding river, then carve roads/bridges and clearings around seeded objectives. Reserve routes from hazards.
3. **Abbey graph:** establish a short main route and seeded branches connecting all 35 rooms. The boss chamber is a leaf connected through its western seal.
4. **WFC assembly:** choose among 45 room tiles: 15 nonempty connection masks × three room variants. Sockets are north/east/south/west; `0` is masonry, `1` a centered arch. Mandatory graph edges and outer/boss boundaries constrain the domains. Optional connections can produce loops.
5. **Room realization:** apply cloister/cistern/collapsed styles, with weights 5/3/2; use two cellular erosion passes in collapsed rooms. Keep arches, center formation space, safe crosses and objectives clear; place scenery on valid ground.
6. **Validation:** check objects, arrivals, reachable objectives/return routes and boss/relic isolation before unlocking. Exhausted WFC attempts use exact graph sockets and cloister rooms; failed world validation uses the retained authored world and records the reason. Invalid fallback worlds throw.

The WFC solver uses 64-bit domains, queued constraint propagation, weighted Shannon entropy and at most eight seeded attempts. Socket masks describe **7 × 7 room chunks**, not individual sprite-sheet cells. Connectivity comes from the graph and validation as well as local socket rules.

Active map generation is in [WorldBuilder.cs](../src/Ithica.Core/Expeditions/WorldBuilder.cs), [WaveFunctionCollapse.cs](../src/Ithica.Core/Expeditions/WaveFunctionCollapse.cs), and [CellularAutomata.cs](../src/Ithica.Core/Expeditions/CellularAutomata.cs). [Zone.cs](../src/Ithica.Core/Expeditions/Zone.cs) contains map/path/visibility operations **and `LegacyWorldBuilder`**, retained for old saves and fallback. Preserve that compatibility path when changing generation.

Named seed streams separate generation stages and combat. A seed reproduces a fresh world for the same generator; changing the generator may change future worlds. Existing version-two saves restore their actual maps without generating replacement layouts. See [the generation guide](PROCEDURAL_GENERATION.md) for full rules and screenshots.

## Exploration hazards

[HazardField.cs](../src/Ithica.Core/Expeditions/HazardField.cs) updates reusable flat buffers every **four successful steps or waits in the active exploration area**. Blocked movement, menus, camps, combat and inactive areas do not advance these fields. Spread uses seed/cell/tick hashes independently of combat RNG.

| Hazard | Evolution | Cost per step/wait on the hazard |
| --- | --- | --- |
| Fire | Woodland ignition near the camp; finite material fuel; up to five ticks per ignition; exhausted trees/grass show dead-tree/grit artwork | Each conscious companion loses 1 HP and gains 1 stress |
| Spores | Persistent source near shrine/cistern; four-tick clouds spread and decay | Each conscious companion loses 1 HP and gains 1 stress |
| Corruption | Persistent source near an encounter; seven-tick growth cycles around it | Each conscious companion gains 3 stress |

Hazards cannot take a conscious hero below 1 HP and do not change terrain collision. Protected roads, clearings, room crosses and objective approaches exclude hazards. **T** spends a torch to clear hazards and their sources within three Manhattan cells and suppress regrowth there for eight hazard ticks; it works at full light when a hazard is nearby. Clearing removes the affected sources permanently. Existing campfire scenery remains a safe rest interaction.

## Sprites and authoring

There are three publisher packs, recorded in [CREDITS.txt](../assets/CREDITS.txt): Urizen by vurmux, Godot node icons by pixel-boy, and keyboard/controller prompts by Hyohnoo. Input prompts contain new and legacy variants. The original asset inventory is 664 PNGs and three ZIPs.

| Coverage | Current count |
| --- | ---: |
| Urizen occupied cells, main sheet + free 2-bit sample | 5,676: 5,551 main + 125 sample |
| Godot icon regions | 653 |
| Input prompt regions | 786 |
| Occupied regions across packs | 7,115 |
| Blank/separator/annotation regions | 4,772 |
| Total indexed regions | 11,887 |
| Colony bindings / font / distinct IDs | 60 aliases / 94 glyphs / 154 IDs |
| RPG bindings / font / distinct IDs | 99 aliases / 94 glyphs / 190 IDs |
| Bound by either game / unbound occupied sprites | 241 / 6,874 |
| Distinct IDs referenced by generation rules | 43 |
| Individually inspected crops in focused Urizen generation review | 104 |

Counts describe indexed regions, including variants/fragments; they do not mean 7,115 unique, fully identified standalone objects. Preserve original PNGs, stable IDs and crop rectangles. Tinting, masks and black-background removal happen in texture copies; the renderer uses nearest sampling. Ground/object data defines collision and occlusion explicitly.

| File | Authority and editing workflow |
| --- | --- |
| [catalog.json](../assets/catalog.json) | Generated schema-3 inventory plus every region's identity evidence, live binding usage, affordances, generation status and future ideas. Use this for current coverage. |
| [sprites.json](../assets/sprites.json) | Frozen import audit with IDs/crops/metrics/historical matches. Its usage flags describe import time. |
| [build_runtime_assets.py](../tools/build_runtime_assets.py) → [ithica-colony-sprites.json](../assets/ithica-colony-sprites.json) | Colony alias selection and identified shared font; selection is authored in the Python builder. |
| [ithica-expedition-bindings.json](../assets/ithica-expedition-bindings.json) | Authored RPG aliases pointing to existing stable sprite IDs. |
| [ithica-generation.json](../assets/ithica-generation.json) | Authored CA/material/room/hazard placement rules; embedded in Core at build. |
| [urizen-generation-review.json](../assets/reference/urizen-generation-review.json) | Authored per-crop observations, decisions and future uses for the focused review. |
| [build_game_catalog.py](../tools/build_game_catalog.py) | Builds catalog enrichments, `ithica-expedition-sprites.json`, `atlas-index.json`, `atlas.html`, `reference/rpg-bindings.md`, and `reference/urizen-generation.md`. |
| [atlas-template.html](../assets/reference/atlas-template.html) | Authored offline viewer/scene-lab template; regenerate `atlas.html` after edits. |
| [urizen-generation.md](../assets/reference/urizen-generation.md) | Generated human-readable table of the 104 inspected crops and actual generation references. |

Generation status is `runtime_rule`, `candidate_requires_rules`, `not_world_generation`, or `not_applicable`. A catalog idea or registered viewer sprite does not imply a gameplay system exists. Unused/unmapped entries retain proposals and required work. Publisher identity and game casting stay separate: the author's dog becomes a Briar hound; a leaf silhouette becomes corruption; uncertain figures/fragments retain their uncertainty.

The catalog builder reads `catalog.json` to retain its original image/archive inventory while replacing generated enrichments. Preserve that inventory; deleting the entire catalog is not a supported clean-regeneration step.

The native **F2 atlas** displays/searches all occupied regions, usage, generation notes and visual observations. The offline [atlas.html](../assets/atlas.html) embeds metadata/original images and adds a scene lab: pin sprites, paint ground/prop/actor layers, and export exact-ID JSON or PNG. Scene recipes are not loaded by the game or automatically learned as WFC samples. Controller artwork is available; controller input is not implemented.

```sh
# After RPG aliases, generation/review data, or atlas-template edits:
python3 tools/build_game_catalog.py
python3 tools/build_game_catalog.py --check

# After changing colony selection, regenerate it before the shared catalog:
python3 tools/build_runtime_assets.py
python3 tools/build_game_catalog.py
```

The normal builders use Python's standard library; `--check` also verifies original PNG/ZIP hashes. Rebuilding the frozen import audit through `build_sprite_reference.py` is a separate development task requiring Pillow and a renewed review of identities/mappings.

## Controls

The clients have separate control schemes. Full tables are in [README](../README.md) and [LANTERN.md](LANTERN.md).

| Action | Regolith | Lantern Below |
| --- | --- | --- |
| Main map input | Click/Enter inspect, rescue or designate | WASD/arrows or click to move; E/Enter interact |
| Zoom and pan | Wheel at pointer; +/− at cursor; right/middle drag | Wheel at pointer; +/− at party; right/middle drag |
| Follow / reset | F selected crew; G home; 0 reset zoom | G follow party; 0 reset/follow |
| Time | Space pause; 1/2/3 speeds; period single tick | Exploration turns on movement; period/Space waits |
| Work / supplies | D/B/P/X mine/build/survey/cancel; V rectangle | T torch/ward; R camp; H potion on selected hero |
| Information | C crew; E log; O environmental overlays | I pack; J journal; F2 atlas; F4 original colors |
| Tutorial | T six-section tutorial; ? controls | F1 four-page field guide |
| Combat | Not implemented | 1–3 skills; 4 guard; 5 potion; 6 retreat; click target/Enter confirm |
| Save / load | S or F5 / Shift+L or F9 | F5 or Ctrl+S / F9 |
| Window / exit | F11 fullscreen; Q quit | F11 fullscreen; Q quit |

Within the RPG atlas, type to search; F3 cycles packs, F6/Tab families, F7 usage, F4 original/masked artwork; arrows select, Page Up/Down or wheel pages, and detail-panel scrolling reads longer notes.

## Saves

| Game | Default path | Format/version |
| --- | --- | --- |
| C# colony | `~/.regolith/colony-csharp.json` | `regolith-csharp`, version 1 |
| C# RPG | `~/.regolith/lantern.json` | `lantern-rpg`, version 2; reads RPG version 1 |
| Archived Python colony | `~/.regolith/colony.json` | Python version 2; archived loader also reads version 1 |

The formats are separate. C# writes use a flushed temporary file followed by atomic replacement. Colony saves preserve authoritative world/crew/jobs/resources/rescues and RNG state; derived routing/power lookups rebuild on load. RPG version-two saves preserve actual terrain/sprite choices/objects/safe masks, discovery, hazard ages/fuel/sources/suppression/timing, party, quest and mid-combat state. RPG version-one saves reconstruct the original authored maps with hazards disabled and can resave as version-two snapshots.

Normal window close/Q saves. Colony loss preserves the previous save. RPG `--demo` and non-town preview scenes autosave only after a load resumes normal play; explicit F5/Ctrl+S can still save previews. `--frames` captures and native smoke runs do not autosave. Use `--save PATH` for inspection that might write a save.

Sources: [colony persistence](../src/Ithica.Core/Colony/Persistence.cs), [RPG persistence](../src/Ithica.Core/Expeditions/Persistence.cs), and the respective client exit/load handlers. Preserve compatibility and exact continuation when changing state or generation.

## Performance and platforms

The games run synchronous simulation on their client thread. The colony advances at eight logical ticks/second at normal speed; its 12 ms frame budget limits starting more ticks, not interrupting one long tick. It slows simulated wall-clock progress under overload. Colony routing allows 64 requests per tick; an individual BFS can still visit the whole map. There is no simulation worker, ECS, chunk streaming or crowd reservation.

Colony terrain/environment and routing use contiguous arrays, reusable buffers, cached active-floor/power connectivity and spatial work buckets. RPG generation runs at campaign creation and uses 35 WFC domains; active-area hazards advance at turn cadence. Rendering is bounded by the visible viewport. These are foundations, not a measured full Dwarf Fortress/Songs of Syx workload.

Recorded colony stress samples on Intel i5-8279U/macOS, .NET 10.0.13, after warm-up and over 120 measured ticks:

| Dense world / agents | Mean / p95 tick | Mining complete / queued |
| --- | --- | --- |
| 256 × 256 / 1,000 | 4.094 / 10.988 ms | 1,000 / 1,000 |
| 512 × 512 / 10,000 | 15.666 / 16.731 ms | 7,215 / 10,000 |
| 1024 × 1024 / 10,000 | 64.465 / 65.901 ms | 7,215 / 10,000 |

These samples use a dense sealed habitat, abundant resources, one nearby mining order per crew member and later idle movement; they exclude rendering, serialization, hazards and incoming rescues. Routing limits leave work queued. See [PERFORMANCE.md](PERFORMANCE.md) and [raw measurements](performance-baseline.json) before making scale/frame-rate claims.

CI is configured for Linux, Windows and macOS core/headless checks. Native graphics and published builds have been exercised locally on **macOS Intel**. Framework-dependent distribution requires .NET 10; installers, signing and store publishing are not configured.

## Verification

Choose checks for the changed subsystem. Documentation-only changes need source/count/link consistency checks; gameplay changes need the relevant core tests. Shared graphics changes warrant both clients; generation changes warrant seed validation plus full quest runs.

```sh
./tools/dotnet.sh restore Ithica.sln --locked-mode
./tools/dotnet.sh build Ithica.sln --configuration Release --no-restore
./tools/dotnet.sh test Ithica.sln --configuration Release --no-build
python3 tools/build_game_catalog.py --check

./play.sh --headless 700 --demo --seed 42
./rpg.sh --headless --seed 42
./rpg.sh --validate-seeds 1000 --seed -500

# Requires a desktop/OpenGL environment; opens real windows:
./play.sh --smoke-test --screenshot /tmp/regolith-smoke.png
./rpg.sh --smoke-test --window-size 1024x720 --screenshot /tmp/lantern-smoke.png
```

Last recorded gameplay verification, 2026-10-20: 97 core cases passed; 1,000 layouts for seeds −500 through 499 had zero fallback zones; 36 real full expeditions completed; colony/RPG native smoke checks passed 12/17 checks; the published RPG passed outside the repository; the offline atlas passed 14 checks; generated content and original asset hashes matched. These are the recorded baseline, not an assertion that a future checkout has just passed them. The extra expedition/browser checks were local verification; the persistent core cases and CI workflow are in the repo.

Ithica planning reconciliation, 2026-10-20: inspected current tick ownership, crew processing, RPG action pacing, fixed-party/site assumptions and save validation to ground the integration plan. The documentation change passed local link/anchor and code-fence checks across eight Markdown files, plus whitespace checks. Gameplay code and generated assets were unchanged; the gameplay suite was not rerun for this documentation-only change.

[CI](../.github/workflows/build.yml) runs locked restore/build/tests, both headless scenarios and 256 procedural seeds on all three desktop OS runners; the catalog check runs on Linux. This does not certify native graphics on every OS.

```sh
./tools/dotnet.sh publish src/Ithica.Client --configuration Release --self-contained false --output artifacts/game
./tools/dotnet.sh publish src/Ithica.Client --configuration Release --self-contained false --output artifacts/lantern
```

Published clients carry runtime artwork/manifests/credits; the RPG also carries its viewer index/all required source images. Keep `bin/`, `obj/`, `artifacts/` and player saves out of source control.

## Current gaps

| Area | Present boundary or future possibility |
| --- | --- |
| Combined campaign | Planned in ITHICA_PLAN.md; no shared population, clock, inventory, phase transition or save yet |
| Product rebrand | Ithica projects and asset configuration migrated; compatibility launchers, historical saves and explicit expedition preview retained |
| RPG world | Three finite areas and a fixed quest; no endless world, biome progression campaign or streaming |
| RPG enemies | Fixed authored encounter compositions and sprite-keyed intents; no procedural bestiary, roaming ecology or respawn system |
| RPG tactics | Top-down battle presentation; no tactical movement, range, cover or flanking |
| RPG items and dialogue | Shared consumables, class equipment, service purchases and fixed dialogue; no hauling, gear swapping, crafting, branching dialogue or per-item shop stock |
| Colony scale | One dense layer, shared inventory, overlapping crew and synchronous simulation; no z-levels, hauling, combat or rich social model |
| Art | Existing crops and palette masks; no confirmed directional animation sets, inferred sprite autotile rules or automatic fragment assembly |
| Input | Keyboard/mouse implemented; controller sprites are cataloged but gamepad controls are absent |
| Authoring tools | Atlas scene lab exports experiments; no runtime scene-recipe import, general map editor or mod API |

Catalog future-use ideas describe possibilities. They are not commitments or shipped features. This table records scope boundaries; later work should follow the user's actual request.

## Keeping this reference current

For a gameplay/content change, update the affected overview, counts, rosters, limits and source links here in the same change. Keep detailed guides synchronized where their behavior changes. Record verification commands/results with their actual scope and date.

Follow [the Ithica plan](ITHICA_PLAN.md) for integration work. Update implemented status here and stage acceptance there together. Preserve the distinction between old prototype inventories and new lunar content; do not claim the shared clock or new launcher exists before it runs and is verified.

For sprite changes, edit the authored inputs listed above and regenerate derived outputs. Retain publisher identity/evidence separately from game roles; leave unreviewed identities uncertain. Check source hashes and never infer animation, species, collision or sockets just from neighboring atlas indices.

For generator/save changes, preserve quest reachability, the locked boss cut, independent RNG state, protected approaches and old-save handling. Update the applicable format/generator version and tests deliberately. Do not reroll a loaded version-two map or remove `LegacyWorldBuilder` as unused code.

For content questions, verify counts in code/configuration: zones in `WorldBuilder`, room variants in `ithica-generation.json`, enemies/encounters in `Combat`, party/skills in RPG `Models`, modules in colony `Models`, and asset coverage in `catalog_summary`. Avoid copying historical chat counts into new documentation without checking those sources.

## Detailed references

| Reference | Use |
| --- | --- |
| [ITHICA_PLAN.md](ITHICA_PLAN.md) | Approved combined-game design, time contract, implementation stages and Ithica naming/compatibility migration |
| [README](../README.md) | Launching, colony play guide and general build instructions |
| [LANTERN.md](LANTERN.md) | RPG walkthrough, complete controls and player-facing rules |
| [PROCEDURAL_GENERATION.md](PROCEDURAL_GENERATION.md) | CA/WFC implementation, hazards, diagnostics and screenshots |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Simulation ownership, routing, power, persistence and extension boundaries |
| [PERFORMANCE.md](PERFORMANCE.md) | Measured colony scenario, limitations and reproduction commands |
| [CATALOG_V2.md](../assets/reference/CATALOG_V2.md) | Current schema-3 catalog documentation; filename retained for older links |
| [rpg-bindings.md](../assets/reference/rpg-bindings.md) | Every current RPG semantic alias and shared glyph mapping |
| [urizen-generation.md](../assets/reference/urizen-generation.md) | Focused visual review with generation use/unused candidates |
| [SPRITE_GUIDE.md](../assets/SPRITE_GUIDE.md) | Original exhaustive audit guide; import-time usage flags |
| [CREDITS.txt](../assets/CREDITS.txt) | Source publishers and recorded provenance/license declarations |
| [legacy Python README](../legacy/python/README.md) | Archived terminal implementation and its independent launch workflow |

Integration checkpoint, 2026-10-20: consolidated Ithica solution, Core and tests; all 97 existing core cases pass. Both prototype clients now live in one executable, with the independent RPG exposed as `--preview`. Shared campaign integration is the next stage.

Shared-domain checkpoint, 2026-10-20: `Ithica.Core.Campaign` now owns the colony clock, resident-backed variable parties, packed supplies, lunar persistent sites, transactional departure/return, rescued recruitment, salvage components, a buildable suit service and atomic unified saves. Thirteen integration cases cover these boundaries, bringing the core suite to **110 passing cases**. The native main client still uses the old standalone colony path at this checkpoint; connecting the shared campaign is the next stage. `regolith-csharp` and `lantern-rpg` readers remain intact.

Unified-client checkpoint, 2026-10-20: the normal launcher now starts the shared campaign. Map-click preparation, configurable supply packs and 1-4 residents, top-down field movement/combat, mouse/cursor zoom and pan, shared saves, arrival reports, six tutorial sections and the full native atlas run in one window. Native smoke passed **18 checks**, including the real rescue → salvage combat → cargo/injury return → constructed upgrade loop. Native surface/preparation/battle/structure/arrival/atlas scenes rendered at 1440×900 and/or 1024×720. Lunar validation covered **1,000 seeds / 2,000 sites with zero fallback**. Source sheets and existing IDs/crops remain preserved. Catalog casting and final reference consolidation are still in progress.

Continuation/recovery checkpoint, 2026-10-20: **125 core cases pass**, including all saved phases, nonsequential IDs in combat, inactive-site persistence, seal prerequisites, paid treatment/refills, exact return travel, component refunds and non-destructive legacy import. A fully incapacitated field party now makes one injured return across all action paths, preventing endless zero-oxygen travel; cargo is lost and recovery remains necessary. The deeper pump/seal/encounter/loot/return loop and its persistent home-water benefit are covered by command-driven checks.

Lunar catalog checkpoint, 2026-10-20: schema 4 adds `sprites[].ithica` to every region, separating **166 current selected IDs**, **14 field aliases/IDs**, **75 preview-only IDs** and **6,874 unbound occupied sprites**. Original identity evidence, all source files/crops, the 104 focused observations and legacy preview assessments are retained. Authored roles/limits and lunar future ideas live in `assets/ithica-casting.json`; the native/offline atlas exposes current lunar use separately.

Completed gameplay checkpoint, 2026-10-20: **131 core tests pass**. Home rest recharges field equipment and powered care improves recovery; escorted people consume oxygen only on logical ticks and retain a recruitment-ID link. Morale projection preserves fractions, home hull completion excludes away residents, and failed components remain recoverable. Frame tick-start budgeting is retained, dangerous home notices stop field routes, and encounter conditions/target reserves are visible. Native/published acceptance and current source-of-truth cleanup are recorded in the following documentation checkpoint.
