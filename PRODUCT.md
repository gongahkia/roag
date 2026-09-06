# Jomon Product Constraints

Jomon is a turn-based roguelike about sending one member of a persistent vessel-household into dangerous medieval settlements and wilderness. A courier trades, investigates, negotiates, and fights under pressure, then returns—or fails to return—with consequences that reshape Jomon, its people, and the local world.

The first product milestone is one complete expedition, specified in [`TODO.md`](TODO.md). It must feel playable before scope expands: a prepared courier leaves Jomon, faces a material problem and escalating pressure, resolves a tactical or non-combat threat using situated tools, and returns or suffers a contextual outcome that persists.

The permanent constraints are:

- seeded, inspectable determinism and action-driven time;
- one directly controlled courier at a time, with a persistent household and physical Jomon;
- material cargo/resources and bounded local markets;
- injury, permanent death, succession, and contextual defeat;
- compact causal feedback and local offline persistence;
- keyboard-first ASCII play with accessible non-colour cues;
- original low-mysticism medieval content, with no copied lore, assets, or mechanics; and
- no sexual violence, slavery, torture, or harm/endangerment of children in authored or generated content.

Technical boundaries are equally permanent: new gameplay must remain local, deterministic, testable, and proportionate to its exercised state. Do not add a server, account, telemetry, multiplayer authority, wall-clock simulation, hidden mutation, or public modding promise. Retain live contracts while code depends on them; use ordinary bounded state where it is sufficient.

`LORE.md` owns setting, tone, and content canon. `TODO.md` owns current scope and frozen work. Historical foundation narratives and detailed verification records live under `docs/archive/medieval-foundation-2026-09/`.
