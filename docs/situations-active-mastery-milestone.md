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

- Planning: repository and production hooks audited; baseline captured;
  implementation not yet started.
- Decisions: keep save format 7; reuse existing actors and reducers; bind all
  added content to ordinary region/voyage/household transitions.
- Remaining: all implementation, automated audits, PTY verification, balance,
  final benchmark and closure assessment.

