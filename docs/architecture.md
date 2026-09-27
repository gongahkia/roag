# Jomon architecture

Jomon has one deterministic backend and two Pygame renderers. This is the canonical cross-layer guide; domain-specific pack contracts remain in the other files under `docs/`.

```text
mechanics and persisted state
        |
stable semantic IDs
        |
commands -> GameSession -> reducers -> CommandOutcome
        |                                  |
immutable views                       runtime events
        |                                  |
selected content pack              selected content pack
        |                                  |
 Pygame graphical renderer      Pygame ASCII renderer
 sprites / effects               BigBlueTerm / icon glyphs
```

The layers answer different questions:

| Layer | Owns | Must not own |
| --- | --- | --- |
| Mechanics | Rules, RNG, costs, collision, availability, AI, state transitions, persistence semantics | Names, prose, sprites, glyphs, layout |
| Stable semantics | Entity, archetype, action, result, option, terrain, status, and event identities | Display wording, menu rows, screen coordinates |
| Content pack | Fictional names, descriptions, narration, labels, semantic visual/audio bindings | Damage, walkability, RNG, costs, AI, save meaning |
| Renderer | Layout, input mapping, camera, panel state, fonts, iconography, animation and audio timing | Game rules or mutable `GameState` |

## Ownership test

Before adding a field, ask what changes when it changes.

- A change to a hit chance, route cost, collision flag, or progress rule is a mechanics change.
- A change to `forest-smoke-tender`, `attack.default`, `move.ok`, or `terrain.vessel.floor` is a semantic-ID change.
- A change from “Smoke Tender” to another fictional name is a content-pack presentation change.
- A change to a panel position, the `Ctrl+S` hint, BigBlueTerm font choice, or an icon is a renderer change.

The central rule is: **mechanics determine what happens; stable IDs identify what it is; packs determine how it is described; renderers determine how it is drawn, heard, and controlled.**

## Stable identities

Use durable semantic identifiers rather than a presentation value or position. An entity instance ID identifies one saved object; an archetype ID identifies its kind; an action/result ID identifies an operation/outcome; a presentation slot identifies text to resolve; an asset ID identifies a logical media resource. They are not interchangeable.

For example, an `ActorView` has a stable `id`, an `actor_kind`, and a `presentation_id`; an `AttackResolved` event carries stable actor and action IDs; `assets.json` maps a semantic ID to a logical resource such as `image.actor.default`. The display name is for the player, never a key for mechanics.

Design IDs to survive translation and reskinning. Do not use a list index or map row unless it is explicitly a persisted, stable identity. When converting old state, add a migration/recovery rule based on old stable facts, never on a current pack's wording.

## Commands, views, and outcomes

`jomon.commands` defines frozen semantic commands. `GameSession.submit()` is the frontend boundary: it validates command shape and delegates to the authoritative reducers. It returns a `CommandOutcome` with a stable `result_id`, revision, and an immutable event tuple.

Commands may carry stable IDs, simulation `Position` values, and meaningful values such as a wager. They must not carry keys, mouse pixels, Pygame objects, rendered labels, or menu indexes. A renderer may use a click or row to select an `InteractionOptionView`, then send its `interaction_id`.

`jomon.views` contains frozen, observational read models. They are renderer-neutral, visibility-safe, RNG-free, and do not advance time. Use `WorldView`, `ActorView`, `InteractionView`, inventory/equipment/quest/travel views, tavern views, setup views, and activity views instead of reading `GameSession._state`.

Runtime events in `jomon.runtime_events` are also frozen. They are command-scoped, deterministic, ordered semantic facts such as `ActorMoved`, `DamageApplied`, `TravelResolved`, and `TavernDiceRolled`. They contain no prose, asset ID, timing, or persistence state. `state.SoundEvent` is a separate mechanical AI-noise stimulus; it is not frontend audio.

## Determinism and compatibility

Presentation must never affect RNG. Seed and sort using stable mechanical values, not a display name, translated prose, glyph, asset path, or pack-authored ordering. Where compatibility requires an old presentation-derived value, preserve it as an explicit engine-owned historical token and document it.

Saves use format **15**. They store mechanical state and stable identities. They do not store runtime events, Pygame surfaces/sounds/fonts, renderer mode, camera, hover/selection state, panel rows, asset paths, or animation queues. Current state is presented again through the selected pack; historical rendered records can remain frozen to preserve old-save history.

`jomon.mechanical_compatibility.main_world_mechanical_fingerprint()` hashes the explicit mechanical projection of the required catalogs. Pack prose, filesystem path, manifest display name, asset manifest, and JSON formatting are excluded. The projection rejects unknown catalog fields, so adding a catalog field requires an explicit mechanical/presentation/ignored classification.

## Semantic topology

`jomon.semantic_topology` owns vessel and tavern cell identity, terrain, feature, walkability, and sight blocking. A `CellView` exposes semantic `terrain_id`, `topology_id`, and `feature_ids`.

```text
terrain.vessel.floor -> renderer chooses a tile or '.'
```

Never reverse that relationship. `#` is an ASCII presentation choice, not evidence that a cell blocks movement. Legacy topology tokens exist only for format-15 compatibility and must not become pack-controlled mechanics.

## Good and bad patterns

```python
# Bad: a reskin changes a rule.
if threat.name == "Smoke Tender":
    apply_smoke_rule()

# Good: mechanics use a durable semantic identity.
if threat.archetype_id == "forest-smoke-tender":
    apply_smoke_rule()

# Bad: presentation affects determinism.
rng.seed(item.description)

# Good: only engine-owned stable data affects determinism.
rng.seed(item.id)

# Bad: a renderer bypasses the application boundary.
session._state.position = target

# Good.
outcome = session.submit(MoveCommand(dx=1, dy=0))

# Bad: glyphs are topology.
if glyph == "#": block_move()

# Good: topology owns mechanics; a renderer maps identity to a glyph.
if cell.terrain_id == "terrain.vessel.wall": block_move()
```

Do not parse a message such as “Courier return:” to recover a new state fact. Persist a stable return-state identity. Do not let a pack declare collision, damage, AI behavior, or a random selection pool.

## Retained legacy compatibility

Some legacy prose and catalog fields remain to migrate old saves, reconstruct historical text, or support tooling. They are compatibility data, not current presentation authority. Dullest Dungeon is likewise dormant compatibility: its backend payload should round-trip in old saves, but it is retired from active gameplay and is not a current frontend extension target.
