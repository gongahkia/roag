# New Jomon: verified baseline and first playable contract

**Audit date:** 2026-09-28
**Repository baseline audited:** `main` at
`92a4cb49197a77b8a91eee2dc6738c0bf487d6bc` (`Remove retired visual
references`).

This is a reconnaissance record for the next game, not an implementation
claim.  The approved product direction lives in
[new-jomon.md](new-jomon.md).  The current tree is an engine reset with a
non-playable template pack; its `tests/fixtures/synthetic_content_pack` exists
only for automated verification.

## 1. Verified starting state

The supplied handoff is mostly accurate at a high level:

- format **16** is the active save baseline in `jomon/state.py:8`; format 15
  errors with `Save belongs to pre-reset content baseline and cannot be loaded`.
- `GameSession` is the application boundary (`jomon/session.py:14`), commands
  are semantic dataclasses (`jomon/commands.py`), views are frozen dataclasses
  (`jomon/views.py`), and runtime events are frozen transient dataclasses
  (`jomon/runtime_events.py`).
- `jomon/content_packs/template` has all five expected files and is structurally
  valid but non-playable.  The synthetic fixture is distinct and playable only
  for tests.
- Debug and ASCII Pygame skins share `PygameFrontend`, a `GameSession`, and the
  same `WorldView`; `FontStack` retains the bundled BigBlueTerm Nerd Font
  infrastructure.

Important differences from the handoff and from a complete game:

1. The baseline provides a compact *kernel*, not full versions of the named
   systems.  It has no mission resolver, feature/interactable topology,
   schedules, explicit clock, NPC AI, crew model, personal/shared funds,
   survival simulation, hacking/network space, stats/skill tree, augmentation,
   succession, recovery, or selective inheritance.
2. `GameState.seed` is persisted, and `test_same_pack_seed_setup_and_commands_are_deterministic`
   compares state, but current reducers consume no RNG or persisted RNG state.
   The handoff's broad "deterministic RNG" description is therefore an
   architectural intention rather than a demonstrated random subsystem.
3. Routes, recipes, quests, and activity categories are catalogue-backed
   minimums.  Travel only advances `turn`; a recipe checks for an input kind and
   appends an output; quest state has no reducer; activity dispatch only checks
   an ID and advances `turn`.  Only production activity is exercised by the
   test fixture.
4. The Pygame shell has a title screen, renderer settings, and CLI `--new` /
   `--load`; it does **not** yet offer a character-setup page, in-app save/load
   menu, pause panel, inventory panel, route panel, activity panel, or general
   interaction UI.  `InteractCommand` is declared but `GameSession.submit()`
   does not handle it.
5. Topology is a static rectangular `.`/`#` grid owned by `GameState.rows`, not
   a separate semantic-topology module.  `WorldView` turns it into
   `terrain.floor` / `terrain.wall`; views enforce a simple Manhattan-radius
   visibility model.  It has no feature IDs, doors, sites, dynamic access, or
   map generation.
6. The asset resolver is a logical binding API.  It validates safe pack-relative
   resource paths and ASCII glyph bindings, but this reset baseline does not
   load pack images or audio into either renderer.

The old world and Dullest Dungeon are absent from production runtime.  Searches
found no curses/browser runtime dependency and no material old setting IDs;
only generic system terms and intentional test fixture IDs remain.

## 2. Capability matrix

Status vocabulary:

- **implemented and tested** — a current test directly exercises the behaviour.
- **implemented but unverified** — source exists, but this audit found no
  focused test for the claimed behaviour.
- **test-only** — useful proof, not shipped game content or a player feature.
- **documented-only** — a target described in documentation but absent in code.
- **absent** — no owning system exists in the current baseline.

