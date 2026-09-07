# Jomon Product Constraints

Jomon is a turn-based roguelike about sending one member of a persistent vessel-household into dangerous medieval settlements and wilderness. A courier trades, investigates, negotiates, and fights under pressure, then returns—or fails to return—with consequences that reshape Jomon, its people, and the local world.

The current product is a fullscreen Python 3.11 terminal game using the standard-library `curses` module. The active expansion keeps Hearthford and adds three substantial, persistent regions: a tidal coast, an open deep forest, and limestone uplands. Travel begins from physical Jomon, whose tavern contains visible household members and bounded recruitable visitors. Couriers organise physically shaped items in a rotatable pack grid, wear armour across six readable body locations, and face terrain, weather, ranged fire, and goal-directed enemies whose plans depend on what they can see and hear.

The permanent constraints are:

- seeded, inspectable determinism and action-driven time;
- one directly controlled courier at a time, with a persistent household and physical Jomon;
- material cargo/resources and bounded local markets;
- injury, permanent death, succession, and contextual defeat;
- compact causal feedback and local offline persistence;
- keyboard-first ASCII play with accessible non-colour cues;
- original low-mysticism medieval content, with no copied lore, assets, or mechanics; and
- no sexual violence, slavery, torture, or harm/endangerment of children in authored or generated content.

Technical boundaries are equally permanent: new gameplay must remain local, deterministic, testable without initializing `curses`, and proportionate to its exercised state. The game targets Linux, macOS terminals, and Windows through WSL; uses Python's standard library only; and runs with `python -m jomon`. Do not add a web runtime, server, account, telemetry, database, multiplayer authority, wall-clock simulation, plugin framework, or public modding promise.

Prefer one primary game state, small dataclasses with actual gameplay state, direct action functions, one turn-resolution path, one atomic JSON save, authored content tables, small causal generators, and bounded significant history. Do not rebuild the browser-era event, replay, fidelity, provenance, dialogue, economy, or frontier frameworks in Python.

Depth should continue to come from coherent geography, sight and sound,
readable enemy intent, elevation, finite supplies, spatial packing,
armour/terrain/equipment/passive/relic interactions, tempting visible treasure,
and persistent material consequences. Each region has a dedicated bounded
generator and authored encounter compositions; these are product features,
not a universal biome, planning, physics, fluid, economy, or content framework.

This milestone explicitly excludes infinite or offline world simulation,
full anatomical or layered-garment simulation, generic GOAP/encounter/quest
languages, real-time progression, graphical tiles, generic spellcasting, and
unbounded regional or onboard content. Voyage events reuse ordinary people,
cargo, inventory, and combat rules in three sporadic authored families.

`LORE.md` owns setting, tone, and content canon. `TODO.md` owns current scope and frozen work. The causal chain is documented in [`docs/causal-generation-and-loop.md`](docs/causal-generation-and-loop.md); browser history is recorded under [`docs/archive/`](docs/archive/).
