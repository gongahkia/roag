# Seamless Hearthford milestone

## Bounded implementation plan

This owner-directed milestone replaces only the expedition's room graph. Jomon
remains a separate compact deck; household, tavern preparation, material trade,
relationships, defeat, succession, merchant, save, and action-clock rules
remain direct parts of the existing `GameState` and action path.

1. Generate one deterministic 96x54 Hearthford region on four aligned
   z-levels (-1, 0, 1, 2). Use small region-specific value fields, constrained
   river carving, anchored structures, direct road carving, and bounded
   underground excavation. Repair critical paths after validation.
2. Place the quay, settlement, floodplain and Reedwood, a small watch
   structure, stacked millworks, roof and gantry spaces, underground culvert,
   quiet travel, objective, and optional treasure in continuous coordinates.
3. Add pure camera, line-of-sight, exploration-memory, vertical-opening, and
   reachability functions in `world.py`. Curses shows current sight normally,
   remembered terrain dimly, unknown terrain hidden, and never remembers
   moving actors.
4. Keep one deterministic turn path. Add bounded patrol movement, sound across
   adjacent levels, periodic weather, local smoke and water propagation,
   pressure escalation, and one timed objective change. None advances while
   input is idle.
5. Deepen the six authored weapons through the existing attack and guard
   controls. Add a bounded passive catalogue, finite supply counts, persistent
   containers, and two finite rule-changing relics. Tavern preparation gains
   passive selection with an explicit carry limit.
6. Increment the development save schema to 3. Room coordinates and state are
   not reliably convertible to continuous `(x, y, z)` geography, so format 2
   saves are rejected with a clear message and no migration chain.
7. Add focused unit coverage, then complete three full expeditions in real
   PTYs, including multiple builds, vertical traversal, treasure, changed time
   state, save/reload, resize, and terminal restoration. Record only performed
   checks in this document's final assessment.

Excluded: additional regions, a reusable biome toolkit, general fluids,
structural engineering, physics, acoustics, item or encounter scripting,
onboard attacks, graphical tiles, distant simulation, and save-migration
infrastructure.

## Assessment

Completed locally on 2026-09-06. The room-screen expedition has been removed;
Jomon remains a compact separate deck and Hearthford is one persistent
coordinate space.

### Implemented region

The region is 96 columns by 54 rows on four aligned levels:

- `z=-1`: the natural cave and buried mill culvert;
- `z=0`: landing, quay, settlement, riverbank, floodplain, Reedwood, old
  cottage, road, watch, mill yard, and mill ground floor;
- `z=1`: watch chamber, mill upper works, objective store, and gantry; and
- `z=2`: watch roof and mill roof.

The dedicated generator uses two small interpolated value fields for wetness
and tree growth, a bounded meandering river, seeded direct-road carving,
anchored structure templates, constrained cave excavation, and explicit
vertical links. Reedwood has two viable approaches and a reconnecting loop.
Seed differences alter the river, wet ground, vegetation, road walks, and
therefore real traversal. A final graph walk validates the contact, objective,
cave, roof, physical return, and all eight containers. A separate in-process
audit reached all 808 generated container placements across 101 seeds.

The curses renderer derives a clamped camera origin from the courier and
available map panel. Outdoor sight normally reaches 13 tiles, enclosed or
underground sight 7, exposed upper levels add 4, river fog caps sight at 7,
and hard rain caps it at 9. Walls, trees, closed doors, and smoke occlude the
integer ray. Current tiles use normal semantic colours, remembered terrain is
dim, unknown terrain is blank, and actor positions are never stored in
exploration memory.

The full-colour semantic plan uses bright yellow for the courier, bright cyan
for allies and exits, green for neutral people and usable objects, bright red
for hostiles and hazards, bright magenta for elites and rare material effects,
blue for water, yellow for cargo, and muted white for terrain and structures.
Glyphs remain authoritative. Terminals without eight colours or enough pairs
fall back to pair zero with bold cues for the player, danger, exits, and
hazards rather than failing initialization.

