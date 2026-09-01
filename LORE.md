# Jomon — Medieval River and Coast Reference

> **Jomon is a working vessel and an itinerant household. Its people carry goods, news, debts, and grudges through a river basin that opens onto a dangerous coast.**

## Authority and purpose

This is the authoritative setting and content reference for Jomon. It defines the game’s medieval direction, its material and mystical limits, and the content grammar used by later implementation work. `TODO.md` is the authoritative execution plan; it records the required systems, status, acceptance criteria, and implementation decisions. The current codebase contains an earlier prototype whose space-fiction terminology and systems are not canon; it is an implementation reference only until replaced by the roadmap in [`TODO.md`](TODO.md).

Jomon is an original composite setting. Real medieval history may inform tools, labour, trade, law, and settlement life, but the game does not portray a real country, dynasty, religion, or language. Procedural history and materially grounded simulation are influences, not permission to copy another game’s lore, text, names, or designs.

## Hard canon

1. **Jomon** is the game’s name and the vessel’s name.
2. Jomon is a river-and-coast trading vessel and the mobile home of an itinerant household.
3. The world is late-medieval in material culture: sail and oar craft, quays, mills, windlasses, workshops, mail and plate, crossbows, paper records, and hand-made goods are ordinary. Gunpowder warfare is not the default frame.
4. Jomon is a low-mysticism medieval fantasy world. Rare relics, totems, boons, curses, rites, omens, and other uncanny forces can have real mechanical effects. They must be legible, finite, seeded where random, tied to an in-world source, and integrated with material people, places, labour, trade, and danger.
5. There is no default mage class or free-form spellcasting profession. Mysticism is exceptional, costly, and embodied in objects, places, conditions, practices, and relationships rather than a generic source of unlimited power. Religions, folk practices, vows, and rituals may exist as original cultures and may sometimes interact with real mysticism, but do not reproduce a real religion.
6. Trade is physical. Goods occupy space, spoil, break, can be stolen, can be delivered, and change what settlements can do.
7. Human conflict, wilderness, and operational hazards are equally important sources of danger. None is merely flavour for another.
8. Every new world’s initial Jomon household is wholly generated from the world seed and resolved configuration. Roster, identities, roles, relationships, histories, equipment, and eligibility must reproduce exactly from the same world manifest.
9. At world creation, the player chooses one eligible generated crew member as the initial courier. The choice advances no world time and permits no individual roster rerolls; changing the household requires a different seed or resolved configuration.
10. The player inhabits one Jomon crew member at a time. Crew members may be switched voluntarily at Jomon’s tavern. When a courier is truly lost, control transfers to an eligible surviving crew member; new playable characters join only by joining Jomon’s household. If no eligible living crew member remains, active play ends and the world is finalized as a read-only, exportable chronicle.
11. Jomon, surviving crew, cargo losses, debts, market conditions, route damage, relationships, local history, and each persistent person’s individual state survive an expedition failure. A rare, explicit mystical safeguard may avert a death or disaster; the irreversible collapse or loss of Jomon ends active play in that world and finalizes it as a read-only, exportable chronicle.
12. Combat remains deterministic, turn-based, and grid-based. Its later redesign must be grounded in medieval weapons, injury, positioning, scarcity, and readable intent.
13. The simulation is action-driven, not wall-clock-driven. Only time-bearing in-world actions advance it; pure inspection, an idle open tab, pause, and a closed browser do not. Every due world event then advances deterministically, even while the courier is elsewhere.
14. ASCII is Jomon’s primary, release-quality visual language. An optional detailed renderer is a second rendering of the same world state and may not conceal or invent consequential gameplay information.
15. A world has no mandatory final campaign. It grows through exploration, Jomon’s physical expansion, individual and institutional history, and escalating base, NG+, and NG++ eras that preserve the same Jomon and surviving crew.
16. Jomon is a single-player, offline-first desktop browser game. It needs no account, server, cloud sync, network connection, remote telemetry, or multiplayer authority. A browser may retain multiple active worlds and read-only chronicles subject to local storage quota, but only one world is mutable in a browser session.
17. Jomon does not include or procedurally generate sexual violence, slavery, torture, or harm/endangerment of children. These are not background texture, simulation events, player actions, contracts, hazards, enemy behaviour, rumours, histories, or player-facing content.

## The river-to-coast frontier

The known starting region is a watershed: tributaries, canals, floodplains, mill towns, ferry crossings, marshes, river cities, estuaries, and a contested coast. Inland and coastal worlds are linked by water, but passage is never automatic. River height, weather, seasonal work, war, tolls, shoals, quarantine, labour shortages, damaged locks, and local law determine which routes are practical.

