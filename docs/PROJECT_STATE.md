# Vagabonds project state

**Last reconciled: 2026-10-22.** Master reference for the independent fantasy RPG. Update alongside behavioral changes; [DEVELOPMENT.md](DEVELOPMENT.md) gives repository guidance and [VERIFICATION.md](VERIFICATION.md) records executed checks.

Vagabonds is the fantasy party RPG extracted from Ithica at `c87d480`, now extended with a finite procedural world and selected systems from the replayed Ithica history. The approved integration direction is recorded in [INTEGRATION.md](INTEGRATION.md). This repository owns its application, Core, graphics, tests, assets, catalog and mappings; it loads no gameplay or assets from a sibling checkout. [ORIGIN.md](ORIGIN.md) records provenance. Archived Ithica guides under `history/ithica/` describe inherited source, not current gameplay.

## Implemented world

| Area | Current implementation |
| --- | --- |
| Runtime | C# / .NET 10, MonoGame DesktopGL 3.8.6; graphics-free deterministic Core |
| Launch | Resume selected world/legacy slot if present; absent slot or explicit `--new` starts a world |
| Geography | Finite 4096×4096 surface, three underground floors; 64×64 tile chunks |
| Content | 24 towns, six cultures/factions, six seeded biome provinces, 48 adventure sites |
| Generation | Seeded settlement/site placement, road loops, gradient-noise terrain, WFC ruins/towns, BSP workings/forts, caverns and ring sanctuaries |
| WFC | Authored semantic 3×3 samples, 99–111 learned patterns, culture palettes, structural/entrance pins, eight attempts then constructive fallback |
| Streaming | Seamless global coordinates; 3×3 prefetch, 25 resident chunks, eight resident footprints |
| Party | Start alone as a generated traveller; recruit generated inhabitants, active party 1–4, stable actor IDs; each member occupies a separate walkable tile and follows with individual smooth movement/facing |
| Exploration | Real time with pause; smooth tile travel/camera, horizontal sprite facing, cursor selector, hover/active approach routes; mouse-only controls, close RPG camera, clear outdoor view, dungeon fog |
| Combat | Content-authored classes/abilities; initiative, authored conditional enemy phases/targets, saved intentions, status effects, mana, stress, revival and levels; one game minute per completed round |
| Content | 148 typed definitions, including 12 discovery templates; stable IDs, reference/sprite/handler validation; runtime characters, classes, abilities and configured conversations |
| Developer tools | `./dev` validated authoring/references/scaffolding/removal/schema, runtime previews for every content family, world audits, native input acceptance, tooling review and tests |
| Adventures | 48 boss archetypes with six variants; freely accessible guardians, seeded final threat and run completion; dungeon caches and three equipment slots |
| Building/work | Timed construction, digging, crafting, forage and repair; reserved supplies, exact cancellation/refunds, assigned party/NPC workers, wear and protected stairs |
| Simulation | 5 Hz, one real second per game minute; shared hunger/fatigue/injury, rest, bounded memories/trust, conditional incidents; workers, stocks, caravans, reroutes, shortages and raids |
| Stories | Generated trade/blight/succession arcs; real entity references, alternative compact outcomes, repair/delivery requests and history |
| Failure | Downed companions revive; abandonment/wipe permanently kills and immediately saves; new lone traveller can continue same world |
| Persistence | `vagabonds-world` v3 manifest with content hash, generation v4/sample hash pinning, immutable checksummed chunk snapshots, exact combat/RNG continuation |
| Inspection | Menu → World inspector: samples/constraints/counts/raw/final/repair views; reports, headless validation and native smoke |

New and loaded worlds begin paused. Play resumes time; clicking reachable ground starts travel immediately. Actions offers explicit one-minute/hour waiting; danger interrupts it. New ordinary runs choose a fresh seed unless --seed is supplied; previews/diagnostics retain deterministic seed 42. Menus, conversations, combat and focus loss freeze the clock; there is no offline progression. Fine movement operates within 100 tiles, while production and trade run regionally farther away. Recruitment creates a job vacancy. Complete road blockades change regional routing; shortages generate delivery requests and affect prices, and repairing roads allows supply to recover. Detailed and regional simulation share the same actor identities and cargo.

Town exteriors are grassy, with WFC structural regions around roads/plazas. Authored generation profiles select algorithms per purpose/floor and biome terrain thresholds. Settlement/site positions vary across the world; a minimum-distance road tree plus a geographic ring supplies alternate routes. Seeded gradient fields form coherent vegetation, rock and water. Footprints are generated before chunk slicing; visitation cannot change layouts. Edits/discovery survive eviction and saves. The inspector distinguishes layout families, raw generation and repairs. `./dev audit-world --seed 40 --samples 8` produces a JSON topology report and SVG comparison. The executed 136-footprint audit found six families, 136 distinct raw layouts and connected stairs throughout; this establishes structural variety, not encounter or reward quality. Stairs remain protected and tunnelling is horizontal.

