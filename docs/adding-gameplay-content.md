# Adding gameplay content safely

Use this guide for a new entity, player action, or system. It is written for both human contributors and coding agents. See [architecture.md](architecture.md) and [content-authoring.md](content-authoring.md) for the boundary being protected.

## The ten-question heuristic

Before editing, answer all ten questions in the change description or design notes.

1. What is the stable mechanical identity?
2. Which engine state and rules actually change?
3. Which presentation can an author rewrite safely?
4. Does RNG use only stable mechanical values and stable ordering?
5. Does persisted state store stable identity rather than rendered text?
6. Can a compatible pack rename and reskin it?
7. Can both Pygame renderers represent it without changing mechanics?
8. Does an existing command, view, or event already express it?
9. Could a new presentation field accidentally enter the mechanical fingerprint?
10. What alternate-pack, save/load, and determinism tests prove the boundary?

## New entity example

This synthetic example adds an archetype called `mire-lantern-keeper`; it does not add shipped content.

1. Add the stable archetype ID and its rules to the appropriate mechanical catalog. Define its stats, behavior, rewards, and legal interactions there.
2. Explicitly classify each new catalog field in `jomon.mechanical_compatibility`; unknown fields must fail projection.
3. Add the matching presentation slots in the relevant pack contract and default-pack JSON. Authors supply names/descriptions/narration, never rules.
4. Add optional `assets.json` bindings for the archetype. Bind a semantic ID to a logical image, effect, or glyph; do not put a path in mechanics.
5. Extend an existing immutable view only if a renderer lacks an authoritative, visibility-safe semantic fact. Do not expose a mutable catalog dictionary.
6. If an action produces a renderer-visible fact, emit a typed runtime event at the reducer/session boundary. Do not derive it by parsing a message or generic state diff.
7. Persist only the new stable state required by the mechanic and add any format-15 loader migration. Never persist a name, asset, event, or panel.
8. Test the mechanic, deterministic behavior, views, event ordering if used, save/load, fingerprint classification, and an alternate presentation pack.

## New player action pipeline

Use the smallest path that keeps one reducer authoritative:

```text
renderer input
  -> frozen semantic command
  -> GameSession.submit()
  -> existing or new authoritative reducer
  -> CommandOutcome(result_id, events)
  -> refreshed immutable views
  -> renderer feedback
```

Add a command only when existing commands cannot express the stable intent. The command may contain IDs, a `Position`, and meaningful values. It may not contain a keycode, row number, label, pixel coordinate, Pygame object, or text used as identity. A runtime event is useful when a renderer must know a semantic occurrence—movement, damage, item use, travel, or a tavern action—without parsing prose. A new panel alone does not justify an event.

## New system design template

Copy this into a proposal:

```text
Feature:
Mechanical purpose:
Stable IDs:
State and authoritative reducers:
Commands:
Views and visibility rules:
Runtime events:
Content-pack additions:
Asset/glyph bindings:
Persistence and format-15 migration:
Mechanical compatibility classification:
Graphical renderer interaction:
ASCII renderer interaction:
Determinism risks:
Tests:
Alternate-pack proof:
```

## Coding-agent workflow

Before editing:

- identify mechanics, semantic identity, pack presentation, and renderer ownership;
- inspect the relevant stable IDs, contract validation, command/session route, views, events, and tests;
- read the closest existing domain guide under `docs/`.

Never:

- use a display name, prose, row, or glyph as mechanics;
- parse a rendered message to introduce a state fact;
- let pack values affect collision, damage, availability, RNG, or AI;
- put Pygame objects or frontend state in `GameState` or a save;
- mutate `GameSession._state` from frontend code;
- make a renderer call a reducer directly;
- persist runtime events or make `state.SoundEvent` serve frontend audio.

After editing:

- run focused mechanic, view, event, and save/load tests;
- run an alternate-pack invariance test and fingerprint check;
- test both Pygame renderers for an active player-facing feature;
- run a headless import/use test so Pygame is not initialized by backend code.

## Stop and reconsider

Stop before merging if new code resembles any of these without an explicit compatibility-only rationale:

```python
if "dragon" in name:
if item.description == "...":
rng.choice(pack_authored_list)
if glyph == "#":
if "completed" in message:
session._state.foo = value
reducer(state, ...)  # called by Pygame directly
```

Replace the presentation value with a stable semantic ID, or place the rule in the engine catalog/state. If the needed repair is not narrow and clear, pause instead of creating a second mechanics implementation in `session.py` or a renderer.

## Testing framework

For a significant change, cover:

- **mechanics:** the authoritative reducer produces the intended state;
- **determinism:** identical state, seed, and commands produce identical state, RNG, result IDs, and events;
- **presentation invariance:** mechanically identical Pack A and Pack B differ in prose/assets/glyphs but not mechanics or event stream;
- **fingerprints:** presentation-only changes retain the mechanical fingerprint;
- **persistence:** format-15 save/load retains stable state;
- **renderers:** graphical and ASCII modes can render/interact with active features through commands/views;
- **headless use:** importing and exercising core/session code does not create a Pygame window, mixer, or font system.
