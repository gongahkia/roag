# Jomon — Medieval River and Coast Reference

> **Jomon is a working vessel and an itinerant household. Its people carry goods, news, debts, and grudges through a river basin that opens onto a dangerous coast.**

## Authority and purpose

This is the authoritative setting and content reference for Jomon. It defines the game’s medieval direction, its material limits, and the content grammar used by later implementation work. The current codebase contains an earlier prototype whose space-fiction terminology and systems are not canon; it is an implementation reference only until replaced by the roadmap in [`TODO.md`](TODO.md).

Jomon is an original composite setting. Real medieval history may inform tools, labour, trade, law, and settlement life, but the game does not portray a real country, dynasty, religion, or language. Procedural history and materially grounded simulation are influences, not permission to copy another game’s lore, text, names, or designs.

## Hard canon

1. **Jomon** is the game’s name and the vessel’s name.
2. Jomon is a river-and-coast trading vessel and the mobile home of an itinerant household.
3. The world is late-medieval in material culture: sail and oar craft, quays, mills, windlasses, workshops, mail and plate, crossbows, paper records, and hand-made goods are ordinary. Gunpowder warfare is not the default frame.
4. There is no magic, supernatural power, occult technology, miracle, monster, or hidden cosmological explanation. Claims about such things are beliefs, mistakes, metaphors, frauds, or incomplete accounts of material events.
5. Religion exists as background culture—burials, vows, calendars, festivals, charity, and moral authority—but is not a source of literal power.
6. Trade is physical. Goods occupy space, spoil, break, can be stolen, can be delivered, and change what settlements can do.
7. Human conflict, wilderness, and operational hazards are equally important sources of danger. None is merely flavour for another.
8. The player inhabits one crew member at a time. Crew members may be switched voluntarily at Jomon’s tavern; death and departure are permanent for that person.
9. Jomon, surviving crew, cargo losses, debts, market conditions, route damage, relationships, and local history persist after an expedition fails.
10. Combat remains deterministic, turn-based, and grid-based. Its later redesign must be grounded in medieval weapons, injury, positioning, scarcity, and readable intent.
11. The simulation advances only through explicit in-game time while the game is running. Closing or leaving the game does not advance the world.
12. ASCII and detailed tiles are two renderings of the same world state. Neither may conceal consequential gameplay information.

## The river-to-coast frontier

The playable region is a watershed: tributaries, canals, floodplains, mill towns, ferry crossings, marshes, river cities, estuaries, and a contested coast. Inland and coastal worlds are linked by water, but passage is never automatic. River height, weather, seasonal work, war, tolls, shoals, quarantine, labour shortages, damaged locks, and local law determine which routes are practical.

The setting has no universal state. Manors, free towns, guild halls, monasteries, river leagues, fishing communities, fortified estates, travelling companies, and local assemblies overlap. A charter may be valid at one quay and meaningless across the next bend. A household survives by knowing who can witness a contract, who controls a lock, who needs a cargo, and when it is safer to leave.

No settlement is a generic medieval backdrop. Each needs a purpose, a water relationship, a labour structure, a material pressure, local authorities, a trade dependency, a route consequence, and visible civilian life.

## Jomon and its household

Jomon is a walkable deck plan, not an abstract hub screen. Its permanent spaces include a tavern, crew berths, chart table, cargo hold, galley, repair space, stores, and gangplank. Later play may expand or rearrange these spaces, but every major vessel function must occur at a represented location.

Operating a vessel prop opens a compact contextual key-choice prompt rather than a detached management screen. The chart table handles route work; the hold handles loading and unloading; the repair space handles vessel maintenance; and the tavern handles conversation, rest, crew selection, and succession. Gangplanks connect Jomon to quays, piers, shore landings, and small craft.

Jomon is an itinerant household, not the private property of one chosen captain. Its people have different skills, obligations, loyalties, and reasons to stay. Useful recognisable roles include a bargemaster, pilot, factor, carpenter, guard, cook, healer, scribe, carter, fisher, and bard. A role describes labour and social access, not a class or supernatural ability.

## The player and crew continuity

The player’s point of view rotates among crew members. While at Jomon’s tavern, the player may select any eligible living crew member as the active character. The current character carries their personal skills, injuries, equipment, relationships, and memories into an expedition.

When a crew member dies, they do not return and their personal development does not transfer automatically. Another survivor may take over at the tavern. The household retains the consequences: a lost worker changes capacity, a debt may pass to the vessel, cargo may remain at a wreck, and witnesses may remember the dead person or the vessel that employed them.

## Trade, contracts, and material consequence

Trade and profit are the main reasons to choose a voyage. Jomon carries tangible commodities such as grain, salt fish, wool, timber, charcoal, ironwork, lime, pottery, paper, dye, medicines, tools, and local specialty goods. Every commodity must have a source, a use, a transport condition, an identifiable buyer, and a failure mode.

Contracts can concern freight, letters, passengers, evidence, repairs, salvage, labour, safe conduct, debt, or witness work. A commission should show what is known, who is accountable, what physical burden it creates, and the immediate consequence of refusal or failure. Cargo is not abstract money: capacity, weight, bulk, fragility, spoilage, handling, theft, and recovery create route decisions.

Markets are local and persistent. Shortages, harvests, blockades, tolls, strikes, wrecks, repairs, and competing carriers change demand and prices. Profitable work can relieve a town, enrich a faction, worsen a dependency, or make Jomon unwelcome elsewhere.

## Dangers and conflict

All three danger families require equal authored and procedural support.

- **Human conflict:** toll collectors, rival carriers, guards, deserters, bandits, soldiers, debt agents, smugglers, guild disputes, and local feuds. People have material goals and can negotiate, flee, deceive, arrest, trade, or fight; civilians are not disposable enemy dressing.
- **Wilderness:** floods, ice, storms, fog, shoals, mud, hunger, disease, exposure, wild animals, insects, and difficult terrain. These create route, supply, and positional problems rather than magical curses.
- **Operational hazards:** snapped lines, collapsing wharves, bad cargo, mill machinery, fire, damaged locks, fouled wells, unstable scaffolds, and neglected vessels. Their causes and counterplay must be legible before severe harm resolves.

The intended atmosphere is tense material unease. Bodies are vulnerable, work is dangerous, and scarcity hurts, but ordinary humour, craftsmanship, music, rest, trade, and mutual aid remain visible.

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

Equipment must be material and multi-use where practical: lines, hooks, poles, shields, lamps, tools, medicine, provisions, weapons, documents, and trade goods can matter in combat, travel, cargo handling, repair, negotiation, or escape. The later guardian system uses grounded large threats such as war machines, siege works, wrecks, beasts, fortified positions, or dangerous industrial works—not magical bosses.

## Language boundaries

New player-facing content must not use spaceflight, planets, hyperlight, carriers, airlocks, aliens, advanced machinery, or the former industrial-science-fiction canon. Use waterways, roads, quays, ports, settlements, holds, taverns, charts, contracts, ledgers, crews, and local institutions instead. Use gangplank and quay for ordinary vessel access.

Do not use supernatural vocabulary as mechanical truth. “Curse,” “spirit,” “miracle,” and similar language may appear only as a person’s unsupported belief, proverb, accusation, or cultural expression, never as the game’s authoritative causal explanation.

## Implementation boundary

This reference does not claim that the current prototype already implements the setting. The first code milestone is a clean save boundary and a walkable Jomon deck. New work must follow [`TODO.md`](TODO.md), keep randomness seeded and inspectable, and validate every player-facing path through focused tests, deterministic automation, browser coverage, and builds.
