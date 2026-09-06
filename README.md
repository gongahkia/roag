# Dumbest Dungeon

A survival-horror party deckbuilder played entirely in a terminal. Choose four of fifteen crew archetypes, explore the derelict survey ship *Orison* through a scrolling top-down ASCII world, evade or intercept moving patrols, and fight through a shared card deck with visible enemy intents and four-rank positioning.

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

Top-down exploration uses content schema 3 and save version 3. Saves from the earlier node-map builds are rejected with an explicit version error rather than loaded incorrectly.

## Controls and rules

- New expeditions begin in the airlock crew hub. Choose four of fifteen archetypes; selection order assigns combat ranks 1–4. The original Warden, Engineer, Medic, and Scout party is selected by default. Space toggles a crew member, left/right changes a selected member's rank, and `C` browses that class's full card library.
- Exploration is a 117×35 top-down ASCII ship. Arrow keys or `h`/`j`/`k`/`l` move the `X` destination cursor and Enter makes the `@` party automatically follow the shortest floor route, up to 18 tiles per order. A first left-click selects and color-highlights a visible tile; click that tile again or press Enter to confirm movement. `Tab` cycles patrols and unresolved facilities; Space recenters the cursor on the party.
- Normal, elite, and boss patrols move each time the party takes a step. Nearby patrols pursue the crew, while distant normal and elite patrols roam around their assigned compartments. Contact immediately opens the existing formation combat screen.
- When a card has several valid targets, its target cursor stays on the battlefield: the selected character sprite is highlighted and bracketed with `>` and `<`. Move between targets with left/right or `h`/`l`, press Enter to confirm, or Escape to cancel.
- The combat hand is rendered as five portrait playing cards with corner cost/class marks and centered ASCII class glyphs. The selected card is inverted, while cards that cannot currently be played are dimmed. Card browsers use a larger portrait version with complete rank, target, and rules text.
- Enemies that lose health flash white-on-red for a short frame with the damage amount over their sprite. Monochrome terminals use reverse video instead.
- Crew who gain health or block, lose stress, receive guard, or gain a positive status flash white-on-green with a compact change label. Party-wide buffs animate together.
- Dodge negates the next direct hostile hit, riposte counters direct attackers, and cleanse removes marked, stun, vulnerable, weak, and wound effects. These statuses are available to both crew cards and enemy intents.
- `E` ends the crew's combat turn, `U` uses a supply, `D` examines the deck, `P` pauses, and `?` opens help.
- Cards state which specialist, origin ranks, and target ranks they require. Spend the party's three shared energy, then end the turn so enemies execute their displayed intents.
- At zero health, a specialist reaches Death's Door. Further damage has a 35% chance to kill them and end the expedition.
- At 100 stress, a specialist gains an affliction and returns to 50 stress. Reaching 100 again while afflicted causes a collapse at Death's Door.
- Every two exploration steps consume one light. Below 30 light, movement adds stress and contact can begin with a surprise enemy phase, but combat offers one additional card reward.
- Camps can recover the crew, upgrade a card, or remove a card. Workshops can upgrade or remove one card.
- Saving and loading are manual and permitted mid-combat. Loading restores the pseudo-random stream as well as visible state, so future results remain reproducible.

## Content authoring

Gameplay definitions live in `dumbest_dungeon/data/game.json`, while `dumbest_dungeon/data/art.json` contains the title, crew and enemy sprites, and class card glyphs. Both catalogs are versioned and validated. Run the validator after editing either file:

The current catalog contains 15 crew archetypes, 105 unique cards, 35 enemy types, and 39 encounter formations. Rewards are filtered to the four classes currently in the expedition.

```sh
python3 -m dumbest_dungeon --validate-content
```

Cards compose reusable operations such as `damage`, `block`, `heal`, `stress`, `move`, `guard`, `status`, `cleanse`, `draw`, `discard`, and `energy`. Events similarly compose resource and party-wide operations. Adding records or combining existing operations needs no Python change. A genuinely new mechanic requires an engine operation plus validation and tests.

Cards have a stable `id`, acting `hero`, energy `cost`, valid `from_ranks`, target mode, optional `target_ranks`, normal `effects`, and complete `upgrade_effects`. Enemy encounters may contain one to four enemy definition IDs. Startup validation rejects duplicate IDs, bad references, invalid ranks, unknown operations, and malformed balance data with a focused error.

Every hero and enemy ID must have a five-line, seven-column ASCII sprite. Every hero class also needs a three-line card glyph. Art is restricted to printable 7-bit ASCII so alignment remains stable across supported terminals. The combat screen displays the full opposing formations, and selected cards expand into bordered previews in combat, reward, deck, and facility screens.

## Verification

```sh
python3 -m unittest discover -v
python3 -m compileall dumbest_dungeon tests
python3 -m dumbest_dungeon --validate-content
```

The test suite covers deterministic generation, spatial pathfinding, roaming patrol contact, mouse-coordinate translation, all card definitions, formation legality, Death's Door and stress collapse, enemy intent identity, facilities, a complete scripted expedition, and exact exploration and mid-combat save round trips.
