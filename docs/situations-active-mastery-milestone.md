# Situations and Active Mastery milestone

Started 11 September 2026 from `2bd5e1692e5a21042b411d8c2bdc727320a0e45f`
on `main`, nine commits ahead of `origin/main`, with a clean worktree. Python
3.14.7 runs the Python 3.11+ game. Save format remains 7 unless production
evidence proves that the existing sparse change ledgers cannot preserve the
new state.

## Purpose

This pass increases combinations per hour rather than catalogue size. It will
reuse Jomon's eight regions, 72 standard enemies, 24 elites, material rules,
institutions, voyages and learned practices in more authored situations. It
must preserve immediate Hearthford departure and the ordinary movement,
combat, material, relationship and save reducers.

## Measured baseline

The machine-readable baseline is
`docs/performance-situations-baseline.json`. At 20 samples, ordinary movement
has a 9.166 ms median and 12.231 ms p99; 80x24 rendering has a 5.482 ms median
and 7.874 ms p99; enemy-heavy turns have a 60.086 ms median and 151.411 ms
p99; environment-heavy turns have a 13.301 ms median and 17.361 ms p99; world
creation has a 711.828 ms median. The deterministic replay matched. The save
was 371,564 bytes and peak RSS was 34,432 KiB.

The production content audit reports eight regions, 72 standard enemy
signatures, 24 elite situations, eight rivals, 36 weapons, 36 armour pieces,
48 passives, 32 techniques, 51 tools/supplies/drinks, 16 relics, 62 persistent
containers, 20 regional questlines, five cross-region arcs, 12 institutions,
38 persistent nonhostile adults, 12 voyage families and 12 stateful variants.

## Production design

### Authored mixed situations

Add exactly three finite situation records per region, one for each pressure
band. Every record names two existing ecological or institutional groups, a
physical duty, a material condition, a visible site, three materially distinct
solutions and consequences. Deterministic selection reads seed, pressure,
season, regional history and prior outcomes. Activation wakes or redirects
existing actors; it does not grow the population. Resolution writes a bounded
regional change and causal ledger entry.

### Active mastery

Expose 12 existing learned practices as deliberate manoeuvres through one
contextual mastery overlay reachable from attack targeting, guard and field
handling. Each manoeuvre has a visible setup, counter, physical condition,
time cost and enemy consumer. The reducer must reuse existing attack, movement,
guard, sound, material and status operations. Passive practice effects remain
valid; mastery adds choices rather than invalidating builds.

### Mutable micro-sites

Add three authored sites per region, anchored to existing landmarks and
selected from the region's history, season and aftermath state. Each site has
two approaches, a material or route consequence and a changed revisit. Site
state is sparse: namespaced keys in `Region.changes`, plus existing
`tile_changes` or material cells. Mandatory paths and quest objects are never
blocked or destroyed.

### Cross-region interference

Add eight bounded events. Each consumes an existing shipment, institution,
rival, route repair or ecological outcome, mutates both origin and destination,
and produces testimony or a route/market/material consequence in each place.
Checks run only at meaningful transitions and each event resolves once.

### Later echoes and household stories

Every one of the 12 voyage variants gains a later, one-time echo through a
person, institution, cargo claim, route mark, or persistent deck scar. Voyage
resolution records the required compact facts; echoes occur on later arrivals
or aboard-Jomon interaction, never immediately as duplicate prose.

Add two late-campaign household developments and one all-region capstone. They
consume relationship/memory state, courier death or succession where present,
vessel condition, institutional accounts and at least four regional outcomes.
They are optional and cannot gate initial or ordinary expedition play.

## Architecture and limits

- Use explicit frozen records and reducers, not a generic encounter, quest,
  narrative or entity DSL.
- Use current actors and spatial indices; no actor is created per turn.
- Evaluate situation/interference/echo eligibility only on region activation,
  departure/return, voyage completion or a pressure-band change.
- Persist only bounded facts and sparse deltas. Do not serialize derived
  selection caches or unbounded prose.
- All selection is deterministic from saved facts and `stage_rng`.
- Inspection distinguishes visible facts, history, forecast and inferred
  counters, with glyph and wording fallbacks at 80x24.
- No background time, networking, copied terminology/content, unrestricted
  destruction, magic system or child endangerment.

## Acceptance gates

- 24/24 situation records validate and occur across a 200-seed audit; no
  situation exceeds 10% of a region/pressure opportunity band.
- Each situation has at least three validated solution classes and a production
  activation, observation and persistent resolution path.
- 12/12 manoeuvres are learnable, inspectable, time-bearing, have a failed
  zero-cost preview, and are consumed by at least one hostile context.
- 24/24 micro-sites are reachable, have two approaches, persist, and present a
  changed revisit without sealing required paths.
- 8/8 interference events alter both named regions and leave causal evidence.
- 12/12 voyage variants have bounded later echoes.
- Two household developments and the capstone have success, altered and
  succession-safe paths.
