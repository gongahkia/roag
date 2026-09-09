# Jomon — Major systemic-world and content expansion

You are continuing development of Jomon, a Python 3.11+ terminal roguelike rendered with `curses`.

The expected starting commit is:

```text
e32997d4eadc09b63a8cd14d4bf4c8b0932539d9
```

The game is already extremely fun in its current state. Preserve its responsiveness, legibility, existing content, immediate playability, and grounded identity.

This is a major implementation tranche. Work autonomously through all phases. Do not stop after research, planning, scaffolding, an architectural foundation, or one vertical slice. Do not ask the project owner further design questions unless an external permission or genuinely destructive action makes progress impossible.

You may use the full available context and execution budget. If subagents are available, use them only for bounded independent research, audits, testing, or content review. The primary agent must own architectural decisions, integration, testing, and final claims.

## Primary objective

Substantially expand Jomon through:

* deeper causal world generation;
* additional regions and geographic variation;
* interacting environmental systems;
* more enemies, wildlife, elites, bosses, and rival actors;
* greater enemy-to-enemy and enemy-to-world interaction;
* new quests and cross-region stories;
* deeper factions, markets, routes, seasons, and consequences;
* considerably more treasure, equipment, techniques, consumables, and build possibilities;
* more functional systems aboard Jomon;
* better persistence and replayability;
* strict performance preservation.

This is not permission to replace the current game with a speculative engine.

The result must remain a playable game throughout implementation.

## First: inspect and preserve the repository

Before modifying anything:

1. Read, in order:

   * `LORE.md`
   * `PRODUCT.md`
   * `TODO.md`
   * `README.md`
   * all current milestone assessments
   * causal-generation documentation
   * encounter-balance documentation
   * build-content matrix
   * questline documentation
2. Inspect the production code and tests rather than trusting documentation counts.
3. Record:

   * actual `HEAD`;
   * current branch;
   * worktree status;
   * remote relationship;
   * Python version;
   * save version;
   * real content counts;
   * real test count and duration.
4. Preserve uncommitted and concurrent work.
5. Do not reset, amend, squash, rewrite history, or discard unrelated changes.
6. Do not push.
7. Make a planning commit before implementation.
8. Commit every small coherent implementation step separately.
9. Keep a live milestone document containing decisions, measurements, completed work, and remaining acceptance gates.

If the starting commit differs, inspect why and adapt without erasing work.

## Research phase

Conduct fresh research before settling the implementation plan. Prefer primary sources, developer talks, official documentation, postmortems, and developer interviews.

At minimum examine:

### Noita