Five authored vertical links connect culvert steps, two mill ladders, and two
watch ladders. Aligned ladders and open shafts permit cross-level sight and
attacks. The sluice places bounded water at three related ground/culvert
openings; furnace smoke rises to the upper works; loud actions alert actors on
an adjacent open level. The marked mill floor can be broken only with a hand
axe or mill-tooth wedge, persists as an open shaft, and causes a fall or a
cliff-cord controlled descent. These effects run only in the accepted-action
turn path.

### Builds, treasure, and tension

The active catalogue contains six weapons, eight secondary items, five crew
supports, six persistent role techniques, three finite field discoveries,
twelve stackable passives, and two finite relics. Eight seed-filled containers
include roadside, ruin, flood-islet, cave, buried, gantry, watch-roof, and
mill-roof stores. Requirements include rope, light, and a mill key; opened
state and returned rewards persist. Passive capacity is five bulk, or seven
with a cargo harness.

The ten explicit cross-system combinations are surveyed soft-step, mobile
hook, shielded set stance, weatherproof aim, controlled breach, smoke walker,
flood rig, deep field binding, valuable leverage, and high-ground drive. They
compose with distinct weapon actions rather than an ability framework:
spear controls spacing, billhook pulls and may exchange positions, cudgel
knocks back and attacks morale, staff sweeps adjacent targets and prepares
guarded movement, axe breaks guard and selected flooring, and crossbow uses
aim/fire/reload commitments with finite ammunition. The buckler and guard
technique now press morale harder together, the mill-tooth wedge specifically
lets a cudgel breach weak flooring, and a waxed bowstring preserves committed
crossbow aim through hard rain.

Seven placed threats exercise five profiles: an actual looping road patrol,
territorial reed boar, two readable ranged keepers, a reach-oriented levy,
timed mill machinery, and dormant valuable-seeking reavers. Staff can engage a
mixed close group; ranged and reach opponents complement each other in the
mill. The seed can make the machinery elite: it telegraphs and alternates
between outer aisles 22/28 and inner aisles 24/26, changing required position
rather than merely health. Material alternatives include
witnessed negotiation, lamplight animal redirection, smoke distraction,
flood-assisted retreat, route avoidance, and tool/rope/carpenter control of
the machinery.

Field healing, ammunition, lamp oil, rope, and smoke are finite. Fog and rain
cycle deterministically from expedition actions; rain makes exposed travel
costlier unless countered. The contact's material opportunity worsens at 150
accepted expedition actions, increasing demand. Time, geographic/elevation
depth, noise, and carried valuables remain individually visible. Their
combined pressure expands alert range, doubles pursuit at critical pressure,
and wakes the reavers. Quiet travel separates the major danger sites, while
the richest optional containers require detours or vertical traversal.

Development save format 3 replaces format 2. Room identifiers and local room
coordinates cannot be mapped reliably into continuous geography, so old
Python room-graph saves are rejected with a visible startup message. New saves
remain one atomically replaced JSON document. Browser saves remain unsupported.

### Automated verification

All required commands passed after implementation:

```text
python -m unittest discover -s tests -v
Ran 38 tests in 3.182s
OK

python -m compileall -q jomon tests
exit 0, no output

git diff --check
exit 0, no output
```

The tests cover deterministic and meaningfully varied geography, representative
seed reachability, aligned transitions, camera clamping, FOV occlusion and
memory, actor non-leakage, cross-level sight/attack, floor destruction and
falling, bounded smoke/sound/water, persistent chests and discoveries, passive
limits and combinations, all six weapon identities, patrol/noise/pressure,
the action-clock deadline, persistence, merchant exchange, contextual defeat,
death, succession, and retention of a primary action consequence after
same-turn hostile intents. A dedicated elite test proves that its outer and
inner danger lanes alternate and affect the same position differently. The
authored watch keeper now occupies its aligned roof opening, so the cross-level
attack test exercises a natural encounter position rather than relocating an
unrelated actor.