| Capability | Actual evidence | Status | Important limit / first change class |
| --- | --- | --- | --- |
| Strict pack selection and validation | `catalog.load_content_pack`, `select_content_pack`; `BaselineTests.test_template_is_strict_and_non_playable` | implemented and tested | Schema is intentionally narrow. New mission/features need a **MINIMAL SYSTEM EXTENSION** to the validated `systems.json` shape. |
| Template and synthetic-pack separation | `jomon/content_packs/template/*`; `tests/fixtures/synthetic_content_pack/*` | implemented and tested | No shipped playable pack exists. Creating one is **CONTENT/AUTHORING** plus only the schema additions actually needed. |
| Pack playability gate | `ContentPack.playable`; `state.create_world`; `RendererTests.test_template_title_disables_join_in_both_renderers` | implemented and tested | Default launch selects the template; Join is correctly disabled. Selecting a future default playable pack is **UI EXPOSURE/CONTENT**. |
| Stable IDs and non-mechanical lore/connections | `catalog._id`, `LoreEntry`, `ContentConnection`, `mechanical_fingerprint`; baseline lore/connection tests | implemented and tested | Connections cannot drive access, hostility, routes, or quest gates. Those require an **ENGINE EXTENSION REQUIRED** owning mechanic. |
| Mechanical fingerprint and content-bound saves | `catalog.mechanical_fingerprint`; `GameState.to_dict`; `game_state_from_dict`; save test | implemented and tested | The test proves lore/connection wording does not alter the fingerprint, not broad pack compatibility. |
| Save/load and pre-reset rejection | `save.save_game/load_game`; `state.SAVE_FORMAT`; `test_pre_reset_saves_reject_clearly` | implemented and tested | No durable mission history, successor, memory package, or RNG state exists yet. |
| Semantic commands and outcomes | command dataclasses; `GameSession.submit`; `CommandOutcome` | implemented and tested for move, attack, equip, travel, craft, production activity | `InteractCommand` is unused. A real operation requires **ENGINE EXTENSION REQUIRED** command(s), validation, outcome state, views, and event(s). |
| Immutable renderer-neutral views | frozen views in `views.py`; Pygame uses `world_view` / `actor_views` | implemented but unverified | No `FeatureView`, operation, clock, network, crew, succession, or memory views. Add only when owning mechanics exist. |
| Transient runtime events | frozen event types; command paths return event tuples | implemented but unverified | No event-persistence test; events must remain feedback, not historical truth. |
| Static movement and collision | `create_world`, `MoveCommand`, `world_view`; synthetic move test | implemented and tested | One static grid, `#` collision, no doors/features/dynamic navigation. Local operation access needs a **MINIMAL SYSTEM EXTENSION**. |
| Visibility and remembered cells | `views._visible/world_view`; both renderers honour `visible or remembered` | implemented but unverified | Radius-only visibility, no line-of-sight, smoke, lighting, sensing, or hidden dynamic feature rules. |
| Actor combat | `AttackCommand` branch and `AttackResolved`; synthetic test | implemented and tested | Adjacent fixed-power attack only: no movement, weapons stats beyond `power`, armour, cover, smoke, ranged attacks, or AI. |
| Inventory/equipment | `Item`, equip/unequip reducers, `inventory_view`; synthetic equip test | implemented and tested | Items always originate from pack state; no loot locations, money, condition, ownership, shared store, or personal store. |
| Quests | `Quest`, `quest_views`, synthetic fixture includes a quest | implemented but unverified | There is no transition command/reducer. A first mission cannot pretend the existing quest row resolves it. |
| Routes/travel | `TravelCommand`, `travel_view`; synthetic travel test | implemented and tested | Valid route only adds one turn; it changes neither location nor availability. It is not a district-travel system. |
| Crafting | `CraftCommand`, `recipe_view`; synthetic craft test | implemented and tested | Input is not consumed and no cost/quantity/recipe consequence is modelled. It can support a small prototype only after explicit rules are added. |
| Activities | `_ACTIVITY_SYSTEMS`, `ActivityCommand`, `activity_views`; production fixture test | implemented and tested for one production row; other categories implemented but unverified | These are generic ID lists with turn advancement, not chemistry, magic, worklines, vessel play, Draw, or Dice. |
| Character setup | `CharacterSetupView`, `CharacterSetupCommand`, `GameSession.create_configured`; engine test | implemented and tested headlessly | Choice values merely persist in `state.setup`; title Join calls `GameSession.create` directly. Proper pre-world selection is **UI EXPOSURE**. |
| Debug Pygame renderer | `PygameFrontend`; renderer test renders synthetic map and selects a cell | implemented and tested as SDL-dummy smoke | Only movement/attack control is exposed. No interactive playtest proves UX. |
| ASCII Pygame renderer | `AsciiPygameFrontend`, `FontStack`; same renderer tests | implemented and tested as SDL-dummy smoke | It is a skin over the same limited controller. The test does not prove live usability or pack media support. |
| Renderer switching | `app_settings.resolve_renderer`, `replacement_renderer`; `test_renderer_replacement_keeps_the_live_session` | implemented and tested | Shell has only title/settings/quit rows; settings file persistence lacks a direct test. |
| BigBlueTerm/Nerd Font infrastructure | bundled TTF, `font_stack.FontStack`, third-party notice | implemented but unverified | It is retained frontend infrastructure; it provides no setting content or gameplay. |
| Asset binding/glyph resolution | `assets.ascii_glyph`, `asset_binding`, `AssetManifest`; empty pack manifests | implemented but unverified | Logical references only; no resource-load proof. |
| Time, schedules, off-screen causal simulation | no state/model/reducer beyond `GameState.turn` | absent | **ENGINE EXTENSION REQUIRED**. A turn count is not a clock or a schedule. |
| Crew governance, personal/shared property, work economy | no crew/fund/property model | absent | **ENGINE EXTENSION REQUIRED**; defer beyond the first operation. |
| Network space, credentials, hacking | no network IDs/state/view/commands | absent | **ENGINE EXTENSION REQUIRED**; plan a bounded follow-up rather than label a menu choice as hacking. |
| Stats, skill tree, augmentation | no state/catalog/reducer/view | absent | **ENGINE EXTENSION REQUIRED**, deferred. |
| Death, successor choice, recovery, selective neural inheritance | actor death only; no courier death branch or continuity state | absent | **ENGINE EXTENSION REQUIRED** and scheduled immediately after the opening operation foundation. |
| Draw/Dice | only empty activity category names; reset policy says former implementation removed | absent | **DEFERRED** reusable-system design, not an active first-pack promise. |

