# Implementation Pass 2.5: base-run calibration

This focused pass starts from `639d678`. It validates and corrects the base
expedition before bosses, progression, or difficulty modes expand. It does not
target a formal win rate and will not treat a constructed route as evidence of
ordinary-play balance.

## Verified baseline

- `main`, `origin/main`, and `origin/HEAD` all pointed to `639d678`; the working
  tree was clean and no intervening external commit was present.
- Content validation and warning-enabled compilation passed. The complete
  warning-enabled suite passed 183 tests in 780.918 seconds on Python 3.14.
- Save version 26 and content schema 19 are active. The catalog still reports
  25 crew, 190 techniques, 70 enemies, 109 encounters, 10 events, 18 boons,
  18 curses, 18 items, 11 biomes, 11 facilities, and six worlds.

## Failed-run reconstruction

The four prior attempts are not four equivalent defeats. Only the Pass 1 run
ended in a wipe. The available evidence supports the following diagnosis.

| Run | Reconstructable evidence | Primary cause classification |
| --- | --- | --- |
| Pass 1 seed 42, Bulkhead Basics | Four normal fights, three objectives, 25-card deck, two curses, four Spare Magazines, one upgraded card, then zero light/supplies, repeated low-light stress collapse, and a full wipe before the Core. No save or complete route record remains. | Resource and encounter attrition followed an optional third objective. The exact reason for that detour cannot be verified. The old UI did not label remaining objectives optional or provide a persistent Core route, so unclear final-path guidance is a supported contributing inference, not a proven sole cause. |
| Pass 2 seed 42, Bulkhead Basics, fracture | A surviving `/tmp` save verifies Hydroponic/Null/Foundry/Cryogenic, 133 weighted ticks, 105 physical steps, light 56, supplies 3, one Foundry objective, two normal fights, one facility, one transformation, two drafted cards, two item types, one boon, and one curse. All four crew were alive. | This was stopped after roughly 25--30 minutes; it was not a defeat or resource exhaustion. The six-round Foundry fight and extensive inspection made it a pacing signal, not evidence of impossible generation. |
| Pass 2 seed 1, Wound Ward, spine | Documentation records Reactor/Storm/Derelict/Hydroponic, the safe Reactor route, one biome event, light 81, supplies restored to 4, and no combat. | Deliberately stopped after roughly 8--10 minutes with one objective. No failure cause exists to balance against. |
| Pass 2 seed 44, Breach Protocol, ring | Documentation records Flooded/Archive/Cryogenic/Fungal, a Flooded facility, a bargain, Undertow reversal, a six-round sentry fight, one reward, and the Hacker surviving Death's Door. | Deliberately stopped after roughly 10--15 minutes. Full-party reversal compounded Flooded's three-tick terrain, supply hazard, and weak direct healing; recovery existed through Ram Charge but depended heavily on drawing it. This is a local biome/party pressure outlier. |

No evidence records the exact Pass 1 objective approaches, route cursor choices,
facility decisions, Core-access tick, HP/stress sequence, or final encounter
position. Those details cannot be reconstructed and will not be invented.

## Initial pressure audit

A read-only 240-seed audit evaluated every pair of the four objectives, both
visit orders, and all four approach combinations for each pair. Each corridor
included travel from arrival to both objective landmarks, all generated stages,
and the boss anchor. It included immediate objective light/supply costs and
completion light/supply returns, but did not simulate combat, moving patrols,
dynamic terrain changes, facilities, or player skill.

Across 240 worlds, the fastest corridor cost 133--264 weighted ticks (median
187; 90th percentile 223). Selecting for final light instead of speed left
12--77 light (median 49) and 2--6 supplies (median 4). Thus a corridor is
mathematically possible in the sample, but the lowest headroom is not practical
evidence of a forgiving base tier once combat and hazards are included.

Layout medians exposed one clear outlier:

