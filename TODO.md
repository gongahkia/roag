# Jomon — Operational Tracker

## Current State

- The expansion in progress has eight persistent regional destinations and a
  16-node route chart. It remains a standard-library Python 3.11+ `curses`
  roguelike with aligned z-levels, camera/FOV and action-clock processes.
- Jomon has three 64×22 decks, a dedicated 64×24 tavern, scheduled named adults, bartender and eight drinks, six initial adults, six bounded regional recruits, a paper-doll equipment view, 10×6 courier pack, 18×10 locker, and optional mouse input.
- Save format 7 migrates Python format 6 without repainting saved geography or
  replacing lost possessions; supported older Python paths chain forward.
  A real save written by the expected format-6 commit was loaded and resaved by
  format 7 with its exact injury, health, market stock and physical identities.
  The final serial run passed 421 tests in 175.189 seconds. The corrected
  1,000-seed audit generated 8,000 regions with zero failures. See the live
  measurement ledger for exact focused audits, performance and candid
  manual/platform limits.
- Each region now has a three-stage questline with two endings, and completing
  any two opens the five-part Working Marks arc. Production generation uses
  finite mixed groups, four seeded alternative elites, and 62 persistent
  containers. The expansion now has 36 weapons, 36 armour pieces, 48 passives,
  32 inherent/learned techniques, 12 relics, eight physical fitting kits,
  51 secondary tools, supplies and drinks,
  sparse materials, three cross-region arcs and twelve bounded voyage families.
  Twenty-four executable build scenarios and dedicated production audits cover
  the numeric gates; exhaustive manual branch/build coverage is not claimed.

## Now

- Implement the Aftermath, Motion and Reciprocity tranche recorded in
  `docs/aftermath-motion-reciprocity-milestone.md`: first route-following and
  interaction ergonomics, then combat reciprocity, aftermath content and its
  bounded production expansion.

## Next

- Close the tranche's automated and real-PTY gates without turning numerical
  content targets into label-only definitions.

## Later

- Judge all eight current regions before any further geographic expansion;
  this tranche deepens them in place.
- Revisit return-route variation after the present expansion is played.
- Add capture or rescue only if play exposes a concrete contextual need.

## Frozen

- infinite terrain; wall-clock or closed-game
  catch-up; unnamed distant-person or universal NPC simulation; unrestricted
  autonomous death; grand-strategy factions; eras; NG+; alternate renderers;
  graphical tiles; unrestricted crafting; skill trees; generic content packs;
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
