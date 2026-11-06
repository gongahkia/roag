# P09-HUD — Dedicated game interface tranche

Added by explicit user direction on 2026-11-07. Implement after the gameplay
systems stabilize and before P09 release verification. Give this tranche its own
worktree ownership, acceptance evidence and report; it is part of the authorized
migration, not an optional suggestion.

## Intended experience

The colony fills the viewport. The interface feels like a game, with contextual
controls connected to people, buildings and events in the world. Replace the long
scrolling webpage of cards and forms with a compact HUD, a build/action palette,
and contextual inspection panels. Use the preserved sprite pack for suitable
frames, icons, objects and indicators, respecting verified sprite identities.

Use a readable bitmap font in the Cozette/Creep style. Inspect the preserved fonts
first; if another font is selected, verify its license and retain attribution.
Choose integer pixel scaling, clear contrast and comfortable text sizes. Sprite
pieces are not assumed to be animation frames; retain uncertain identifications.
Only selected derived crops and necessary font files enter the web distribution.

## Interface work

- Full-screen colony view with a compact resource/time/raid HUD; the camera stays
  useful while menus are open. Narrow screens use drawers/palettes rather than a
  full-page stack of every job and structure.
- Build and designation tools use map placement with a visible footprint, costs
  and access/occupancy explanations. Retain an accessible precise-input alternative.
- Selecting a founder or structure opens its contextual actions and inspector.
  Jobs and detailed ledgers move into focused panels; ordinary play should not
  require scrolling a webpage to find the relevant card.
- Place appropriate feedback in the world: selected actors, wounded states,
  carried supplies, work progress, danger telegraphs and actionable notifications.
  Use diegetic elements where they remain legible; do not obscure urgent information
  merely to make it decorative.
- Present raids, bosses, weather and consequences consistently through the HUD.
  Use the actual artwork, checked crops and restrained motion. Preserve provenance
  and credits in an accessible menu.
- Keep keyboard navigation, focus handling, pause/speed/save/reload, reduced motion
  and readable non-color status cues. Menus must not accidentally issue world orders.

## Acceptance

An ordinary production-build journey must establish, build, defend, recover and
save/reload using the game HUD. Capture and inspect desktop and narrow views with
menus both open and closed. Exercise pointer and keyboard placement, selection,
menu focus/escape, camera controls, error states and both defeat/new-run flows.
Verify all simulation/continuation/conservation regressions remain passing.
Recheck asset provenance, font licensing/credits, distribution size and the final
performance workload. The HUD pass cannot mark gameplay features complete or
weaken existing acceptance gates.
