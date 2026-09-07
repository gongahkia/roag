# Diegetic interface, living vessel, route chart, and seasonal world

## Baseline

Work begins from clean commit `79ed0c7f325bb7112e1716c244fa47b3ffa27368`.
The active format-4 build has four persistent regions, one compact 48×16
aboard map, an immediate four-destination chart menu, a populated but single-map
tavern, transactional manual spatial packing, and no calendar or named-person
schedule clock. Its recorded verification baseline is 92 tests, a clean
compile and whitespace check, a 100-seed encounter audit, and eight complete
PTY expeditions.

No discrepancy, uncommitted work, or concurrent commit was present at the
start of this tranche. `HEAD` and `origin/main` both named the expected commit.

## Bounded implementation plan

1. Replace direct destination selection with one seed-derived, persistent
   route graph whose nodes have distinct time, supply, market, weather, cargo,
   encounter, charting, integrity, and seasonal consequences. Render it as an
   ASCII chart and animate only its presentation; route resolution remains an
   explicit deterministic state transition.
2. Add a persistent calendar, three aligned vessel decks, a dedicated tavern,
   a named bartender and eight bounded drinks. Advance scheduled named adults
   and causal social incidents only when accepted actions move the world clock.
3. Improve the existing inventory rather than replace it: committed-source
   placement ghosts, item preview art, a six-location paper doll, pinning,
   deterministic auto-place, transactional auto-pack, bulk actions, and a
   central optional curses mouse-normalisation boundary.
4. Use one consistent choice treatment for conversations, recruits, bar,
   merchant, voyage, objective, environmental, and route decisions. Repair the
   startup warning with a pure, resize-safe centred wrapping calculation.
5. Increment the save to format 5 and deterministically migrate format 4,
   preserving people, regions, exploration, possessions, cargo, markets,
   consequences, vessel integrity, and voyage history while assigning route,
   deck, schedule, and calendar state without wall-clock input.
6. Run focused and full tests, the existing encounter audit, a deterministic
   route/schedule audit, compile and whitespace checks, then exercise normal
   play through real PTYs at required dimensions. Record only paths actually
   completed.

## Architectural limits

`GameState` remains the single persistence root. A small route module may own
the authored graph and route consequences; a small vessel module may own deck
maps, schedules, drinks, and bounded incidents. `inventory.py`, `travel.py`,
`people.py`, and `terminal.py` retain their existing responsibilities. There
will be no generic graph generator, schedule language, needs model, windowing
toolkit, story engine, real-time sailing loop, offline catch-up, fifth region,
or general social simulation.

## Assessment

The milestone is playable and directly usable through the ordinary curses
loop. It was not completed in a single uninterrupted history: after the
baseline documentation commit, two concurrent commits appeared on `main` and
were preserved rather than reset. `7bc17ec` (`forthebestsupport`) introduced
most of the state, route, calendar, vessel, inventory, and terminal work;
`186dc72` (`otwtothebest`) completed part of the inventory and terminal work.
The integrated gameplay commit is `22a2f6a`, followed by `3f47b21`, which fixes
two PTY-found boundaries: scheduled workers occupying physical controls and
limited terminals rejecting cursor-visibility control.

### Implemented shape

`GameState` remains the persistence root. Four small ownership boundaries were
added without a generic framework:

- `route_chart.py` owns one authored, seed-varied network, connected cursor
  movement, leg availability, and projected consequences;
- `calendar.py` derives calendar and bounded seasonal effects only from the
  action clock;
- `vessel.py` owns three fixed deck maps, the tavern, named-person schedules,
  bartender stock and drinks, and bounded causal incidents; and
- `living_audit.py` samples route and schedule determinism locally.

Existing `travel.py`, `inventory.py`, `actions.py`, `state.py`, and
`terminal.py` resolve travel, transactional packing, time-bearing actions,
persistence, and presentation. Animation frame creation is pure and does not
mutate state or read wall time. There is no route-generation toolkit, schedule
language, needs simulation, real-time sailing loop, or offline catch-up.

### Route chart and travel

The persistent chart has 12 nodes: four established regions, Reed Anchor,
Charter Market, Ebb Crossing, Coast Refuge, Willow Ferry, Old Lock, Chalk
Steps, and Storm Post. Thirteen required edges keep the network connected;
zero to three seed-derived cross-links make 13–16 total. Each leg carries
travel time, supply cost, cargo and weather exposure, voyage probability,
season closure, hazard description, and optional integrity requirement.
Nodes expose bounded market, resupply, anchorage, hazard, contact, and partial
chart information rather than pretending every stop is a full region.

The full-screen ASCII chart draws water, linked nodes, unknown state, reachable
state, Jomon's marker, selected cursor, legend, and consequence panel. The
cursor follows graph connectivity with arrows, `WASD`, or `HJKL`; `Tab` cycles
route/risk, market/supply, and season/contact layers. `Enter` previews then
confirms one adjacent leg. Left click selects and double-click can confirm when
the terminal reports those events. The animation supplies 5–9 display frames,
can be skipped with Space/Enter/Escape, tolerates resize, and stops at the
deterministic midpoint when a voyage event interrupts. Skip and normal paths
produce identical game state.

