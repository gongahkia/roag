# Golf With Your Enemies

An original, ASCII-isometric mini-golf party game with player-authored courses, local hot-seat play, and up to four AI enemies.

## Run locally

```sh
npm install
npm run dev
```

Run the verification suite with `npm run check`.

Capture a randomly seeded comparison set with `npm run capture:levels -- --count 25`. The command writes PNGs and a seed manifest under `output/levels/`; set `BROWSER_BIN` when Chromium is not available as `chromium-browser`.

## Current scope

The game is a static browser client designed for GitHub Pages. The local transport keeps commands, state, course manifests, and seeds separate from the UI so a later authoritative multiplayer backend or WebRTC transport can be added without replacing game rules.

Each round begins with a build phase. Every player authors one course on a blank 20×14 isometric grid, then must sink that course once before it enters the competitive course set. AI players use the same seeded generator and complete the same validation requirement. Once every course is accepted, all players play every authored course in author order; lowest aggregate strokes wins.

Builders can place fairway, rough, sand, ice, walls, boosters, conveyors, tee and cup markers, sweepers, timed gates, recovery pads, and chaos pads. Height is editable from zero to three, with adjacent tiles sharing ramp-edge heights. The builder also has a deterministic terrain generator: terrain density, ramp frequency, and timed-hazard count are adjustable before generating, and the result remains fully editable. It uses seeded route carving rather than unbounded procedural noise, so the same run seed, author, and settings reproduce the same starting terrain. Each author also selects one competitive perk while building, keeping the existing upgrade rules available without adding a third between-course phase.

Every resolved turn advances the course phase once: sweeping arms rotate to their next fixed orientation and gates open or close. The phase is locked while a ball is in flight, so aim previews, bots, and committed shots use the same board state. Three visible item pads spawn on each hole. Recovery pads and the optional extra-charge upgrade favor turbo or shield for players at least two strokes behind; chaos pads retain a wider item pool. Pads are single-use and only appear when power-ups are enabled.

Shots use grounded mini-golf physics: calibrated rolling resistance varies by surface, ramps use continuous shared-height floor corners, and rebounds preserve tangential direction on glancing wall hits. The aim guide uses the same isometric projection as the simulation. Boosters and conveyors add capped, time-scaled acceleration, and the simulator always commits a stopped ball.

Ball collisions are optional in run controls: when enabled, a moving ball transfers momentum to resting opponents. Drafted upgrades are active rules rather than labels—heavy ball, ice skates, extra charge, bank shot, hazard shield, and chaos magnet respectively affect momentum, ice, item drops, wall rebounds, void recovery, and item recharge. Generated courses include sparse wall bumpers for bank shots.

## Controls and accessibility

During building, select a palette tool and click an isometric grid cell; use the builder sliders and `R` to generate a terrain pass. Enter begins author validation once the board has a tee, cup, and at least eight playable tiles. During validation and competitive play, mouse movement aims a human player's shot; Space shoots, `-`/`=` change power, and `P` uses a held chaos item. The five ASCII emote buttons send `\o/`, `>:]`, `!?`, `*_*`, or `GG` above the current human player's ball. `?` opens the shortcut list and F1 opens settings, where all shortcuts can be remapped.

Settings persist locally and include reduced motion/flash and a high-contrast terminal mode. Shortcuts do not fire while an input field has focus.
