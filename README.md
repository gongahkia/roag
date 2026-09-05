# Jomon

Jomon is a single-player, offline-first, browser-based procedural low-mysticism medieval river-and-coast roguelike. Jomon is both the game and a working vessel: an itinerant household carrying people, goods, debts, and news between river settlements and a contested coast.

Pixelify Sans is bundled locally as the default interface font, with BigBlueTerm retained as a bundled fallback; no runtime font host is required.

The project is at a documented clean-break boundary. The checked-in prototype still contains superseded space-era implementation and is **not** the current game canon. The rebuild now includes bounded local movement on Jomon's full known deck, a zero-time map legend through remappable command help, voluntary tavern-ledger courier switching, renderer-independent permanent courier continuity, source-backed station readouts, bounded persisted station-action feedback, and bounded physical cargo-hold lots. That proof resumes a validated source on the local quay approach, crosses the physical gangplank, switches at the tavern, records the gangplank readout, and exits back to the quay through real keyboard input. Immutable Jomon provenance defines eight canonical deck props: chart table, cargo-hold rack, repair-space rack, stores rack, berth, galley hearth, task ledger, and gangplank. Each prop retains only its latest typed action outcome; the cargo-hold separately retains at most 24 current lots, never an unbounded operation history.

The initial six-member household is immutable deterministic creation evidence: normalized world seed plus resolved configuration reproduce its roster, roles, equipment, histories, directional relationships, and zero-time eligible active-crew projection exactly. It has no individual rerolls or selected courier. The creation chooser consumes that projection in canonical order; Enter fixes immutable-in-practice `state.courier.initialCourierId` and initializes `state.courier.activeCourierId`, while Escape returns without selection. At each exact prop anchor, the remappable contextual control opens its source-backed zero-time surface: cargo hold reports bounded current lots and capacity; repair space reports current/max integrity only; stores says provisions/inventory are unmodeled; berths report slot capacity only; the galley says meals, rations, and cooking are unmodeled; chart table and gangplank keep route comparison and quay travel unavailable; and `prop:task-ledger` preserves its distinct living-available switch list and household availability/loss ledger. The cargo-hold reducer permits only exact-anchor typed load, unload, catalogue-compatible damage/spoilage/loss, and recovery transitions; it does not create a market, source, price, trade, route, or travel fact. Enter at a non-ledger station records that bounded readout as a typed zero-time action only after the authoritative save succeeds; the existing tavern-switch command likewise records the ledger's latest outcome. Escape cancels every prompt without mutation. Normal-world status and messages project only those persisted, canonical latest outcomes. No prompt or feedback reveals causal cargo IDs, source place, buyer, price, route/site facts, loss cause, location, possession, relationship, memory, hidden person, or individual loss control. A retained validated continuity command may state only that the active perspective continued after a recorded permanent loss; crew extinction remains a read-only no-active-courier chronicle. FoundationWorld is v15 with strict read-only validation and deterministic v14-to-v15 static-prop conversion; mutable state is v16 / courier v3, navigation v1, causal replay projection v8, manifest v6, and IndexedDB layout v4. No new store, layout, generation, or RNG stream exists.

The closed commodity catalogue v1 defines eight physical goods—charcoal, grain, ironwork, lime, paper, salt fish, timber, and wool—with a source, use, weight/bulk class, condition, handling requirement, failure mode, buyer category, and safety classification for every entry. It remains compiled, deterministic content: the separate cargo-hold owner may reference only its closed IDs and weight/bulk/failure semantics, but the catalogue does not instantiate a buyer, market, location, price, trade, route, or broader persisted economy fact.

The closed named-settlement profile v1 defines Hearthford Mill Quay as a future material trade reference: its shared mill reach, millers/quay carriers/weir tenders, mill lease and quay-ward authority, future civilian services, closed `ironwork`/`salt-fish` demand references, mill-race work pressure, and visible milling-and-river-supply purpose are all source-backed and safety-audited. It is not yet a generated world site, physical location, market, contract, route, or player-visible settlement surface.

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
- [Vessel prop action-state and feedback contract](docs/vessel-prop-action-contract.md)
- [Cargo hold contract](docs/cargo-hold-contract.md)
- [Closed commodity catalogue contract](docs/commodity-catalogue-contract.md)
- [Named settlement profile contract](docs/settlement-profile-contract.md)

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