## 3. Change classification and gap boundary

The first game should use the current kernel where it is real.  It must not
represent gaps through invented dialogue, a generic activity click, or a lore
connection.

### CONTENT/AUTHORING

- A separate shipped playable pack, provisionally named
  `jomon/content_packs/first-playable/` until content authors choose an enduring
  pack ID.  It is distinct from the template and from tests.
- A shared base, compact local topology, one initial operation, a few setup
  options, tool/weapon/item definitions, an objective, and pack-owned
  provisional presentation.  No first-district name or broader culture is
  decided by this audit.
- `lore.json`, `connections.json`, glyph bindings, and assets only for
  presentation/narrative context.  Their edits cannot gate the operation.

### MINIMAL SYSTEM EXTENSION

- Semantic map features with stable IDs and positions, exposed in views and
  rendered through pack asset/glyph bindings.  This is the smallest way to
  represent a base and an operation site without equating a visible glyph with
  a rule.
- An explicit operation state and feature-aware action path: assignment,
  eligible method, resolution method, persistent consequence ID(s), and return
  at the base.  It must update durable state rather than merely emit prose.
- A narrow frontend controller/panel for pending character setup, current
  operation status, feature interaction, save, and return feedback in both
  renderers.

### ENGINE EXTENSION REQUIRED

These cannot be represented by the present `Quest`, `ActivityCommand`,
`connections.json`, or static routes without misleading the player:

- **First-operation kernel:** operation definitions, method prerequisites,
  stable operation/method/result IDs, persistent operation state, command
  validation, immutable views, and runtime events.  Existing quests have no
  reducer and `InteractCommand` is unsupported.
- **Feature/access topology:** semantic feature IDs and an explicit access or
  interaction rule.  Current rows carry only `.` and `#`.
