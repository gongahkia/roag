# Golf With Your Enemies

An original, ASCII-isometric mini-golf party game with public pre-hole voting, local hot-seat play, and up to four AI enemies.

## Run locally

```sh
npm install
npm run dev
```

For an online room, run the authoritative game server in a second terminal, then use the `online room` card on the clubhouse page:

```sh
npm run server
```

The server listens on `ws://localhost:8787`, stores rooms in `data/golf-with-your-enemies.sqlite`, and requires Node 22.5+ for its built-in SQLite driver. Configure `PORT`, `GAME_DATABASE`, and the comma-separated `APP_ORIGINS` allowlist when deploying it; production clients must use `wss://`.

Run the verification suite with `npm run check`.

Capture a randomly seeded comparison set with `npm run capture:levels -- --count 25`. The command writes PNGs and a seed manifest under `output/levels/`; set `BROWSER_BIN` when Chromium is not available as `chromium-browser`.

## Game flow and modes

The clubhouse is the main page. It starts a local hot-seat campaign or creates and joins a separate online lobby. A lobby displays the shareable six-character room code, player presence, selected seed/rules, AI settings, and the host-only start action. Once a room starts, the WebSocket server is authoritative for player commands, timers, bots, votes, course transitions, pause state, and scores; room snapshots are persisted in SQLite and reconnect tokens are kept in the browser session. Online rooms currently retain disconnected seats for reconnection and do not support spectators or mid-game joins.

Every match is a fixed nine-hole local hot-seat campaign. Before tee-off, each local player votes through a public three-card selection for every hole in the match; bots cast deterministic seeded-random ballots, plurality wins, and a seed-based tie-break settles any draw. The resulting compact, seeded match plan is locked before play starts—there is no loading screen between selection and the first shot. After each completed hole, the current arena pieces fly outward in the reverse of the creation animation, then the next planned arena pieces fly in before play resumes. After hole nine, a final clubhouse screen shows the podium, complete standings, tied champions when applicable, last place, and a one-click replay. Lowest aggregate strokes wins.

The deterministic backend recipe exposes route length, bendiness, lane width, side routes, terrain density, ramp frequency and height cap, rough/sand/ice/booster/conveyor rates, wall count, sweeper count, gate count, portal-pair count, separate recovery/chaos-pad counts, and independent sinkhole-pair, thorn-zone, and pulse-field counts. Eight curated biomes—balanced, speedway, hazard run, ice rink, quarry, Drift, Bloom, and Pulse—bias those ingredients while retaining the same underlying granular controls. Drift has visible paired sinkholes that reroute a ball to their exit; Bloom thorns knock a ball away; Pulse fields launch it along a marked direction. Biomes use distinct arena palettes, voting glyphs, course-transition treatment, and an in-game legend, while full recipe details remain programmatic rather than player-editable so game logic can construct, validate, and reproduce every ballot option from its seed.

Each package also sets per-hole gameplay quantities: turn timer, stroke cap, collisions, power-up availability and recovery bias, launch strength, rolling resistance, wall bounce, terrain acceleration, hazard impulse, portal speed, cup size, hazard phase count, score multiplier, starting item, and one or two shared boons. Boons reuse the existing upgrade effects—heavy ball, ice skates, extra charge, bank shot, hazard shield, chaos magnet, portal savvy, second wind, and scavenger—but affect every player only for the winning hole.

Every resolved turn advances the course phase once: sweeping arms rotate to their next fixed orientation and gates open or close. The phase is locked while a ball is in flight, so aim previews, bots, and committed shots use the same board state. Visible item pads spawn according to the selected recipe. Recovery pads and the optional extra-charge boon favor protective and self-directed items for players at least two strokes behind; chaos pads retain the full item pool. In addition to turbo, shield, bomb, freeze, swap, two putts, and the one-shot ball forms, both pad types can grant nine universal items: cup magnet, slipstream, rebound rig, phase shift, sandbag, popper pad, snare patch, blast mine, and slick patch. Targeted items expose an opponent selector. Gadgets enter a two-step placement mode: choose an open playable tile, then click it again or press Enter to confirm; Escape cancels without consuming the item. Each player can keep one gadget active, gadgets affect their owner too, and all but slick patches disappear when triggered.

Shots support two deterministic stroke types: putts roll along the ground with calibrated resistance, while chips launch on an airborne arc to clear walls and closed gates before landing on legal terrain. Cup capture uses circular overlap rather than a center-point hit: a slow ball drops once roughly one third of its footprint covers the cup. Ramps use continuous shared-height floor corners, rebounds preserve tangential direction on glancing wall hits, and the aim guide draws either the grounded line or the matching chip arc. Boosters and conveyors add capped, time-scaled acceleration, and the simulator always commits a stopped ball.

Ball collisions are package-controlled: when enabled, a moving ball transfers momentum to resting opponents. Two putts keeps the player on the same hazard phase for an immediate second shot. Shared boons are active rules rather than labels—heavy ball, ice skates, extra charge, bank shot, hazard shield, chaos magnet, portal savvy, second wind, and scavenger respectively affect momentum, ice, item drops, wall rebounds, void recovery, item recharge, portal exits, bonus putts, and inventory capacity. Board badges always show every player's held item, active ball form, armed putts, and boons. Generated courses include sparse wall bumpers for bank shots.

## Controls, pause, and accessibility

During local match selection, pass the device to the named player and click their preferred package card; in online play, each player can cast only their own ballot. Small pills on each card show every ballot cast so far. The course remains hidden until all holes are locked. During play, mouse movement aims a human player's shot; Space shoots, `-`/`=` change power, `C` switches between putt and chip, and `P` uses a held chaos item. The five ASCII emote buttons send `\o/`, `>:]`, `!?`, `*_*`, or `GG` above the current human player's ball. Escape cancels gadget placement first, then pauses a local match; online pause and resume are host-only. `?` opens the shortcut list and F1 opens settings, where all shortcuts can be remapped.

Standard-mapped controllers are supported: the left stick aims, A shoots, B uses the held item, Y switches putt/chip, D-pad left/right adjusts power, and Menu/Start pauses. In gadget placement mode, the D-pad moves the tile cursor, A locks then confirms the tile, and B cancels. Settings persist locally and include reduced motion/flash, high contrast, master/effects-volume preferences, controller vibration, stick deadzone, aim sensitivity, and keyboard remapping. Shortcuts do not fire while an input field has focus. Stored room snapshots are normalized on load so pre-biome saves without gadget or feature fields remain playable.

The server rejects unknown message shapes, rejects client transition-completion requests, verifies that a player owns each ballot/emote and the active turn, bounds shot input, rate-limits messages, limits WebSocket payload size, and checks browser Origins against `APP_ORIGINS` before upgrade. It is intentionally a small self-hosted room service rather than a hosted matchmaking, account, anti-cheat, or voice platform.