* [Exploring the Tech and Design of Noita](https://www.youtube.com/watch?v=prXuyMCgbTc)
* [Road to the IGF: Nolla Games’ Noita](https://www.gamedeveloper.com/game-platforms/road-to-the-igf-nolla-games-i-noita-i-)
* [Noita falling-sand simulation interview](https://80.lv/articles/noita-a-game-based-on-falling-sand-simulation)
* [Official Noita site](https://noitagame.com/)

Extract applicable lessons concerning:

* simple local rules producing large consequences;
* materials interacting consistently;
* player, enemies, items, and environments using the same rules;
* authored macro-structure combined with procedural micro-variation;
* Herringbone Wang-tile or comparable constrained assembly;
* discoveries that teach transferable systemic knowledge;
* modular build combinations;
* hazards that can become tools;
* profiling from the beginning;
* chunks, dirty regions, and selective simulation;
* why simulation without readable gameplay is frequently worthless.

Do not attempt literal per-pixel falling-sand simulation in Python or `curses`.

### Caves of Qud

* [Official Caves of Qud site](https://cavesofqud.com/)
* [Systems-driven design in Caves of Qud](https://unity.com/resources/systems-driven-design-in-caves-of-qud)
* [End-to-End Procedural Generation in Caves of Qud](https://www.gdcvault.com/play/1026313/Math-for-Game-Developers-End)
* [Procedurally Generating History in Caves of Qud](https://www.gdcvault.com/play/1024990/Procedurally-Generating-History-in-Caves)
* [Tile-Based Map Generation using Wave Function Collapse in Caves of Qud](https://www.youtube.com/watch?v=AdCgi9E90jw)
* [Data-Driven Engines of Qud and Sproggiwood](https://www.youtube.com/watch?v=U03XXzcThGU)

Extract applicable lessons concerning:

* hybrid authored and procedural worlds;
* a stable authored backbone surrounded by generated possibility;
* composable entity capabilities rather than brittle inheritance;
* using the same meaningful properties for players, NPCs, enemies, and objects;
* multi-stage generation where history affects culture, structures, people, quests, and artifacts;
* historical state machines and post-hoc causal rationalization instead of simulating every year;
* population tables, encounter roles, builders, painters, and layered map passes;
* providing “interesting bones” upon which stories emerge;
* authored systems producing stories rather than random incidents;
* content expressed through inspectable, composable data.

### Research deliverable

Create a concise research document that records:

* techniques adopted;
* techniques rejected;
* how each adopted technique is translated to a deterministic terminal game;
* performance consequences;
* copyright and identity boundaries;
* specific Jomon systems that will consume it.

Do not copy Noita’s or Caves of Qud’s source code, terminology, enemies, lore, characters, maps, text, names, or distinctive content. Jomon must remain original.

## Canon and product constraints

Preserve all hard rules in `LORE.md`, including:

* original medieval river-and-coast setting;
* no portrayal of real countries, religions, or languages;
* low mysticism;
* no generic mage class;
* no unlimited free-form spellcasting;
* no sexual violence;
* no slavery;
* no torture;
* no harm or endangerment of children;
* local-only operation;
* no accounts, server, telemetry, cloud sync, multiplayer, or network runtime;
* deterministic seeded generation;
* action-clock time;
* idle terminal time never advances the world;
* ASCII and keyboard-first presentation;
* mouse as an optional enhancement;
* Linux, macOS, and WSL support;
* contextual injury, death, loss, and succession.

Preserve immediate play:

* a new world begins with an equipped bargemaster at Jomon’s gangplank;
* pressing `E` can immediately begin the Hearthford expedition;
* preparation, inventory optimization, crew management, and crafting remain optional opportunities rather than mandatory chores.

## Design thesis

Implement the following synthesis:

### From Noita

A small number of understandable physical rules interact across many pieces of content.

Examples:

* fire spreads through suitable material;
* water extinguishes fire and changes soil;
* smoke obscures sight and travels with wind or elevation;
* floods follow elevation and openings;
* load-bearing structures fail when damaged;
* substances coat actors and items;
* hazards affect player, enemies, cargo, structures, and NPCs consistently;
* enemy abilities can be exploited to alter terrain or expose treasure.

### From Caves of Qud

Authored content and procedural systems reinforce each other.

Examples:

* regional history changes geography and structures;
* geography determines resources and settlement work;
* resources influence markets and institutions;
* institutions influence quests, equipment, patrols, and relationships;
* historical events leave physical ruins, named items, testimony, and disputes;
* entities are composed from useful capabilities;
* generated variation connects to authored narrative anchors.

### Jomon’s own identity

Jomon should remain about:

* an itinerant household;
* one embodied courier at a time;
* physical cargo and equipment;
* river and coastal travel;
* household continuity;
* material obligations;
* dangerous expeditions;
* persistent relationships;
* environmental and tactical problem-solving;
* rare and ambiguous mysticism;
* causal consequences.

## Performance is a feature

Before adding systems, construct a reproducible benchmark suite.

Measure at least:

* cold startup;
* new-world generation;
* existing-world load;
* regional entry;
* ordinary movement;
* enemy-heavy turns;
* environmental-heavy turns;
* field-of-view calculation;
* pathfinding;
* rendering;
* inventory opening and auto-packing;
* route-chart opening and navigation;
* saving;
* save size;
* peak resident memory;
* deterministic replay;
* 100-seed and 1,000-seed generation audits.

Record baseline median, p95, p99, and worst-case measurements where meaningful.

### Performance targets

Use the measured baseline intelligently, but aim for:

* normal input-to-render median under 25 ms;
* normal input-to-render p95 under 50 ms;
* normal p99 under 100 ms;
* unusually heavy environmental or encounter turns under 250 ms;
* cold world creation under 3 seconds on the development machine;
* ordinary save and load under 1 second;
* no unbounded growth in scheduled events, dirty cells, caches, histories, actors, or pathfinding work;
* no persistent memory growth while repeatedly traveling;
* no more than a 20% regression from baseline in existing ordinary scenarios without a documented, user-visible justification.

If the current baseline already exceeds a target, improve it where practical and enforce no regression.

### Required performance architecture

Use bounded techniques appropriate to Python:

* spatial chunking;
* dirty-cell or dirty-chunk environmental updates;
* active-region simulation;
* scheduled-event queues;
* spatial indices for actors and interactables;
* cached path fragments with safe invalidation;
* hierarchical or bounded pathfinding;
* reusable FOV buffers;
* sparse persistent deltas;
* lazy regional generation;
* coarse distant-state advancement;
* deterministic cache reconstruction;
* strict per-turn work budgets.

Do not:

* update every cell in every region every turn;
* pathfind for every distant NPC;
* replay full world history on every load;
* serialize disposable derived caches;
* introduce nondeterministic background threads;
* hide long work behind uncontrolled asynchronous state;
* advance simulation while the player is idle.

Use profiling evidence rather than intuition.

## System 1 — Deeper causal world generation

Replace shallow independent placement with a visible causal generation pipeline.

The pipeline should approximately follow:

```text
seed and world parameters
→ watershed, coast and elevation
→ climate exposure and seasonal tendencies
→ geology, soil and water behavior
→ plant and animal ecology
→ resources and production opportunities
→ settlements and structures
→ institutions and material conflicts
→ bounded regional histories
→ routes, trade and travel hazards
→ ruins, caches and legendary objects
→ quests, inhabitants and encounter ecology
```

Every layer must affect at least two later systems. Do not generate invisible metadata merely to claim depth.

Examples:

* geology changes terrain, structures, mined goods, armour materials, hazards, and enemy equipment;
* flood history changes abandoned buildings, raised walkways, testimony, markets, and present flood controls;
* a past fire changes forest cover, charcoal production, smoke ecology, faction memory, and buried caches;
* route collapse changes prices, patrol behavior, quests, and voyage danger;
* an old dispute produces opposed accounts, a named object, a damaged site, and different contact attitudes.

### Historical generation

Use bounded event chains rather than simulating thousands of empty years.

For each major region, generate approximately three to seven meaningful historical events. Events should:

* mutate concrete world state;
* reference people, institutions, places, materials, or prior events;
* leave physical or social evidence;
* produce at least one current tension or opportunity;
* support conflicting interpretations where appropriate;
* remain inspectable through a generation ledger.

Historical causality must be reproducible from the seed.

A player should be able to encounter history through:

* altered terrain;
* ruins;
* repairs;
* inscriptions or records;
* oral testimony;
* named equipment;
* institutional customs;
* market anomalies;
* inherited obligations;
* enemy occupation;
* route names.

### Geographic generation

Retain the existing four regions and deepen their internal variation.

Add at least four substantial new regional families consistent with the river-and-coast setting. Design them after reading the lore. They should not be palette-swapped rectangular dungeons.

Suitable geographic directions may include, but are not limited to:

* peat fens and bog islands;
* river gorges and mineral cuts;
* agricultural flood terraces and clayworks;
* cold estuaries or seasonal ice channels;
* shingle islands and exposed channels;
* ancient raised causeways and drowned lowlands;
* wind-cut coastal cliffs;
* braided river deltas.

Choose the strongest original set.

The completed world should contain at least eight major explorable regional destinations, plus route nodes, minor sites, vessel stops, and discoverable landmarks.

Every region should contain a region-specific combination of:

* open terrain;
* structures;
* caves or below-ground areas;
* elevation;
* at least one loop;
* multiple approaches;
* optional dangerous depth;
* visible landmarks;
* at least one non-dungeon traversal area;
* at least one site altered by history;
* at least one route changed by season or weather;
* at least one environment-driven tactical situation.

Continue using seamless camera-following maps and aligned z-levels.

### Structure generation

Build structures through constrained authored pieces and multi-pass generation.

Possible stages include:

1. select function and historical condition;
2. establish footprint and access points;
3. assemble compatible authored pieces;
4. place load-bearing and environmental systems;
5. damage or modify according to history;
6. populate according to current use;
7. validate navigation and tactical space;
8. add evidence, treasure, and affordances.

Use Wang-style, WFC-style, grammar-based, stamp-based, or graph-guided assembly only where it produces better controlled layouts. Do not introduce an algorithm merely because another game uses it.

Validate that:

* mandatory sites are reachable;
* vertical connectors align;
* objectives cannot spawn sealed;
* return routes exist;
* structures have plausible entrances;
* seasonal changes cannot block every path;
* no actor begins with an unavoidable attack;
* generated maps contain meaningful loops, cover, sightlines, and open areas;
* environmental systems cannot immediately destroy required quest objects.

Add topology and generation-quality audits.

## System 2 — Bounded material simulation

Implement a deterministic, turn-based material simulation inspired by Noita’s systemic consistency but appropriate for a terminal roguelike.

Do not simulate pixels. Simulate sparse tile and entity states.

### Suggested material properties

Use a bounded, data-driven set such as:

* flammable;
* combustible;
* wet;
* absorbent;
* brittle;
* load-bearing;
* loose;
* viscous;
* buoyant;
* corrosive;
* insulating;
* smoke-producing;
* wind-affected;
* freeze-sensitive;
* structurally supported.

Only retain properties that produce gameplay.

### Suggested sparse fields

Use a limited number of layers:

* fluid type and depth;
* heat or fire intensity;
* gas or smoke density;
* structural support or damage;
* surface coating or contamination.

Do not make every tile carry a giant mutable object if sparse overlays are sufficient.

### Required interactions

Implement a coherent bounded set including:

* water extinguishing fire;
* rain reducing fire and increasing wet ground;
* shallow water and soil producing mud;
* fire spreading through dry reeds, timber, pitch, cloth, and suitable cargo;
* smoke reducing sight and rising or drifting across z-levels;
* wind moving smoke and affecting fire direction;
* shallow water freezing or thawing under suitable seasonal conditions;
* damaged supports causing telegraphed collapse;
* floodwater following openings and elevation;
* falling debris damaging actors and blocking routes;
* saltwater, lime, ash, resin, oil, and charcoal participating in selected grounded reactions;
* wet, burning, smoking, coated, chilled, or burdened states attaching to actors and physical items.

Every interaction must be:

* deterministic;
* bounded;
* readable;
* inspectable;
* applicable to enemies and NPCs where relevant;
* useful to the player as well as dangerous.

### Player verbs

Expose environmental agency through existing controls or contextual actions:

* ignite;
* extinguish;
* pour;
* throw;
* cut;
* brace;
* lever;
* dig;
* break;
* push;
* pull;
* open or close;
* redirect;
* climb;
* descend;
* drop.

Do not turn the controls into a keyboard memorization test. Use contextual menus and highlighted valid targets.

### Readability

The player must be able to inspect:

* material;
* active reaction;
* predicted immediate consequence;
* structural danger;
* fluid depth;
* smoke severity;
* relevant equipment protection;
* whether an action consumes time or an item.

Environmental deaths must be causally explainable.

## System 3 — Composable entities without a rewrite

Move toward capability composition without replacing the whole codebase with a new ECS.

Use small components, traits, records, or protocols for concrete capabilities such as:

* body and body locations;
* inventory;
* equipment;
* faction or institution;
* perception;
* goal selection;
* movement modes;
* container;
* door or barrier;
* structural support;
* flammability;
* liquid storage;
* merchant service;
* dialogue memory;
* treatment;
* recruitability;
* destructibility.

The same reducer should handle the same capability regardless of whether it belongs to:

* the player;
* a crew member;
* a hostile;
* a neutral NPC;
* an animal;
* a door;
* a chest;
* cargo;
* furniture;
* a regional control.

Enemies should be able to use suitable equipment, doors, cover, ladders, alarms, healing supplies, and environmental controls.

Do not perform a large abstract refactor before proving the capabilities in production content.

Every new abstraction must have at least three real consumers or remove immediate duplication.

## System 4 — Ecology and population

Give each region an ecology rather than merely an encounter table.

Define bounded ecological roles:

* territorial animal;
* predator;
* prey or nuisance creature;
* scavenger;
* herd or pack;
* nesting or denning creature;
* opportunistic human group;
* regional patrol;
* resource worker;
* environmental controller.

Actors should respond to:

* food or scavenging opportunities;
* home territory;
* predators;
* rival groups;
* alarms;
* fire;
* smoke;
* flooding;
* weather;
* injury;
* valuable dropped cargo;
* corpses or abandoned equipment;
* player noise;
* existing regional changes.

Do not simulate a global food chain turn by turn.

Use two layers:

* high-fidelity actors in the active region or nearby chunks;
* coarse population state elsewhere.

Persist:

* named actors;
* unique enemies;
* killed or recruited actors;
* materially changed groups;
* extinctions or major population shifts;
* stolen unique items;
* established lairs;
* quest-relevant patrols.

Ordinary anonymous populations may be summarized and regenerated deterministically from remaining population state.

## System 5 — Expanded enemy intelligence and interaction

Preserve the current perceived-information model:

* line-of-sight;
* sound;
* last-known position;
* group alerts;
* morale;
* injury;
* ammunition;
* territory;
* material duties.

Add bounded goal-oriented behavior rather than omniscience.

### Required enemy goals

Support enemies that attempt to:

* hunt;
* defend territory;
* escort;
* patrol;
* ambush;
* flank;
* suppress;
* seek elevation;
* hold a crossing;
* raise an alarm;
* extinguish a dangerous fire;
* start or feed a controlled fire;
* flood or drain an area;
* protect a leader;
* cover a retreat;
* rescue a wounded ally;
* steal cargo;
* retrieve a named object;
* escape with stolen property;
* scavenge dropped equipment;
* pursue a quest objective;
* retreat to a defensible location;
* negotiate or surrender;
* betray an agreement under explicit conditions.

### Enemy-to-enemy interaction

Different groups should be able to:

* fight;
* avoid one another;
* cooperate;
* protect allies;
* prey on animals;
* exploit another group’s alarm;
* steal from each other;
* respond differently to regional hazards;
* temporarily align around a shared threat.

These relationships must come from regional ecology, institutions, history, or current goals—not random hostility rolls.

### Tactical cognition tiers

Use bounded cognition levels:

* reactive creatures;
* territorial or pack animals;
* trained combatants;
* specialists;
* named leaders and rivals.

Simple creatures should not run expensive general-purpose planning.

Named enemies may have richer utility scoring, memory, equipment use, and retreat logic.

### Fairness

All dangerous actions require appropriate counterplay through:

* intent;
* animation or turn staging;
* sound;
* visible target cells;
* predicted path;
* known ability;
* environmental cue.

Avoid unavoidable off-screen attacks.

## System 6 — Large enemy-content expansion

Audit the current enemy count and then reach at least:

* 48 mechanically distinct standard archetypes in total;
* 16 elite situations in total;
* at least four new named recurring enemies, regional bosses, or rival figures;
* broad representation across every region and voyage type.

Counts alone are not success.

Every standard enemy needs:

* a geographic or social reason to exist;
* a distinguishable glyph and semantic colour;
* a primary goal;
* a secondary reaction;
* a material or terrain interaction;
* a tactical strength;
* at least two counters;
* a morale or retreat condition;
* a place in mixed encounters.

Every elite needs:

* a rule-changing mechanic;
* multiple phases, goals, or spatial states;
* at least two tactical answers;
* at least one environmental, social, or material answer;
* persistent consequences;
* a distinctive reward or world change.

Every named boss or rival should be able to participate in narrative rather than merely wait in an arena.

Where appropriate, allow them to:

* retreat;
* remember the courier;
* acquire an injury;
* lose equipment;
* change tactics;
* recruit followers;
* alter a route;
* attack Jomon;
* negotiate;
* die permanently;
* be replaced by a successor.

Do not build a universal nemesis framework unless repeated implemented cases justify a helper.

## System 7 — Factions, institutions, and relationships

Deepen the world’s institutions without adding a grand-strategy simulator.

Create a bounded network of original:

* work associations;
* river authorities;
* settlement councils;
* repair or salvage groups;
* patrol companies;
* coastal communities;
* travelling households;
* material-interest groups.

Use existing contacts and regional histories where possible.

Each institution needs:

* one material dependency;
* one current goal;
* one relationship or dispute;
* one regional presence;
* one service or benefit;
* one reason it may oppose the player;
* readable reputation consequences.

Track separately where useful:

* courier-specific memory;
* household reputation;
* obligations;
* witnessed acts;
* market confidence;
* institutional trust.

Reputation must unlock or close concrete options such as:

* routes;
* prices;
* recruitment;
* treatment;
* safe shelter;
* information;
* negotiation;
* equipment;
* warnings;
* reinforcements.

Avoid a single universal reputation number.

## System 8 — Economy and cargo

Deepen the existing physical cargo and market design.

Use causal regional production:

```text
resources
→ work and seasonal yield
→ stock
→ local consumption
→ shortage or surplus
→ prices and demand
→ contracts
→ patrol, theft and route pressure
→ household and institutional consequences
```

Preserve bounded cargo and physical lots.

Implement:

* regional production and consumption;
* seasonal stock changes;
* route disruption;
* cargo condition;
* handling requirements;
* market memory;
* shortages caused by actual events;
* surpluses caused by successful intervention;
* contracts tied to world conditions;
* merchants who transport real stock;
* stolen or destroyed cargo affecting named parties.

Do not simulate every individual purchase.

Use daily or action-clock scheduled aggregate updates.

Expand commodities only if consistent with `LORE.md`. Every new commodity needs:

* source;
* use;
* weight;
* bulk;
* handling;
* failure mode;
* interested buyers;
* at least one environmental interaction;
* at least one quest or equipment use.

## System 9 — Equipment composition and Noita-like synergy

Do not clone Noita’s wand system.

Create a grounded Jomon equivalent based on physical equipment preparation and modular technique interaction.

Possible axes include:

* weapon family;
* material;
* head, haft, binding, string, stock, or edge;
* ammunition;
* coating;
* stance or learned manoeuvre;
* armour;
* secondary tool;
* crew preparation;
* passive discovery;
* relic;
* weather;
* terrain;
* injury.

Add a bounded fitting or modification system aboard Jomon’s workshop.

For example:

* a weapon may accept one structural fitting and one treatment;
* ammunition may accept one recoverable or consumable preparation;
* armour may accept one lining or reinforcement;
* tools may be repurposed contextually;
* modifications remain physical and occupy inventory or locker space;
* fitting, removal, damage, and repair have causal costs.

Avoid combinatorial UI chaos. Preview the resulting actions and trade-offs before confirmation.

### Content targets

After auditing current totals, reach at least:

* 36 weapon families;
* 36 armour or protective clothing pieces;
* 64 mechanically active passives or learned techniques;
* 32 secondary tools, field supplies, drinks, or usable preparations;
* 12 finite relics;
* 60 persistent containers, named caches, vaults, wreck stores, or hidden deposits.

At least one third of weapons should support ranged, thrown, reach, control, or hybrid play.

New weapons should include meaningful differences such as:

* minimum range;
* arcing fire;
* bracing;
* suppression;
* knockback;
* pulling;
* pinning;
* area denial;
* ammunition recovery;
* elevation advantage;
* structural damage;
* smoke creation;
* loudness;
* multi-turn reload;
* restraint;
* anti-armour;
* anti-animal;
* shield interaction.

Do not create damage-number reskins.

### Synergy quality

Every retained item must have:

* an observable production effect;
* an inspection description that explains it;
* at least one interaction with another system;
* a test of its core behavior.

Major treasures and relics should support at least two cross-system interactions.

Demonstrate at least 24 genuinely distinct builds in documentation and automated scenarios.

Examples of distinct build identities:

* elevated marksman;
* smoke hunter;
* flood controller;
* heavily armoured brace fighter;
* mobile reach fighter;
* thrown-weapon retriever;
* cargo-backed negotiator;
* terrain-breaking salvager;
* quiet treasure scout;
* healer-survivor;
* weather-prepared traveller;
* relic-driven rule breaker.

## System 10 — Procedural legendary objects and treasure histories

Generate rare named equipment and treasure from actual world history.

A legendary object should derive from combinations such as:

* maker;
* institution;
* material;
* original purpose;
* historical event;
* damage or repair;
* prior owner;
* current resting place;
* associated claim or dispute.

The generated history must affect mechanics.

Examples:

* flood-surviving bindings resist water;
* fire-scarred armour handles heat but creates smoke;
* a disputed toll weapon changes negotiation with involved institutions;
* a salvaged signal lens reveals distant caches;
* a repaired quarry brace changes structural interactions.

Do not generate long meaningless adjective chains.

Each important object should have:

* a readable short name;
* a concise provenance;
* one major mechanic;
* one trade-off;
* at least one interested character or institution;
* a discoverable clue trail.

Improve treasure signposting using:

* rumors;
* maps;
* elevated views;
* tracks;
* sound;
* records;
* merchant testimony;
* regional history;
* enemy behavior;
* route-chart marks.

## System 11 — Quests and narrative content

Retain and deepen the existing four regional questlines and Four Working Marks arc.

Add:

* a second substantial questline to each existing region;
* at least one substantial questline for every new region;
* at least two additional cross-region arcs;
* bounded repeatable contracts derived from real market, route, ecological, or institutional state;
* personal developments for crew members and recruits;
* named rival and elite story outcomes.

Reach at least 12 substantial regional questlines overall and three cross-region arcs overall.

### Quest requirements

Every substantial questline must include:

* multiple stages;
* at least two disclosed approaches;
* at least two meaningful consequences;
* a tactical encounter;
* a non-combat or environmental approach;
* named characters;
* physical objectives or evidence;
* persistent regional change;
* failure or alteration paths;
* contextual rewards;
* save/reload, defeat, death, and succession resilience.

Generated contracts must not be arbitrary Mad Libs.

They must arise from conditions such as:

* shortage;
* damaged route;
* migration;
* occupation;
* fire;
* flood;
* weather;
* missing shipment;
* institutional dispute;
* threatened settlement work;
* named enemy activity.

Generated contracts should use authored structures with validated participants, destinations, objects, and solutions.

## System 12 — Seasons, weather, and time

Deepen the existing calendar.

Seasons must affect:

* daylight;
* visibility;
* temperature;
* rain, fog, wind, storms, frost, or thaw;
* tides and waterways;
* soil and vegetation;
* animal behavior;
* patrols;
* NPC schedules;
* market production;
* route accessibility;
* fire and smoke;
* regional quests;
* voyage danger.

Weather must be deterministic and forecastable to a useful degree.

Add diegetic forecasting through:

* bartender rumors;
* crew expertise;
* route-chart marks;
* instruments;
* visible sky and water cues.

Do not make weather random punishment.

The player should sometimes exploit weather intentionally.

## System 13 — Jomon as a functional systemic space

Activate currently decorative or read-only vessel areas.

Jomon’s decks should support:

* cargo storage and handling;
* repair;
* bilge and flooding response;
* cooking or preparation;
* injury treatment;
* equipment fitting;
* crew rest;
* watch duty;
* route observation;
* signals;
* navigation;
* visitor trade;
* social interaction;
* emergency response.

Avoid forcing the player through every station before departure.

Crew should perform bounded autonomous work based on:

* role;
* schedule;
* injury;
* relationships;
* vessel condition;
* assigned support;
* current voyage;
* danger.

Expose meaningful results without requiring repetitive micromanagement.

### Voyage and onboard content

Expand the current voyage system to at least 12 encounter families.

Include grounded and rare low-mysticism situations such as:

* raiders;
* boarding parties;
* cargo thieves;
* storms;
* shoals;
* drifting wreckage;
* fire;
* hull damage;
* flooding;
* territorial river creatures;
* misleading voices or songs;
* strange mineral resonance;
* desperate travellers;
* disputed inspections;
* crew conflict.

At least six encounter families should support physical tactical play aboard Jomon’s connected decks.

Use the same systems as regional play:

* fire;
* smoke;
* water;
* elevation;
* doors;
* cover;
* cargo;
* morale;
* negotiation;
* injuries;
* environmental controls.

Jomon can be damaged, repaired, altered, boarded, defended, and remembered. Do not create a separate shallow naval minigame.

## System 14 — Survival without chores

Deepen:

* injury;
* treatment;
* fatigue;
* temperature;
* provisions;
* rest;
* encumbrance;
* equipment condition.

These systems should create expedition decisions, not routine maintenance spam.

Principles:

* supplies should matter mainly under pressure;
* well-prepared travel should resolve smoothly;
* injuries change tactics rather than only lowering a number;
* crew and contacts can provide treatment;
* rest advances the action clock and may alter objectives;
* hunger or provisions should operate over meaningful expedition timescales;
* automatic crew work should handle mundane replenishment when resources permit.

Always explain the cause and remedy of negative states.

## System 15 — Information, inspection, and discoverability

As complexity grows, improve the player’s ability to understand it.

Add or deepen:

* contextual look/inspect;
* material and structure descriptions;
* enemy goals and visible intent;
* quest journal;
* rumor and clue log;
* regional history record;
* route consequences;
* item provenance;
* build interaction explanations;
* status causes and remedies;
* world-generation provenance.

The interface must distinguish:

* fact;
* rumor;
* forecast;
* remembered information;
* inferred danger;
* currently visible state.

Use semantic colours plus glyph and bold fallbacks.

Do not dump implementation statistics into ordinary play.

## Saving and migration

If persistent state changes, introduce save format 7 with a deterministic format-6 migration.

Preserve:

* people;
* deaths;
* injuries;
* succession;
* relationships;
* equipment;
* exact physical item placements;
* containers;
* cargo;
* markets;
* contacts;
* quests;
* regional processes;
* exploration;
* schedules;
* seasons;
* routes;
* histories;
* institutions;
* elite and rival outcomes.

For newly generated immutable geography, consider storing the seed, generation version, manifest, and sparse mutations rather than serializing every derived cell.

However:

* do not invalidate existing format-6 geography;
* do not move existing items;
* do not resurrect dead actors;
* do not regenerate lost unique items;
* do not silently change completed quest outcomes.

Add corruption rejection and round-trip tests.

## Content architecture

Use data-driven definitions where it reduces repetition and improves auditing.

Content definitions should support validation of:

* unique IDs;
* glyph and colour collisions;
* item sizes and rotations;
* valid equipment slots;
* ammunition;
* capabilities;
* regional availability;
* encounter budgets;
* quest references;
* dialogue references;
* loot tables;
* history references;
* migration stability.

Do not put arbitrary executable Python expressions into content data.

Keep production behavior in explicit tested reducers.

## Validation and testing

Add tests continuously rather than at the end.

### Generation validation

Audit at least 1,000 seeds for:

* determinism;
* valid geography;
* route connectivity;
* vertical connectivity;
* objective reachability;
* return paths;
* open-area ratios;
* loops and alternate routes;
* valid structures;
* seasonally accessible alternatives;
* treasure reachability;
* safe player starts;
* no immediate unavoidable attacks;
* valid history references;
* valid institutions;
* valid quest participants;
* valid markets and resources.

### Environmental validation

Test:

* fire spread;
* extinguishing;
* smoke movement;
* wind effects;
* flooding;
* mud;
* freezing and thawing;
* structural support;
* collapse;
* falling debris;
* cross-level propagation;
* deterministic dirty updates;
* bounded update work;
* save/reload reconstruction;
* effects on player, enemies, NPCs, items, cargo, and structures.

### AI validation

Test:

* perception;
* sound investigation;
* memory;
* territory;
* elevation seeking;
* alarms;
* protection;
* retreat;
* rescue;
* theft;
* item recovery;
* scavenging;
* faction conflict;
* hazard avoidance;
* hazard exploitation;
* environmental control;
* ranged fairness;
* boss phase transitions;
* named-rival persistence.

### Content validation

Every retained:

* enemy;
* weapon;
* armour piece;
* passive;
* technique;
* tool;
* drink;
* relic;
* commodity;
* quest;
* elite;
* boss;
* voyage encounter

must have a production path and focused behavior test.

### Persistence validation

Test:

* format-6 migration;
* save round trips;
* mid-quest saves;
* mid-reaction saves;
* enemy-carried stolen items;
* dead courier possessions;
* named rival retreat;
* world-history evidence;
* seasonal transitions;
* markets;
* sparse terrain mutations;
* Jomon damage;
* succession;
* world ending.

### Test-suite performance

The current full suite reportedly contains 177 tests and takes roughly nine and a half minutes.

Do not let testing become unusably slow.

Create:

* a fast developer suite targeting under 90 seconds;
* focused subsystem commands;
* a full correctness suite;
* a slow seed/soak audit;
* benchmark commands producing machine-readable output.

Share fixtures and cache immutable generation safely where possible without masking nondeterminism.

## Manual play verification

Perform substantial real-PTY testing.

At minimum complete:

* one full expedition in every region;
* at least two seeds per new region family;
* both approaches for every new authored questline;
* one ending for each new cross-region arc;
* at least one extended campaign crossing all major regions;
* at least 24 distinct build demonstrations;
* six onboard tactical encounters;
* several environmental chain reactions;
* one treasure recovered through enemy manipulation of the environment;
* one institutional conflict;
* one regional ecological change;
* one named rival retreat and return;
* one permanent rival death;
* one player injury and recovery;
* one courier death and succession;
* one Jomon damage and repair sequence;
* one winter or cold-season expedition;
* one storm or flood expedition;
* one save/reload during an active material reaction;
* one migration from a real format-6 save;
* native mouse inventory and targeting where supported;
* 80×24 and 100×32 layouts;
* resize below minimum and recovery;
* clean terminal restoration after normal quit and injected failure.

Do not claim every content path was manually played if it was not.

## Quality gates

A system is complete only if:

1. It changes actual player decisions.
2. It interacts with at least two other systems.
3. It is inspectable.
4. It is deterministic.
5. It persists correctly where relevant.
6. It has bounded performance.
7. It has focused tests.
8. It appears in ordinary production play.
9. Its consequences are causally logged.
10. It remains understandable at 80×24.

Content is complete only if it is more than a label.

World-generation depth is complete only if generated facts appear physically or socially in play.

Enemy intelligence is complete only if players can observe and exploit it.

Emergence is complete only if multiple authored rules combine naturally; do not script a bespoke “emergent” outcome and count it.

## Explicit exclusions

Do not add:

* literal per-pixel simulation;
* real-time background world advancement;
* full distant-person simulation;
* a general-purpose theorem-proving planner;
* uncontrolled procedural text;
* generative AI runtime dependencies;
* online services;
* multiplayer;
* cloud saves;
* telemetry;
* a total engine rewrite;
* a separate graphical client;
* a new magic system;
* generic mages;
* unlimited spells;
* copied Noita or Caves of Qud content;
* real-world countries, religions, or languages;
* forbidden content from `LORE.md`.

## Suggested implementation order

Use this order unless repository evidence supports a safer dependency ordering:

1. research and baseline;
2. profiling and benchmark harness;
3. save-format and sparse-state plan;
4. causal world-generation manifest;
5. regional geography and structure generators;
6. bounded environmental materials;
7. shared entity capabilities;
8. ecology and populations;
9. AI goals and cross-actor interaction;
10. institutions and reputation;
11. economy and cargo;
12. equipment fitting and synergy;
13. new regions;
14. enemies, elites, rivals, and bosses;
15. treasure and legendary objects;
16. regional quests and cross-region arcs;
17. seasons and weather;
18. functional Jomon systems;
19. voyage content;
20. UI, inspection, and accessibility;
21. performance optimization;
22. migration;
23. automated verification;
24. real PTY play;
25. balance pass;
26. final documentation and assessment.

Keep the game runnable after each phase.

## Final verification commands

Run at minimum:

```bash
python -m unittest discover -s tests -v
python -m compileall -q jomon tests
git diff --check
```

Also run the new:

* fast test suite;
* generation audit;
* encounter audit;
* quest audit;
* content audit;
* persistence audit;
* living-world audit;
* deterministic replay audit;
* performance benchmark;
* memory/long-session soak.

## Final report

The final response must include:

* starting and ending commits;
* every commit created;
* concurrent commits encountered;
* final branch and worktree state;
* remote relationship;
* confirmation that no push was performed;
* sources researched;
* techniques adopted and rejected;
* architectural changes;
* generation pipeline;
* performance architecture;
* benchmark comparison before and after;
* save migration behavior;
* new regions;
* new histories;
* new institutions;
* new environmental rules;
* new enemy interactions;
* exact enemy, elite, boss, weapon, armour, passive, tool, relic, container, quest, character, commodity, and voyage counts;
* questline and arc summaries;
* build-synergy examples;
* automated test results;
* seed-audit results;
* manual PTY sessions actually completed;
* untested platforms or paths;
* candid remaining repetition or weaknesses.

Do not end with “the foundations are ready for future implementation.”

The requested outcome is the implemented, integrated, tested, optimized content expansion itself.

Above all: preserve what currently makes Jomon fun. Add systemic possibility, not merely machinery.

