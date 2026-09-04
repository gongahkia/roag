# Golf With Your Enemies

An isometric arcade mini-golf party game. The default mode, **Coursewright Rules**, is a build-then-play format for two to four golfers: everyone installs two constrained modules into a shared hole, then plays the physical result immediately.

Lowest adjusted stroke total wins a six-hole match. Each golfer receives one hidden architect contract; completing it during the hole removes one stroke from that hole’s score. The fixed fairway spine always remains playable, so a legal build cannot dead-end the course.

`Party Rules` and `custom` remain available as legacy/experimental presets with the earlier generated-course, card, and merchant systems.

## Run locally

```sh
npm install
npm run dev
```

Open the local URL printed by Vite. For an online private room, start the authoritative server in a second terminal:

```sh
npm run server
```

The server listens on `ws://localhost:8787`, stores room snapshots in `data/golf-with-your-enemies.sqlite`, and requires Node 22.5+ for the built-in SQLite driver.

## Coursewright flow

1. Start a two-to-four-player local game or private room. AI can fill empty seats.
2. A deterministic Speedway, Quarry, or Carnival shell opens with two build sockets per golfer.
3. Players place one module at a time in snake order. A builder has 28 seconds; an expired timer makes a deterministic legal placement.
4. The hole goes live. Putts and chips use the existing shared physics, collisions, and turn timer.
5. A completed secret contract is revealed and subtracts one stroke from its owner’s hole score.
6. The next hole immediately opens as a new construction shell. There is no merchant or random course reroll in Coursewright Rules.

Available build pieces are bank walls, springboards, high bridges, timed gates, splitters, and cushion runs. Socket markers are rendered in the game world; choose a module in the control rail, then click an open marker or its letter button.

## Online authority and privacy

The WebSocket server owns game time, bot turns, scores, construction commands, pause state, and course transitions. It validates module and socket identifiers and accepts construction commands only from the active human builder. Before a contract is revealed, snapshots sent to opponents omit its condition and description.

Private rooms require a host-selected passphrase. Only salted passphrase hashes and reconnect-token hashes are stored. Configure `HOST`, `PORT`, `GAME_DATABASE`, the comma-separated `APP_ORIGINS` allowlist, `VITE_GAME_SERVER_URL`, and optionally `MAX_COURSE_TILES` for deployment. Production clients should use `wss://`.

## Verification

```sh
npm run build
npm test
```

`npm run check` additionally runs the server type check. The focused Coursewright tests verify deterministic shells, legal placement completion, timeout fallback, contract resolution, and fresh-shell transitions. Legacy Party Rules tests remain explicit about that preset rather than relying on the default.

## Controls and accessibility

During construction, click a module then an open cyan socket. During play, left-drag to putt and right-drag to chip; release to strike. Space shoots, `-`/`=` adjust power, `C` changes putt/chip, and `P` uses a held legacy item where that preset permits it. Host-only pause applies to online rooms.

Settings persist locally and include reduced motion, high contrast, master/effects volume, controller vibration, stick deadzone, aim sensitivity, and keyboard remapping. Controller support follows the standard mapping: left stick aims, A shoots, B uses a held item, Y switches putt/chip, D-pad adjusts power, and Menu/Start pauses.

## Deployment

Build both artifacts with `npm run build`. Serve `dist/` as static frontend assets and run `npm run start:server` on one persistent process/VM. The repository’s deployment files include examples for Caddy, systemd, backups, and health checks. The game server exposes `/healthz`, `/readyz`, and `/metrics`.

## Naming note

The repository currently retains its working title. It must be renamed before a public commercial release: an itch.io game already uses “Golf With Your Enemies,” and Steam also uses the same phrase for a bundle. No replacement public title has been selected in this repository.
