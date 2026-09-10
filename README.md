# Dullest Dungeon

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
python3 -m dumbest_dungeon --audit-expeditions 60
python3 -m dumbest_dungeon --telemetry
```

A generated seed appears in the map HUD and ending screen. Supplying `--seed` makes new expeditions in that process reproducible. The default save is `$XDG_STATE_HOME/dullest-dungeon/run.save.json`, or `~/.local/state/dullest-dungeon/run.save.json` when `XDG_STATE_HOME` is unset. The existing `dumbest_dungeon` Python package name remains the launch path; it is an internal compatibility detail, not the public title.

Biome worlds use content schema 25 and save version 39. The explicit migration chain supports the recorded version-26/content-20 baseline and versions 27–38; embedded schema-21 through schema-24 rules and the archived content-20 rules remain loadable when their fingerprints match; unavailable rules and unsupported versions are rejected. See the [persistence reference](docs/PERSISTENCE.md).

Completed and abandoned expeditions are recorded locally under `history/` beside
the save file. **Run history** on the title screen supports text filtering and
scrollable records of choices, casualties, encounters and arithmetic by source.
The CLI reader is `python3 -m dumbest_dungeon.history --filter warden --detail`.
History does not require an account or network connection. `--telemetry` explicitly
enables additional local `history/telemetry/*.ndjson` exports; it is off by default.
Session duration includes menus and pauses, is restored with manual saves, and
does not advance any game rule. Older saves disclose their missing early history.

## Controls and rules

- New expeditions first offer five curated squads with explained playstyles, formations, strengths, weaknesses, signature interactions, and complexity. Advanced custom selection keeps all twenty-five archetypes available. Selection order assigns combat ranks 1–4; Space toggles a crew member, left/right changes a selected member's rank, `C` browses that class's full card library, and `D` previews the combined starter deck. Formation and role warnings inform unusual parties without blocking them. The roster scrolls at 80×24.
- Each seed selects one of six world structures—branching, spine, ring, clustered, zigzag, or fractured—and four distinct biomes from the full eleven-biome catalog. A world's listed biomes are weighted affinities, not exclusions. Every 117×35 map then receives variable room footprints, routed corridors, side alcoves, pillars, and the selected mixture while preserving full walkable connectivity. Arrow keys or `h`/`j`/`k`/`l` move the `X` destination cursor and Enter makes the `@` party follow the lowest-travel-cost route, up to 18 travel ticks per order before item bonuses. A first left-click selects and color-highlights a visible tile; click that tile again or press Enter to confirm movement. During auto-pathing, `X`, Escape, or right-click cancels before the next atomic tile step. `Tab` cycles visible points of interest; after access opens it prioritises the next reachable Core leg. `G` selects that leg directly, `O` opens the scrollable expedition status, and Space recenters on the party.
- Eleven biomes provide distinct terrain-backed traversal, persistent three-tile hazard zones, patrol cadence and behavior, information ranges, combat conditions, access objectives, room identities, enemies, and formations: Derelict Decks (`.`), Cryogenic Vaults (`_`), Hydroponic Canopy (`"`), Ash Foundry (`;`), Reactor Choir (`:`), Mycelial Warrens (`%`), Flooded Bilges (`~`), Ion Stormworks (`` ` ``), Impossible Archive (`-`), Null Expanse (`'`), and Ossuary Engine (`o`). A `,` is always two-tick debris and `=` is always a one-tick service rail; biome-authored pockets, channels, or bands now place them rather than global decoration. Known hazard footprints remain marked after first perception and each cell can trigger once. Every selected biome also places one known `F` facility with two disclosed, irreversible procedures: these can suppress a remaining hazard zone, reveal regional sites, or make a biome-specific resource trade. Press `B` while exploring for the current biome's complete rules; see the [biome field guide](docs/BIOMES.md) for the compact catalog.
- Four visible access objectives (`K`), one for each selected biome, begin at authored ASCII landmarks. Each discloses two irreversible approaches with a resource category, danger class, travel burden, combat expectation, and completion benefit before commitment; the chosen approach then creates one or more seeded map stages. Completing any two produces a blocking access notice, changes the locked Core marker (`L`) to a stable open marker (`B`), and exposes its full route estimate. The other objectives remain visibly optional and record their outcomes for later systems. Clearing every room or biome is unnecessary.
- Ten biome-affinity crew have complete starter and draft pools rather than starter-only packages. Affinity cards remain usable everywhere but gain their listed +1 to +3 potency on damage, block, healing, and stress relief inside the matching biome.
- Each run places twelve discoveries across the ship: three visible boon signals (`+`), five visible salvage caches (`*`), two visible bargains (`!`), and two hidden curse traps. Boons and curses stack on individual heroes; items stack party-wide. Bargains exchange a boon or multiple item copies for a known curse. Six curses are unplayable cards that trigger while drawn or held.
- Normal and elite patrols follow biome-specific aggression, movement cadence, and readable doctrines: local roaming, static sentry duty, landmark circuits, facility sweeps, migrations, stalking, erratic motion, or active hunts. Committing to an objective temporarily alerts patrols in that biome. Only patrols inside the current biome's information range are displayed, and aiming at one names its doctrine and alert state. Each patrol receives a seeded, biome-specific enemy selection within its threat budget and a pressure, disruption, screen, sustain, or setup/payoff plan before deploying into a compatible rank order. Contact immediately opens the formation combat screen.
- When a card has several valid targets, its target cursor stays on the battlefield: the selected character sprite is highlighted and bracketed with `>` and `<`. Move between targets with left/right or `h`/`l`, press Enter to confirm, or Escape to cancel.
- The combat hand is rendered as five portrait playing cards with corner cost/class marks and centered ASCII class glyphs. The selected card is inverted, while cards that cannot currently be played are dimmed. Card browsers use a larger portrait version with complete rank, target, and rules text.
- Enemies that lose health flash white-on-red for a short frame with the damage amount over their sprite. Monochrome terminals use reverse video instead.
- Crew who gain health or block, lose stress, receive guard, or gain a positive status flash white-on-green with a compact change label. Party-wide buffs animate together.
- Dodge negates the next direct hostile hit, riposte counters direct attackers, and cleanse removes marked, stun, vulnerable, weak, and wound effects. Enemy groups can mark, weaken, wound, or expose crew for a partner to exploit, and can heal, block, focus, or protect their weakest member. Intent selection favors setup attacks, primed payoffs, and support actions when an ally actually needs them. Intent text labels coordinated `SET` and `CASH` actions before they resolve.
- `E` ends the crew's combat turn, `U` uses a supply, `D` examines the deck, `C` inspects the selected hand card, `R` inspects crew, `I` opens the run-effect browser from exploration or combat, `P` pauses, and `?` opens help. Enemy actions play one actor at a time; `F` switches between readable and fast playback, while Space skips the remaining presentation without skipping any game actions.
- Cards state which specialist, origin ranks, and target ranks they require. Spend the party's three shared energy, then end the turn so enemies execute their displayed intents.
- Card rewards present neutral, seeded tradeoffs such as reinforcing a synergy, covering a missing function, opening a new line, accepting positional risk, or committing to a duplicate. The labels explain why a card is present without declaring it correct. The player may skip to keep the shared deck lean, and repeated copies receive progressively less offer weight.
- Workshops can transform one technique into one of three same-specialist alternatives. The browser compares source and destination energy, legal ranks, effects, and authored build tags. Transformation preserves deck size and removes the old upgrade, so retaining the source can remain the better choice.
- Fifty signature or rare techniques—two per specialist—can take one irreversible mastery after their ordinary upgrade. The `ENGINE` branch intensifies one disclosed effect; `COVERAGE` widens the copy's legal origin ranks. Transformation clears both upgrade and mastery and discloses that loss before selection.
- At zero health, a specialist reaches Death's Door. Further damage has a base 35% chance to kill them permanently. Their cards are removed from the shared deck and surviving crew close ranks; the expedition ends only on a full-party wipe.
- At 100 stress, a specialist gains an affliction and returns to 50 stress. Reaching 100 again while afflicted causes a collapse at Death's Door.
- Every three travel ticks consume one light; costly frozen, overgrown, flooded, archival, and void terrain therefore creates meaningful detours. Below 30 light, movement adds stress and contact can begin with a surprise enemy phase, but combat offers one additional card reward.
- Camps can recover the crew, upgrade or remove a card, or spend 2 supplies to remove one curse stack. Removing a curse card at a workshop also removes its matching hero-bound curse stack.
- Saving and loading are manual and permitted mid-combat. Loading restores the pseudo-random stream as well as visible state, so future results remain reproducible.
- The optional tutorial expedition is recommended from the title screen and can be replayed. It uses the normal movement, combat, intent, reward, deck, and save rules, but never becomes or modifies a normal expedition.

Keyboard control covers every required flow. Mouse input is deliberately limited to exploration destination selection, confirmation, and route cancellation; menus, party selection, combat, targeting, and facilities remain keyboard-driven.

## Content authoring

Gameplay definitions live in `dumbest_dungeon/data/game.json`, authored card build tags and upgrade explanations live in `dumbest_dungeon/data/card_metadata.json`, and `dumbest_dungeon/data/art.json` contains the title, crew and enemy sprites, and class card glyphs. All catalogs are versioned and validated. Run the validator after editing any of them:

The validator currently reports 25 crew archetypes, 290 technique cards, 50 bounded mastery definitions, 16 infusion rules, 6 curse cards, 70 enemy types, 109 encounter formations, 11 biomes, 6 world types, and 18 definitions each for boons, curses, and stackable items. Each crew member has exactly four schema-23 expansion techniques and two schema-24 signature/rare mastery candidates. These totals are diagnostics, not validity requirements. Rewards are filtered to living classes currently in the expedition.

`--audit-expeditions` is a bounded structural check for 1--500 fresh seeds. It
enumerates two-objective approach corridors and optional facility detours, then
reports route, light, supply, hazard, backtracking, and patrol-post exposure by
layout. It does not play combat, predict moving patrols, or estimate a win rate.

`python3 -m dumbest_dungeon.content_audit` prints a canonical JSON census of owner
pools, starter and pool rank access, signed structural effect groups, tags,
opcodes, biome encounter density and enemy action counts. Similarity groups are
review signals; the census does not establish that designs play identically.

Reproducible headless regression policies use normal player commands, ordinary
HP/energy/rewards, the visible hand and intents, and known map sites:

```sh
python3 -m dumbest_dungeon.policies --seed 42 --policy explorer --output /tmp/expedition.json
```

Use `--squad breach_protocol`, `--policy rusher` or `greedy` to compare routes,
or `--checkpoint-every 17` to exercise save continuation. The report retains
commands, canonical state hashes, offers, encounters, resource use and the final
snapshot. Policy CPU duration is not human play duration; these myopic policies
are regression instruments and do not establish fun or a representative win rate.

```sh
python3 -m dumbest_dungeon --validate-content
```

Cards compose reusable operations such as `damage`, `block`, `heal`, `stress`, `move`, `guard`, `status`, `cleanse`, `draw`, `discard`, and `energy`. Events similarly compose resource and party-wide operations. Persistent effects declare an effect key plus a `linear`, `diminishing`, `threshold`, or `special` stack curve and cap. Adding records that use existing operations needs no Python change. A genuinely new mechanic requires an engine operation plus validation and tests.

Expedition Pressure advances from weighted travel, committed enemy phases,
objective stages, and facility procedures—not wall time or interface activity.
`T` shows its exact band, threshold, route cost, recent causes, and bounded
director profile. Encounters freeze that profile before their first intent;
Pressure earned during combat affects later contacts. Light remains the separate
depletable visibility, surprise, stress, and reward-opportunity resource.

Cards have a stable `id`, acting `hero`, energy `cost`, valid `from_ranks`, target mode, optional `target_ranks`, normal `effects`, complete `upgrade_effects`, an authored upgrade explanation, and validated build tags. Affinity cards also declare a `biome` and bounded `biome_bonus`. Reward and transformation logic uses the authored tags to recognise setup/payoff and formation bridges across crew owners without exposing an opaque best-card score. Enemy encounter records provide one-to-four-member biome and tier templates. Run generation draws from those pools, scores complementary striker, controller, defender, and support roles, enforces 40–60 HP normal or 62–100 HP elite threat budgets, and serializes the resulting ordered formation and tactical plan into its room. Each biome definition owns validated hazard, traversal, patrol, visibility, combat, and objective sections. Startup and save validation reject duplicate IDs, bad references, incompatible biome members, out-of-budget formations, invalid ranks, unknown operations, incomplete biome rules, and malformed balance data with a focused error.

Every hero and enemy ID must have a five-line, seven-column ASCII sprite. Every hero class also needs a three-line card glyph. Art is restricted to printable 7-bit ASCII so alignment remains stable across supported terminals. The combat screen displays the full opposing formations, and selected cards expand into bordered previews in combat, reward, deck, and facility screens.

## Verification

```sh
python3 -m unittest discover -v
python3 -m compileall dumbest_dungeon tests
python3 -m dumbest_dungeon --validate-content
```

The test suite covers all twenty-five authored crew identities and draft pools, curated and custom squads, card metadata and rank legality, neutral reward diversity and skipping, transformations, positional recovery, the tutorial, scrollable 80×24 screens, sequential enemy playback, and deterministic save/load. It also retains coverage for all six world layouts, all eleven biome mechanics, objective and hazard reachability, Overseer gating, encounter formations and intents, Death's Door, facilities, malformed state rejection, and complete scripted expedition flow. These rule tests do not establish numerical balance, comprehension, or ordinary-run win rates.

In combat, press **V** for scrollable source arithmetic and trigger order. Page
Up/Down and Home/End navigate long records; Enter or Escape returns to combat.
