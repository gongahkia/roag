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
