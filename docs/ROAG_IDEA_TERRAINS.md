# Three optional terrain mechanics

Implements the approved heat vents, push-floor strips and fragile crossings.
They appear in newly generated floors; old saves are never silently populated.
Run `make game` when convenient. **E** or a click beside a visible terrain
marker opens free inspection through the existing interaction menu; **Esc**
closes it. The existing dotted outlines show a vent's committed impact cell.

## Rules

| Terrain | Actual behavior |
| --- | --- |
| Heat vent | On a committed action, a visible, ready vent begins a **100 world-time** warning. Its locked one-cell impact uses the existing effect scheduler, so impact wins a tie with a player decision. Base **2 fire damage**, with ordinary defense, wards, deaths and neutral friendly fire; no new Burning effect. Then **300 world-time** cooldown. Unseen vents do not arm or deliver hidden impacts. No render-time activation or guaranteed reaction action. |
| Push floor | Ending a move on the arrow pushes the actor one legal cardinal tile. Both player and enemies use existing collision and displacement rules. Walls, blocking occupants, stairs and exits stop the push; displacement interrupts windups normally. At most **four** pushes per actor/action prevents loops. The original move/ability keeps its normal recovery. Passing over an intermediate arrow during Dash does not retroactively redirect the final landing actor. |
| Fragile shortcut | The first crossing arms the tile. It remains solid while occupied, then becomes a permanent gap after everyone leaves. No collapse damage, forced fall, refill or occupied-cell closure. Walkers and Dash crossings both count. Gap uses existing water collision: blocks movement, permits sight/projectiles. |

At most one of each per floor, with distance and placement exclusions. Push
floors require a clear local bypass and an independently proven alternate route;
they cannot form a mandatory one-way chokepoint. A floor
may omit a mechanic if no safe candidate exists. Fragile shortcuts use only
procedurally editable cells: **never** authored protected cells or primary
corridor cells. Before placement, flood checks prove that removing the tile
preserves both the geometric reachable component and currently usable routes
around static props. Stairs, chests, doors, room services, start/elite/exit rooms
and their reserved cells are excluded. This is an optional shortcut, not a
collapsing mandatory corridor or a new terrain generation algorithm.

Uncollected supplies, revival commitments, placed companions and class markers
also hold an armed shortcut open. Collected/swallowed reward coordinates are
historical and may refer to that verified gap; active loose supplies cannot.
Expired ground overlays are pruned when the cell closes.

## Art and state

Ten stable terrain image bindings cover the vent phases, four arrow directions
and three shortcut states. Both Kenney choices resolve **425/425** required
bindings. Mixed uses Tiny 9/29 for vents, RPG 495 planks and Caves 94 remnants.
The 1-Bit choice uses a cached true two-tone Tiny vent derivative, preserving
source alpha and original PNG bytes. Missing/corrupt optional Tiny art falls
back to native 565; its resolver signature changes so F5 can activate a restored
counterpart. Native 70/600 mark the shortcut states.

All related Micro 112–115 markings were inspected. Their target/bar/elbow/junction
shapes are not a clear complete set of cardinal arrows. The approved, unambiguous
1-Bit **1003 north / 1004 east / 1005 south / 1006 west** are used in both looks.
No excluded new art or new source PNG is introduced.

Optional `idea_terrain` version 1 stores stable node IDs, floor/cell, direction,
phase, cooldown, and an exact pending heat schedule ID/due/action. The existing
`roag-run/11`, content and boon versions remain unchanged. Old snapshots lacking
these nodes remain unchanged. Heat work and cooldowns pause/rebase with their
floor. Invalid phase/direction, duplicate nodes, protected collapse or mismatched
pending work reject before a run activates. Concrete collapsed map rows remain
authoritative on Continue. There is no simulation RNG draw for placement,
inspection, preview or resource loading.

## Reproduce without opening LÖVE

```sh
TMPDIR=/var/tmp ROAG_TEST_RANGE=590-609 luajit tests/run.lua
sh tests/idea_terrain_disk_resume.sh
TMPDIR=/var/tmp luajit tests/idea_terrain_acceptance.lua
TMPDIR=/var/tmp luajit tests/run.lua
make item-room-review-test sprite-ideas-test enemy-review-check surplus-audit-check
python3 tools/sprite_audit.py --check
git diff --check
```

Twenty focused tests cover genuine timed hits/wards, locked cells, fog,
shoves and bounded cycles, enemy movement, Dash, occupancy-safe collapse,
protected saves, malformed state, exact replay and a **56-floor** corpus across
all seven biomes. Three two-process checked-store scenarios compare complete
future events, snapshot, RNG and schedule through generations 1 → 2. Three
natural generated scenarios use only legal public actions, normal Sentinel
stats and an exact replay with mid-trace restore. See the
[progress log](goal/ROAG_IDEAS_IMPLEMENTATION_PROGRESS.md) for full-suite results,
seeds, action counts and retained diagnostic failures.

No LÖVE window, rasterized game screenshot, physical input or native GPU test
was run, following the user's standing restriction. Art contact sheets were
inspected offline; visual readability and human tactical acceptance remain
pending. No boon, class, enemy, scheduler priority or ordinary reward value was
changed. Environment/art is the separately committed next batch; the 50+ boon
batch remains deferred.
