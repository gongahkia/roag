# ROEG — Architecture & Gameplay Specification v1.0

**Status:** Agreed design baseline, with explicitly identified provisional values.  
**Date:** 10 October 2026  
**Project:** ROEG (R-O-E-G), a fresh codebase; do not inherit ROAG's implementation.  
**Engine:** LÖVE 11.5 / Lua, subject to a local toolchain check before implementation.  
**Purpose:** Authoritative reference for staged implementation and design reviews.

## How to read this specification

- **FIXED** means an explicit product/design decision that should not be silently changed by an implementation agent.
- **PROVISIONAL** means a proposed implementation detail or balance value that should be measured and adjusted through playtesting.
- **DEFERRED** means a future requirement that should not be fully implemented in the first prototype. Preserve sensible boundaries, not speculative frameworks.
- An implementation tranche may satisfy only a subset of this document. Do not interpret a later-game requirement as permission to build it early.
- If implementation reveals an inconsistency, report it with alternatives instead of inventing a new product decision.

---

## 1. Vision and scope

**FIXED — Gameplay identity:** A 2D, turn-based, speed-scheduled, stage-based roguelike. The principal rewards are mastering difficult, readable tactical combat and assembling extremely powerful, synergistic boon combinations. There is a secondary but first-class discovery pillar: deep, learnable, procedurally situated secrets. Inspiration includes Risk of Rain's class/item interactions, Caves of Qud's grid-based exploration and atmosphere, the original NES Legend of Zelda's visual legibility, Dead Cells' branching biomes, and Animal Well's layered mysteries. These are design references, not asset/style reproduction mandates.

**FIXED — Product shape:** Long-term hobby game, initial PC development under LÖVE. Favor a clean extension path for new boons, enemies, bosses, biomes, secret patterns, assets and class skills. Minimal dialogue and lore. No external modding requirement. Windows, macOS, Linux, mobile and eventual controllers are portability goals, not simultaneous launch targets.

**FIXED — No premature engine:** Build a game with reusable internal systems, not a general-purpose game engine, full ECS, editor, visual scripting environment or universal procedural-generation plugin framework before gameplay is enjoyable.

**PROVISIONAL — Pacing:** A completed run aims for approximately 20–30 minutes; measure actual playtimes rather than engineering to a fixed room count alone.

## 2. Run structure and progression

1. **FIXED:** The intended full run consists of **three biome stages** followed by a **final boss encounter**. Each biome culminates in an elite or miniboss.
2. **FIXED:** Subsequent biomes are selected through a branching path system (Dead Cells-like). The exact number and ordering of biome choices is **PROVISIONAL**.
3. **FIXED:** Exploration is continuous inside a biome; enemies can traverse open corridors and follow the player between rooms. Ordinary room boundaries are not gameplay/loading barriers. Certain special encounters may seal temporarily.
4. **FIXED:** Each biome has a critical route to its elite plus optional branches, loot, and secrets. After the elite is defeated, the player receives an extraction grace period measured in **simulation world time**. A dangerous, effectively invincible pursuer eventually appears, pressuring the player to leave for the next biome. The exact grace duration and pursuit behavior are **PROVISIONAL**.
5. **FIXED:** Encounter pressure increases with elapsed simulated world time, primarily through reinforcements and spawn pressure rather than hidden unconditional enemy stat inflation. Both initial population and time-driven spawn systems exist.
6. **FIXED:** Death ends the active run. Permanent progression unlocks future **variety** (e.g. more classes, boon pools, enemies or encounters), not compulsory permanent raw stat advantages. The exact unlock list is **PROVISIONAL**.
7. **FIXED:** Normal enemy kills reward experience and currency. Currency buys chests. A regular chest presents **three randomly generated boon choices**; the player chooses one. Normal chests favor Common/Uncommon. Elites award a free chest with higher-tier odds; significant bosses similarly favor better rewards. Typical biome target: approximately **five ordinary chests**, subject to balancing.
8. **FIXED:** Leveling automatically and randomly increases one of the character's statistics, with a clear short notification. The player does **not** select a level-up upgrade. The starting stat vocabulary is max health, damage, defense, action speed, crit chance, crit damage. Class-specific weighted random growth is **PROVISIONAL**, not a mandate.
9. **FIXED:** Class abilities are changed structurally only by designated transformation boons, not ordinary levels. Compatible transformations modify a stable ability identity; incompatible transformations are explicitly declared.