- **Continuity kernel:** crew roster/relationships, courier death state,
  successor selection, recovery location, memory packages, selective retention,
  and save semantics.  The current `Actor.alive` flag is insufficient.
- **Clock/jobs/schedules:** a durable explicit clock, schedule model, time-block
  jobs, and interruption rules.  `turn` is only a counter today.
- **Network space:** a distinct network topology/state, credentials, access
  commands, physical links, and consequence rules.  Do not call a terminal
  button or lore entry a network system.

### UI EXPOSURE

- A pre-world setup page using the existing stable setup IDs and
  `GameSession.create_configured`.
- A title flow that selects the shipped playable pack by default while retaining
  the template's correctly-disabled Join behaviour when explicitly selected.
- Contextual, stable-ID operation controls and read-only operation/inventory/
  status presentation for both Pygame skins.
- In-app save/load/pause panels are desirable shell work, but only the minimal
  save feedback needed by the opening loop belongs in the next contract.

### DEFERRED

Multiple districts, broad history generation, complete schedules, voting and
property policy, survival balancing, economy breadth, rich network space,
augmentations, skill tree, remote recovery service rules, Draw/Dice, vehicles,
vessel systems, and detailed cultures remain deferred.  They are not rejected.

## 4. Dependency-ordered near-term sequence

1. **CYBER-01 — one real local operation.**  Add a small shipped playable pack
   and only the semantic feature/operation/access state needed to begin at a
   base, prepare, reach an objective by two materially different real methods,
   return, and save the result.  Its full executable contract appears below.
2. **CYBER-02 — continuity kernel.**  Before a full city simulation, introduce
   a small crew roster, courier death, successor selection, recovery of a
   deceased implant/body component, memory-package selection, and a durable
   continuity record.  Start with fixed, tested local recovery; do not assume
   remote sync or permanent memory accumulation.
3. **CYBER-03 — time and livelihood.**  Add a durable clock, a bounded routine
   job model, interruptions, personal/shared resource ownership, and the first
   schedule-driven inhabitant behaviours.  Test causal off-screen outcomes
   without narrative fabrication.
4. **CYBER-04 — network access space.**  Add an independently navigable but
   bounded network topology linked to physical devices, credentials, and local
   access consequences.  The physical/network operation contrast becomes real
   here; it must use the same session/view/event/save boundaries as the world.

The first operation deliberately uses a prepared-tool method and a combat
method.  That choice is not a rejection of hacking: current code has no
network model, so claiming a network option in CYBER-01 would be a fake menu.

## 5. Executable next-tranche contract

### JOMON-CYBER-01 — First playable local operation

**Goal**

Ship the first real, compact playable pack and a bounded operation/access
kernel.  From a normal Pygame launch, one operative selects setup, begins at a
shared base with an assigned objective, prepares, moves through local topology,
resolves the objective through either a prepared-tool path or a combat-cleared
path, returns to base, saves, reloads, and sees the same durable result in both
Debug and ASCII renderers.

**In scope**

1. Add one pack under `jomon/content_packs/first-playable/` (final pack ID must
   be stable and validated).  It supplies only a compact local area and
   provisional, easily rewritten cyberpunk presentation; it is not a decision
   about first-district canon.
2. Extend `catalog.py`'s strict system schema only with the minimum explicit
   `world` feature and `operations` definitions required by this slice.  Define
   stable feature, operation, method, result, and consequence IDs.  Validate
   uniqueness and references to known system entities; reject display-name or
   dangling references.
3. Extend `state.py` with serialisable semantic features and an operation record
   (at least assigned/resolved/returned state, chosen method ID, and durable
   consequence ID(s)).  Keep format 16 only if decoder defaults let current
   baseline format-16 synthetic saves load safely; otherwise make one explicit,
   tested format decision.  Do not introduce an event log or a hidden frontend
   field.
4. Add a stable `OperationAttemptCommand(operation_id, method_id)` (and a
   separate stable return command only if it makes the base transition clearer).
   Add a reducer path in `GameSession`, immutable operation/feature views, and
   prose-free operation runtime events.  The reducer, not either renderer,
   validates location, prerequisite, method, state transition, and consequence.
