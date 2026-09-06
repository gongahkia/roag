# Jomon Product Constraints

Jomon is a turn-based roguelike about sending one member of a persistent vessel-household into dangerous medieval settlements and wilderness. A courier trades, investigates, negotiates, and fights under pressure, then returns—or fails to return—with consequences that reshape Jomon, its people, and the local world.

The current product is a fullscreen Python 3.11 terminal game using the standard-library `curses` module. Its playable region sends a prepared courier across one seamless, camera-followed Hearthford landscape containing settlement, floodplain, wilderness, roads, stacked millworks, roofs, and an underground culvert. Exploration memory, vertical geometry, treasure, patrols, weather, finite supplies, visible pressure, and materially different builds shape the route before physical return or contextual defeat. A bounded visiting merchant can turn prior outcomes into later build choices.

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
readable enemy intent, elevation, finite supplies, equipment/passive/relic
interactions, tempting treasure, and persistent material consequences.
Hearthford's dedicated regional generator is a bounded product feature, not
the beginning of a universal biome, physics, fluid, or content framework.

`LORE.md` owns setting, tone, and content canon. `TODO.md` owns current scope and frozen work. The causal chain is documented in [`docs/causal-generation-and-loop.md`](docs/causal-generation-and-loop.md); browser history is recorded under [`docs/archive/`](docs/archive/).