## 3. Player and controls

**FIXED — First character:** Sword-wielding Adventurer. Eventually up to three initial playable classes, each with a distinct attack/skill identity and mainly shared boon access.

**FIXED — Movement:** Standard movement is four cardinal directions. Basic Sword Strike can target any one of the eight adjacent tiles. Future classes may have different attacks, targeting patterns, ranges, and potentially unique movement patterns.

**FIXED — Hybrid input:** Attempting cardinal movement into an enemy performs the basic bump attack. An explicit attack input followed by an eight-directional selection performs the basic attack. Special abilities use explicit inputs. Aiming, menu navigation and canceling an uncommitted selection consume no simulated time.

**FIXED — Empty attacks:** An explicitly committed attack into an empty tile consumes time and emits applicable `AttackPerformed` / `OnAttack` events, but not `HitConfirmed` / `DamageTaken` events. A blocked wall movement attempt consumes no time.

**PROVISIONAL — Adventurer moves:**

| Action | Base simulation-time cost | Behavior |
| --- | ---: | --- |
| Cardinal move | 100 | One tile |
| Sword Strike | 100 | One targeted adjacent tile; all eight directions |
| Sweeping Slash | 150 | Three-tile arc in a selected facing; precise arc rules to be tuned |
| Dash | 130 | Up to two cardinal tiles with per-tile traversability checks |
| Wait | 100 | Consume time without moving |

**FIXED — Ability resource model:** Actions are restricted by their simulation-time cost/recovery, **not baseline mana, stamina or cooldowns**. Attacks generally resolve immediately on commitment; the action cost is recovery time. Different future mechanics can be proposed later, but do not invent resources for the first version.

## 4. Deterministic simulation and scheduling

**FIXED:** The authoritative simulation must execute headlessly, without `love.*` dependencies. UI frame delta (`love.update(dt)`) is presentation time only and never independently advances turn simulation. World time advances when a valid gameplay action is committed, including Wait and attacks on empty tiles.

**Architecture:** Explicit game/session state, serializable actors and pending events, integer simulation ticks, a deterministic priority-based scheduler, a simulation-owned seeded RNG abstraction, and an action-resolution pipeline. No global `math.random` or wall-clock randomness in simulation decisions; render-only randomness must not affect gameplay RNG.

**PROVISIONAL:** Effective recovery cost can initially use `max(1, rounded(base_cost * 100 / action_speed))`, with action speed 100 as normal. Guard against zero/negative speed, overflow and floating nondeterminism. The exact scaling formula is balance-tunable, but monotonic speed behavior and deterministic ordering are required.

**FIXED — Tie ordering:** At the same simulated timestamp, **already committed scheduled attacks/effects resolve before new actor decisions**. After those committed events finish, the player wins ties against enemies becoming newly ready. Remaining tie order must be stable and deterministic (e.g. immutable event sequence number followed by stable entity ID); never rely on unordered table iteration.

**FIXED — Enemy telegraphs:** Enemies may select a shape/tiles and enter a windup before executing an attack. The target tiles are **locked when committed**, not tracked afterward. Telegraph and attack resolution obey **strict scheduling**; a fast enemy may finish the attack before the player receives another opportunity. Do not grant a synthetic reaction turn. The UI must present timing/telegraphs honestly.

**FIXED — Windup interrupts:** Ordinary damage does not interrupt a windup. Stun, displacement, and death cancel or invalidate it. Interruption checks happen through authoritative state, not animation callbacks.

**FIXED — Friendly fire:** Enemy attacks can hurt allied enemies by default. Attack targeting/collision and faction-dependent exceptional rules remain data-driven; do not automatically exempt the attacker's allies.

