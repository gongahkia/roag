# Jomon

Jomon is a single-player, offline-first, browser-based procedural low-mysticism medieval river-and-coast roguelike. Jomon is both the game and a working vessel: an itinerant household carrying people, goods, debts, and news between river settlements and a contested coast.

The project is at a documented clean-break boundary. The checked-in prototype still contains superseded space-era implementation and is **not** the current game canon. The rebuild now includes bounded local movement on Jomon's full known deck, a zero-time map legend through remappable command help, voluntary tavern-ledger courier switching, renderer-independent permanent courier continuity, and a compact physical ledger availability/loss readout. A pure vessel-proximity contract now defines all three existing deck-plan prop rules: the tavern task ledger is the sole operation, while the chart table and gangplank have only bounded physical reserved prompts for their deferred route-comparison and quay-travel domains.

The initial six-member household is immutable deterministic creation evidence: normalized world seed plus resolved configuration reproduce its roster, roles, equipment, histories, directional relationships, and zero-time eligible active-crew projection exactly. It has no individual rerolls or selected courier. The creation chooser consumes that projection in canonical order; Enter fixes immutable-in-practice `state.courier.initialCourierId` and initializes `state.courier.activeCourierId`, while Escape returns without selection. At each exact prop anchor, the remappable contextual control opens its source-backed zero-time prompt: at `prop:task-ledger`, the switch list remains limited to distinct living available members and its compact readout lists all six canonical household members as active, available, committed, temporarily unavailable, permanently departed, or dead; at the chart table and gangplank, one disabled Enter option truthfully reports only the deferred route-comparison or quay-travel domain. Escape cancels every prompt without mutation. No prompt reveals a loss cause, location, possession, relationship, memory, hidden person, or individual loss control. A retained validated continuity command may state only that the active perspective continued after a recorded permanent loss; crew extinction remains a read-only no-active-courier chronicle. FoundationWorld remains v14, mutable state is v14 / courier v3, navigation remains v1, causal replay projection is v6, the manifest remains v6, IndexedDB layout remains v4, and this presentation-only v10 / detailed-adapter v4 change adds no schema, store, migration, generation, or RNG change.

- [Lore and content reference](LORE.md)
- [Authoritative roadmap](TODO.md)
- [Desktop-browser performance and storage baseline](docs/performance-storage-baseline.md)
- [Medieval local persistence layout](docs/persistence-layout.md)
- [Closed internal-content boundary for maintainers (no public mod/content-pack API)](docs/internal-content-boundary.md)
- [Renderer-independent effect model (foundation only)](docs/effects-model.md)
- [Mystical effect policy and audit boundary](docs/mystical-effect-policy.md)
- [Courier loss policy assessment boundary](docs/courier-loss-policy.md)
- [Courier continuity implementation contract](docs/courier-continuity-contract.md)
- [Jomon integrity policy assessment boundary](docs/jomon-integrity-policy.md)
- [Semantic palette and accessibility contract](docs/semantic-palette.md)
- [Static Jomon deck-plan, map legend, and primary ASCII projection](docs/jomon-deck-plan.md)
- [Initial immutable household and active-crew contract](docs/initial-household-contract.md)
- [Tavern courier-switch contract](docs/tavern-courier-switch-contract.md)
- [Vessel proximity and operation contract](docs/vessel-proximity-operation-contract.md)

## Development

```console
$ npm ci
$ npm run dev
$ npm run build
$ npm run preview
```

## Current verification commands

```console
$ npm test
$ npm run test:autoplay:tasks
$ npm run test:e2e
$ npm run build
```

These commands currently validate the superseded prototype while the medieval implementation is built. They must not be cited as evidence that the new setting or its planned mechanics are already implemented.
