# Dumbest Dungeon

A survival-horror party deckbuilder played entirely in a terminal. Four specialists cross the derelict survey ship *Orison*, manage health, stress, light, and supplies, and fight through a shared card deck with visible enemy intents and four-rank positioning.

The game uses only the Python standard library. No installation or third-party package is required.

## Run

Python 3.11 or newer and an interactive terminal of at least 80×24 are required.

```sh
python3 -m dumbest_dungeon
```

Useful options:

```sh
python3 -m dumbest_dungeon --seed 12345
python3 -m dumbest_dungeon --save-file ./expedition.save.json
python3 -m dumbest_dungeon --validate-content
```

A generated seed appears in the map HUD and ending screen. Supplying `--seed` makes new expeditions in that process reproducible. The default save is `$XDG_STATE_HOME/dumbest-dungeon/run.save.json`, or `~/.local/state/dumbest-dungeon/run.save.json` when `XDG_STATE_HOME` is unset.

## Controls and rules

- Arrow keys or `h`/`j`/`k`/`l` navigate, Enter confirms, and Escape cancels or pauses.
- `E` ends the crew's combat turn, `U` uses a supply, `D` examines the deck, `P` pauses, and `?` opens help.
- Cards state which specialist, origin ranks, and target ranks they require. Spend the party's three shared energy, then end the turn so enemies execute their displayed intents.
- At zero health, a specialist reaches Death's Door. Further damage has a 35% chance to kill them and end the expedition.
- At 100 stress, a specialist gains an affliction and returns to 50 stress. Reaching 100 again while afflicted causes a collapse at Death's Door.
- Moving consumes six light. Below 30 light, travel adds stress and can cause ambushes, but combat offers one additional card reward.
- Camps can recover the crew, upgrade a card, or remove a card. Workshops can upgrade or remove one card.
- Saving and loading are manual and permitted mid-combat. Loading restores the pseudo-random stream as well as visible state, so future results remain reproducible.

## Content authoring

All gameplay content is in `dumbest_dungeon/data/game.json`. It contains versioned definitions for heroes, cards, enemies, encounters, events, afflictions, and balance constants. Run the validator after editing it:

```sh
python3 -m dumbest_dungeon --validate-content
```

Cards compose reusable operations such as `damage`, `block`, `heal`, `stress`, `move`, `guard`, `status`, `draw`, `discard`, and `energy`. Events similarly compose resource and party-wide operations. Adding records or combining existing operations needs no Python change. A genuinely new mechanic requires an engine operation plus validation and tests.

Cards have a stable `id`, acting `hero`, energy `cost`, valid `from_ranks`, target mode, optional `target_ranks`, normal `effects`, and complete `upgrade_effects`. Enemy encounters may contain one to four enemy definition IDs. Startup validation rejects duplicate IDs, bad references, invalid ranks, unknown operations, and malformed balance data with a focused error.

## Verification

```sh
python3 -m unittest discover -v
python3 -m compileall dumbest_dungeon tests
python3 -m dumbest_dungeon --validate-content
```

The test suite covers deterministic generation, map connectivity, all card definitions, formation legality, Death's Door and stress collapse, enemy intent identity, facilities, a complete scripted expedition, and exact exploration and mid-combat save round trips.
