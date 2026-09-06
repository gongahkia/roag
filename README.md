# Jomon

Jomon is a keyboard-first, turn-based medieval roguelike about sending one courier from a persistent vessel-household into dangerous settlements and wilderness. The courier’s material choices, injuries, relationships, cargo, and failures reshape the household and local world.

The repository currently contains a substantial medieval foundation, not a complete playable expedition. The active goal and its hard scope boundary are in [`TODO.md`](TODO.md); permanent product constraints are in [`PRODUCT.md`](PRODUCT.md); setting and content canon are in [`LORE.md`](LORE.md).

Creep is bundled locally as the default interface font. Its browser-facing outline build is used because Chromium Canvas 2D loads the upstream bitmap-only face but paints it blank.

## Current bounded play

The existing foundation supports deterministic world creation, a persistent household, physical keyboard navigation on Jomon, local persistence, source-backed deck prompts, cargo, and the Hearthford Mill Lease handoff. At Hearthford’s public tally, a courier may accept an ironwork burden, deliver it to Jomon’s hold, then either fit it to relieve the lease or retain it for lease credit. This is a bounded market/worksite slice, not a trade, travel, combat, or expedition system.

## Documentation

- [Setting and content canon](LORE.md)
- [Permanent product and technical constraints](PRODUCT.md)
- [Operational tracker](TODO.md)
- [Historical medieval-foundation archive](docs/archive/medieval-foundation-2026-09/)

Live implementation contracts remain in [`docs/`](docs/), including the [persistence layout](docs/persistence-layout.md), [deck plan](docs/jomon-deck-plan.md), [cargo hold](docs/cargo-hold-contract.md), [settlement trading](docs/settlement-trading-contract.md), and [content boundary](docs/internal-content-boundary.md).

## Development

```console
$ npm ci
$ npm run dev
$ npm run build
$ npm run preview
```

Useful checks are `npm test`, `npm run test:e2e`, and `npm run build`. Passing infrastructure checks are not evidence that the expedition loop is fun or complete.