**FIXED — Resolution:** Player attacks apply damage immediately on commitment and schedule their next-ready time afterward. Multi-target hits perform one logical **damage batch** across targets before dispatching dependent on-damage/on-death effects. Deterministic target iteration and a consistent snapshot/evaluation point are required.

**Presentation:** Simulation emits immutable ordered records (event IDs, simulation times, entity refs and effect outcomes). The LÖVE presentation layer animates these records at its own pace, preserving meaningful telegraph-before-impact visual order even when multiple world-time events resolve between player decisions. Visual playback does not decide outcomes.

## 5. Entity/component model

**FIXED:** Start with a lightweight component-based entity model, **not** an external/full ECS framework.

Typical serializable components/state records:

- Identity: unique runtime instance ID and stable content definition ID.
- Position: `floor_id`, `x`, `y`; one consistent logical tile grid.
- Collision/occlusion capabilities: movement blocking, visibility blocking and projectile interaction independently.
- Combatant: health, stats, faction and resistances as needed.
- Actions/abilities: stable ability IDs, availability and relevant state.
- Controller: player input identity or AI behavior ID and serializable AI memory/state.
- Statuses: active effects with durations/expiry and source identifiers.
- Owned boons: per boon ID, **counts by rarity** and any serializable boon-instance data.
- Pending actions: source ID, locked targeting geometry, committed state, scheduled time and interruptibility.
- Interactables: door, chest, switch and secret mechanism state.

Behavior belongs to Lua modules and systems; never serialize closures, coroutines or live LÖVE userdata. Entities such as doors, chests and switches occupy the same world grids as combatants; they are not artificial room transitions.

## 6. Boons, triggers and causal effects

**FIXED — Priority:** Make compositional synergy a first-class, extensible system. Each boon definition has a stable ID, tags, trigger subscription(s), observation scope, reusable logical predicates, rarity-specific numerical parameters, stacking formula(s), and effect declarations/behavior IDs. Unusual behavior can use explicit Lua modules rather than expanding a generic scripting language.

**FIXED — Rarities:** Common, Uncommon, Rare, Legendary. Every boon mechanic can appear in different rarities; **rarity changes strength, not its identity**. For illustration only, Storm Conductor may proc lightning with Common 2%, Uncommon 6%, Rare 18%, Legendary 50%. These exact percentages are not balance commitments.

**FIXED — Inventory:** No hard limit on total boons or copies of the same boon. Track mixed-rarity copies without loss of provenance. For probability-based offensive boons, the default stacks add chance across rarities, with **overflow**: e.g. 150% means one guaranteed proc and a 50% chance at a second. Other boon categories can specify different stack formulas (extra projectiles, additive power, diminishing avoidance, etc.).

**FIXED — Events:** Both player and enemy actions and cross-entity/world relationships are observable. Examples: attack committed, hit landed, HP lost, damage blocked, kill, enemy windup started, moved, item used, status applied, ally damaged, adjacency predicates. An event contains event ID, source/instigator and targets, relevant actor/target tags, simulated timestamp, originating action ID, causal-chain ID, ancestry/provenance and sufficient immutable spatial snapshots to make same-chain checks unambiguous.

**FIXED — Damage semantics:** `DamageTaken` requires **positive actual HP loss**. Negated/fully blocked damage is a different event. Multiple distinct damage events within the **same causal chain** may satisfy combined conditions (e.g. two adjacent goblins both lost HP in one chain). Configurable multi-turn histories are **DEFERRED**. State whether spatial predicates are evaluated at the documented captured event positions or at another explicitly declared point; do not let removal/movement silently reinterpret previous events.

**FIXED — Scope:** Each boon declares observation range/scope, from local distance to entire active floor. Owner/source attribution and faction rules must be explicit. Prefer event-kind indexing, candidate filtering and spatial queries rather than scanning every boon against every entity every frame.