The world has an expanding frontier rather than an artificial map edge. As Jomon reaches new waterways and coasts, further regions are generated and become explorable; their identities may already be heard through rumours, charts, cargo marks, travellers, letters, trade, and old stories. Regions must have distinct geography, ecology, peoples, mystical pressures, architecture, goods, enemies, and practical problems. Expansion must remain causally connected to the known world and optimized so it never erases or contradicts remembered people or history.

The setting has no universal state. Manors, free towns, guild halls, monasteries, river leagues, fishing communities, fortified estates, travelling companies, and local assemblies overlap. A charter may be valid at one quay and meaningless across the next bend. A household survives by knowing who can witness a contract, who controls a lock, who needs a cargo, and when it is safer to leave.

No settlement is a generic medieval backdrop. Each needs a purpose, a water relationship, a labour structure, a material pressure, local authorities, a trade dependency, a route consequence, and visible civilian life.

## Jomon and its household

Jomon is a walkable deck plan, not merely an abstract hub screen. Its permanent spaces include a tavern, crew berths, chart table, cargo hold, galley, repair space, stores, and gangplank. Later play may add rooms, refit workspaces, improve tools and rigging, increase crew capacity, and add small craft. Every expansion is a physical, persistent change with material, labour, capacity, upkeep, route, or social trade-offs.

Operating a vessel prop opens a compact contextual key-choice prompt. The chart table handles route work; the hold handles loading and unloading; the repair space handles vessel maintenance; and the tavern handles conversation, rest, crew selection, and succession. Gangplanks connect Jomon to quays, piers, shore landings, and small craft.

A compact, strategic management sidebar is visible by default and may be collapsed. It provides a deliberately comprehensive view of all information known by the household—tasks, people, work, risks, sites, routes, and history—without displacing the map as the primary view. It labels source, discovery time, and freshness when relevant, but never reveals undiscovered global truth. It is not a replacement for walking through Jomon or acting on props. The map, messages, conversations, ledgers, notices, and management view must each reveal useful, non-duplicative facets of the same world state.

Jomon is an itinerant household, not the private property of one chosen captain. Its people have different skills, obligations, loyalties, needs, possessions, injuries, families, memories, and reasons to stay. Useful recognisable roles include a bargemaster, pilot, factor, carpenter, guard, cook, healer, scribe, carter, fisher, and bard. A role describes labour and social access, not a rigid character class; mystical aptitude, where present, is tied to a specific history, practice, place, or object.

## The player and crew continuity

At world creation, the player chooses an initial courier from the deterministic eligible household roster. The choice neither changes time nor rerolls an individual person. Thereafter, the player’s point of view rotates among Jomon’s crew members only at the tavern: the player may select any eligible living crew member as the active character. People become playable only after joining the household through employment, rescue, family ties, debt, persuasion, defection, contract, or another diegetic relationship. The current character carries their personal skills, injuries, equipment, relationships, obligations, and memories into an expedition.

The active courier normally controls only themself. They can use conversation, trust, standing, shared goals, and material offers to delegate work to other crew and NPCs; people may agree, refuse, negotiate, delay, fail, succeed, or change their minds according to their own circumstances. Delegated tasks include repair, rigging, cooking, treatment, cargo handling, trade research, barter, bookkeeping, scouting, charting, gathering, hunting, guiding, watch duty, guarding, rescue, evacuation, recruitment, correspondence, witness work, and negotiation. These are real jobs with time, risk, cost, and visible outcomes—not free background buffs.

When a crew member dies, they do not return and their personal development does not transfer automatically. Another eligible survivor takes over. If no eligible living crew member remains, there is no invisible replacement or management-screen recruitment: active play ends and the world is kept only as a read-only, exportable chronicle. Exceptionally rare relics, rites, or other safeguards may avert or reverse a death through an explicit and costly rule; these are neither routine nor a substitute for loss. The household retains the consequences: a lost worker changes capacity, a debt may pass to the vessel, cargo may remain at a wreck, and witnesses may remember the dead person or the vessel that employed them.

Every instantiated person has an individual persistent record including family, work, needs, relationships, injury, possessions, birth, and death. Nearby people and people whose actions currently matter receive detailed simulation. Distant people retain their full individual records but advance through deterministic, scheduled summaries whenever the player causes in-world time to pass. An ungenerated frontier has deterministic regional commitments and any already revealed facts, not latent mutable people; when a place or named person must exist, the world materializes the full persistent record without contradicting prior knowledge. Resulting moves must be consistent with known history and discoverable when they become relevant.

## Trade, contracts, and material consequence