5. Implement two genuinely distinct methods for the same assigned objective:
   - **prepared tool:** the operative equips a pack-defined tool, reaches the
     objective feature, and resolves it through the matching method;
   - **combat clearance:** the operative defeats the pack-defined blocker using
     existing combat, reaches the feature, and resolves through the matching
     method.

   Each route must produce a mechanically distinguishable state: for example,
   the combat route leaves a defeated actor while the tool route preserves it;
   the selected method and its consequence are persisted; the operation cannot
   resolve before its own verified conditions hold.  It must not be two labels
   for one unconditional reducer branch.
6. Tie resolution and return to an explicit objective/operation record.  It
   must be visible through a view and survive save/load.  A completed operation
   may update the existing generic quest row only as a real, reducer-owned state
   transition, never by parsing its title.
7. Update `pygame_frontend.py` and `pygame_ascii.py` only as presentation and
   input adapters.  Both render semantic features, pending/current operation
   status, selected legal method(s), and the returned result; both submit stable
   IDs through `GameSession`.  Implement a compact pre-world setup page using
   `pending_character_setup_view` and `create_configured`; do not create a
   world merely to render choices.
8. Make the new pack the normal default only once it validates and is playable.
   `JOMON_CONTENT_PACK` must still select the non-playable template or a custom
   pack; the template must still disable Join cleanly.

**Out of scope**

- Separate network-space play, credentials, real hacking, cloud recovery,
  procedural history, schedules, crew votes, shared funds, survival meters,
  jobs, skill trees, augmentation, successor selection, and broad city design.
- Extra districts, factions, names, cultures, assets, audio, or setting lore
  beyond the small pack content strictly needed for a comprehensible local
  operation.
- Draw/Dice, Dullest Dungeon, external services, runtime LLMs, event sourcing,
  or a large frontend framework.

**Provisional defaults**

| Default | Rationale | Replacement impact |
| --- | --- | --- |
| One local static map and one base feature | Demonstrates the complete loop without pretending to be a district generator. | Add world generation and travel later behind the feature/topology view boundary. |
| One assigned operation at session creation | Satisfies the approved opening without defining crew governance. | Replace assignment source later; retain operation IDs/states. |
| Tool route and combat route | Current engine can truthfully support equipment and combat; it cannot support network play. | CYBER-04 adds a network method system rather than re-labeling either route. |
| `turn` remains the only temporal cost in this slice | It is the only verified current time-like state. | CYBER-03 introduces an explicit clock and migrates action costs deliberately. |
| Format 16 with defaulted new fields when safely testable | The reset baseline has no released playable saves. | Bump only if a real schema incompatibility is proven. |

**Acceptance scenarios**

1. With the template selected, title/settings open under both renderers, Join is
   disabled, and no `GameSession` can be created.
2. With the first-playable pack selected, title → Join enters setup without a
   world.  Changing a selection or renderer consumes no game state/RNG; confirm
   creates one configured session and starts at the base with the operation
   assigned.
3. Invalid setup IDs, operation IDs, methods, wrong location, missing equipped
   tool, unresolved blocker, duplicate resolve, and premature return fail
   deterministically without mutation or event leakage.
4. Tool path: equip the required stable item, reach the target feature, submit
   the tool method, return to base.  It resolves and persists its method and
   consequence while leaving the blocker alive.
5. Combat path: defeat the blocker through existing `AttackCommand`, reach the
   target feature, submit the combat method, return.  It resolves and persists
   its different method/consequence while retaining the dead actor state.
6. Both paths use the same seed and command sequence deterministically; save
   after resolution, load, and continue with identical operation/quest/actor/
   inventory/position/turn state.  Pack text, glyph, asset binding, or renderer
   choice does not change the mechanical fingerprint or outcome.
7. Debug and ASCII each render the same feature/operation view and can complete
   either path through semantic commands.  A dummy-SDL smoke test is required;
   one manual live-window checklist must distinguish that from usability proof.