A main arc accepts evidence after a patron dies, and can resolve through the original town or a different compact. Severe sustained shortages can explicitly fail it. The side arc never gates the final encounter. The world continues after its resolution or failure; defeating the selected final boss completes the run. A wiped party leaves a recoverable storage cache; deaths must be saved before either continuation choice is accepted. Generated successor heroes receive new stable IDs while earlier deaths and the changed world remain.

## Controls and tools

Characters and abilities use typed JSON under `content/`. Conversations display clickable configured interactions, including recruitment/trade when offered. Healers offer paid treatment and inhabitants can share stories/meals. See [CONTENT_AUTHORING.md](CONTENT_AUTHORING.md) for stable IDs, registered actions/effects, reference-safe editing and reproducible previews. Equipment, bosses, recipes, shared simulation and incident definitions drive gameplay. Journal/Tactics describes the final threat and current boss tactics. Party offers clickable companions, equipment and eligible revival/parting/abandonment actions.

The cursor brackets its current map cell. Hovering previews an observed route in teal; clicking keeps the selected route in gold and starts travel even while paused. Clicking people, caches, storage or stairs finds an observed approach, then interacts on arrival. Moving actors use their visible interpolated sprite bounds for clicks. Panels and disabled controls consume clicks before map input. Menus and combat hide exploration overlays; build/dig retain the selector. Travellers and inhabitants slide between committed cells, face left/right with horizontal movement and retain facing on vertical movement. Stairs, loads and long transfers reset interpolation; collision, costs and the simulation clock remain grid-based.

The **4096×4096 world is retained**. Exploration uses a close default camera of 36–64 pixels per tile, with a 24-pixel minimum and at most 48 columns / 32 rows even on larger displays. +/- buttons or the mouse wheel zoom; View restores the default. The separate World map shows known places. Outdoor terrain and inhabitants are visible throughout the local camera view, including behind walls. Only the observed area joins discovery; distant places stay unknown. Underground retains radius/line-of-sight fog and remembered dim terrain. Routes require observed, walkable cells. Partial edge tiles/sprites are clipped to the map.

World gameplay is mouse-only; keyboard movement and hotkeys are disabled. Play/Pause, Save, Load and Menu remain visible. Bottom buttons offer Interact, Party, Journal, World map, Actions, Build, Dig, Work, Craft and Stop. Actions includes rest/wait/gather/repair/storage/evidence/delivery. Construction, worker assignment/cancellation, recipes, storage transfers and trade have clickable choices. Right-click closes panels and clears tools/travel. In combat, click an ability then its target; self/group abilities execute immediately. Menu contains the guide, inspector, fullscreen and Save and quit. Endings offer clickable continuation/new-world choices. [RPG.md](RPG.md) has the complete guide.

```sh
./vagabonds.sh --new --seed 1337
./vagabonds.sh --scene world
./vagabonds.sh --scene world-site
./vagabonds.sh --scene world-boss
./vagabonds.sh --scene world-battle
./vagabonds.sh --scene world-inspector
./vagabonds.sh --world-headless
./vagabonds.sh --world-report
./vagabonds.sh --validate-worlds 1000 --seed -500
./vagabonds.sh --world-smoke --frames 360 --screenshot artifacts/world.png
./vagabonds.sh --scene atlas
```

Default window 1440×900, minimum 1024×720. Explicit scenes, `--ui-report PATH` diagnostics and frame-limited captures ignore saves and disable automatic saving; explicit `--load` reads the selected slot. The Save button saves explicitly; successful Load resumes autosaving. World Load accepts only compatible world saves; restart to open a legacy slot. Invalid loads preserve the current session, route, panel and autosave mode. Headless/report/seed checks do not read or write player saves.

World party formation is authoritative: every non-dead member, including a downed companion, occupies a different walkable tile. Followers take at most one cardinal step per leader step, using observed terrain; corners and reversing through a one-tile corridor preserve distinct destinations. Recruitment retains a companion's nearby position where possible. Stairs and retreat choose connected landing cells for everyone before moving anyone, and blocked transfers leave the party untouched. Enemies can contact any member. Removing the leader promotes the next member at their current tile. Each sprite has its own movement interpolation and facing.

Compatible world saves from before this formation change may contain the old whole-party stack at the leader's cell. Loading spreads that exact representation across connected walkable cells deterministically, without consuming time/randomness or rewriting the slot. Already separated formations retain exact saved continuation. Malformed partial overlaps and enclosed landings without enough room fail safely. This formation fix changes neither the current content hash nor world/generation versions.

## Legacy compatibility

Existing `lantern-rpg` v1/v2 slots remain playable through the original client with original maps, IDs, rules and exact continuation. Four fixed companions explore Hearth 36×26, Ashwood 48×32 and Sunken Abbey 51×37; iron key → seal → Abbot → relic → return. Defeat retains the old recovery rules. No world migration rewrites these saves. Explicit town/forest/crypt/battle/victory/atlas scenes and the old headless/report/seed/native checks retain this mode. [LEGACY_RPG.md](LEGACY_RPG.md) documents its controls, including Q quit and F2 atlas.