### Vessel, tavern, and named adults

Jomon now has three aligned 64×22 decks. The lower deck contains cargo hold,
18×10 locker, bilge, provisions, workshop, repair stores, berths, and hull
access. The main deck contains gangplank, galley, tavern entrance, chronicle,
berths, repair, cargo access, and gathering positions. The upper deck contains
helm, route chart, lookout, signals, rigging, repair, and exposed defensive
positions. Fixed hatches and stairs join all three. The dedicated 64×24 tavern
contains an entrance, fireplace, bar and stools, serving store, five table
groups, seats, and walking space.

The six household adults, six established recruit possibilities, regional
contacts, visitors, and bartender have persistent schedule records. Visible
adults path one tile at a time without stacking or sealing critical paths;
off-screen adults cross bounded activity and location boundaries rather than
simulating every step. Roles, injuries, current region, voyage alarms, and
time-of-day choose activities such as sleeping, eating, drinking, socialising,
standing watch, steering, consulting the chart, repair, cargo work, training,
treatment, and regional work. Loading a save advances nothing.

Sena Quill is a stable named bartender with a biography, memories,
relationships, schedule, seasonal counted stock, and eight drinks. Hearth Ale,
Winter Juniper, Willow Bitter, Miller's Small Beer, Stillroom Cordial,
Smokeleaf Infusion, Reed Tonic, and the rare Ebbglass Measure couple strong
benefits to noise, aiming, fatigue, movement, contact trust, wet crossings,
awareness, or strange memory drawbacks. Measures and bottles consume credit,
stock, and one action; a bottle remains at the bar if its shape cannot fit.

Social resolution is intentionally small. A schedule boundary can cause a
shared meal, assistance, or an argument because of named relationships,
injury, and remembered work. A present argument asks the courier to mediate,
take a side, or allow a bounded fight. The chronicle records the cause and
result. Routine off-screen incidents can alter relationship, memory, and
nonlethal injury but floor health above death; lethal consequences still
require an explicit visible crisis.

### Inventory and dialogue usability

Every authored item has a three-line ASCII preview. The equipment panel places
readied weapon, secondary tool, head, torso, paired arms/hands, legs, feet, and
pack around a paper silhouette while preserving the existing six logical
armour locations. Injury, empty, condition, provenance, protection, weight,
and terrain consequences remain visible through the selected item's details.

Lifting leaves the committed source intact until confirmation. A live ghost
shows rotated cells, valid placement, collision or bounds failure, blockers,
and resulting weight/load. Escape restores the exact opening snapshot. Auto
placement considers both orientations and prefers contiguous free space with
category adjacency. Auto-pack combines compatible stacks, preserves pinned
items, places large awkward shapes first, considers rotation, and either
commits the entire layout or restores it. Multi-select supports one item,
all items, or the focused category; transfer and drop operate transactionally
with counts, total weight, occupied cells, and confirmation for dropping.

The central mouse normaliser uses `mousemask`, `KEY_MOUSE`, and `getmouse` but
silently returns to keyboard-only input when unavailable. In supported Linux
PTYs, left-click selects/places, right-click rotates the ghost, double-click
quick-transfers, Shift-click marks, and the wheel changes paper-doll selection.
Drag and hover are deliberately nonessential and are not implemented because
terminal reports are inconsistent. Dialogue choices consistently show direct
keys, a pointer, current reverse-video selection, semantic colour, unavailable
state, and material requirement; arrows/`WASD`, direct keys, Escape, and mouse
click share one selection path.

### Calendar and migration

The calendar has 36 actions per day, 24 days per season, four seasons, five
readable day periods, spring/autumn equinoxes, and summer/winter solstices. A
seed-derived origin starts early in spring. Accepted actions and explicit
travel are the only source of advancement. Season and daylight currently
affect visibility, route closure, high water/soft banks, exposed shoals,
crosswinds, winter ice and rigging, regional weather/terrain, schedules, and
drink stock. Observances enter the bounded chronicle.

Save format 5 migrates format 4 deterministically. It retains household and
recruits, injuries, relationships, memories, all regions and exploration,
exact item positions/orientations, cargo and markets, contacts/objectives,
opened containers, vessel integrity, and voyage history. It constructs the
route graph, deck/tavern state, schedules, stock, and calendar origin from seed
and recorded action time. A retained format-3 path first applies its prior
migration. Older saves and browser saves remain unsupported.

### Automated verification

Final commands and results:

```text
python -m unittest discover -s tests -v
Ran 126 tests in 174.832s — OK

python -m compileall -q jomon tests
exit 0; no output

python -m jomon.audit
100 seeds / 900 encounter plans; 136 unique compositions; 21/21 archetypes;
502 ranged appearances; 14 elites; 0 invalid groups; 0 unreachable actors;
0 unavoidable opening attacks

python -m jomon.living_audit
100 route seeds; 13–16 edges; 0 disconnected or nondeterministic graphs;
12 schedule seeds / 20 actors each; 0 nondeterministic schedules,
validation failures, or vessel-position overlaps

git diff --check
exit 0; no output
```

