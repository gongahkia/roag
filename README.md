# Jomon

Jomon is a single-player, offline-first, browser-based procedural low-mysticism medieval river-and-coast roguelike. Jomon is both the game and a working vessel: an itinerant household carrying people, goods, debts, and news between river settlements and a contested coast.

The project is at a documented clean-break boundary. The checked-in prototype still contains superseded space-era implementation and is **not** the current game canon. The rebuild now includes bounded local movement on Jomon's full known deck, a zero-time map legend through remappable command help, voluntary tavern-ledger courier switching, and renderer-independent permanent courier continuity. Gangplank/quay departure and every other contextual vessel interaction remain deferred.

The initial six-member household is immutable deterministic creation evidence: normalized world seed plus resolved configuration reproduce its roster, roles, equipment, histories, directional relationships, and zero-time eligible active-crew projection exactly. It has no individual rerolls or selected courier. The creation chooser consumes that projection in canonical order; Enter fixes immutable-in-practice `state.courier.initialCourierId` and initializes `state.courier.activeCourierId`, while Escape returns without selection. At the existing `prop:task-ledger` anchor in Jomon's tavern, the remappable contextual control opens a keyboard-first, zero-time switch list of distinct living and available eligible household members. Permanent death/departure is renderer-independent: it keeps initial provenance, permanently records death or departure, chooses the first canonical eligible living non-departed successor (including a temporarily unavailable survivor), or atomically finalizes `crew-extinction` with no replacement. It introduces no player loss control or physical loss display. FoundationWorld remains v14, mutable state is v14, navigation remains v1, causal replay projection is v6, the manifest remains v6, IndexedDB layout remains v4, and valid v14/v13 or v14/v12 envelopes upgrade only on read without a storage rewrite.

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