The generic Core `Campaign.Load` retains inherited field compatibility for source callers. Legacy startup, F9 and import use `Campaign.LoadRpg`, which rejects lunar field sessions. World startup dispatches by format and rejects them as well. Failed load attempts do not install a replacement session.

## Saves and boundaries

Default selected slot is `~/.vagabonds/campaign.json`; `--save PATH` selects another. An existing unreadable/invalid slot fails without silently replacing it. `--new` explicitly starts over; normal saving/quitting then replaces that selected slot. `--load` requires a save and cannot combine with `--new`. No real player slot is used by validation tooling.

World saves use format version three and pin the authored content hash alongside generation/sample versions. Earlier world saves are rejected safely; this development expansion does not migrate them. World saves pair the JSON manifest with `<slot>.chunks/`. Chunk snapshots are immutable SHA-256 files, flushed before atomically replacing the manifest. Save/load retains actors, party, shared body state/memories/trust, clock, reserved work/progress/assignments, structures/condition, inventories/equipment/cargo, incidents, arc/history, edits/discovery/storage, battle plans/order and random state. Generation v4 and the exact authored sample hash are required to prevent silent regeneration changes. Unreferenced snapshots are retained so backups of earlier manifests remain recoverable; automatic snapshot garbage collection is deferred. Back up both manifest and snapshot directory.

`--import-legacy PATH [--save DESTINATION]` validates/copies an old RPG slot atomically, exits and leaves the source untouched; it rejects self-overwrite, colony saves and field sessions. The inherited `lantern-rpg` format name, object IDs and asset filenames retain their identities intentionally.

## Projects, assets and scope

The solution contains Core (world/legacy rules, content, generation, persistence and simulation), Graphics (sprites/text/camera), Client (world/legacy presentation/input/atlas), Tools (graphics-free authoring/preview commands) and Core.Tests (graphics-free rules and client startup/reload tests). All references and content resolve within this checkout. No colony game is shipped.

All 7,115 occupied crops / 11,887 catalog records, 664 original PNGs / three archives and 22 derived reference images remain local. Original IDs, artwork, provenance, uncertain identities and measured colors are preserved. The canonical schema-5 catalog is tracked as `assets/catalog.json.gz`; the readable export is ignored. Historical `ithica-expedition-bindings.json`, `ithica-generation.json` and runtime manifest names are intentional. New structural samples are original authoring inputs in `assets/world/samples.json`. Read [ASSETS.md](ASSETS.md) before changing sprite mappings.

This is a playable integration, with limits to its simulation fidelity. Party members and background actors share body rules: food reduces hunger, exhaustion slows combat/work, large hits and incapacitation cause wounds, and rest/treatment recover them. Classes/levels/equipment retain their RPG effects; traits modify work/fatigue. Hunger reduces health to one rather than silently killing an unattended actor. Background gathering uses coarse regional work; it does not deplete individual plants or simulate a physical supply chain underground. Residents keep one identity and their job/history/equipment on recruitment or parting ways at home. Assigned workers leave their ordinary production while occupied.

Construction/digging apply terrain only after actual work. Costs are reserved, cancelled/invalidated jobs return them once, and dismantling returns recorded construction costs. NPC workers approach through local paths; party workers wait for the player to move nearby. Workers refuse unsafe fatigue, hunger and injury. Camps/structures wear; repairs and crafting are optional support for expeditions. Incidents depend on actual hunger, exhaustion, wounds, work and trust, with saved independent randomness and cooldowns. These are bounded social rules rather than a full dialogue/relationship simulation. Regional food warnings include low reserves as well as immediate unmet demand.

Distant people still use regional production rather than individual hauling; autonomous food gathering is coarse, local routines are simple, and workers do not navigate stairs automatically. Arc families retain their evidence/return structure as optional side stories. Tactical walking/range/cover, directional animation, gamepad support and a sample painting editor remain outside this implementation. Chunk generation is synchronous and may cause hitches. Structural audits establish connectivity/diversity, and encounter previews establish executable behaviours; long-form balance and enjoyment still require playtesting. [WORLD.md](WORLD.md) describes the architecture and research sources.


The five integration checkpoints are implemented and locally verified; INTEGRATION.md records the scope. Integration acceptance included 210 .NET tests, 38 catalog tests, published-world/legacy headless checks and 15 native input checks. Subsequent mouse/camera acceptance is recorded in VERIFICATION.md. The current world uses manifest v3 and generation v4; the party formation fix preserves those versions and the content hash, so compatible current saves remain usable. The 136-footprint structural report and graphical captures are development artifacts under ignored `artifacts/integration/`. The local Ithica reference stays there for future development and is neither published nor needed by the game.
