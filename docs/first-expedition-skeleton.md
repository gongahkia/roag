# First Expedition Skeleton

## Narrow integration note

The first expedition is one authored Hearthford map rendered by the existing
keyboard-first canvas view. It adds a single bounded `ExpeditionState` to the
validated mutable world envelope. The state owns only the selected loadout and
support, the courier's authored-map position, the single contact/objective,
one threat, one material resource, and the current expedition consequence.

The existing Jomon deck remains the preparation and departure space. The chart
table is the physical preparation prop and the gangplank is the physical
departure/return prop. No frontier, settlement population, travel, dialogue,
or generic encounter system is introduced.

## Existing APIs retained unchanged

- `moveFoundationWorldCourier` for normal Jomon-deck movement.
- `advanceFoundationWorldTime` for every expedition action that changes the
  world; inspect, cancellation, preparation selection, and contact decisions
  remain zero-time.
- `resolveCourierContinuityLossForVerifiedWorld` and the repository continuity
  transaction for permanent courier death and succession.
- The existing local repository and full-envelope save/load validation.
- The canvas, terminal controls, ASCII glyph styling, content-safety audit,
  and existing cargo/market/worksite systems.

## Deliberately authored and narrow

- One settlement-and-marsh layout, one adult contact, one seal-cord objective,
  one marsh hound, and one reed-screen environmental interaction.
- Three two-item loadouts and three crew-support preparations.
- A local hound turn state, direct attack, brace, and an escape route requiring
  the reed screen plus quiet movement or the smoke-and-hook loadout.
- Four displayed pressure contributors: elapsed minutes, map depth, noise,
  and carried seal cord. Their thresholds are fixed and deterministic.

## Verification record

Filled in with the completed milestone's focused tests, browser flows,
screenshots, and broader-suite results.
