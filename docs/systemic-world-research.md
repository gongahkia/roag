# Systemic-world research

Research conducted 9 September 2026. These are design references, not sources
of game content or code. Direct playback of the requested YouTube talks was
unavailable; official talk descriptions, published slides, developer papers,
and the interviews below were inspected instead. No claim is made to have
watched those videos in full.

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
- [Qud's official site](https://cavesofqud.com/), [official talk
  index](https://cavesofqud.com/press-kit/), and [systems-driven developer
  interview](https://unity.com/resources/systems-driven-design-in-caves-of-qud):
  reusable meaningful properties and an authored backbone create interesting
  situations. Jomon retains explicit reducers and adds shared capability
  helpers only for multiple real consumers; no ECS rewrite is proposed.
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
  useful structure. Jomon will validate entrances, alternate routes, height
  links, cover, and objective access after authored terrain passes.

## Deliberate exclusions

Universal WFC, a general Wang-tile toolkit, inherited entity trees, a symbolic
planner, unrestricted prose generation, per-pixel thermal/fluid simulation,
worldwide turn-by-turn ecology, and thousands of simulated history years are
not justified by this game. Constrained stamps and short material rules must
earn their cost through visible play. A simulation feature with no readable
decision is not counted as content.

No names, lore, enemies, spells, maps, text, art, distinctive equipment systems,
or source code from these games will be copied. Jomon's physical household,
finite low mysticism, medieval work, cargo, and material obligations remain
the content authority. Benchmarks and production-path audits must distinguish
the cost and reachability of these translations from their descriptions.
