# Jomon

Jomon is a single-player, offline-first, browser-based procedural low-mysticism medieval river-and-coast roguelike. Jomon is both the game and a working vessel: an itinerant household carrying people, goods, debts, and news between river settlements and a contested coast.

The project is at a documented clean-break boundary. The checked-in prototype still contains superseded space-era implementation and is **not** the current game canon. The rebuild begins with foundational world systems; the walkable Jomon deck, tavern crew switching, gangplank/quay access, and contextual vessel interactions follow those foundations.

- [Lore and content reference](LORE.md)
- [Authoritative roadmap](TODO.md)

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
