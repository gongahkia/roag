# Golf With Your Enemies

An original isometric mini-golf party game with a shared pre-hole course slot machine, local hot-seat play, and up to four AI enemies.

## Run locally

```sh
npm install
npm run dev
```

For an online room, run the authoritative game server in a second terminal, then use the `online room` card on the clubhouse page:

```sh
npm run server
```

The server listens on `ws://localhost:8787`, stores rooms in `data/golf-with-your-enemies.sqlite`, and requires Node 22.5+ for its built-in SQLite driver. Configure `HOST`, `PORT`, `GAME_DATABASE`, the comma-separated `APP_ORIGINS` allowlist, `VITE_GAME_SERVER_URL`, and optionally `MAX_COURSE_TILES` when deploying it; production clients must use `wss://`. Online rooms default to a conservative 4,096-tile budget so an untrusted lobby request cannot exhaust the server.

## Free single-server deployment

This build is intentionally a one-process service: SQLite, room timers, and active WebSocket sessions belong to one persistent VM. Build both deployable artifacts with `npm run build`, serve `dist/` from Cloudflare Pages, and run `npm run start:server` under systemd on one persistent VM. Set `VITE_GAME_SERVER_URL=wss://ws.example.com` while building the frontend; set `APP_ORIGINS=https://app.example.com` and bind the server to loopback behind Caddy. Copy `.env.example` to the VM's environment file and use the examples in `deploy/` for Caddy, systemd, and daily SQLite backups. The service exposes `/healthz`, `/readyz`, and `/metrics` for the VM monitor.

Run the verification suite with `npm run check`.

Capture a randomly seeded comparison set with `npm run capture:levels -- --count 25`. The command writes PNGs and a seed manifest under `output/levels/`; set `BROWSER_BIN` when Chromium is not available as `chromium-browser`.

## Party Rules vertical slice

Ordinary local play now starts in the `Party Rules` preset: a deliberately constrained nine-hole hot-seat campaign built to test the core social loop—**the slot creates the problem, the player creates the outcome, and the group creates the story**. Existing content remains available through the programmatic `custom` ruleset for experiments and legacy saves.

Party Rules uses three representative biomes—Speedway, Quarry, and Carnival—and three authored course grammars—Ribbon, Fork, and Courtyard. Every generated hole marks a safe line, a skill line, and a conflict line. The slot machine gives each golfer one influence choice before they ready the lever, permits at most one chaos reel, and reveals a concise player-facing course headline rather than raw terrain parameters.

The default Trick Card pool is exactly: Turbo, Shield, Heavy, Airhorn, Freeze, Popper Pad, Rescue Drone, and Glider. Players hold at most two cards and use one per shot. Effects expire on that shot or at the end of the hole; Freeze selects a moving gate or sweeper for the owner’s next shot. Between holes, the clubhouse offers exactly three Trick Cards, supports replacing a held card, and lets players pass immediately.

Each resolved hole records a versioned materialized recipe containing the generator version, seed, stopped reel IDs, chaos modifier, content identifiers, route-role assignments, and a stable course hash. New recipes use the current generator, while recorded `party-slice-v1` recipes continue to use their historical geometry. Completed holes remain visibly stitched into the final course, while Party Rules keeps gameplay systems active only on the current hole. The in-game intel drawer exposes the compact recipe hash and local development instrumentation records recipes, slot actions, shot types, card use, collisions, recoveries, and per-hole strokes.

## Party Rules developer validation

The first-time guide is contextual, skippable, and stored only on the local device. It introduces the shared objective, one slot decision, course-route roles, putt/chip input, visible Trick Card targets and expiries, social receipts, the three-card shop, and connected-course memory. Settings includes a local reset for the guide.

Every reveal now opens a short, skippable briefing with the biome, layout, one headline modifier, and safe/skill/conflict route roles. Local games with two or more human golfers show a pass-the-device handoff before a new human turn. Ball collisions, cards, and recoveries create structured receipts and the finale gives at most three factual, non-punitive awards. The result screen can copy the materialized replay recipe.

The project owner is the sole subjective gameplay validator. Automated tests, deterministic simulations, visual audits, telemetry, generator stress, and authoritative client/server parity validate implementation correctness but do not independently prove fun or audience reception. External playtesting may be performed voluntarily later, but it is not a development gate.

Use the reproducible verification helpers for developer validation:

