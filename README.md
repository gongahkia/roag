# Jomon

Jomon is a single-player, offline-first, browser-based procedural low-mysticism medieval river-and-coast roguelike. Jomon is both the game and a working vessel: an itinerant household carrying people, goods, debts, and news between river settlements and a contested coast.

The project is at a documented clean-break boundary. The checked-in prototype still contains superseded space-era implementation and is **not** the current game canon. The rebuild now includes bounded local movement on Jomon's full known deck, a zero-time map legend through remappable command help, voluntary tavern-ledger courier switching, renderer-independent permanent courier continuity, and source-backed minimal station readouts. Immutable Jomon provenance now defines eight canonical deck props: chart table, cargo-hold rack, repair-space rack, stores rack, berth, galley hearth, task ledger, and gangplank. The tavern task ledger is the sole mutable operation; every other station is a bounded zero-time readout with its future domain explicitly deferred.

The initial six-member household is immutable deterministic creation evidence: normalized world seed plus resolved configuration reproduce its roster, roles, equipment, histories, directional relationships, and zero-time eligible active-crew projection exactly. It has no individual rerolls or selected courier. The creation chooser consumes that projection in canonical order; Enter fixes immutable-in-practice `state.courier.initialCourierId` and initializes `state.courier.activeCourierId`, while Escape returns without selection. At each exact prop anchor, the remappable contextual control opens its source-backed zero-time surface: cargo hold reports capacity only; repair space reports current/max integrity only; stores says provisions/inventory are unmodeled; berths report slot capacity only; the galley says meals, rations, and cooking are unmodeled; chart table and gangplank keep route comparison and quay travel unavailable; and `prop:task-ledger` preserves its distinct living-available switch list and household availability/loss ledger. Enter on an inspection-only station reports the same bounded result without mutation; Escape cancels every prompt without mutation. No prompt reveals cargo contents, route/site facts, loss cause, location, possession, relationship, memory, hidden person, or individual loss control. A retained validated continuity command may state only that the active perspective continued after a recorded permanent loss; crew extinction remains a read-only no-active-courier chronicle. FoundationWorld is v15 with strict read-only validation and deterministic v14-to-v15 static-prop conversion; mutable state remains v14 / courier v3, navigation v1, causal replay projection v6, manifest v6, and IndexedDB layout v4. No new mutable prop state, causal command, store, migration, generation, or RNG stream exists.

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
- [Vessel station readout contract](docs/vessel-station-readout-contract.md)

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
