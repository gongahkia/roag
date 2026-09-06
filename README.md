# Dumbest Dungeon

A survival-horror party deckbuilder played entirely in a terminal. Choose four of ten crew archetypes to cross the derelict survey ship *Orison*, manage health, stress, light, and supplies, and fight through a shared card deck with visible enemy intents and four-rank positioning.

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

- New expeditions begin in the airlock crew hub. Choose four of ten archetypes; selection order assigns combat ranks 1–4. The original Warden, Engineer, Medic, and Scout party is selected by default. Space toggles a crew member, left/right changes a selected member's rank, and `C` browses that class's full card library.
- Arrow keys or `h`/`j`/`k`/`l` navigate, Enter confirms, and Escape cancels or pauses.
- When a card has several valid targets, its target cursor stays on the battlefield: the selected character sprite is highlighted and bracketed with `>` and `<`. Move between targets with left/right or `h`/`l`, press Enter to confirm, or Escape to cancel.
- Enemies that lose health flash white-on-red for a short frame with the damage amount over their sprite. Monochrome terminals use reverse video instead.
- Crew who gain health or block, lose stress, receive guard, or gain a positive status flash white-on-green with a compact change label. Party-wide buffs animate together.
- `E` ends the crew's combat turn, `U` uses a supply, `D` examines the deck, `P` pauses, and `?` opens help.
- Cards state which specialist, origin ranks, and target ranks they require. Spend the party's three shared energy, then end the turn so enemies execute their displayed intents.
- At zero health, a specialist reaches Death's Door. Further damage has a 35% chance to kill them and end the expedition.
- At 100 stress, a specialist gains an affliction and returns to 50 stress. Reaching 100 again while afflicted causes a collapse at Death's Door.
- Moving consumes six light. Below 30 light, travel adds stress and can cause ambushes, but combat offers one additional card reward.
- Camps can recover the crew, upgrade a card, or remove a card. Workshops can upgrade or remove one card.
- Saving and loading are manual and permitted mid-combat. Loading restores the pseudo-random stream as well as visible state, so future results remain reproducible.

## Content authoring

Gameplay definitions live in `dumbest_dungeon/data/game.json`, while `dumbest_dungeon/data/art.json` contains the title, crew and enemy sprites, and class card glyphs. Both catalogs are versioned and validated. Run the validator after editing either file:

The current catalog contains 10 crew archetypes, 75 unique cards, 25 enemy types, and 19 encounter formations. Rewards are filtered to the four classes currently in the expedition.

```sh
python3 -m dumbest_dungeon --validate-content
```

Cards compose reusable operations such as `damage`, `block`, `heal`, `stress`, `move`, `guard`, `status`, `draw`, `discard`, and `energy`. Events similarly compose resource and party-wide operations. Adding records or combining existing operations needs no Python change. A genuinely new mechanic requires an engine operation plus validation and tests.

Cards have a stable `id`, acting `hero`, energy `cost`, valid `from_ranks`, target mode, optional `target_ranks`, normal `effects`, and complete `upgrade_effects`. Enemy encounters may contain one to four enemy definition IDs. Startup validation rejects duplicate IDs, bad references, invalid ranks, unknown operations, and malformed balance data with a focused error.

Every hero and enemy ID must have a five-line, seven-column ASCII sprite. Every hero class also needs a three-line card glyph. Art is restricted to printable 7-bit ASCII so alignment remains stable across supported terminals. The combat screen displays the full opposing formations, and selected cards expand into bordered previews in combat, reward, deck, and facility screens.

## Verification

```sh
python3 -m unittest discover -v
python3 -m compileall dumbest_dungeon tests
python3 -m dumbest_dungeon --validate-content
```

The test suite covers deterministic generation, map connectivity, all card definitions, formation legality, Death's Door and stress collapse, enemy intent identity, facilities, a complete scripted expedition, and exact exploration and mid-combat save round trips.
