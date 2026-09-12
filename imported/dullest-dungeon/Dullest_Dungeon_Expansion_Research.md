# Dullest Dungeon: Escalation, Buildcraft, and Replayability Research

## Executive decision

Dullest Dungeon should not pursue replayability by indiscriminately enlarging every global content pool. It already has enormous nominal permutation space: 25 crew create 12,650 unordered four-person parties; 11 biomes create 330 four-biome sets; across six layouts that is more than 25 million high-level configurations before formation, cards, objectives, encounters, rewards, and seeds are counted. The problem is whether those configurations create recognizably different decisions.

The highest-leverage next direction is a large but structured expansion built around four linked systems:

1. A visible, action-based expedition pressure director that creates a rush-versus-greed decision.
2. A deterministic interaction kernel that permits spectacular stacking and retrigger chains without allowing hangs or opaque arithmetic.
3. Curated expansion of the existing 25 crew and 11 biomes through broad shared mechanics, scoped reward lanes, enemy mutations, bosses, and alternate run rules.
4. A finite, calibrated base expedition followed by optional escalating loops, a global difficulty ladder, challenges, and run records.

This is the useful part of the Risk of Rain analogy. The game should let the player become absurdly powerful, while the world converts elapsed expedition actions into qualitatively more dangerous encounters. The standard run must remain finishable in roughly 30–45 minutes. After a recorded victory, the player may extract or carry the broken build into increasingly hostile loops.

“Infinitely fun” and “infinitely replayable” are not testable outcomes. The implementation should instead measure build diversity, encounter repetition, reward convergence, boss reach, win rate, duration, and loop depth. Model capacity can generate content candidates; it cannot replace curation or ordinary play.

## Current-state interpretation

The last supplied verified state ends at commit `639d678db2f57bc2b4c5df800b5ce3efaa6984c9`, after Implementation Pass 2. At that point the game has:

- 25 crew archetypes and 190 techniques plus six curse cards.
- 70 enemies and 109 encounter templates.
- 11 mechanically differentiated biomes, 11 facilities, and six world layouts.
- 18 boons, 18 curses, and 18 stackable items.
- Four biome objectives per run; completing any two opens the Overseer Core.
- A shared five-card hand and three energy across four owner-bound crew.
- Crew death that removes the dead owner’s cards and collapses ranks while survivors continue.
- Deterministic generation, serialized random state, save version 26, content schema 19, and 183 passing tests.

Pass 2’s three natural runs did not reach the Core, and Flooded Undertow appeared especially severe. A later calibration pass may already exist in the live repository. The next Codex run must inspect current history rather than assuming the pasted state is still HEAD. If base-run completion has not subsequently been demonstrated with a natural Core reach, a natural boss win, and a fair loss, that remains a hard gate before difficulty and content expansion.

The distinctive pitch is not the catalog size. It is:

> Four owner-bound crew share one position-dependent deck; the world grows more hostile as they detour for power, and a casualty tears that crew member’s cards out of the build without ending the expedition.

## What the comparison games actually teach

