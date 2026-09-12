# Implementation Pass 2 verification

This record covers the objective, exploration, biome, and layout work that
began at `a6af6a3`. It does not claim that exploration is balanced or fun, that
the 30--45-minute run target has been met, or that the public demo is ready.

## Implemented structure

- Four objectives still generate from the four selected biomes and any two
  open the Overseer Core route. Each objective now begins at a three-by-three
  authored landmark, offers two disclosed approaches, sends the party through
  bounded seeded stage sites, and records its approach, outcome, and effects.
- All eleven biomes have distinct validated contracts spanning honest terrain,
  information range, a persistent three-tile hazard footprint, a patrol
  doctrine, a one-use two-choice facility, a formation-sensitive combat rule,
  landmark art, and an objective using those systems. Two hazard footprints
  and one facility are generated for each selected biome.
- Terrain glyphs now own their movement costs. Route finding, route preview,
  light projection, inspection, and movement resolution all use the traversed
  tiles rather than the nearest room anchor.
- Static discoveries become durable knowledge while patrol positions and
  unremembered hazard detail remain limited by simulation perception. Terminal
  dimensions do not mutate or expand simulation knowledge.
- The six twelve-anchor layouts retain comparable encounter/reward opportunity
  but now have different graph properties: branching sorties, a spine with
  bypasses, two ring arcs with contested crossings, cyclic clusters with two
  transfers, a folded zigzag with shortcuts, and central-versus-peripheral
  fracture crossings.
- Patrols use serialized circuit, erratic, hunt, migrate, roam, sentry, stalk,
  or sweep doctrines. Objective and event choices can alter regional alert,
  hazards, terrain, and knowledge through a bounded effect vocabulary.
- Existing events remain ten in number, but generation binds them to compatible
  biomes and every choice now discloses its cost, risk category, persistence,
  and irreversibility before confirmation.
- The content schema is 19 and the save version is 26. Older save compatibility
  is intentionally not provided; invalid versions fail at the existing save
  validation boundary.

No archetypes, techniques, enemies, encounters, boons, curses, items, world
layouts, or final bosses were added. Content validation reports 25 crew, 190
techniques, 70 enemies, 109 encounters, 10 events, 18 boons, 18 curses, 18
items, 11 biomes, 11 facilities, and six worlds.

## Pacing evidence

A deterministic 60-seed route diagnostic measured the cheapest travel-only
route through any two objective approaches and onward to the Core. It found a
median of 185.5 weighted travel ticks, a 90th percentile of 213, and a range of
133--259. Seed 42's cheapest path was 199 ticks. At the former cadence of one
light per two ticks, that best-case travel consumed a median 92.5 light before
combat detours, hazards, or discoveries. The cadence is now one light per three
ticks, reducing that diagnostic median to 61.5 and the 90th percentile to 71.
Low-light stress, ambush risk, extra reward choice, and biome-specific costs
were not reduced.

This is evidence for correcting cumulative route cost, not evidence that the
whole pressure curve is balanced. Tests explicitly retain consequences at zero
light and zero supplies.

## Automated verification

On Python 3.14, the final committed code passed:

- `PYTHONWARNINGS=error python3 -m unittest discover -s tests -v`: 183 tests
  passed in 773.945 seconds.
- `PYTHONWARNINGS=error python3 -m compileall -q dumbest_dungeon tests`.
- `python3 -m dumbest_dungeon --validate-content`.
- `git diff --check`.

The suite includes two 200-seed generation sweeps. Between them they cover all
six layouts and eleven biomes, varied biome mixtures, landmark and hazard
placement, world connectivity, encounter budgets, biome-compatible enemy
formations, and seeded formation permutations. Focused scenarios cover both
approaches for every biome, any-two-of-four gating, optional objectives,
reduced-party completion, terrain-derived route cost, hidden information,
patrol doctrines, facility and event effects, exhausted resources, route
cancellation/interruption, and save/load at the new state transitions.

Cross-process generation snapshots were also compared under
`PYTHONHASHSEED=0`, `1`, `2`, and `random`; their hashes matched. The tests and
diagnostics establish rule invariants and determinism, not player comprehension
or balance.

## Real-terminal verification

The optional tutorial was completed through its normal controls at 80x24. Help
was scrolled from the first through the final entry. Exploration was inspected
at both 80x24 and 140x60; the larger terminal showed more durable static map
space but did not expand simulation knowledge or distant patrol perception.

Every biome map, biome-rule panel, objective menu, and objective telegraph was
inspected at 80x24. Every layout was observed: fracture, spine, and ring during
fresh natural expeditions, and branching, clusters, and zigzag in valid focused
states. The focused objective states were used to inspect coverage, not to
claim eleven naturally completed expeditions. Route cancellation succeeded
before the next atomic step; a second attempt close to a sentry lost the race
to the already resolving step and entered combat, matching the documented
atomic boundary. A valid save with one permanently dead Warden loaded at
80x24, showed `DEAD`, retained the cleaned 16-card deck, collapsed survivors to
ranks one through three, and continued exploration.

Three fresh non-cheated expeditions were attempted:

1. Seed 42, Bulkhead Basics, fracture, with Hydroponic, Null, Foundry, and
   Cryogenic regions. The party won a three-round patrol fight and a six-round
   Foundry objective fight, took two card rewards, stacked an item, accepted a
   bargain, used a Null facility, transformed Baton Strike into Last Watch, and
   saved/loaded a developed deck and partial objective. The non-default Foundry
   route was completed at light 56 and supplies 3. The run was stopped after
   roughly 25--30 minutes with one objective complete.
2. Seed 1, Wound Ward, spine, with Reactor, Storm, Derelict, and Hydroponic
   regions. The safer two-stage Reactor route grounded half its hazard tiles;
   a biome-bound event then suppressed the remainder while alerting patrols.
   The run was stopped after roughly 8--10 minutes with one objective complete.
3. Seed 44, Breach Protocol, ring, with Flooded, Archive, Cryogenic, and Fungal
   regions. A Flooded facility, bargain, Undertow reversal, and sentry contact
   led to a six-round fight in which rank correction mattered and the Hacker
   survived Death's Door. The run was stopped after roughly 10--15 minutes
   following that combat.

None of these natural attempts reached the Core. The first therefore does not
reproduce Pass 1's full seed-42 route, because the expedition structure and
seeded random consumption have materially changed. A successful-run duration
and ordinary completion pressure remain unverified.

## Remaining uncertainty

- Flooded Undertow was legible but unusually severe for Breach Protocol: the
  reversal stranded its Breacher until movement cards restored formation. More
  initial-tier parties must be played before deciding whether this is useful
  biome identity or excessive opening variance.
- The objective approaches are structurally different and disclose their
  stakes, but only two were completed naturally in this verification. Their
  long-run decision quality and reward balance remain unproven.
- The six layouts satisfy different topology invariants, but they retain twelve
  room anchors. Natural run timing is needed before changing opportunity count.
- Event count remains ten. Systemic biome binding should be evaluated before
  deciding whether repetition requires more authored events.
- No natural run reached the final encounter, so the complete pressure curve,
  temptation of third and fourth objectives, and 30--45-minute target remain
  unresolved.