Trade and profit are the main reasons to choose a voyage. Jomon carries tangible commodities such as grain, salt fish, wool, timber, charcoal, ironwork, lime, pottery, paper, dye, medicines, tools, and local specialty goods. Every commodity must have a source, a use, a transport condition, an identifiable buyer, and a failure mode.

Contracts can concern freight, letters, passengers, evidence, repairs, salvage, labour, safe conduct, debt, or witness work. A commission should show what is known, who is accountable, what physical burden it creates, and the immediate consequence of refusal or failure. Cargo is not abstract money: capacity, weight, bulk, fragility, spoilage, handling, theft, and recovery create route decisions.

Markets are local and persistent. Shortages, harvests, blockades, tolls, strikes, wrecks, repairs, and competing carriers change demand and prices. Profitable work can relieve a town, enrich a faction, worsen a dependency, or make Jomon unwelcome elsewhere.

## Dangers and conflict

All three danger families require equal authored and procedural support.

- **Human conflict:** toll collectors, rival carriers, guards, deserters, bandits, soldiers, debt agents, smugglers, guild disputes, and local feuds. People have material goals and can negotiate, flee, deceive, arrest, trade, or fight; civilians are not disposable enemy dressing.
- **Wilderness:** floods, ice, storms, fog, shoals, mud, hunger, disease, exposure, wild animals, insects, difficult terrain, and rare uncanny places. These create route, supply, and positional problems; mystical effects, when present, need specific sources and readable counterplay.
- **Operational hazards:** snapped lines, collapsing wharves, bad cargo, mill machinery, fire, damaged locks, fouled wells, unstable scaffolds, and neglected vessels. Their causes and counterplay must be legible before severe harm resolves.

The intended atmosphere is tense material unease with rare genuine mystery. Bodies are vulnerable, work is dangerous, and scarcity hurts, but ordinary humour, craftsmanship, music, rest, trade, and mutual aid remain visible. The uncanny should make material life stranger and more dangerous, not replace it.

## Tactical and content grammar

Future content uses recognisable medieval terms and silhouettes. A bard is a bard; a mill is a mill; a ferry, wharf, market, watchtower, tavern, manor, guildhall, shrine, and caravan have practical purposes players can infer. Avoid opaque invented labels where a familiar material word communicates the function.

Each authored or generated location should declare:

- settlement or route purpose;
- water, terrain, and season relationship;
- local goods and dependencies;
- authorities and labour groups;
- human, wilderness, and operational pressures;
- accessible services and physical interaction points;
- expedition objective, rewards, and later world consequence;
- readable architecture, props, tools, hazards, and foes.

Equipment must be material and multi-use where practical: lines, hooks, poles, shields, lamps, tools, medicine, provisions, weapons, documents, and trade goods can matter in combat, travel, cargo handling, repair, negotiation, or escape. Rare relics, totems, boons, curses, and preparations can add slightly fantastical effects. Combat power may chain through equipment, relics, learned techniques, temporary preparations, crew support, enemy weaknesses, and environmental interactions; each effect must be legible, bounded, and compatible with tactical speed. The later guardian system can use grounded large threats such as war machines, siege works, wrecks, beasts, fortified positions, or dangerous industrial works, and rare mystical threats where their cause, counterplay, and world consequence are equally concrete.

## Language boundaries

New player-facing content must not use spaceflight, planets, hyperlight, carriers, airlocks, aliens, advanced machinery, or the former industrial-science-fiction canon. Use waterways, roads, quays, ports, settlements, holds, taverns, charts, contracts, ledgers, crews, and local institutions instead. Use gangplank and quay for ordinary vessel access.

Use supernatural vocabulary precisely. “Curse,” “spirit,” “miracle,” “relic,” and similar terms may be mechanical truth when Jomon’s world establishes a real mystical source and readable rules; they may also remain belief, metaphor, fraud, or uncertainty when appropriate. Do not make ambiguity an excuse to hide consequential mechanics from the player.

Content boundaries apply to all generated and authored material. Do not include, allude to, or encode sexual violence, slavery, torture, or harm/endangerment of children in simulation records, procedural history, off-screen summaries, ambient text, social systems, contracts, hazards, enemy behaviour, or player action. If a system could generate one, it must reject and replace the result deterministically.

## Implementation boundary

This reference does not claim that the current prototype already implements the setting. The first code milestone is the clean game boundary and foundational world systems in [`TODO.md`](TODO.md); the walkable Jomon deck follows those foundations. New work must follow [`TODO.md`](TODO.md), keep randomness seeded and inspectable, and validate every player-facing path through focused tests, deterministic automation, browser coverage, and builds.