- Save-format-7 round trips preserve active and resolved situations,
  manoeuvre setup, sites, interference, echoes and household outcomes.
- Focused, fast, full, content, generation, encounter, quest, persistence,
  living-world, replay, benchmark and soak checks pass.
- Existing ordinary scenarios remain within the 20% regression gate unless a
  measured user-visible reason is documented.
- Real PTY checks cover all regions, mastery from attack/guard/field, site
  revisits, an interference pair, a voyage echo, a household development,
  80x24, resize recovery, save/reload and clean quit.

## Live ledger

- Planning: repository and production hooks audited; baseline captured at
  `2bd5e1692e5a21042b411d8c2bdc727320a0e45f` before implementation.
- Decisions: save format remains 7; existing actors and reducers remain the
  authority; new content is bound to ordinary region, pressure, voyage and
  household transitions. Site locations are computed once during regional
  generation, persisted as sparse deltas and never searched from the render
  loop. Voyage echoes are bounded once per variant, not once per trip.
- Implemented: 24 mixed situations and mutable sites; 12 active manoeuvres;
  eight two-region interference events; one later echo for every 12 voyage
  variants; two household developments; one all-region capstone; production
  overlays, causal records, save validation and focused audits for all of them.
- Compatibility corrections: situation hazards activate on real departure or
  pressure transitions, not test-only region switches; untrained couriers do
  not gain a dead mastery menu entry; hook and shove reducers respect occupied
  receiving cells; corrupt IDs and malformed sparse records are rejected.

## Final measurements

The final 20-sample machine-readable result is
`docs/performance-situations-final.json`. Against the same seed and benchmark:

| scenario | baseline median | final median | final p99 |
|---|---:|---:|---:|
| ordinary movement | 9.166 ms | 5.956 ms | 6.761 ms |
| input to 80x24 layout | 12.856 ms | 9.414 ms | 10.397 ms |
| 80x24 render layout | 5.482 ms | 3.530 ms | 4.655 ms |
| enemy-heavy turn | 60.086 ms | 24.402 ms | 27.804 ms |
| environmental-heavy turn | 13.301 ms | 6.473 ms | 7.014 ms |
| world creation | 711.828 ms | 356.425 ms | 366.065 ms |
| load | 222.884 ms | 138.550 ms | 162.159 ms |
| save | 24.378 ms | 16.044 ms | 20.908 ms |

The final save is 374,976 bytes versus 371,564 (+0.9%); peak RSS is 36,736
KiB versus 34,432 (+6.7%). Deterministic replay matches. Lazy frontier entry
is 45.951 ms median. Region entry has a 0.019 ms median and a single 1.937 ms
p99 activation sample: an absolute two-millisecond transition, not recurring
input work. Every ordinary and heavy target is met.

## Verification closure

- Fast developer suite: 97 tests in 36.071 seconds, passing.
- Full suite: 541 tests in 277.302 seconds, passing.
- Situation audit: 200 seeds, 4,800 opportunities, all 24 distinct records,
  maximum share 4.17%, no failures.
- Generation audit: 1,000 seeds / 8,000 regions, deterministic duplicate
  generation and format-7 reconstruction, zero topology/content failures;
  3,712.878 seconds, worst seed 38.263 seconds. This deliberately exhaustive
  tier is not part of the fast developer loop.
- Encounter audit: 200 samples / 1,800 plans, 418 production compositions,
  zero invalid actors, unreachable actors or unavoidable opening attacks.
- Quest audit: 50 seeds / 400 regions, 400 unique geographies, 20 regional
  questlines, five arcs and no unreachable or invalid references.
- Living-world audit: 200 route graphs, all connected and deterministic; 12
  schedule samples, no invalid schedules or vessel overlaps.
- Persistence audit: format-7 round trip and deterministic format-6 migration
  pass; item identities and existing regions remain exact; corrupt state is
  rejected. The expanded eight-region audit save is 648,393 bytes.
- Replay audit: 50 samples of 12 actions match exactly.
- Memory soak: 100 voyage legs; logs, items, route marks and sparse material
  cells remain bounded; 8,060 bytes traced post-warmup growth and no failure.

Real curses sessions used isolated temporary state. They covered new-world
creation and immediate `E` departure, situation/history inspection, return,
save, clean quit, reload at 100x32, resize to 60x20 and recovery, and all eight
regional palettes/situation openings at 80x24. Controlled production-UI
sessions exercised target, field and guard mastery, a tool solution and
changed-site revisit, and the two-stage witnessed household development. The
other situation solutions, all interference/echo records, both branches of
each development and all capstone branches were exercised automatically, not
claimed as manual play.

Remaining limitations are intentional and candid: the 1,000-seed audit is
slow; anonymous actors still share bounded cognition profiles beneath their
different roles; and finite authored situations can repeat across campaigns.
No gate for this pass remains open.