Focused coverage includes landing wrapping/resize calculations, limited-colour
and cursor fallback, graph connectivity and connected navigation, keyboard/
mouse parity, animation skip parity, no wall-clock progression, three-deck and
tavern reachability, bartender stock/effects, dialogue semantics, paper doll
and item previews, ghost/collision/rotation/cancellation, auto-place failure,
transactional auto-pack/pins/bulk transfer, schedules and interruption,
bounded catch-up, nonlethal causal incidents, calendar/observances/seasonal
closure, exact format-4 migration, and save/reload.

### PTY evidence actually exercised

The following were run through real `curses` PTYs, not only reducers:

- `/tmp/jomon-diegetic-pty-20260907`, seed `living-vessel-pty`: fresh world,
  80×24 tavern movement, direct conversation with Vela Lark and courier switch,
  locker-to-pack billhook transfer/equip, buckler equip, paper-doll preview,
  route-support preparation, physical tavern exit and upper-deck chart access,
  Hearthford → Willow Ferry → Greenwold travel, Greenwold departure and
  physical return, recruit persistence, save/quit/reload at 100×32, chart mouse
  selection, Greenwold → Willow Ferry animation skip, 70×20 minimum-size
  warning and recovery, and clean terminal restoration;
- `/tmp/jomon-incompatible-landing`: an incompatible format-2 warning centred
  and completely wrapped at both 80×24 and 100×32 with a highlighted new-save/
  return action;
- `/tmp/jomon-bar-season`: eight-drink stock and coloured dialogue, bottle
  purchase into the pack, Hearth Ale consumed, a spring-to-summer action-clock
  transition, seasonal restock, visible schedule movement, item art, save, and
  clean quit;
- `/tmp/jomon-social-incident`: a relationship/memory-grounded argument,
  coloured intervention choices, bounded fight outcome, chronicle evidence,
  and no grave off-screen harm;
- `/tmp/jomon-voyage-*`, seed `voyage-pty-17`: keyboard chart selection,
  normal animated departure, deterministic mid-edge river-creature pause,
  pilot/route-survey evade response, arrival at Reed Anchor, and clean quit;
- `/tmp/jomon-v4-migration`: a handcrafted format-4 save with 21 items, injury,
  memory, and opened Hearthford container loaded, saved, and inspected as
  format 5 with all 21 items and consequences retained plus 20 schedules;
- `/tmp/jomon-mouse-inventory`: actual xterm mouse click lifted a 1×5 spear,
  right-click rotated the ghost to 5×1, click placed it, and Escape restored
  the original vertical committed layout;
- `/tmp/jomon-autopack-bulk`: six manually arranged items were marked, the
  spear pinned, the remainder auto-packed, all six bulk-transferred to the
  locker, committed, saved, and inspected with all six present and the spear
  still pinned;
- `/tmp/jomon-autoplace-failure.*`: a completely full 10×6 pack rejected a
  bottle with the explicit “remains at the bar” explanation and no item or
  credit loss; and
- `/tmp/jomon-lower-deck.*`: `TERM=vt100` first exposed a cursor-control crash;
  after the fix, the same 80×24 low-capability terminal started with bold/glyph
  fallback, physically traversed the main-deck hatch to the lower hold, and
  restored the terminal cleanly.

macOS and WSL mouse reporting were not available and remain unverified. Mouse
drag, hover, Shift-click, wheel, and double-click were normalized or tested as
deterministic events, but only ordinary click/right-click placement and route
selection were exercised through the actual Linux PTY. The forced voyage path
exercised one interrupted leg; the two other voyage families retain their
prior milestone PTY and automated evidence rather than new manual runs here.

### Candid assessment

The chart reads as a map rather than a destination menu: the cursor moves along
visible links, intermediate places have concrete use, route consequences vary,
and revisiting/traversal state remains visible. At 80×24, however, some long
detail lines are ellipsized; wrapping that panel is the smallest improvement
supported by this play evidence.

Manual packing is materially safer and less clerical because every tentative
move is previewed and cancel is exact; pinning plus transactional auto-pack
makes the dense locker manageable without surrendering spatial decisions. The
18×10 locker can still look crowded at 80 columns, but keyboard focus, preview
art, and automatic packing kept it usable in the exercised flow.

The tavern and vessel now feel inhabited in motion: adults occupy tables and
work positions, leave seats as schedules advance, cross decks for duties, and
respond to voyage alarms. The simulation is intentionally coarse. It creates
evidence-backed activity rather than a deep needs model, and the small social
incident vocabulary will become repetitive before the regional content does.
The exercised argument felt causal because its named relationship and memory
were shown before the decision; this evidence does not establish that every
future seeded incident cadence will feel equally natural.