| Game | Relevant design evidence | Implication for Dullest Dungeon |
| --- | --- | --- |
| Slay the Spire | Mega Crit brainstormed thousands of cards, cut the chaff, iterated for years, and explicitly embraced powerful “Rube Goldberg” combinations in a single-player game. Path choice adds risk/reward without excessive rules. | Generate generously but ship selectively. Every card needs a job; powerful engines are desirable, but reward pools need pruning, skip options, and varied encounter tests. |
| Balatro | LocalThunk’s own Joker guidelines favor one simple effect, conditional power, broad keywords rather than named-card pairs, few exceptional rule breakers, and no average-run auto-picks. The developer distinguishes fun overpowered discoveries from easy dominant lines that suppress exploration. | Use shared tags such as wound, guard, mark, movement, stress, rank, owner, and Death’s Door. Avoid bespoke pair dependencies and mechanic creep. Make rare effects outrageous but conditional and legible. |
| Risk of Rain | Hopoo describes the core choice as looting for power while time raises enemy difficulty, or rushing the teleporter underprepared. The official game supports surprising item combinations and indefinite looping. Unlocks add complexity without strictly replacing old content. | Use a game-action clock, not wall time. Improve rewards at higher pressure, scale threat qualitatively, keep the base game winnable from a fresh profile, and put excessive builds into optional post-win loops. |
| Risk of Rain 2 item systems | Items use different stacking curves; proc propagation is controlled; Void items irreversibly transform their normal counterparts. Recent design work trades explicit burdens for exceptional power. | Author first-stack and later-stack behavior, add corruption/conversion branches, and bound automatic trigger chains. Use disclosed danger as the price of exceptional rewards. |
| Cobalt Core | Three selected crew contribute to a mixed deck; cards have split upgrade paths; the designers treat each enemy as a distinct tactical question and cut character mechanics that feel detached from the common rules. Screen constraints are treated as productive. | Preserve the shared deck. Add bounded mastery branches and make every enemy ask a positional question. Do not add isolated character minigames or text the 80×24 interface cannot explain. |
| Roguebook | Two heroes contribute five starters each to a combined deck. Its designer values emergent systems, improvisation, positional skill, controlled risk, and understandable losses. | Broad semantic bridges and disclosed risk suit Dullest Dungeon. Do not copy its deliberate large-deck incentive because four owners and rank legality already create hand friction. |
| Monster Train / Monster Train 2 | Clan mixing, bounded card-upgrade layers, Room and Equipment cards, Covenants, handcrafted challenges, alternate bosses, and Endless occupy different replayability axes. Pact Shards grant power while strengthening enemies. | Keep techniques, items, boons, curses, doctrines, and card augments in distinct acquisition lanes. Let optional power raise future threat. Separate ranked difficulty from expressive challenge modes. |
| Hades | The Pact separates chosen difficulty conditions; Extreme Measures and Middle Management transform bosses and minibosses instead of only inflating numbers. | A long ladder should include encounter-changing anchor ranks. Higher difficulty should alter formations, enemy plans, hazards, and bosses before merely adding health. |
| Wildfrost | Cards, companions, charms, challenge bells, and daily runs extend different axes. A major update added cards, enemies, charms, challenges, and balance changes together rather than relying on one count. | A bounded card-infusion layer can create combinations without flooding normal rewards. Complex chain consequences require clear previews and inspection. |
| Caves of Qud / Cogmind | Both combine authored structure with procedural assembly. Their developers emphasize identity, interface work, records, mastery, and procedural systems that remain strategically learnable. | Use authored enemy families, landmarks, bosses, and rules inside seeded worlds. A terminal game must be unusually legible and polished; randomness alone is not meaningful variety. |

