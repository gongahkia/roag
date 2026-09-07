# Dumbest Dungeon

A survival-horror party deckbuilder played entirely in a terminal. Choose four of twenty-five crew archetypes, cross one of six scrolling top-down ASCII worlds, evade or intercept biome-specific patrols, and fight through a shared card deck with visible enemy intents and four-rank positioning.

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

Biome worlds use content schema 6 and save version 10. Saves from earlier builds are rejected with an explicit version error rather than loaded incorrectly.

## Controls and rules

- New expeditions begin at a crew threshold. Choose four of twenty-five archetypes; selection order assigns combat ranks 1–4. The original Warden, Engineer, Medic, and Scout party is selected by default. Space toggles a crew member, left/right changes a selected member's rank, and `C` browses that class's full card library. The roster scrolls to support all twenty-five choices at 80×24.
- Each seed selects one of six world structures—branching, spine, ring, clustered, zigzag, or fractured—and four distinct biomes from the full eleven-biome catalog. A world's listed biomes are weighted affinities, not exclusions. Every 117×35 map then receives variable room footprints, routed corridors, side alcoves, pillars, and the selected mixture while preserving full walkable connectivity. Arrow keys or `h`/`j`/`k`/`l` move the `X` destination cursor and Enter makes the `@` party follow the lowest-travel-cost route, up to 18 travel ticks per order before item bonuses. A first left-click selects and color-highlights a visible tile; click that tile again or press Enter to confirm movement. During auto-pathing, `X`, Escape, or right-click cancels before the next atomic tile step. `Tab` cycles visible patrols, discoveries, objectives, and facilities; Space recenters the cursor on the party.
- Eleven biomes provide distinct traversal costs, environmental hazards, patrol cadence and behavior, information ranges, combat conditions, access objectives, room identities, enemies, and formations: Derelict Decks (`.`), Cryogenic Vaults (`_`), Hydroponic Canopy (`"`), Ash Foundry (`;`), Reactor Choir (`:`), Mycelial Warrens (`%`), Flooded Bilges (`~`), Ion Stormworks (`` ` ``), Impossible Archive (`-`), Null Expanse (`'`), and Ossuary Engine (`o`). Press `B` while exploring for the current biome's complete rules; see the [biome field guide](docs/BIOMES.md) for the compact catalog.
- Four visible access objectives (`K`), one for each selected biome, provide safe supply procedures and dangerous forced alternatives. Completing any two opens the locked (`L`) Overseer Core and activates its patrol; clearing every room or biome is unnecessary.
- Ten biome-affinity crew each contribute five new cards. Affinity cards remain usable everywhere but gain their listed +1 to +3 potency on damage, block, healing, and stress relief inside the matching biome.
- Each run places twelve discoveries across the ship: three visible boon signals (`+`), five visible salvage caches (`*`), two visible bargains (`!`), and two hidden curse traps. Boons and curses stack on individual heroes; items stack party-wide. Bargains exchange a boon or multiple item copies for a known curse. Six curses are unplayable cards that trigger while drawn or held.
- Normal and elite patrols follow biome-specific aggression, movement cadence, territorial leash, and hunt/guard/roam/erratic behavior. Only patrols inside the current biome's information range are displayed. Each patrol receives a seeded, biome-specific enemy selection within its threat budget and a pressure, disruption, screen, sustain, or setup/payoff plan before deploying into a compatible rank order. Contact immediately opens the formation combat screen.
- When a card has several valid targets, its target cursor stays on the battlefield: the selected character sprite is highlighted and bracketed with `>` and `<`. Move between targets with left/right or `h`/`l`, press Enter to confirm, or Escape to cancel.
- The combat hand is rendered as five portrait playing cards with corner cost/class marks and centered ASCII class glyphs. The selected card is inverted, while cards that cannot currently be played are dimmed. Card browsers use a larger portrait version with complete rank, target, and rules text.
- Enemies that lose health flash white-on-red for a short frame with the damage amount over their sprite. Monochrome terminals use reverse video instead.
- Crew who gain health or block, lose stress, receive guard, or gain a positive status flash white-on-green with a compact change label. Party-wide buffs animate together.
- Dodge negates the next direct hostile hit, riposte counters direct attackers, and cleanse removes marked, stun, vulnerable, weak, and wound effects. Enemy groups can mark, weaken, wound, or expose crew for a partner to exploit, and can heal, block, focus, or protect their weakest member. Intent selection favors setup attacks, primed payoffs, and support actions when an ally actually needs them. Intent text labels coordinated `SET` and `CASH` actions before they resolve.
- `E` ends the crew's combat turn, `U` uses a supply, `D` examines the deck, `I` opens the run-effect browser from exploration or combat, `P` pauses, and `?` opens help.
- Cards state which specialist, origin ranks, and target ranks they require. Spend the party's three shared energy, then end the turn so enemies execute their displayed intents.
- Card rewards deliberately present a setup/payoff Bridge, an unowned Corrective for a thin deck discipline, and a seeded Wildcard. Duplicate cards remain legal, but repeated copies receive progressively less offer weight. Upgraded card previews show their changed rules rather than the base text.
- Workshops can transform one technique into one of three same-specialist alternatives. Offers score current-rank usability, cross-crew setup/payoff bridges, broader coverage, and low duplication; transformation preserves deck size and removes the old upgrade.
- At zero health, a specialist reaches Death's Door. Further damage has a base 35% chance to kill them permanently. Their cards are removed from the shared deck and surviving crew close ranks; the expedition ends only on a full-party wipe.
- At 100 stress, a specialist gains an affliction and returns to 50 stress. Reaching 100 again while afflicted causes a collapse at Death's Door.
- Every two travel ticks consume one light; costly frozen, overgrown, flooded, archival, and void terrain therefore creates meaningful detours. Below 30 light, movement adds stress and contact can begin with a surprise enemy phase, but combat offers one additional card reward.
- Camps can recover the crew, upgrade or remove a card, or spend 2 supplies to remove one curse stack. Removing a curse card at a workshop also removes its matching hero-bound curse stack.
- Saving and loading are manual and permitted mid-combat. Loading restores the pseudo-random stream as well as visible state, so future results remain reproducible.

## Content authoring

Gameplay definitions live in `dumbest_dungeon/data/game.json`, while `dumbest_dungeon/data/art.json` contains the title, crew and enemy sprites, and class card glyphs. Both catalogs are versioned and validated. Run the validator after editing either file:

The current catalog contains 25 crew archetypes, 155 technique cards, 6 curse cards, 70 enemy types, 109 encounter formations, 11 biomes, 6 world types, and 18 definitions each for boons, curses, and stackable items. Rewards are filtered to living classes currently in the expedition.

```sh
python3 -m dumbest_dungeon --validate-content
```

Cards compose reusable operations such as `damage`, `block`, `heal`, `stress`, `move`, `guard`, `status`, `cleanse`, `draw`, `discard`, and `energy`. Events similarly compose resource and party-wide operations. Persistent effects declare an effect key plus a `linear`, `diminishing`, `threshold`, or `special` stack curve and cap. Adding records that use existing operations needs no Python change. A genuinely new mechanic requires an engine operation plus validation and tests.

Cards have a stable `id`, acting `hero`, energy `cost`, valid `from_ranks`, target mode, optional `target_ranks`, normal `effects`, and complete `upgrade_effects`. Affinity cards also declare a `biome` and bounded `biome_bonus`. Reward and transformation scoring derives build tags from those effects, so authored setup/payoff and formation tools can bridge across crew owners. Enemy encounter records provide one-to-four-member biome and tier templates. Run generation draws from those pools, scores complementary striker, controller, defender, and support roles, enforces 40–60 HP normal or 62–100 HP elite threat budgets, and serializes the resulting ordered formation and tactical plan into its room. Each biome definition owns validated hazard, traversal, patrol, visibility, combat, and objective sections. Startup and save validation reject duplicate IDs, bad references, incompatible biome members, out-of-budget formations, invalid ranks, unknown operations, incomplete biome rules, and malformed balance data with a focused error.

Every hero and enemy ID must have a five-line, seven-column ASCII sprite. Every hero class also needs a three-line card glyph. Art is restricted to printable 7-bit ASCII so alignment remains stable across supported terminals. The combat screen displays the full opposing formations, and selected cards expand into bordered previews in combat, reward, deck, and facility screens.

## Verification

```sh
python3 -m unittest discover -v
python3 -m compileall dumbest_dungeon tests
python3 -m dumbest_dungeon --validate-content
```

The test suite covers all six world layouts, all eleven biome mechanics, mixed four-biome selection, objective and hazard reachability, Overseer gating, biome encounter filtering and affinity bonuses, deterministic and cross-seed terrain generation, full-map connectivity, weighted pathfinding, patrol contact, mouse-coordinate translation, all card and enemy definitions, threat-bounded formation permutations, setup/exploit and wounded-ally intent weighting, conditional enemy damage, Death's Door and stress collapse, enemy intent identity, facilities, malformed state rejection, a complete gated expedition, and exact exploration, hazard, objective, and mid-combat save round trips.