```sh
npm run simulate:party -- --seed party-validation --players 4 --holes 9 --out output/simulations/party-validation.json
npm run parity:party
npm run stress:party -- --count 100 --out output/stress/party-100.json
```

`stress:party` accepts up to 10,000 seeded courses; use a smaller count for a quick local smoke test. The Party Rules description above is authoritative for ordinary play. Broad catalog, two-chaos-reel, and seven-card-shop references below describe the retained `custom`/legacy experimentation path, not the Party Rules default.

## Game flow and modes

The clubhouse is the main page. A couch campaign supports one or more people passing a single device, with AI able to fill unused seats. Local multiplayer requires at least two human golfers on that same shared screen; play remains turn-based, so it uses one active-ball camera rather than split-screen. The clubhouse can also create and join an online lobby. A lobby displays the shareable six-character room code, player presence, selected seed/rules, AI settings, and the host-only start action. New online rooms require a host-chosen passphrase; only a salted passphrase hash and reconnect-token hashes are persisted. Once a room starts, the WebSocket server is authoritative for player commands, slot timers and wagers, bots, course transitions, pause state, and scores. Clients reconnect automatically with their browser-session token. Inactive rooms are removed after seven days; spectators and mid-game joins are intentionally unsupported.

Every match is a nine-hole campaign. Before each hole, the table loads a real three-reel course machine: biome, layout/size, and rules/hazards stop independently, then compose the next deterministic course. Each visible duplicate ticket is one unit of real probability. Players can add a generated wild stop or load another ticket into a preferred stop before everyone readies the lever. After a result, the table has eight seconds to fill a shared reroll pot ($3, then $5); incomplete pots refund their cash. A funded reroll reopens the machine and can bolt on up to two chaos reels, whose stopped modifiers are applied to the same course recipe. Quick Start skips the wagering windows and uses seeded automatic results. After each completed hole, its cup becomes the tee for the next hole: the prior arena remains reachable, and the selected next course is deterministically rotated into the least-conflicted open direction before filling only open terrain. Shared edges are re-smoothed into one continuous height field, so the campaign grows outward as one arena. After hole nine, the final screen zooms out over the one accumulated route while leaving the podium and standings available in a compact foreground panel. Lowest aggregate strokes wins.

The deterministic backend recipe exposes route length, bendiness, lane width, side routes, terrain density, ramp frequency and height cap, rough/sand/ice/booster/conveyor/cushion/spring rates, bumper banks, wall count, sweeper count, gate count, portal-pair count, separate recovery/chaos-pad counts, independent sinkhole-pair, thorn-zone, pulse-field, updraft, low-bar, air-ring, and gust-lane counts. The layout reel selects a compact, standard, or full board bounded by the host’s selected width and height, and an archetype where possible: ribbon, switchback, fork, courtyard, or slalom. Eleven curated biome stops—balanced, speedway, hazard run, ice rink, quarry, Drift, Bloom, Pulse, Carnival, Marsh, and Zephyr—bias those ingredients while retaining the same underlying granular controls. Carnival foregrounds spring launches and bumper banks, Marsh creates braking cushion-turf detours, and Zephyr adds gust lanes that steer balls on the ground and in the air. Drift has visible paired sinkholes that reroute a ball to their exit; Bloom thorns knock a ball away; Pulse fields launch it along a marked direction. Updrafts steer airborne balls, low bars punish poorly timed chips, and air rings reward a clean elevated pass with extra carry. Cushion turf brakes hard, spring tiles turn rolling shots into low arcs, and bumper banks reflect putts while sufficiently high chips clear them. Biomes use distinct arena palettes, patterns, synthesized terrain cues, slot glyphs, course-transition treatment, and an in-game legend, while full recipe details remain programmatic rather than player-editable so game logic can construct, validate, and reproduce every stop from its seed.

Each package sets per-hole gameplay quantities: turn timer, stroke cap, collisions, power-up availability and recovery bias, launch strength, rolling resistance, wall bounce, terrain acceleration, hazard impulse, portal speed, cup size, hazard phase count, and score multiplier. Large generated routes automatically receive a larger stroke cap. Course packages never grant shared boons or starting items.