Primary evidence includes [Mega Crit’s Slay the Spire design interview](https://www.gamedeveloper.com/game-platforms/road-to-the-igf-mega-crit-games-i-slay-the-spire-i-), [LocalThunk’s Joker-design guidelines](https://www.reddit.com/r/balatro/comments/1czo9g0/guidelines_for_joker_design/), [LocalThunk’s Game Informer interview](https://gameinformer.com/interview/2024/03/21/balatro-was-almost-called-joker-poker-and-other-details-from-its-creator), [Hopoo’s Risk of Rain interview](https://www.gamedeveloper.com/design/risk-of-rain-interview), the [official Risk of Rain overview](https://riskofrain.2k.com/), the [Cobalt Core designer interview](https://www.gamedeveloper.com/design/how-cobalt-core-makes-movement-as-exciting-as-fighting-in-its-roguelike-deckbuilder-combat), the [Roguebook designer interview](https://www.gamedeveloper.com/design/tackling-deckbuilding-design-in-abrakam-s-roguebook), [Monster Train’s Pact Shard explanation](https://news.xbox.com/en-us/2021/04/01/monster-train-the-last-divinity-dlc-available-now/), [Monster Train 2’s official features](https://shinyshoe.com/games/monster-train-2/), and [Hades’s Pact and encounter-variant notes](https://www.supergiantgames.com/blog/hades-welcome-to-hell-update-patch-notes/).

## The escalation model

### Expedition Pressure, not real time

The world should gain a serialized, monotonic pressure value. It advances only through game-world actions:

- Weighted movement ticks.
- Completed enemy rounds.
- Objective stages and major facility use.
- Explicitly disclosed event or bargain choices.
- Optional power purchases that say how much pressure they add.

Opening menus, reading cards, resizing the terminal, animations, and real-world thinking time must never advance pressure. This preserves accessibility and the deliberative nature of turn-based play.

Pressure and light must remain distinct:

- Light is a depletable expedition resource that affects local visibility, ambush risk, stress, and rewards.
- Pressure is the irreversible state of an increasingly alert and hostile world.

Route previews should show projected light and pressure separately. Encounters freeze their composition, modifiers, and intent data when contact begins; pressure gained during that combat changes future encounters, not the promises already shown on screen.

### Named bands and qualitative escalation

Pressure should have visible named bands, current progress, causes, the next threshold, and a short forecast. Exact names should fit the project’s fiction. Successive bands should first unlock qualitative changes:

- More coordinated encounter plans.
- One or more visible elite mutations.
- Reinforcement tickets or multi-stage enemies within the four-rank limit.
- Advanced enemy actions and reactions.
- More aggressive patrol doctrines and regional alert responses.
- Stronger or wider biome hazards.
- Better or stranger reward lanes.
- Loop-only enemy families and boss modules.

Raw health and damage can rise modestly, but should not be the primary expression. A four-rank enemy line cannot become a Risk of Rain horde, so excess director budget must become richer actors, mutations, reinforcements, formation complications, and phases.

Higher pressure must improve selected rewards. Otherwise optimal play collapses toward rushing. The intended dilemma is: rush the finale with an immature deck, or accept a stronger world to assemble a stronger engine.

### Optional post-victory loops

After a final boss dies, the base victory is recorded immediately. The player then chooses:

- `EXTRACT`: finish the run and preserve the normal victory record.
- `DESCEND AGAIN`: keep the surviving party and build, raise the pressure floor, and enter a deterministically remixed world loop.

Loop death must not erase the recorded clear. No core unlock should require deep looping. Each loop can remix biomes, introduce corrupted variants, add affix slots, raise hazard and patrol behavior, offer paired power-plus-burden rewards, and culminate in a mutated guardian or finale. The purpose is a stress test and score chase for excessive builds, not the balance target for the standard expedition.

This deliberately differs from an endless-first design. Mega Crit has recently argued that longer runs can make deckbuilding less exciting when the mechanics do not support natural infinite scaling. The base Dullest Dungeon run should therefore remain finite; looping is optional and allowed to become unfair eventually.

## Broken builds without broken software

### Explicit event queue

Automatic effects and retriggers should resolve through a deterministic event queue rather than nested callbacks. Every event should carry a root action, parent, source, targets, type, depth, and payload. A stable phase order should cover prevention/replacement, before, primary, after, death, and cleanup.

Listener order must be canonical and independent of Python set or dictionary iteration. A sensible sort key is explicit phase, priority, entity creation ID, then stable effect ID. Rendering must never consume simulation randomness or mutate the queue.

Each potentially recursive trigger needs an authored limiter such as once per root action, card play, turn, combat, finite charge, maximum retrigger count, or proc-family exclusion. Static validation should reject trigger cycles unless every path consumes a finite resource or includes a proven limiter.

A generous per-root event budget should be the last-resort safety valve. Exceeding it seals only the repeated automatic chain, emits a readable `CHAIN SEALED` diagnostic, and records a reproducible ancestry trace. It must not silently flatten all high damage or ban deliberate player-created sequences.

### Stacking vocabulary

Every stackable item, boon, curse, and status should declare a supported mode:

- Linear.
- Multiplicative.
- Independent chance.
- Hyperbolic or diminishing.
- Threshold.
- Duration refresh.
- Unique.
- Conversion or corruption.
- An authored integer table.

The UI should show current effect, next-stack delta, formula or table, cap or soft cap, and trigger permissions. Integer arithmetic or basis points are preferable to binary floats.

Damage, setup, copying, conversion, and conditional retriggers may scale explosively. Repeated stun, dodge, Death’s Door resistance, cooldown avoidance, and other interaction-removing defenses should usually diminish. The goal is not symmetric balance; it is to keep the enemy capable of asking a question.

### Soft answers, not hard counters

Enemies may pressure low-cost card spam, repeated cards, slow setup, block, healing, wounds, marks, movement, stress, narrow ranks, or a single protected carry. Ordinary enemies should not say that an entire valid build tag does nothing.

Useful counter forms include:

- A visible reaction after every Nth card.
- Partial resistance rather than immunity.
- A guard or cleanse that can be interrupted.
- A movement threat with more than one positional answer.
- Retaliation that creates a timing window.
- Linked enemies where killing one empowers another.
- Alternate targets, summons, or formation objectives.

The Risk of Rain 2 team’s decision to give one boss component partial proc-chain resistance rather than universal immunity is the right shape. Strong builds still feel strong; specific fights demand adjustment.

## Content expansion with anti-bloat rules

### Keep 25 crew

Do not add a twenty-sixth archetype in this pass. Twenty-five crew already create 12,650 unordered parties and 303,600 ordered starting formations. A twenty-sixth crew adds 2,300 unordered parties while worsening the onboarding problem that Implementation Pass 1 only recently addressed.

“More party types” should mean:

- Approximately 10–12 broad, tag-derived squad doctrines.
- Approximately 12–15 curated squads, up from five.
- Alternate starter packages or advanced loadouts for existing crew.
- A bounded set of A/B mastery upgrades.
- Authored challenges that constrain party, formation, biome, or reward pools.

Doctrines should have one disclosed strength, one liability, and a formation or sequencing rule. They should emerge from broad mechanics—dance, mark/cash, wound attrition, guard/riposte, stress conversion, discard velocity, control, Death’s Door gamble—not named-card or named-pair bonuses.

### Techniques

A defensible large expansion target is four authored techniques per existing archetype, roughly 100 new cards relative to the current repository:

1. One deeper card for build direction A.
2. One deeper card for build direction B.
3. One cross-owner bridge using broad shared grammar.
4. One conditional rare rule breaker.

This is a candidate and quality target, not permission to ship numerical clones. Every card must do at least one of the following:

- Enable a previously unavailable engine.
- Deepen an existing branch through a new sequencing or positional choice.
- Bridge two underconnected broad mechanics.
- Salvage a weak or disrupted formation without becoming an auto-pick.
- Make an existing boon, item, curse, or underused card newly desirable.

Descriptions must fit the terminal, preferably one simple effect plus one condition. Avoid dependencies on a named card, a specific crew pair, or a private mechanic no one else can interact with. New opcodes should support several content pieces, not one novelty.

Do not require split upgrades for the entire enlarged catalog. Add mutually exclusive A/B mastery only to a bounded signature set, for example two cards per archetype. One branch should intensify the owner’s engine; the other should improve positional reliability or a cross-owner line. Preserve the ordinary one-upgrade ceiling elsewhere.

### Card infusions and acquisition lanes

A small socket-like infusion layer can multiply cards without flooding technique rewards. A card copy may hold at most one infusion. Infusions should use existing mechanics—retain, exhaust, rank access, conditional cost, copied status, once-per-combat echo—and be rare enough that a run develops a few memorable cards rather than an inventory-management chore.

Keep reward lanes distinct:

- Normal fights: glue, coverage, modest branch deepeners.
- Elites: engines, infusions, scarce conversion tools.
- Objectives and biome facilities: biome affinities, route-altering rewards, targeted transformations.
- Bargains and curses: exceptional power with disclosed burden.
- Guardians and final bosses: signature rule changes and loop invitations.

Rewards remain skippable. The larger global catalog must not become the active pool in every run.

### Items, boons, and curses

The next pass can credibly add at least 18 stackable items, 12 boons, and 12 curses, while revising old scalar entries that no longer earn their slot.

New items should include simple consistency pieces, threshold engines, converters/corruptors, and rare multiplicative or retrigger effects. Every stack must have authored first-stack and additional-stack behavior.

Boons should be unique or nearly unique rule modifiers, not a second item list. Curses should include genuine burdens, build-around liabilities, and temporary contracts that become rewards only after a dangerous condition. The strongest rewards can require immediate pressure, an elite patrol, a persistent wound, reduced visibility, or an objective obligation.

Balatro’s lesson is especially important here: a few unusual rule breakers are exciting because the surrounding set is concise and reliable. If every reward introduces a new keyword, none of them can recombine.

### Enemies and encounters

Enemy expansion should use semantic density floors rather than a vanity total:

- At least six mechanically distinct normal enemies per biome.
- At least two native elites per biome.
- At least one biome guardian or boss per biome.
- Several encounter families per biome with alternate formations.
- At least three meaningful actions or reactions for ordinary enemies unless a deliberately simple enemy has a documented role.

The current thin biome pools likely require approximately 35–55 new enemy definitions including guardians, but the live repository must be audited first. Existing two-action enemies should be enriched where repetition comes from predictable behavior rather than insufficient roster size.

Threat budgeting should use a vector rather than maximum HP alone: durability, sustained damage, burst, control, sustain, reach, and tempo. Add explicit composition costs for mark/payoff, movement/rank punishment, guard/protection, debuff/exploit, formation screening, and overlapping lock potential. Normal and elite encounters need both a total budget and dimensional ceilings so an affordable group cannot concentrate all value into turn-one burst or repeated lockout.

The deterministic composer should choose an encounter family, enumerate and stably sort compatible candidates, enforce vector/role/rank constraints, score behavioral novelty, make one seeded selection, then freeze the result. Avoid random retry loops whose RNG consumption changes when the catalog changes.

Add a reusable set of clearly visible elite mutations. Mutations should change tactics—new reactions, protection relationships, reinforcement behavior, formation rules—not merely add stat soup. Base runs see few; high pressure and loops combine more.

### Bosses

Add 11 biome guardians and multiple final bosses while protecting run length. A base run should encounter one biome guardian and one finale, not all new bosses. Guardians should replace an objective culmination, elite, or final-gate test rather than creating mandatory detours.

Keep the Overseer Core as one possible final boss and add several genuinely different finales. Objective outcomes, completed biomes, layout, and pressure can weight selection and modules. Reveal the likely defining pressures after the first objective and the exact finale after the second so the deck can adapt.

Every boss needs:

- One clear tactical thesis and at most one major rule break at a time.
- Multiple phases or modules whose transitions are telegraphed.
- Several possible answers rather than a mandatory keyword.
- Partial pressure on dominant engines instead of blanket immunity.
- Stable, inspectable intent and reaction ordering.
- Objective- or difficulty-dependent variants that alter behavior before stats.

## Long-term replay structure

### Run history and local evidence

Before content volume grows, add a terminal-readable run history and morgue record. It should include seed, versions, layout, biomes, objectives, crew, deaths, cards and reward choices, item stacks, boons, curses, encounter families, pressure, rounds, damage/stress sources, outcome, duration, build tags, and loop depth.

Local telemetry should be opt-in and network-free. Newline-delimited JSON is sufficient. Record offers as well as picks, because pick rate and win rate alone misread narrow synergy cards. Include rank-invalid dead draws, trigger depth, overkill, wasted block, control-lock streaks, and objective abandonment.

### Twenty-rank ladder

Implement the previously selected long ascending ladder globally, not separately for every crew or party. Twenty cumulative ranks are appropriate. Each rank should state one clear change; major anchor ranks should transform systems.

- Ranks 1–4: expedition pressure and resource economy.
- Ranks 5–8: encounter budgets, formations, and elite frequency.
- Ranks 9–12: enemy actions, coordination, and mutations.
- Ranks 13–16: objectives, hazards, patrols, and route pressure.
- Ranks 17–20: guardians, finales, and overlapping endgame rules.

Unlock the next rank on a win. Track best global rank and per-crew/preset records for recognition, but do not demand hundreds of composition-specific unlock wins.

### Horizontal unlocks and modes

The base profile must be fun and winnable. Do not add permanent health, damage, energy, or reward-quality upgrades. Unlock complexity and alternatives:

- Advanced cards and infusions.
- Alternate starter packages.
- Squad doctrines and curated presets.
- Bosses, mutations, and challenge rules.
- Lore, alternate ASCII portraits, titles, and records.

Separate ranked difficulty from expressive modes:

- Approximately 15–25 authored contracts.
- A custom expedition with compatibility-validated modifiers.
- An offline daily expedition derived from date, ruleset version, and fixed salt.
- Short versioned challenge codes containing seed and modifiers.
- Optional post-victory looping.

No server, account, online leaderboard, or network dependency is needed.

## Architecture and verification requirements

The enlarged game needs explicit contracts rather than more ad hoc JSON dictionaries.

### Data contracts

Load validated JSON into immutable typed objects and sorted indexes. Add explicit contracts for effects, synergy edges, triggers, stack rules, threat profiles, content manifests, pack IDs, and fingerprints. JSON may compose registered opcodes; it must not execute expressions or become an embedded scripting language.

IDs should be immutable and namespaced. Renames need aliases or migrations. Keep content schema, run-save schema, profile schema, telemetry schema, and RNG architecture version separate.

The synergy graph should model `produces`, `exploits`, `spends`, `converts`, `requires`, and `covers`. Validation should find orphan signals, payoffs without producer density, structurally identical build branches, death/rank bottlenecks, inert cross-owner engines, and diluted reward pools.

### Determinism and saves

All randomness belongs to the simulation. Rendering, animation speed, help, inspection, resizing, and skips must not consume RNG. Use stable enumeration and named RNG domains. Do not seed from Python’s randomized `hash()`; use a guaranteed `hashlib` digest if deriving streams.

Maintain explicit pure save migrations and golden fixtures. Never silently regenerate missing durable state from current content. Store content fingerprint and enabled packs. Reject duplicate JSON keys and non-finite numbers. Preserve uninterrupted-versus-save/load equivalence at event boundaries.

### Testing

Required layers include:

- Strict loader, references, effect legality, ranks, targets, and honest tags.
- Synergy graph density and normalized duplicate detection.
- Trigger dependency cycles, limiters, canonical ordering, and event-budget drainage.
- Stack formulas, caps, monotonicity, exact previews, and integer safety.
- Threat-vector budgets, spike ceilings, formations, and coordination.
- Deterministic state hashes and event logs.
- Save/load at travel, combat, trigger, death, reward, objective, boss, and loop boundaries.
- Multiple `PYTHONHASHSEED` processes.
- Random legal-command fuzzing with failing seed and transcript retention.
- Targeted three-way coverage for trigger × stack × death/save interactions.
- 80×24 and large-terminal PTY checks.
- Natural play, including a rushed attempt, greedy loss, ordinary clear, deliberately excessive build, loop continuation, and loop death or extraction.

Headless policies can catch distribution regressions—objective rusher, reward detourer, greedy damage, defensive survival, tag maximizer, and position-aware—but cannot prove fun. Compare identical seed cohorts before and after changes and report distributions, not only averages.

## Market and scope reality

The category is viable but crowded. A January 2026 analysis counted 212 roguelike deckbuilders released on Steam in 2025; 11, or about 5.1%, crossed its 1,000-review proxy, down from 6.71% in 2024. The author explicitly notes tag noise and that the threshold excludes smaller successes. [How To Market A Game’s 2025 analysis](https://howtomarketagame.com/2026/01/27/what-the-hell-happened-in-2025/)

Content count is therefore weak positioning. The closest caution is the developer’s retrospective for [Meteorfall: Bramble Royale](https://slothwerks.substack.com/p/bramble-royale-steam-launch-retro): years of refinement and more production value did not make a team deckbuilder’s improvements into a sufficiently marketable hook.

Terminal presentation can support a dedicated niche, but it raises the polish threshold. Cogmind’s developer attributes early support partly to strong moment-to-moment presentation and a gameplay-ready release while acknowledging the niche audience. [Cogmind’s commercial ASCII postmortem](https://www.gamedeveloper.com/business/releasing-a-commercial-ascii-roguelike-a-post-mortem) Caves of Qud’s developers describe abstract presentation as liberating only because the world retains specificity and authored structure. [Caves of Qud procedural-generation interview](https://www.gamedeveloper.com/design/tapping-into-the-potential-of-procedural-generation-in-caves-of-qud)

The expansion should therefore sharpen the one-sentence hook and produce visible stories: a casualty deleting a combo piece, a detour pushing the world into a new pressure band, a curse corrupting an item stack, a coordinated enemy family forcing a formation pivot, and a broken build choosing to descend again.

## Recommended implementation order

1. Verify or finish base-run calibration and record a natural standard victory.
2. Add run history, opt-in local telemetry, canonical state hashes, and comparison cohorts.
3. Formalize typed content, synergy, event, trigger, stack, threat, RNG, and migration contracts.
4. Implement visible action-based expedition pressure and qualitative director responses.
5. Refactor existing rewards into distinct lanes and explicit stack behavior.
6. Add squad doctrines, curated parties, bounded masteries, and card infusions.
7. Author and curate the large card, item, boon, and curse expansion.
8. Enrich enemy behavior, fill biome density gaps, add mutations, and rebuild threat budgeting.
9. Add biome guardians and multiple objective-influenced finales.
10. Add the global twenty-rank ladder, authored/custom/offline-daily modes, horizontal unlocks, and optional loops.
11. Run deterministic sweeps, fuzzing, PTY checks, multiple ordinary playthroughs, and targeted excessive-build scenarios.

This order is intentionally systems-first within one larger content pass. It ensures that the new catalog can be evaluated, found, combined, saved, replayed, and challenged without turning the game into an untestable pile of JSON.

## Core sources

- Mega Crit, [Road to the IGF: Slay the Spire](https://www.gamedeveloper.com/game-platforms/road-to-the-igf-mega-crit-games-i-slay-the-spire-i-).
- Mega Crit, [Slay the Spire: Metrics Driven Design and Balance](https://www.gdcvault.com/play/1025731/-Slay-the-Spire-Metrics%EF%BB%BF).
- LocalThunk, [Guidelines for Joker Design](https://www.reddit.com/r/balatro/comments/1czo9g0/guidelines_for_joker_design/).
- Game Informer, [Balatro creator interview](https://gameinformer.com/interview/2024/03/21/balatro-was-almost-called-joker-poker-and-other-details-from-its-creator).
- Playstack, [Balatro official feature overview](https://www.playstack.com/games/balatro/).
- Hopoo Games, [Risk of Rain Returns design interview](https://www.gamedeveloper.com/design/risk-of-rain-interview).
- Risk of Rain, [official game overview](https://riskofrain.2k.com/).
- 2K, [Risk of Rain 2 December 2025 balance notes](https://support.2k.com/hc/en-us/articles/47210814399123-Risk-of-Rain-2-Patch-Notes-December-9-2025).
- Risk of Rain team, [Hallowed Concepts design goals and Hex/Hallowed item notes](https://store.steampowered.com/news/app/632360/view/710026912607507071).
- Rocket Rat Games, [Cobalt Core design interview](https://www.gamedeveloper.com/design/how-cobalt-core-makes-movement-as-exciting-as-fighting-in-its-roguelike-deckbuilder-combat).
- Cobalt Core, [official feature overview](https://store.steampowered.com/app/2179850/Cobalt_Core/).
- Abrakam, [Roguebook design interview](https://www.gamedeveloper.com/design/tackling-deckbuilding-design-in-abrakam-s-roguebook).
- Shiny Shoe, [Monster Train 2 official overview](https://shinyshoe.com/games/monster-train-2/).
- Shiny Shoe/Xbox, [Pact Shards and The Last Divinity](https://news.xbox.com/en-us/2021/04/01/monster-train-the-last-divinity-dlc-available-now/).
- Supergiant Games, [Hades Welcome to Hell / Pact notes](https://www.supergiantgames.com/blog/hades-welcome-to-hell-update-patch-notes/).
- Chucklefish, [Wildfrost Friends and Foes update](https://chucklefish.org/blog/wildfrost-friends-and-foes/).
- Whatboy Games, [Trials of Fire Early Access and telemetry interview](https://www.unrealengine.com/developer-interviews/how-early-access-helped-shape-tactical-deck-building-adventure-trials-of-fire).
- Red Hook, [Darkest Dungeon design postmortem](https://www.gdcvault.com/play/1023435/Darkest-Dungeon-A-Design).
- Freehold Games, [Caves of Qud procedural-generation interview](https://www.gamedeveloper.com/design/tapping-into-the-potential-of-procedural-generation-in-caves-of-qud).
- Josh Ge, [commercial ASCII roguelike postmortem](https://www.gamedeveloper.com/business/releasing-a-commercial-ascii-roguelike-a-post-mortem).
- Chris Zukowski, [2025 Steam genre analysis](https://howtomarketagame.com/2026/01/27/what-the-hell-happened-in-2025/).
- Python, [`random`](https://docs.python.org/3/library/random.html), [`hashlib`](https://docs.python.org/3/library/hashlib.html), [`json`](https://docs.python.org/3/library/json.html), and [`os.replace`](https://docs.python.org/3/library/os.html#os.replace) documentation.
- NIST, [combinatorial software testing guidance](https://csrc.nist.gov/projects/automated-combinatorial-testing-for-software/software-testing-methodology/dos-and-don-ts-of-testing).