| Layout | Samples | Median fastest ticks | Maximum | Median best final light | Minimum |
| --- | ---: | ---: | ---: | ---: | ---: |
| Branching | 46 | 181 | 247 | 45 | 18 |
| Clusters | 39 | 216 | 264 | 36 | 12 |
| Fracture | 28 | 185.5 | 211 | 52.5 | 28 |
| Ring | 41 | 188 | 259 | 50 | 14 |
| Spine | 40 | 171 | 239 | 60.5 | 25 |
| Zigzag | 46 | 185.5 | 232 | 49.5 | 27 |

Cluster transfers are mandatory graph cuts but inherit whichever two- or
three-tick biome owns the nearest anchor. That contradicts their fiction as
purpose-built transfer corridors and creates unexplained global cost. The
correction should make the two narrow crossings explicit one-tick service rail,
retaining their patrol chokepoint identity without making their resource cost
seed-dependent.

Light and supplies are already mechanically distinct. Light is a continuous
route economy tied to terrain, perception pressure, low-light stress, ambush,
and reward temptation. Supplies are discrete choices for recovery, flares,
facilities, objective methods, and hazard counterplay. The sampled best
corridors did not require supply exhaustion, so neither starting resources nor
the three-tick light cadence should change without further natural-play
evidence.

## Confirmed usability defect

When the second objective opens access, the locked `L` marker disappears. The
Core patrol activates but remains hidden outside ordinary biome perception and
is excluded from `Tab` targets. Some patrol doctrines can also move the boss
away from its anchor before the player sees it. Consequently `Core OPEN` can be
true while the final destination has vanished from practical navigation. This
is a direct explanation for ambiguity after two objectives and must be fixed
before attributing third-objective play to strategy.

## Bounded implementation sequence

1. Retain a standard-library corridor diagnostic that evaluates objective
   pairs, approach costs, hazard exposure, backtracking, and optional facility
   detours without simulating combat or acting as a player.
2. Keep the Core at its anchor until approached and expose a stable Core target
   and route projection after access opens.
3. Add an unmistakable access announcement, required/optional status language,
   an 80x24 mission overview, and a keyboard Core waypoint command that always
   respects maximum command reach.
4. Make the two cluster transfer corridors honest one-tick service rails, then
   rerun the corridor audit before considering any other layout change.
5. Replace Flooded's full-party reversal with a disclosed two-rank displacement
   of the front specialist. This preserves Undertow and rewards correction
   cards while leaving every curated frontliner at least one legal fallback.
6. Audit facility-assisted corridors and all biome pressure contracts. Make no
   global resource or combat change without a new demonstrated outlier.
7. Perform real-PTY natural runs across every curated squad and layout,
   including a direct two-objective route, optional third-objective play,
   Flooded Breach Protocol, one fair defeat, one Core reach, and one boss win.
8. Run bounded and cross-process seed checks, the complete suite, content
   validation, warning-enabled compilation, and whitespace checks; record only
   observed results.

## Acceptance criteria

- Opening the Core produces an explicit notice, stable map target, full route
  estimate, and a reachable next waypoint. Remaining objectives say `OPTIONAL`
  and expose their possible outcome categories.
- Every sampled world has a non-optional two-objective corridor with nonnegative
  light and supplies before combat. Layout distributions and outliers are
  reported rather than hidden behind a single average.
- Clustered worlds no longer pay arbitrary high-cost biome multipliers on both
  mandatory transfers. Other layouts retain their established topology.
- Flooded Undertow disrupts Breach Protocol without reversing all four owners
  into their weakest ranks; its counterplay is visible in biome help.
- Focused tests, all-layout/all-biome sweeps, save/load after Core access and at
  resource depletion, 80x24 and large-terminal checks, and the complete suite
  pass. Natural play supplies at least one Core reach, one boss victory, and one
  understandable defeat, or the remaining blocker is reported without a
  manufactured success.