Sweepers and timed gates run continuously during active play, including aiming and shot playback. Each moving obstacle receives a seeded slow (blue, 12-second), standard (amber, 8-second), or fast (red, 6-second) cycle when its course is generated. The physics simulator advances those obstacles throughout every shot, while online rooms use the authoritative server clock for collision timing and clients smoothly predict it for rendering. Visible item pads spawn according to the selected recipe. Recovery pads and the optional extra-charge boon favor protective and self-directed items for players at least two strokes behind; chaos pads retain the full item pool. Alongside turbo, shield, bomb, freeze, swap, and two putts, pads can grant nine ball forms—heavy, bouncy, ghost, magnet, ice, portal, glider, sticky, and orbit—and utility items including cup magnet, slipstream, rebound rig, phase shift, sandbag, rescue drone, airhorn, popper pad, snare patch, blast mine, slick patch, and sky spring. Glider improves airborne time, sticky brakes and deadens rebounds, orbit enlarges cup capture, rescue drone advances the user along the safe route, and airhorn forces an opponent's next shot into a chip. Targeted items expose an opponent selector. Gadgets enter a two-step placement mode: choose an open playable tile, then click it again or press Enter to confirm; Escape cancels without consuming the item. Each player can keep one gadget active—or two with gadgeteer—gadgets affect their owner too, and all but slick patches disappear when triggered.

Shots support two deterministic stroke types: putts roll along the ground with calibrated resistance, while chips launch on an airborne arc to clear walls and closed gates before landing on legal terrain. On oversized courses, a smooth follow camera tracks the ball in play and the renderer culls offscreen terrain. Cup capture uses circular overlap rather than a center-point hit: a slow ball drops once roughly one third of its footprint covers the cup. Ramps use continuous shared-height floor corners, rebounds preserve tangential direction on glancing wall hits, and the aim guide draws either the grounded line or the matching chip arc. Boosters and conveyors add capped, time-scaled acceleration, and the simulator always commits a stopped ball.

Ball collisions are package-controlled: when enabled, a moving ball transfers momentum to resting opponents. Two putts immediately returns the club to the same player for a second shot while hazards keep moving. After every non-final hole, players visit a shared seven-card clubhouse shelf in sink order. They earn catch-up cash, vote once on a free reroll, then buy one item each in turn: three stackable personal Caddy slots, Contraband, Reality Cards, and Chrono Cards. The 150-entry catalog includes specialist Caddies for cushion, spring, bumper, slope, and gust terrain; tactical pocket cards that can be placed on any active player; and short, round, or hole-duration strategy cards. Shared Draft and Rescue Pact deliberately attach to both the caster and the chosen ally, while Clubhouse Pool rewards both after the covered hole. New Reality Cards can make banks hot, turn boosters into springs, intensify wind, or replace sand with cushion turf. Board badges and the merchant’s optional table-holdings view show held cards, Caddy stacks, active forms, and ongoing effects so every shopper can make an informed decision.

## Controls, pause, and accessibility

During each local slot window, pass the device to the next unready player so they can load tickets into a specific reel, add a wild primary stop, bolt on chaos during a reroll, or pull the lever; online players can only place their own wagers. The course remains hidden until its stopped component combination locks in. During play, pull back on the course and release: left mouse button sends a putt and right mouse button sends a chip. The strength bar reflects the active pull. Keyboard shortcuts remain available for accessibility: Space shoots, `-`/`=` change power, `C` switches between putt and chip, and `P` uses a held chaos item. The reaction dock sends pixel-text reactions (`++`, `>:)`, `!!`, `**`, or `OK`) above the current human player's ball. Escape cancels gadget placement first, then pauses a local match; online pause and resume are host-only. `?` opens the shortcut list and F1 opens settings, where all shortcuts can be remapped.

Standard-mapped controllers are supported: the left stick aims, A shoots, B uses the held item, Y switches putt/chip, D-pad left/right adjusts power, and Menu/Start pauses. In gadget placement mode, the D-pad moves the tile cursor, A locks then confirms the tile, and B cancels. Settings persist locally and include reduced motion/flash, high contrast, master/effects-volume preferences, controller vibration, stick deadzone, aim sensitivity, and keyboard remapping. Shortcuts do not fire while an input field has focus. Stored room snapshots are normalized on load so pre-biome saves without gadget or feature fields remain playable.

The server rejects unknown message shapes, rejects client transition-completion requests, verifies that a player owns each slot wager/emote and the active turn, bounds shot input, rate-limits sessions and room attempts, limits WebSocket payload size, and checks browser Origins against `APP_ORIGINS` before upgrade. It is intentionally a small private-room service rather than hosted matchmaking, accounts, spectators, anti-cheat, or voice.