### Manual PTY verification

Manual play used a real PTY and a disposable `JOMON_DATA_DIR`. Startup, fresh
seed entry, continue, tavern preparation, departure, save, reload, and quit
were exercised. Three complete successful expeditions physically left and
returned through the gangplank:

1. Guard Oren used spear, buckler, and field care. The route included direct
   combat against pressure reavers, guarding a readable crossbow aim, a late
   objective completion, physical return, a merchant visit, and purchase of a
   willow dressing.
2. Carpenter Vela used billhook, rope, and carpenter rig. The courier crossed
   mill ground, upper works, and roof; opened gantry and roof containers;
   displaced a crossbow keeper; respected the machinery's marked timing;
   braked it at the furnace; changed culvert/ground water at the sluice; and
   returned with a counterweight ring and waxed bowstring.
3. Factor Galen used cudgel, smoke pot, and factor surety while carrying the
   two returned passives. Finite smoke broke a boar pursuit; the factor climbed
   the old watch, knocked back and dazed its ranged keeper, opened the roof
   coffer, descended into the culvert, spent lamp oil to open its box, activated
   valuable leverage with four carried passives, and returned. An echo bead
   and witness token persisted, and the merchant appeared again.

The play also directly observed broad outdoor sight, restricted interiors and
cave sight, dim exploration memory, fog and rain visibility, camera travel,
patrol movement and avoidance, sound alerting a threat on another level, water
crossing levels, persistent treasure depletion, and the action-clock objective
change. A preliminary defeat permanently killed a pilot and selected a living
successor; that failed attempt was not counted among the three full
expeditions.

The layout was played at 80x24 and 100x32. It was resized to 70x20, displayed
the exact minimum-size warning, and recovered at 100x32. Quit confirmation
exited with status 0 and the PTY process ended, demonstrating normal wrapper
restoration.

A final controlled-save PTY check placed an axe-bearing courier on the authored
weak mill floor. Interacting broke it into a visible shaft, moved the courier
from `z=1` to the aligned `z=0` tile, and reduced health from 10 to 8. That
check exposed and then verified a fix for same-turn threat lines hiding the
floor/fall consequence. A hostile responding through an adjacent open level
was exercised in ordinary play. A further controlled PTY check put the courier
at the authored watch ladder: climbing was visibly blocked by the keeper above,
and a spear attack from `z=1` struck it at `z=2` for 2 harm and forced it away
from the opening. The smoke path was used on one level; water, not smoke, was
the cross-level propagation manually exercised.

A second controlled-save PTY check exercised the elite crown wheel directly.
Its log first marked outer aisles 22/28; the courier remained safe on row 24.
The next cycle visibly marked inner aisles 24/26, and remaining on row 24 then
dealt 3 harm. This verifies a positioning-changing elite rule rather than a
larger-health variant.

### Candid play assessment

Hearthford now reads as an open geographic place rather than a chain of
interchangeable rooms. The river edge, quiet approach, settlement mass,
looping Reedwood, distant industrial block, stacked watch/mill spaces, cave,
and long physical return create useful scale and route memory. The cost is
that returning over known ground can become repetitive.

The three tested builds played materially differently. Spear/buckler play
rewarded measured spacing and committed guard timing; billhook/rope/carpenter
play rewarded displacement, vertical treasure routes, and material control;
cudgel/smoke/factor play attacked morale, broke sightlines, and turned treasure
bulk into negotiation leverage. This is a real divergence in positioning,
supply use, and route choice, although the catalogue still supports only one
region's worth of discovery variety.

The smallest next improvement justified by this play is bounded,
obstacle-aware path selection for already engaged threats, together with
suppression of pursuit messages when a threat cannot move. The boar sometimes
announced pursuit while caught behind dense Reedwood terrain; correcting that
would improve danger truthfulness without expanding the architecture.