**Likely files/systems**

| Area | Likely files | Intended responsibility |
| --- | --- | --- |
| Strict content schema | `jomon/catalog.py`, template and new pack `systems.json` | Add and validate explicit feature/operation definitions. |
| Durable mechanics | `jomon/state.py`, `jomon/session.py`, `jomon/commands.py`, `jomon/runtime_events.py`, `jomon/views.py` | State, reducer, stable command, view, event, save representation. |
| Presentation | `jomon/assets.py`, new pack `assets.json`, `pygame_frontend.py`, `pygame_ascii.py` | Feature glyph/assets and semantic UI only. |
| Tests | replace/add `tests/test_engine_baseline.py` coverage; extend `tests/test_pygame_baseline.py`; add clearly named first-playable fixture tests if needed | Headless mechanics, invalid actions, deterministic branches, save/load, both renderer paths. |
| Documentation | `docs/ENGINE_BASELINE.md`, `docs/CONTENT_AUTHORING.md`, and this design record if implementation changes the baseline contract | Record actual schema, pack selection, and save policy; never edit README. |

**Validation commands**

Use only commands already supported by the repository:

```bash
uv run python -m unittest discover -v
uv run python -m compileall -q jomon tests
git diff --check
uv run python -m jomon --help
uv run python -m jomon --renderer debug
uv run python -m jomon --renderer ascii
```

The implementation must add focused tests for the acceptance scenarios before
claiming the slice complete.  The last two commands are interactive renderer
checks; automated dummy-SDL tests do not prove their controls or layout are
usable.

**Stopping condition**

Stop after one local operation works with the two stated real methods, durable
return result, save/load, stable-ID command/view/event boundary, and both
renderer paths.  Do not begin a network space, city simulation, continuity
mechanics, or additional setting design in the same tranche.

## 6. Persistence audit and future continuity boundary

Current saves contain seed, pack ID, mechanical fingerprint, static rows,
position, courier, items, actors, quests, route/recipe IDs, turn, setup, and
remembered positions (`GameState.to_dict`).  They do **not** retain runtime
events, renderer state, fonts, settings, causality/history, or an RNG state.

CYBER-01 must persist an explicit operation record because the opening result
is meant to remain after reload.  It must not use `CommandOutcome.events` or
frontend text as a substitute.  CYBER-02 must likewise define first-class
continuity state; an actor's `alive` flag alone cannot encode a deceased
operative's recoverable implant, selected memories, or successor decision.

## 7. Content-authoring boundary for the new pack

Use the five files already required by the loader:

| File | Current verified ownership | CYBER-01 use |
| --- | --- | --- |
| `manifest.json` | stable pack ID, format version, `playable` | Declare the separate playable pack. |
| `systems.json` | content instances and mechanical definitions | Own local rows/start, setup choices, items, actors, quest/route/recipe rows, then validated feature/operation data. |
| `lore.json` | presentation-only stable lore entries | Optional concise context; it cannot unlock an operation. |
| `connections.json` | non-mechanical stable-ID narrative relations | Optional context only; methods, blockers, and consequences stay in system definitions. |
| `assets.json` | logical presentation resource/binding metadata | Optional terrain/feature/actor/event glyph or asset bindings with safe relative paths. |

Do not turn test fixture content into shipped content.  Do not add a broad
universal text file or encode mission conditions in descriptions.

## 8. Baseline validation limits

The current suite proves a narrow headless path and dummy-SDL smoke rendering.
It does not prove interactive UX, actual image/audio resource loading, a
randomised system, operation flow, or any of the approved future mechanics.
Future tasks must report fresh command output and distinguish these categories
instead of citing this audit as proof of a later implementation.

## 9. CYBER-01 implementation record

CYBER-01 is now implemented against format 16.  `jomon/content_packs/first-playable`
is the default shipped playable pack; the template remains non-playable and can
still be selected explicitly with `JOMON_CONTENT_PACK`.

### Authoring fields added for this slice

