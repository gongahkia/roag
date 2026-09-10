# Systemic-world research

Research conducted 9 September 2026 and independently rechecked 10 September
2026. These are design references, not sources of game content or code. Direct
playback of the requested YouTube talks was unavailable; their official GDC
descriptions, published slides, developer papers, developer-maintained
documentation, press-kit summaries and the interviews below were inspected
instead. No claim is made to have watched those videos in full.

## Sources and translation

- [Noita official site](https://noitagame.com/), the [GDC talk
  overview](https://www.gdcvault.com/play/1025695/Exploring-the-Tech-and-DesignAt),
  [Nolla's IGF interview](https://www.gamedeveloper.com/game-platforms/road-to-the-igf-nolla-games-i-noita-i-),
  and [falling-sand development interview](https://80.lv/articles/noita-a-game-based-on-falling-sand-simulation):
  simple consistent reactions teach transferable knowledge; hazards can become
  tools; authored large-scale structure makes procedural detail meaningful.
  Jomon will use sparse tile reactions, shared exposure resolution, authored
  regional anchors, and bounded local assembly—not pixel simulation.
- The Noita development interview describes selective simulation and continuous
  profiling. Jomon's consumers are active-region dirty chunks, capped reaction
  work, disposable spatial indexes, and deterministic replay benchmarks. No
  background simulation or nondeterministic worker threads are adopted.
- Noita's modular expression is adopted only as a design lesson. Jomon uses
  bounded physical weapon fittings, treatments, ammunition preparations,
  clothing, carried discoveries and finite relics. Parts occupy real storage,
  wear, can be removed, and expose their combined action trade-off before work
  is confirmed. A wand, spell language or unlimited casting analogue is
  explicitly rejected.
- [Qud's official site](https://cavesofqud.com/), [official talk
  index](https://cavesofqud.com/press-kit/), and [systems-driven developer
  interview](https://unity.com/resources/systems-driven-design-in-caves-of-qud):
  reusable meaningful properties and an authored backbone create interesting
  situations. Jomon retains explicit reducers and adds shared capability
  helpers only for multiple real consumers; no ECS rewrite is proposed.
- The requested [Data-Driven Engines of Qud and
  Sproggiwood](https://www.youtube.com/watch?v=U03XXzcThGU) talk is identified
  by the developers' press kit as their component-architecture talk. The
  developer-authored [object/component
  documentation](https://freehold.atlassian.net/wiki/spaces/CQP/pages/23691295/Object%2BModding)
  demonstrates content assembled from reusable parts. Jomon translates this
  narrowly: doors, actors, cargo, armour and structures expose small useful
  capabilities to the same explicit reducers. A helper is retained only after
  three production consumers need it.
- [End-to-End Generation slides](https://media.gdcvault.com/gdc2019/presentations/Grinblat_Jason_End-to-End_Procedural_Generation.pdf)
  and [History Generation slides](https://media.gdcvault.com/gdc2018/presentations/Grinblat_Jason_Procedurally_Generating_History.pdf),
  together with the developers' [mythic-biography paper](https://www.freeholdgames.com/papers/Generation_of_Mythic_Biographies_in_CavesofQud.pdf):
  bounded causal event chains can leave evidence without simulating empty
  centuries. Jomon's generation manifest links geology, water, work, disputes,
  damaged sites, named possessions, testimony, market conditions, and quests.
  Each retained fact needs at least two concrete consumers.
- [Tile-based generation talk](https://www.gdcvault.com/play/1025913/Math-for-Game-Developers-Tile)
  and the [official population documentation](https://freehold.atlassian.net/wiki/spaces/CQP/pages/25690114/Encounter%2Band%2BPopulation%2BModding):
  constrained local assembly and bounded population tables help preserve
  useful structure. Jomon applies distinct passes for coarse geography and
  authored anchors, constrained local stamps, connectivity repair, historical
  scars and evidence, then population. It validates entrances, alternate
  routes, height links, cover and objective access after those passes. A
  universal WFC implementation is unnecessary for these eight authored
  regional families.

## Deterministic terminal translation and cost

The adopted techniques have explicit limits rather than relying on their source
games' scale or runtime:

| Technique | Jomon translation | Cost boundary and consumers |
|---|---|---|
| shared material rules | sparse overlays and one reducer for actors, named adults, items, cargo and structures | stable neighbour order, 64-cell turn budget and 512-cell regional cap; combat, cargo, equipment, quests and voyage crises consume it |
| authored macro / generated micro | fixed landmarks and route purpose followed by seed-stage terrain variation, scars and bounded placements | a finite pass sequence and repaired mandatory paths; all eight regional builders and topology audits consume it |
| selective simulation | active spatial buckets, dirty chunks and reconstructible one-entry FOV/path caches | no background threads or idle-time work; ecology, materials, FOV, pathfinding and rendering consume it |
| bounded history | choose three to seven material events, mutate entity state, then derive competing testimony from the accumulated ledger | at most seven records and stable iteration/tie-breaking; geography, institutions, markets, quests, named treasure and inspection consume it |
| composable capabilities | small data properties interpreted by explicit tested reducers | no dynamic expressions and no abstract dispatch graph; actors, equipment, doors, containers and vessel controls consume it |
| population roles | validated regional pools composed after geography and history | local actor and encounter budgets; ecology, patrols, quests, rival interests and market pressure consume it |
| physical build combinations | one structural fitting and treatment per weapon, one lining per armour piece, plus finite ammunition and carried discoveries | preview before confirmation, bounded sockets and real pack/locker cost; workshop, combat, materials, travel and treatment consume it |

Every stochastic choice uses a named SHA-256 seed stream. Iteration and ties are
stable, only accepted actions advance the clock, and disposable caches are
reconstructed rather than serialized. History is generated as present evidence,
not replayed year by year. This is the practical form of post-hoc causal
rationalization: a curated event mutates known people, places, materials and
institutions, then testimony is derived from that state and may disagree about
the event without contradicting its physical evidence.

The design target is "interesting bones": authored places, obligations and
rules whose ordinary collisions support stories. Random incidents without a
material cause, readable decision or persistent consequence do not count.

## Deliberate exclusions

Universal WFC, a general Wang-tile toolkit, Noita's concurrent checkerboard
pixel update machinery, inherited entity trees, a new ECS, a symbolic planner,
unrestricted prose generation, per-pixel thermal/fluid simulation, worldwide
turn-by-turn ecology, thousands of simulated history years, and fully
destructible buildings that commonly become unusable rubble are not justified
by this game. Constrained stamps and short material rules must earn their cost
through visible play. A realistic or slow process with no readable decision is
not counted as content.

No names, lore, enemies, spells, maps, text, art, distinctive equipment systems,
or source code from these games will be copied. Jomon's physical household,
finite low mysticism, medieval work, cargo, and material obligations remain
the content authority. Benchmarks and production-path audits must distinguish
the cost and reachability of these translations from their descriptions.

## Requested source index

- [Exploring the Tech and Design of Noita](https://www.youtube.com/watch?v=prXuyMCgbTc)
  and its [official GDC overview](https://www.gdcvault.com/play/1025695/Exploring-the-Tech-and-Design)
- [Road to the IGF: Nolla Games' Noita](https://www.gamedeveloper.com/game-platforms/road-to-the-igf-nolla-games-i-noita-i-)
- [Noita falling-sand development interview](https://80.lv/articles/noita-a-game-based-on-falling-sand-simulation)
- [Official Noita site](https://noitagame.com/)
- [Official Caves of Qud site](https://cavesofqud.com/)
- [Systems-driven design in Caves of Qud](https://unity.com/resources/systems-driven-design-in-caves-of-qud)
- [End-to-End Procedural Generation](https://www.gdcvault.com/play/1026313/Math-for-Game-Developers-End)
- [Procedurally Generating History](https://www.gdcvault.com/play/1024990/Procedurally-Generating-History-in-Caves)
- [Tile-Based Map Generation using Wave Function Collapse](https://www.youtube.com/watch?v=AdCgi9E90jw)
  and its [official GDC description](https://www.gdcvault.com/play/1025913/Math-for-Game-Developers-Tile)
- [Data-Driven Engines of Qud and Sproggiwood](https://www.youtube.com/watch?v=U03XXzcThGU)
- [Subverting Historical Cause & Effect: Generation of Mythic Biographies in
  Caves of Qud](https://www.freeholdgames.com/papers/Generation_of_Mythic_Biographies_in_CavesofQud.pdf)