**FIXED — Chains:** Effects may trigger other boons, including effects from enemy actions and friendly fire. The same boon may activate multiple times in **independent branches** of a causal chain, but a branch must not recursively reactivate a boon already in its ancestry. Count each qualifying multi-event group once per chain by default unless a particular effect explicitly opts into additional matches. Process effects in deterministic order through a queue. A finite safety budget is an **error/diagnostic failsafe**, not permission to silently truncate legitimate gameplay. If resolution is expensive, the engine may spread visual/processing work across frames without accepting further player input before the authoritative chain is complete.

**FIXED — Proc coefficients:** Attacks and secondary effects specify proc coefficients, defaulting to 1.0; adjust eligible activation probability accordingly. Do not globally disable secondary triggers or impose an arbitrary cap on duplicate items.

**FIXED — Delayed effects:** Effects may be scheduled for a future **simulated** timestamp. Save their committed payload, source, provenance/ancestry and queue order. Waiting/reloading/transitioning must not erase outstanding effects erroneously.

**FIXED — Transformations:** A transformation boon modifies an ability's stable identity via a declared modifier, with explicit compatibility/conflict rules. The baseline ability remains referenceable by other boons. Do not silently select one transformation and discard another when they are compatible.

## 7. World model and procedural stages

**FIXED — Floors:** The first playable biome has one 2D continuous floor. The model uses floor identities and explicit positions so multiple floors and stairs can be added later. Only the active floor advances; inactive floors retain state with frozen local simulation time. Full multi-floor play is **DEFERRED**.

**FIXED — Tile model:** A consistent logical grid across all biomes, with independent walkability, line-of-sight blocking, projectile collision and visuals. Larger/offset sprites are permitted. Logical layers may distinguish ground, structures, decoration, placed entities and gameplay metadata; rendering need not mirror storage layers one-to-one.

**FIXED — Overgrown Ruins:** First biome is an overgrown, moss-covered ruined dungeon. Target approximately 8–12 varied connected rooms with corridors, side branches and loops. Default reference sizes: small 9×7, medium 15×11, large 23×17, with irregular/nonrectangular shapes allowed. Corridor lengths and forms should be meaningfully varied; **do not impose a strict short-corridor bias**.

**FIXED — Room authoring:** Start from handcrafted templates with bounded geometric variation. Templates describe tile layers, protected geometry, optional editable masks, role tags (combat/treasure/landmark/secret/elite; multiple permitted), doorway/socket markers, enemy/reward placement regions, and per-template rotation/mirroring permissions. Socket locations and their orientations transform along with room geometry.

**FIXED — Generation architecture:** A graph-first plan creates mandatory route, optional branches, loops and possible secret placements; selected room templates and corridors instantiate it. Each biome can eventually select genuinely different algorithms/heuristics while emitting a common generated tilemap/floor contract. WFC, example-driven heuristics and alternative algorithms are **DEFERRED**; do not implement them now just because templates may later serve as inputs.

**FIXED — Validation/repair:** Validate room graph connectivity, entrance→elite route, usable exits, combat clearance, chest access, and intended secret solvability. On failure, first attempt **bounded deterministic automatic repair** respecting authored protected masks and secret rules; revalidate after each repair. If unrepaired, retry deterministically, then fall back to a prevalidated safe layout. Log repair decisions and seed for diagnostics. Never silently accept an invalid level or destroy authored secret constraints.

**FIXED — Navigation/visibility:** Enemies can cross room boundaries; navigation uses the same world collision truth as players. Fog of war shows current visibility and retains previously explored terrain on an automatically updated map. Spawns should not appear without warning inside currently visible/occupied tiles.

**FIXED — Secrets:** Typically 1–2 ordinary secrets per biome, with rare much deeper mysteries. Secret rules and learnable clue patterns remain consistent between runs while positions, geometry and parameters may change. Discover them through ordinary movement, attacks and item use/placement. **No dedicated inspect/secret-search button.** In the initial version, terrain is primarily visual: limited explicit secret-triggering tiles or objects are permitted, but full environmental physics (fire, gases, water, destructibility) is **DEFERRED**. Secrets are optional and reward currency, chests, stronger/Legendary boons; no secret-specific permanent lore progression required.

## 8. Content data and the future editor

