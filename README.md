# Golf With Your Enemies

An original, ASCII-isometric mini-golf party game with public pre-hole voting, local hot-seat play, and up to four AI enemies.

## Run locally

```sh
npm install
npm run dev
```

Run the verification suite with `npm run check`.

Capture a randomly seeded comparison set with `npm run capture:levels -- --count 25`. The command writes PNGs and a seed manifest under `output/levels/`; set `BROWSER_BIN` when Chromium is not available as `chromium-browser`.

## Current scope

The game is a static browser client designed for GitHub Pages. The local transport keeps commands, state, course recipes, and seeds separate from the UI so a later authoritative multiplayer backend or WebRTC transport can be added without replacing game rules.

Every match is a fixed nine-hole local hot-seat campaign. Before each hole, everyone votes publicly and concurrently between three seeded course-and-rules packages; bots cast deterministic seeded-random ballots, plurality wins, and a seed-based tie-break settles any draw. The selected package generates the next 20×14 isometric course, applies temporary rules to every player, and resets before the next vote; lowest aggregate strokes wins.

The deterministic backend recipe exposes route length, bendiness, lane width, side routes, terrain density, ramp frequency and height cap, rough/sand/ice/booster/conveyor rates, wall count, sweeper count, gate count, portal-pair count, and separate recovery/chaos-pad counts. Curated personalities—balanced, speedway, hazard run, ice rink, and quarry—bias those ingredients; complete recipes remain programmatic rather than player-editable so game logic can construct, validate, and reproduce every ballot option from its seed.

Each package also sets per-hole gameplay quantities: turn timer, stroke cap, collisions, power-up availability and recovery bias, launch strength, rolling resistance, wall bounce, terrain acceleration, hazard impulse, portal speed, cup size, hazard phase count, score multiplier, starting item, and one or two shared boons. Boons reuse the existing upgrade effects—heavy ball, ice skates, extra charge, bank shot, hazard shield, chaos magnet, portal savvy, second wind, and scavenger—but affect every player only for the winning hole.

Every resolved turn advances the course phase once: sweeping arms rotate to their next fixed orientation and gates open or close. The phase is locked while a ball is in flight, so aim previews, bots, and committed shots use the same board state. Visible item pads spawn according to the selected recipe. Recovery pads and the optional extra-charge boon favor protective and self-directed items for players at least two strokes behind; chaos pads retain the full item pool. Pads can grant turbo, shield, bomb, freeze, swap, two putts, or a one-shot heavy, bouncy, ghost, magnet, ice, or portal-ball transformation. Pads are single-use and only appear when the winning package enables power-ups.

Shots use grounded mini-golf physics: calibrated rolling resistance varies by surface, ramps use continuous shared-height floor corners, and rebounds preserve tangential direction on glancing wall hits. The aim guide uses the same isometric projection as the simulation. Boosters and conveyors add capped, time-scaled acceleration, and the simulator always commits a stopped ball.

Ball collisions are package-controlled: when enabled, a moving ball transfers momentum to resting opponents. Two putts keeps the player on the same hazard phase for an immediate second shot. Shared boons are active rules rather than labels—heavy ball, ice skates, extra charge, bank shot, hazard shield, chaos magnet, portal savvy, second wind, and scavenger respectively affect momentum, ice, item drops, wall rebounds, void recovery, item recharge, portal exits, bonus putts, and inventory capacity. Board badges always show every player's held item, active ball form, armed putts, and boons. Generated courses include sparse wall bumpers for bank shots.

## Controls and accessibility

During voting, select an option on your named row in the public ballot; you can change it until the last outstanding ballot is cast. During play, mouse movement aims a human player's shot; Space shoots, `-`/`=` change power, and `P` uses a held chaos item. The five ASCII emote buttons send `\o/`, `>:]`, `!?`, `*_*`, or `GG` above the current human player's ball. `?` opens the shortcut list and F1 opens settings, where all shortcuts can be remapped.

Settings persist locally and include reduced motion/flash and a high-contrast terminal mode. Shortcuts do not fire while an input field has focus.
