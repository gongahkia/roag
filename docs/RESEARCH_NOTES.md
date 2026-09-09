# Research and design notes

Research was used to choose small, legible systems that reinforce the existing
game rather than importing another game's content.

## Material sources

- Red Hook's [Darkest Dungeon design postmortem](https://www.gdcvault.com/play/1023435/Darkest-Dungeon-A-Design)
  describes the value of layered stress, party, expedition, and combat systems.
  This supports letting a death damage several existing systems while preserving
  the run, instead of adding a separate lives system.
- Mega Crit's [Slay the Spire metrics talk](https://www.gdcvault.com/play/1025731/-Slay-the-Spire-Metrics%EF%BB%BF)
  and [developer interview](https://www.gamedeveloper.com/design/how-i-slay-the-spire-i-s-devs-use-data-to-balance-their-roguelike-deck-builder)
  emphasize pick-rate/outcome evidence, qualitative play feedback, aggressive
  iteration, and adding cards in coherent archetype batches. Dullest Dungeon
  therefore needs build-aware offers and structural upgrades before more small
  numerical variants.
- Mega Crit's [Road to the IGF interview](https://www.gamedeveloper.com/game-platforms/road-to-the-igf-mega-crit-games-i-slay-the-spire-i-)
  explains how route risk/reward and enemies that challenge different strategies
  add decisions without excessive rules. That informs optional biome objectives
  and encounter plans rather than mandatory map clearing.
- Subset Games' [Into the Breach postmortem](https://www.gdcvault.com/play/1025772/-Into-the-Breach-Design)
  and [developer interview](https://www.gamedeveloper.com/game-platforms/road-to-the-igf-subset-games-i-into-the-breach-i-)
  center clear rules, telegraphed attacks, varied goals, and constrained enemy
  counts. This directly motivates intent targets that are selected, serialized,
  and displayed before execution.
- The GDC talks on [Catacomb Kids](https://www.gdcvault.com/play/1021877/Constructing-the-Catacombs-Procedural-Architecture),
  [The Flame in the Flood](https://www.gdcvault.com/play/1023266/Forging-The-River-in-The),
  and [Eldritch](https://media.gdcvault.com/gdc2015/presentations/Pittman_David_Procedural%20Level%20Design.pdf)
  treat procedural generation as authored design constraints across several time
  scales. Biome profiles therefore participate in path cost, patrols, resources,
  combat, and objectives; they are not cosmetic post-processing.
- Supergiant's [Hades FAQ](https://www.supergiantgames.com/blog/hades-faq/)
  and [Beefy Update notes](https://www.supergiantgames.com/blog/get-pumped-for-the-beefy-update/)
  pair regions with their own foes and encounters while allowing run-long builds
  to stack across regions. Biome-affinity mechanics here remain bonuses and
  tactical hooks, not hard gates that invalidate a four-biome party.
- The analysis [Enemy design and enemy AI for melee combat systems](https://www.gamedeveloper.com/design/enemy-design-and-enemy-ai-for-melee-combat-systems)
  argues for enemies with distinct player-facing functions. Encounter generation
  should combine complementary functions around a visible plan, not maximize
  independent unit value.
- The [Risk of Rain 2 Director reference](https://riskofrain2.wiki.gg/wiki/Directors)
  documents credit-priced spawn choices. It reinforces the existing threat
  budget, but Dullest Dungeon also constrains role and combo coherence so equal
  budgets do not imply interchangeable encounters.
- Analyses of [survival-horror balance](https://www.gamedeveloper.com/design/the-fine-balance-of-survival-horror-design-and-dead-space-2)
  and [mechanical tension](https://www.gamedeveloper.com/design/the-mechanics-of-tension)
  distinguish consequential scarcity from arbitrary punishment. Biome routes
  spend the existing light, supply, HP, and stress economies and expose avoidable
  costs; no food, ammunition, inventory cap, or global timer is added.

### Pass 2: procedural expedition structure

- Grid Sage Games' [Procedural Map Generation](https://www.gridsagegames.com/blog/2014/06/procedural-map-generation/)
  argues that topology must reflect how a map plays, that content placement
  should use terrain deliberately, and that small handmade prefabs make
  significant locations memorable inside generated maps. Dullest Dungeon
  therefore embeds bounded objective landmarks and tests layouts for routing
  properties instead of judging them by appearance.
- [Map Composition](https://www.gridsagegames.com/blog/2015/05/map-composition/)
  describes recording structural facts during layout generation and using them
  to place weighted authored encounters. This supports a small landmark-template
  vocabulary plus topology-aware objective spread; it does not support a
  universal quest scripting engine.
- [Map Intel: Information Warfare, Revisited](https://www.gridsagegames.com/blog/2015/02/map-intel-information-warfare-revisited/)
  distinguishes durable knowledge of static machines from position reports for
  mobile squads that become stale. Pass 2 likewise remembers discovered
  facilities and landmarks while deriving current patrol visibility from
  simulation range, independently of viewport size.
- [The Map Ruler](https://www.gridsagegames.com/blog/2021/02/the-map-ruler-and-other-overlay-qol/)
  treats exact distance display as interface support when miscounting can change
  a tactical decision, while warning that complete hostile-field overlays are
  not neutral quality of life. Route inspection therefore reports exact known
  terrain cost and expected light, but only qualitative currently perceived
  patrol exposure and no hidden-hazard coordinates.

These sources do not imply that Cogmind's stealth, alert, hacking, fog-of-war,
or simulation scope fits this project. Those systems remain out of scope.

## Resulting constraints

- Randomness chooses among valid, authored tactical possibilities; it does not
  excuse incoherent formations or inaccurate previews.
- A biome mechanic must change a route, resource, information, encounter, or
  build decision. A renamed damage roll does not qualify.
- Rewards should offer a bridge into an existing synergy, a corrective/general
  option, and a speculative option, preserving uncertainty without three dead
  choices.
- Severe outcomes remain recoverable only through existing scarce systems. A
  surviving three-person party is viable to continue, but death permanently
  removes a body and its cards.

## Pass 3 source review — 2026-09-10

These are design inputs, not evidence that this implementation is balanced or
enjoyable. Implementation status is explicit below; planned applications are
inferences to test, not shipped features. Earlier notes are retained as history.

| Primary/developer source | Observation and specific application |
| --- | --- |
| [Mega Crit, Road to the IGF](https://www.gamedeveloper.com/game-platforms/road-to-the-igf-mega-crit-games-i-slay-the-spire-i-) | The developers describe discarding weak candidates, rewarding combinations and asking different questions through enemies and paths. **Applied:** the signed structural census identifies review candidates without declaring every similarity bad. **Planned:** require a sequencing, coverage, engine or route justification for each new entry. |
| [Mega Crit, metrics talk](https://www.gdcvault.com/play/1025731/-Slay-the-Spire-Metrics), [official slides](https://media.gdcvault.com/gdc2019/presentations/Giovannetti_Anthony_SlayTheSpire.pdf) | Read the 22-page slide deck; the supplied URL with its encoded suffix initially timed out. The slides combine iteration, metrics, player feedback and difficulty stratification, and warn against treating data as a conclusion. **Applied:** identical-seed route cohorts, preserved commands and explicit withdrawal of a bug-dependent loss. Policy success is not a measured human win rate. **Planned:** separate offers, picks, plays and conditioned outcomes in local records. |
| [LocalThunk, Joker guidelines](https://www.reddit.com/r/balatro/comments/1czo9g0/guidelines_for_joker_design/) | The author favors brief rules, conditional power, useful reliable glue and broad interactions over named pair requirements. **Planned:** four distinct roles in each crew's new card batch; reject scalar clones and single-purpose opcodes. A concise rule still needs usable producer density. |
| [LocalThunk interview](https://gameinformer.com/interview/2024/03/21/balatro-was-almost-called-joker-poker-and-other-details-from-its-creator) | Short descriptions constrain design; weak effects are culled; overpowered play becomes a problem when it crowds out other interesting play. Seeds and beta feedback revealed uses the developer had not anticipated. **Applied:** repeatable seeded policies and fully reachable long-menu consequences. **Planned:** inspect spectacular engines and competing alternatives before reducing power. |
| [Hopoo interview](https://www.gamedeveloper.com/design/risk-of-rain-interview) | Searching for items spends time while enemies strengthen; rushing can leave the player underprepared. The interview also discusses keeping unlocks useful without diluting the item pool. **Applied:** rusher, explorer and greedy routes measured on identical seeds. **Planned inference:** replace real time with disclosed simulated expedition actions, with scoped reward pools and fresh-profile viability. |
| [Official Risk of Rain description](https://riskofrain.2k.com/) | The page describes interacting item collections, simultaneous player/enemy growth and a choice between escaping and continuing. **Planned inference:** preserve base victory before optional loops; permit large player engines while changing enemy behavior. It does not justify a real-time clock in this terminal game. |
| [2K, December 9 2025 patch](https://support.2k.com/hc/en-us/articles/47210814399123-Risk-of-Rain-2-Patch-Notes-December-9-2025) | Solus Wing weak points receive a local 25% proc-chain damage reduction. **Planned:** if a boss needs proc resistance, scope and disclose it locally; do not invalidate every ordinary use of a broad build tag. This source does not establish an appropriate coefficient for Dullest Dungeon. |
| [Risk of Rain team, Hexes and Hallowed Items](https://store.steampowered.com/news/app/632360/view/710026912607507071) | The article's HTML was empty; its English body was retrieved from Steam's own `events/ajaxgetpartnerevent` endpoint using `event_gid=710026912607507071`. It describes burdens exchangeable for exceptional items and a consumed, once-per-stage substitution effect. **Planned:** visible curse contracts and finite consumed retriggers, with the loss/conversion preview before acceptance. No content or fiction is copied. |
| [Rocket Rat, Cobalt Core interview](https://www.gamedeveloper.com/design/how-cobalt-core-makes-movement-as-exciting-as-fighting-in-its-roguelike-deckbuilder-combat) | Small positional questions support several answers. Characters should reinterpret shared systems; isolated character games were cut. Screen constraints influenced the rules. **Applied:** preserve the four-owner formation, repair casualty rank collapse, keep decisions reachable at 80×24. **Planned:** movement bridges and enemy formations with multiple legible responses. |
| [Abrakam, Roguebook interview](https://www.gamedeveloper.com/design/tackling-deckbuilding-design-in-abrakam-s-roguebook) | Shared characters, position and broad keywords make combinations multifaceted. Improvisation and player-controlled risk help explain losses. **Applied:** loss narratives distinguish route attrition, casualty loss and exhausted answers. **Planned:** owner-bound pivots and skippable rewards. Its preference for large decks and its permanent perks are not adopted. |
| [Monster Train, Pact Shards](https://news.xbox.com/en-us/2021/04/01/monster-train-the-last-divinity-dlc-available-now/) | Gaining a benefit also strengthens future opposition, making the cost part of the acquisition decision. **Planned inference:** explicit pressure prices on selected exceptional rewards, frozen before the choice; no hidden scaling to player strength. |
| [Shiny Shoe, Monster Train 2](https://shinyshoe.com/games/monster-train-2/) | The page distinguishes room/equipment layers, authored challenges, daily configurations and records. **Planned:** keep technique, infusion, item and doctrine jobs distinct and separate ranked progression from expressive contracts. The current page does not itself explain Endless rules; those details are not claimed as verified from it. |
| [Supergiant, Welcome to Hell patch notes](https://www.supergiantgames.com/blog/hades-welcome-to-hell-update-patch-notes/) | Pact changes include boss behavior and encounter composition, alongside adjustments to where modifiers apply. **Planned:** a cumulative ladder with behavioral anchor ranks and compatibility checks, rather than twenty health/damage increments. Real-time deadline rules are excluded. |
| [Whatboy Games, Trials of Fire](https://www.unrealengine.com/developer-interviews/how-early-access-helped-shape-tactical-deck-building-adventure-trials-of-fire) | The developers describe players exceeding their own system expectations and using card/item/hero outcome data with feedback. **Applied:** legal-command cohorts and replayable failure investigation. **Planned:** deterministic fuzzing and constructed stress builds, clearly separated from ordinary play. |
| [Freehold Games, Caves of Qud](https://www.gamedeveloper.com/design/tapping-into-the-potential-of-procedural-generation-in-caves-of-qud) | Static areas supply an authored backbone inside procedural worlds; abstract presentation supports imagination. **Applied:** retain the existing seeded landmarks and objectives. **Planned:** guardians replace a culmination on that backbone; procedural variety must not add every boss to every base run. |
| [Josh Ge, Cogmind ASCII postmortem](https://www.gamedeveloper.com/business/releasing-a-commercial-ascii-roguelike-a-post-mortem) | The developer describes presentation polish and a coherent release experience within traditional roguelike constraints. **Applied inference:** actual-terminal testing and readable, scrollable decisions are acceptance criteria. This is not evidence that Cogmind runs in curses or that a graphical shell belongs here. |

The immediate calibration result is an empirical design question: all thirty
greedy runs in the first corrected cohort won, while three other routes lost.
This does not prove greedy play dominates universally. It does justify testing
whether the proposed pressure system creates an actual cost for those detours.