**FIXED — Authoritative content families:** tiles, room templates, biome rules, entity archetypes, abilities, boons, secrets and asset manifests. Use stable human-readable content IDs (e.g. `core:enemy/mossbound_guard`) and separate serializable runtime instance IDs.

**FIXED — Three concepts:** (1) authored immutable definitions, (2) generated concrete layout/placements, (3) mutable runtime state. Do not mutate source templates by running the game. Content definitions must validate types, uniqueness, references, sockets, boundaries and essential behavior IDs before gameplay.

**FIXED — Storage:** Prefer human-readable structured, non-executable content for editor-authored definitions, with Lua modules for genuinely new behaviors. Exact file format (Lua data/JSON/etc.) may be chosen pragmatically when implemented, but must support stable schema and editor-safe read/write. Avoid function closures inside content data.

**FIXED — Future editor:** A separate mode/tool **built with LÖVE**, sharing content models, validation and asset loading with the game. Eventually supports tile/terrain painting, sprite drawing, map layouts, placement zones, shader/visual parameters, external asset import and in-editor editing. Preserve editable source assets and build derived runtime atlases/optimized assets. Import-first is acceptable. Do **not** implement the editor or WFC in the first gameplay tranches.

## 9. Persistence and version boundaries

**FIXED:** Players can quit and resume an active run exactly where they left off on a compatible build. Keep active-run data separate from permanent unlock/progression data. Design save schemas at the start; complete UI/storage can arrive in a later tranche.

**FIXED — Snapshot contents:** simulation time, scheduled actions and event sequence order, pending telegraphs/targets, actor/component states and AI memory, boons with counts and rarities, transformation modifiers, generated map and runtime edits, discovery/fog state, chests and offered rewards, run/stage progression, RNG state(s), and any outstanding delayed effects/provenance.

**FIXED — Save timing:** Save only at a stable authoritative boundary (e.g. after a committed action and all immediate causal effects resolve, typically when awaiting player input). Pending future enemy attacks/delayed effects remain serialized in the scheduler. Do not serialize presentation animations as authoritative outcomes.

**FIXED — Compatibility:** Include schema and content/build compatibility identifiers. **No cross-version active-run migration requirement.** Detect and explain incompatible saves; don't load potentially corrupt state. Permanent progression is a separate file/schema so invalidating an active run does not inherently erase it.

**PROVISIONAL — Storage reliability:** Validate and checksum data; use a crash-safe approach supported by the chosen platform adapter (e.g. protected/dual-slot files if direct atomic replace is unavailable). The core serializer must not depend on LÖVE's filesystem.

## 10. LÖVE presentation and performance

**FIXED:** Camera scrolls continuously and smoothly with the player, but authoritative coordinates stay snapped to the logical grid. Animation is visual only. Display locked enemy telegraphs and timing clearly, including when strict scheduling leaves no player response before impact.

**PROVISIONAL target:** Stable 60 FPS on an ordinary laptop with room for approximately 50–100 active enemies and dense boon chains. Profile before complex optimization. Favor tile/sprite atlases, SpriteBatch when beneficial, cached static map layers and controlled particles; LÖVE can provide rendering statistics. Keep shader code/assets separate from gameplay rules. Avoid desktop-only native dependencies in the shared game core.

## 11. Prototype content and staged delivery

**FIXED — First vertical slice:** One Adventurer; one Overgrown Ruins biome; roughly three enemy archetypes (a slow telegraphed melee bruiser, a ranged lane attacker, a fast flanker); one distinct Thorn Warden-like elite; a handful (~8) of varied synergistic boon identities, each with four possible rarities; XP, currency, chest choice; at least one diegetic procedural secret; an elite reward, extraction countdown and pursuer. Names and exact statistics are **PROVISIONAL**.

**Suggested small verified tranches (adapt to findings without bundling):**

