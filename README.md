# Golf With Your Enemies

An original, ASCII-isometric mini-golf roguelike with freeform generated courses, a visible generation inspector, local hot-seat play, and up to four AI enemies.

## Run locally

```sh
npm install
npm run dev
```

Run the verification suite with `npm run check`.

## Current scope

The game is a static browser client designed for GitHub Pages. The local transport keeps commands, state, course manifests, and seeds separate from the UI so a later authoritative multiplayer backend or WebRTC transport can be added without replacing game rules.

Each generated hole creates a route, applies freeform terrain and hazards, then retains candidates only when its shot solver finds a viable cup line. The generator inspector presents candidate seed and scoring data before the hole is locked into the nine-hole campaign.
