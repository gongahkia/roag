# Jomon — Operational Tracker

## Current State

- The current game has eight persistent regional destinations and a 16-node
  route chart. It remains a standard-library Python 3.11+ `curses` roguelike
  with aligned z-levels, camera/FOV and action-clock processes.
- Jomon has three 64×22 decks, a dedicated 64×24 tavern, scheduled named adults, bartender and eight drinks, six initial adults, six bounded regional recruits, a paper-doll equipment view, 10×6 courier pack, 18×10 locker, and optional mouse input.
- Save format 14 is current; formats 3–13 migrate forward. The format-6 to
  format-7 migration preserved recorded geography and physical identities in
  its historical fixture. Historical milestone checks are recorded under
  `docs/`; they are not a substitute for checks on the current tree.
- The current content verifier reports 20 regional lines, five cross-region
  arcs, 24 mixed situations, eight sanctums, 94 physical containers, 72 weapons,
  36 armour pieces, 48 passives, 32 techniques, 16 relics, 11 vessel refits,
  twelve voyage families and twelve stateful variants. Twenty-four executable
  build scenarios cover the numeric gates; exhaustive manual branch and build
  coverage is not claimed.

## Now

- Run and time a complete all-region human campaign and one replay with
  different choices. The [final content audit](docs/final-content-release-audit-2026-09-14.md)
  records the checked breadth, but automated route and catalogue evidence
  cannot certify the requested ten-hour replayable-play target.

## Next

- Exercise native macOS/WSL terminals before claiming those platforms are
  release-verified.

## Later

- Judge all eight current regions before any further geographic expansion.
- Revisit return-route variation after the present expansion is played.
- Add capture or rescue only if play exposes a concrete contextual need.

## Frozen

- infinite terrain; wall-clock or closed-game
  catch-up; unnamed distant-person or universal NPC simulation; unrestricted
  autonomous death; grand-strategy factions; eras; NG+; alternate renderers;
  graphical tiles; unrestricted crafting; unbounded skill trees; generic content packs;
  schedule/GOAP/encounter/quest/narrative DSLs; endless procedural quests;
  full needs/anatomy/garment/
  fluid/weather/economy simulation; servers; networking; telemetry; databases;
  plugins; ECS; real-time combat or travel; and generic magic.

## Done

- Browser v19 is archived at local branch `archive/web-v19` and annotated tag `jomon-web-v19-final`, both targeting `de1c1e8`; its active runtime was retired.
- The initial, deeper, seamless-Hearthford, four-region, and living-vessel milestones are recorded under [`docs/`](docs/).
- New worlds start with an on-duty courier at the gangplank; all eligible
  couriers receive one role-appropriate physical working issue so preparation
  can be discovered after an immediate first departure rather than blocking it.
- Physical-state integrity, retained build hooks, production encounter
  composition, bounded negotiation, explicit ranged targeting, four regional
  questlines, the Working Marks arc, causal recruits and merchant, four seeded
  elite alternatives, and eight bounded tactical rewards are implemented and
  covered by the regional-quest milestone assessment.