| Tranche | Complete, testable outcome |
| --- | --- |
| 0 — Foundation | Fresh LÖVE project, headless action/scheduler/RNG seam, deterministic tests, visible grid/movement smoke test, document and minimal serialization contract |
| 1 — Adventurer | All inputs and actions, eight-direction attacks, camera, targeting, action-time feedback |
| 2 — Enemies | Three enemy behavior types, strict telegraph timing/locked tiles, AI, friendly fire, stun/displacement, deterministic tests |
| 3 — Boons | Basic content registry, four rarities, event queue, predicates, stacks, proc coefficients, chain rules, a few real synergies |
| 4 — Progression | XP/random stat levels, currency, free elite chest and choose-one-of-three chest economy |
| 5 — Generation | Overgrown Ruins graph/templates, sockets, rooms/corridors, reachability and deterministic repair |
| 6 — Exploration | Fog, map, secret pattern(s), interactable doors/chests; further boon/event extensions |
| 7 — Run loop | Elite, extraction countdown, pursuer, stage-choice/stage-transition foundations |
| 8 — Persistence | Full compatible-build save/resume, pending schedule restore, corruption/compatibility handling |
| 9 — Gameplay acceptance | Real playthroughs, seeded regression tests, 60 FPS profiling, balancing and iteration |

Later extend to three branching biome stages/final boss, additional classes, deeper secrets, and eventually the editor. **Do not wait until tranche 8 to define or test serializable state.**

## 12. Non-negotiable verification invariants

1. No player action → no authoritative simulation movement/time advance, even if many real seconds pass.
2. Same initial compatible state + RNG state + action sequence → same final authoritative state and ordered gameplay events.
3. Valid actions have specified recovery costs; invalid wall movement and canceled targeting consume no time; an empty basic attack does.
4. Already committed attacks beat newly ready actors on exact timestamp ties; player wins ties against newly ready enemies.
5. Enemy attack targets stay locked; strict schedule is followed; damage alone doesn't interrupt, but stun/displacement/death do.
6. Batched multi-target damage resolves before dependent boon-event triggering.
7. A boon can trigger through different causal branches, but no ancestry branch may recurse infinitely; deterministic budget exhaustion surfaces a diagnostic failure instead of silently discarding effects.
8. Inactive floors' simulations remain frozen; their state is preserved.
9. Generated content validates connectivity and optional secret solvability; protected geometry is not silently overwritten by repairs.
10. A stable save/reload round trip preserves RNG and pending scheduler actions with an equivalent future simulation.
11. Pure simulation tests run without a LÖVE window and without `love.*` globals.
12. The game can be launched with LÖVE and shows actual interactive behavior, not only scaffolding.

## 13. Explicitly unresolved / tunable details

The following are deliberately **not** fixed design commitments: exact damage/HP/defense formula, crit distribution, enemy speeds, numerical skill arc and dash edge cases, boon activation probabilities and rarity distribution, chest prices, XP growth/stat weights, pursuer arrival time and AI, number of next-biome branch choices, specific biomes after Overgrown Ruins, final boss design, rendering pixel scale/resolution, and detailed friendly-fire kill/assist reward attribution. Initial implementation choices must be documented as provisional and covered by tests where they affect invariants.

## 14. Deferred functionality — do not build yet

Full editor, native sprite-painting UI, WFC, generalized rule-heuristic learning, several floors running concurrently, water/fire/gas simulation, online/multiplayer/subscriptions/leaderboards, elaborate dialogue/lore, external modding, fully generic ECS, and speculative plugins. Support extension through simple contracts only where current features need them.

## 15. Technical reference notes (not product requirements)

- LÖVE official download page currently advertises 11.5: https://love2d.org/
- LÖVE default `love.run` processes frame delta separately from callbacks: https://love2d.org/wiki/love.run
- LÖVE `RandomGenerator:getState`/`setState` support restoring a PRNG's state **within the same major LÖVE version**: https://love2d.org/wiki/RandomGenerator:getState
- `love.filesystem` is an application-platform filesystem adapter; serialization must not be coupled to it: https://love2d.org/wiki/love.filesystem
- SpriteBatch reference: https://love2d.org/wiki/SpriteBatch

**End of authoritative design baseline v1.0.** Changes should be intentional, written down, and reflected in acceptance tests before downstream implementation tranches rely on them.
