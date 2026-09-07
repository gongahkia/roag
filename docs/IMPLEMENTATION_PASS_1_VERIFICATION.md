# Implementation Pass 1 verification

This record covers the crew, deckbuilding, onboarding, and closely related
terminal-clarity work that began at `68c81fd`. It does not claim overall game
balance, public-demo readiness, legal title clearance, or a measured run
duration.

## Measured content and structure

- Content validation reports 25 crew, 190 technique cards, 70 enemies, 109
  encounters, 10 events, 18 boons, 18 curses, 18 stackable items, 11 biomes,
  and six world layouts. Counts are diagnostic rather than validity rules.
- All 25 crew own seven or eight techniques. Every pool has five starters and
  at least three non-starter drafts, two authored build directions, and a
  starter-to-non-starter transformation.
- All 190 techniques have validated authored tags and upgrade descriptions
  after content metadata is merged. Sixty-two upgrades change their operation
  sequence, compared with 31 at the starting commit.
- Techniques legal from at least three ranks fell from 108 at the starting
  commit to 38; four-rank techniques fell from 51 to 23. The earlier audit's
  stated figure of 96 broad-rank techniques could not be reproduced from
  `68c81fd`.
- With numeric magnitudes normalized but cost, source ranks, targets, target
  ranks, and effect structure retained, repeated structural copies fell from
  41 cards in 17 groups to 32 cards in 14 groups despite the larger catalog.
  This is a structural diagnostic, not evidence that every card is balanced.

## Automated verification

On Python 3.14, the following completed successfully:

- `PYTHONWARNINGS=error python3 -m unittest discover -s tests -v`: 133 tests
  passed in 227.708 seconds.
- `PYTHONWARNINGS=error python3 -m compileall -q dumbest_dungeon tests`.
- `python3 -m dumbest_dungeon --validate-content`.
- `git diff --check`.

The suite covers semantic content validation, every crew pool, rank recovery,
reward and transformation determinism, pending-choice save/load, tutorial
state, run-effect sequencing, destination reach, enemy-action events and
playback controls, minimum-size rendering, and existing generation/save/combat
regressions. It does not prove drafting quality, encounter balance, player
comprehension, or fun.

## Real-terminal verification

The deterministic tutorial was completed with only its normal controls at
80x24. It required route confirmation, formation repair, healing a hero at
Death's Door, reading a setup/payoff intent, allowing an enemy phase, winning
combat, choosing among a tagged reward dilemma, and inspecting the resulting
21-card deck. Card and crew inspection, enemy playback, fast playback, and the
post-tutorial return all remained readable. All five preset descriptions and
the advanced 25-crew browser were separately inspected at 80x24. A 140x60 PTY
confirmed centered world and combat rendering. A fresh post-fix 80x24 launch
confirmed the public title and tutorial entry screens.

One unmodified seed-42 expedition was then played without injected damage,
energy, enemies, or rewards. Bulkhead Basics cleared four ordinary encounters
in two to five rounds, secured three of the two required access signals, grew
the deck from 20 to 25 cards, accepted two curses, stacked Spare Magazine to
four, acquired three boons and Quiet Bearings, upgraded Called Shot, and saved
and reloaded the developed state. The drafted mark payoff, high-stress Warden
damage, healing coverage, opening draw, and curse/item interaction all changed
individual card and route decisions during this run.

The expedition did not reach the Overseer Core. It exhausted supplies and
light, accumulated repeated low-light stress collapses, put all four crew at
Death's Door, and ended in a full wipe during later travel. This single run is
useful regression and qualitative evidence, but it does not establish a win
rate or a 30-45 minute duration target.

## Remaining uncertainty

- Reward labels and build tags made choices easier to parse, but their balance
  across 25 crew needs substantially more ordinary play.
- Narrower source ranks made displacement and collapsed enemy ranks matter in
  the natural run. More party compositions are needed to determine whether the
  fallback coverage is sufficient rather than merely harsh.
- The zero-light failure was severe and legible, but one loss cannot determine
  whether the pressure curve is fair.
- Objectives, boss variety, progression, and a difficulty ladder remain out of
  scope for this pass. Mouse input remains limited to exploration routing.
