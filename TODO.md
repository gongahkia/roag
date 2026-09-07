# Jomon — Operational Tracker

## Current State

- The active product is a standard-library Python 3.11 `curses` roguelike with four persistent seamless regions, a 12-node route chart, four seasons, aligned z-levels, camera/FOV exploration, and action-clock regional processes.
- Jomon has three 64×22 decks, a dedicated 64×24 tavern, scheduled named adults, bartender and eight drinks, six initial adults, six bounded regional recruits, a paper-doll equipment view, 10×6 courier pack, 18×10 locker, and optional mouse input.
- Save format 5 migrates Python format 4 without resetting consequences. The verified build passes 129 tests, compilation, whitespace checks, 100-seed encounter and route audits, 12 schedule samples, and real-PTY checks of travel, migration, schedules, mouse packing, low-capability fallback, and vessel life.

## Now

- Implement one materially distinct questline in each existing region and one
  grounded cross-region arc, while preserving immediate first departure.
- Reconcile physical objective cargo, defeat/death ownership, inventory
  cancellation, ammunition, bottled drinks, selected relics, and reconstructed
  regional process state in save format 6.
- Activate retained passives, recruit techniques, armour fields, statuses, and
  enemy capabilities through ordinary production actions; then use bounded
  production encounter composition and explicit ranged targeting.
- Improve cache signposting, add bounded tactical rewards and alternative
  elites, and verify twelve readable three-system build combinations.

## Next

- Collect owner-play evidence on quest branches, treasure discovery, ranged
  targeting, encounter repetition, and the cross-region endings.

## Later

- Wrap rather than ellipsize dense route-consequence text in the 80×24 chart
  details panel if ordinary play confirms the PTY-observed readability cost.
- Add another regional map only after the current four are played and judged.
- Revisit return-route variation and expand onboard incidents only after ordinary expedition pacing is stable.
- Add capture or rescue only if play exposes a concrete contextual need.

## Frozen

- more regions in this milestone; infinite terrain; wall-clock or closed-game
  catch-up; unnamed distant-person or universal NPC simulation; unrestricted
  autonomous death; factions; eras; NG+; alternate renderers; graphical tiles;
  crafting; skill trees; replay infrastructure; generic content packs;
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