`systems.json.world` may include `features`.  Each feature has a stable `id`,
`kind`, `position`, display `name`/`description`, and only the typed mechanical
references valid for its kind:

- `base` is the return feature;
- `maintenance_latch` names an `access_id` and an equipped item requirement;
- `access_gate` occupies a wall cell and becomes traversable only when its
  declared access ID is opened;
- `objective_cache` names its owning operation and objective item.

`systems.json.operations` is optional for packs that do not use operations.  A
row owns a stable operation ID, quest projection, objective feature/item,
return feature, and one or more stable methods.  Each method requires exactly
one physical fact: a declared opened access ID or a declared defeated actor.
It records a stable consequence ID.  Validation rejects dangling references,
wrong geometry, unsupported feature fields, and arbitrary method conditions.

`InteractCommand(feature_id)` is the local command for latch opening, objective
pickup, and base delivery.  It cannot grant remote access or bypass movement.
The operation record is the progress owner; `QuestView` projects its lifecycle
rather than keeping a separately mutable copy.

### Persistence and response policy

Format 16 remains current.  New operation-bearing saves persist feature
instances, opened access IDs, operation lifecycle/evidence/consequences,
objective disposition, actor health/alive state, inventory/equipment, position,
and turn.  Missing operation state in a save for an operation-bearing pack is
rejected.  A genuine pre-CYBER-01 format-16 save from the synthetic pack loads
with empty feature/operation defaults; format 15 remains unsupported.

The one provisional defender policy is `adjacent-on-valid-action`.  After a
successful time-consuming player move, attack, or local interaction, the
lexically first eligible living adjacent defender deals its configured fixed
damage once.  It does not act after death, on rejected commands, view queries,
rendering, save/load, setup, equipment changes, or UI navigation.  It does not
advance a second turn and it is not patrol, pathfinding, initiative, or RNG.

### Launch and controls

```bash
uv run python -m jomon --renderer debug
uv run python -m jomon --renderer ascii
uv run python -m jomon --load /path/to/save.json
JOMON_CONTENT_PACK=/path/to/template uv run python -m jomon
```

At title, choose **Join Game**, select setup options, and confirm with Enter.
No world exists until confirmation.  During play: arrows/WASD move, click a
visible or remembered cell to select it, `E` uses a selected local feature,
`F` attacks a selected adjacent actor, `I` opens inventory/equipment, `H` or
Escape opens/closes help, and Ctrl+S saves to the app's configured default save
path.  Loading remains available through `--load PATH` in this bounded slice.

Network space, crew simulation, succession/inheritance, schedules, survival,
Draw/Dice, and broad world generation remain absent and deferred.

## 10. CYBER-02A continuity record

**ENGINE EXTENSION REQUIRED:** `GameState` now owns an optional roster of
`CrewMember` records and one `active_member_id`. A crew member is the sole
owner of their condition, position, equipped state, and personal item instances.
The legacy courier/item fields remain only for packs without a `crew` domain.
This is deliberately narrower than a crew AI, voting, or shared-property system.

A crew-bearing pack declares `systems.json.crew` rows with stable identity,
position, health, personal item kinds, and presentation. The first member is
the setup-configured opening operative; later members retain their authored
health and kit. `SelectSuccessorCommand(member_id)` is valid only while the
active person is dead and selects an existing living roster member without a
turn or defender response. If no survivor remains, the dead world remains
saveable and no replacement is made.

A dead member retains their item instances at their final position.
`RecoverRemainsItemCommand(member_id, item_id)` is a local same-cell/adjacent
physical transfer into the living active member's inventory. It validates the
dead body, range, and exact instance before mutation, clears equipped state,
consumes one turn, and may trigger the existing defender response. It is
ordinary property recovery only: neural implants, memories, learned abilities,
body components, banking, sharing rules, and consent are not implemented.

Operation-objective uniqueness is validated across every crew member's custody.
A resolved objective may remain on a dead carrier; a successor must physically
recover it before returning to deliver it. First-playable saves now require the
crew roster and active identity and reject older fingerprints for that changed
pack. Format-16 synthetic no-crew saves remain compatible.
