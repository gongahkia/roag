# Golf With Your Enemies

An original, ASCII-isometric mini-golf roguelike with freeform generated courses, a visible generation inspector, local hot-seat play, and up to four AI enemies.

## Run locally

```sh
npm install
npm run dev
```

Run the verification suite with `npm run check`.

Capture a randomly seeded comparison set with `npm run capture:levels -- --count 25`. The command writes PNGs and a seed manifest under `output/levels/`; set `BROWSER_BIN` when Chromium is not available as `chromium-browser`.

## Current scope

The game is a static browser client designed for GitHub Pages. The local transport keeps commands, state, course manifests, and seeds separate from the UI so a later authoritative multiplayer backend or WebRTC transport can be added without replacing game rules.

Each generated hole creates a route, applies freeform terrain and hazards, then retains candidates only when its shot solver finds a viable cup line. The generator inspector presents candidate seed and scoring data before the hole is locked into the nine-hole campaign.

Ball collisions are optional in run controls: when enabled, a moving ball transfers momentum to resting opponents. Drafted upgrades are active rules rather than labels—heavy ball, ice skates, extra charge, bank shot, hazard shield, and chaos magnet respectively affect momentum, ice, item drops, wall rebounds, void recovery, and item recharge. Generated courses include sparse wall bumpers for bank shots.

## Controls and accessibility

Mouse movement aims a human player's shot. By default, Space shoots, `-`/`=` change power, Enter locks a generator candidate, `R` rerolls candidates, and `P` uses a held chaos item. The five ASCII emote buttons send `\o/`, `>:]`, `!?`, `*_*`, or `GG` above the current human player's ball. `?` opens the shortcut list and F1 opens settings, where all shortcuts can be remapped.

Settings persist locally and include reduced motion/flash and a high-contrast terminal mode. Shortcuts do not fire while an input field has focus.
