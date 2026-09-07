# Regions, inventory, and encounter-variety milestone

## Outcome

The milestone is implemented and locally verified. Jomon now has four
persistent seamless regions, a physical populated tavern, a rotatable spatial
inventory, six-location armour, terrain exposure, bounded perceptual enemy
goals, fair long-range combat, region-specific mixed encounters, six regional
recruits, and three sporadic voyage situations. The architecture remains direct:
one `GameState`, authored tables, four small region builders, utility selection
over concrete enemy actions, one curses renderer, and one versioned JSON save.

Work began from a clean
`f1c397f033e2895ca35e5f0b0dedb9d8bba8a53e`. Two concurrent commits named
`thepeopleyoullmeetthethingsyoull` appeared during the tranche:
`4f618f3` after the inventory phase and `8e465e1` after the voyage phase. Both
were preserved without reset, amend, or rewrite. During the pause,
`origin/main` also advanced to `8e465e1`; this work did not push it.

## Implemented shape

The main new ownership boundaries are intentionally narrow:

- `inventory.py` owns item shapes, placement, equipment, protection, load, and
  terrain-status rules;
- `people.py` owns physical tavern positions and voluntary recruitment;
- `enemy_ai.py` owns limited perception, goal scores, path choice, and group
  alerts, while `actions.py` executes the concrete actions;
- `encounters.py` composes bounded authored regional roles and provides the
  offline audit;
- `regions.py` contains three dedicated generators rather than a biome
  framework; and
- `travel.py` contains destination travel and three voyage resolutions without
  a separate naval game.

No planner, encounter language, item scripting language, global economy,
offline simulation, alternate renderer, or generic content framework was
introduced.

## Physical inventory, armour, and builds

The courier pack is 10×6 cells and Jomon's locker is 18×10. Every physical
item has a stable ID, dimensions, orientation, weight, category, condition,
provenance, and exactly one location: pack, locker, readied/body slot,
container, ground, enemy possession, lost, or destroyed. Inventory cursor work
is zero-time. A confirmed threatened repack is one action. Escape restores the
opening transaction, and failed ranged preparation now restores the previous
weapon and packing rather than applying half a loadout.

The build catalogue contains:

- 14 weapon families: billhook, spear, cudgel, staff, hand axe, windlass
  crossbow, longbow, sling, heavy crossbow, pike, paired knives, javelins, war
  hammer, and weighted net;
- eight secondary tools or approaches and five crew preparations;
- six persistent household techniques plus six recruit techniques;
- exactly 18 armour pieces, three alternatives at each of head, torso, arms,
  hands, legs, and feet;
- 30 stackable passive discoveries, including 18 added here;
- 15 authored supplies/discoveries, including arrows, stones, quarrels, nets,
  treatment, light, smoke, keys, wedges, and trade records; and
- five finite relics, of which ebbglass spindle, coalheart seed, and hollow-bell
  shard are the three new region-sourced possibilities.

Armour distinguishes cut, pierce, and blunt protection plus coverage, noise,
mobility, condition, and material terrain tags. Context chooses among six hit
locations; head, arm, hand, leg, foot, and torso injuries persist and affect the
relevant actions. Load is separately reported as light, laden, encumbered, or
overloaded. It changes action cost, noise, retreat, climbing, current risk,
fragile footing, and defeat loss. Wet water-heavy armour adds physical burden.

There are 21 explicit build combinations. Ten added regional interactions are
covered together in tests, including accounted ebb, buoyant cargo rig,
wind-read aim, masked smoke passage, weatherfast grip, crosswind decoy,
thorn-held momentum, quiet scree step, high sling arc, weighted floor brace,
and directed fall. These sit alongside mobile hook, shielded set stance,
controlled breach, smoke walking, flood rig, deep field binding, valuable
leverage, and high-ground drive.

Terrain can apply bogging, wetness, poor footing, cut feet, thorn scratching,
smoke inhalation, salt grit, and deep-current exposure. Suitable boots, waders,
arm/hand cover, head protection, load, and existing injuries materially change
those results. Status causes, consequences, and remaining action-clock duration
are saved and shown without repeating low-value prose every step.

## Physical Jomon and people

Jomon's 48×16 aboard map now has a walkable tavern with a bar doorway, tables,
six seated household adults, and visible persistent visitors. Speaking beside a
person opens their dossier and is the normal route to switching courier or
offering a voluntary berth. The bar chooses only crew support; the spatial
inventory handles equipment. The route chart at `P` selects a destination.

The household begins with the same deterministic six adults. Six original
region-linked adults can visit and join, two from each new region, subject to a
nine-adult berth limit and witnessed regional or credit terms. Invitations may
be deferred. Recruits retain their technique, affinities, relationships,
memories, injury, death, and succession behavior. Each new region also has two
named material contacts; not every named adult is recruitable.

## Regions and causal generation

All regions use levels `-1`, `0`, `1`, and `2` in common world coordinates.
Every added region has five named geographic zones, five vertical links, six
persistent named containers, two contacts, seven placed threats, a material
objective, and an action-clock regional process.

| Region | Footprint | Generator and traversal identity | Major landmarks | Organic process |
|---|---:|---|---|---|
| Hearthford Low Wood | 96×54 | smoothed wetness/elevation with carved river, road, settlement and mill templates | quay, settlement, floodplain, watch, ruin, culvert/cave, multi-level millworks | fog, rain, mill water and material timing |
| Greywash Tidal Reach | 104×56 | constrained moving shoreline, channel, three tide-parallel route bands and dunes | salt village, pans, wreck flat, sea cave, signal mast, chain house | working tide covers the low wreck road |
| Greenwold Charcoal March | 100×58 | cellular canopy clusters opened into connected clearings and three recut authored trails | village, open woodland, resin yard, root cellar, raised burnworks, watch tree/canopy | wind carries burn smoke across levels and changes salvage |
| Whitecairn Limestone Rise | 98×60 | terrace bands, switchbacks, quarry loop and aligned cliff structures | village, quarry, kiln, sink cave, ridge bridge, bell tower | rockfall blocks the direct stair while a cave loop remains |

The causal order is terrain and climate → work and settlement form → material
shortage → contacts and objective → hostile interest and patrols → optional
treasure → persistent market, relationship, and geographic consequence.
Critical landmarks, containers, vertical links, and return paths are validated.
PTY work found a Greenwold seed where closed canopy separated required content;
the fix recuts its three authored trails after structure placement. The same
seed is now a regression fixture, and the 100-seed audit builds every region.

Closed containers are bright `C` glyphs and opened ones are `o`. The 18 new
regional containers each expose three physical rewards: one build item, one
armour piece, and one supply. Ordinary route-side stores teach the glyph before
rope, light, key, cave, canopy, wreck, and parapet caches ask for more risk.

## Enemy goals, range, and encounter composition

The three new regions have 18 standard archetypes and three elites. An actor
uses only current line-of-sight, audible events, a bounded last-known position,
a group alert, its own injuries, morale, ammunition, nearby allies, home,
patrol, and material duty. A short deterministic breadth-first search handles
obstacles and authored z-links. Lost actors investigate the recorded origin and
then return to patrol or guard; they do not fall through to hidden courier
coordinates. Repeated blocked prose is suppressed.

Concrete behaviors include alarm and shared last-known positions, physical
protector interception, visible side flanking, a telegraphed net cell, cargo
theft followed by escape, territorial/sound tracking, morale retreat, patrol,
smoke/water displacement, mobile ranged withdrawal, suppression, reload, and
finite ammunition. Longbow, sling, heavy-crossbow, lookout, skirmisher,
suppressor, protector, flanker, controller, thief, animal, and elite plans have
different ranges or decisions. Ranged awareness always aims or sets up before a
severe shot; cover, movement, smoke, weather, elevation, ammunition, and injury
affect the lane.

The elites change rules rather than only health:

- Greywash's storm-chain captain telegraphs then floods three marked flat cells;
  dogging the tide windlass removes that control;
- Greenwold's ash-cloak warden lays a three-cell smoke line; redirecting the
  shutters strips the plan; and
- Whitecairn's false-bell master marks a cell before rockfall converts it to
  scree; seating the quarry braces denies the release.

The Whitecairn brace shares a hoist coordinate. PTY play found that climbing
masked the control; the first unused interaction now operates the brace, and a
later interaction uses the aligned ladder.

## Travel, time pressure, and persistence

Changing region at the chart costs six world measures and preserves the region
being left. A deterministic but sporadic check based on seed, route, weather,
cargo, and voyage count selects no event or one of three families: cargo-seeking
raiders, a territorial broad-backed river grazer, or a rare finite mineral
resonance that mimics familiar voices from the wrong bank. Each has three
material, navigational, equipment, cargo, or social responses. These reuse
ordinary crew, inventory, injury, and cargo rules.

Tide, burn wind, quarry instability, weather, sound aging, patrols, objectives,
and pressure advance only through accepted actions. New-region squalls and rain
now reduce both enemy and courier sight. High pressure wakes a dormant stronger
threat and expands alert/pursuit rather than only adding health. Healing,
ammunition, rope, light, smoke, nets, and treatments remain finite.

Save format 4 stores every regional map/change, seen terrain, container and
item state, exact grid orientation, armour condition, statuses, enemy state,
people and tavern position, active travel, contacts, markets, and history. A
deterministic format-3 migration preserves people, injuries, relationships,
succession, Hearthford state, cargo, equipment, discoveries, markets, and
contacts. It places what fits into active slots/pack and moves overflow to the
bounded locker; tests force real overflow and account for every item. Other
legacy formats and browser saves remain unsupported.

## Automated verification

All required commands passed from repository root after integration:

```text
python -m unittest discover -s tests -v
Ran 92 tests in 39.012s
OK

python -m compileall -q jomon tests
exit 0

python -m jomon.audit
samples 100; plans 900; all 21 archetypes represented
136 unique compositions; most repeated composition 49
ranged actor frequency 502; elite frequency 14
invalid/forbidden 0; unreachable actors 0; unavoidable opening attacks 0

git diff --check
exit 0
```

An earlier integrated run was 89 tests in 42.866 seconds before the final three
role/weather regressions.

## Real PTY verification

All sessions below launched the actual application through `python -m jomon`
under an allocated `xterm-256color` PTY. Key input was automation-assisted so
long routes were reproducible; it still exercised startup, curses rendering,
keyboard dispatch, overlays, action resolution, physical return, save, quit,
and terminal teardown. No reducer-only run is presented as PTY evidence.

Eight complete expedition saves record physical return and completed objectives:

| PTY data directory | Region / courier | Build | Final world time |
|---|---|---|---:|
| `/tmp/jomon-pty-expedition` | Hearthford / Risa Vale, guard | spear, rope, field care | 265 |
| `/tmp/jomon-pty-grey-bow` | Greywash / Kelan Moss, pilot | longbow, quiet shoes, route survey | 497 |
| `/tmp/jomon-pty-grey-net` | Greywash / Dena Wren, guard | weighted net, cargo harness, porter watch | 266 |
| `/tmp/jomon-pty-green-knives` | Greenwold / Tavi Gull, pilot | paired knives, smoke pot, route survey | 250 |
| `/tmp/jomon-pty-green-control` | Greenwold / Jora Barrow, healer | staff, hooded lantern, field care | 226 |
| `/tmp/jomon-pty-white-hammer-light` | Whitecairn / Kelan Keel, carpenter | war hammer, repair tools, carpenter rig | 280 |
| `/tmp/jomon-pty-white-sling-clean` | Whitecairn / Bera Ford, factor | sling, quiet shoes, route survey | 201 |
| `/tmp/jomon-pty-voyage-expedition` | raider voyage then Greywash / Dena Elm, bargemaster | pike, rope, route survey | 252 |

Those paths crossed settlement and quiet outdoor ground, accepted the regional
request, reached cargo on another level where applicable, survived the full
regional process, returned over the map, crossed Jomon's gangplank, saved, and
quit with status 0. Saved exploration ranges were 1,941–3,068 tiles. Greywash
covered ground and upper chain works after the tide closed the low road;
Greenwold covered open woodland and the underground root objective; Whitecairn
covered terraces, scree, switchbacks, and the sink cave. The voyage run resolved
boarders by pike before its complete Greywash expedition.

Additional real-PTY checks completed:

- 100×32 fresh-world startup, physical tavern walking, two dossiers, courier
  switch, zero-time support selection, and clean quit;
- 80×24 physical walk through the corrected bar doorway, voluntary recruitment
  of Jessa Flint, save/reload confirmation, and visible semantic colours;
- pack/locker transfer, six worn armour slots, longbow readied, rotation at the
  boundary, a rejected horizontal placement with the item still held, safe
  replacement, and zero-time aboard confirmation;
- visible sharp-ground and mud statuses without protection, and absence of the
  same status with hobnailed boots or marsh waders;
- a 31/34 encumbered load taking the slower movement path, a 48/34 overloaded
  six-heavy-piece load, and a refused bell-tower climb;
- deterministic hostile hits to head, arms, legs, and feet, with concussion,
  cut arm, strained leg, and wounded foot shown in the body panel;
- lookout alarm plus longbow aim, longbow fire while withdrawing, sling lane
  suppression, heavy-crossbow hit/reload, a close hunter yielding distance,
  sound investigation without current-position knowledge, cargo theft and
  escape, morale retreat, protector interception, side flanking, and a net cell
  avoided by reposition;
- all three regional elite rule sequences and all three material controls;
- six ordinary named containers plus underground sea-cave, canopy, and parapet
  caches; a full pack left three rewards visibly in a coffer; a witness token
  was deliberately dropped at Greywash landing, returned without, and remained
  saved on the ground;
- deterministic forced creature and rare lure voyages through their actual
  overlay, followed by save/reload at Greenwold and Whitecairn respectively;
- a format-3 fixture loaded through the startup menu, saved as format 4, and
  retained courier and discoveries;
- a fatal contextual hit returned play to Jomon, selected a living successor,
  saved the death, and reloaded it; and
- live resize from 100×32 to 60×20 showed the 80×24 minimum message, resizing
  back recovered the game, and quit restored the terminal with status 0.

Every completed/controlled PTY process used `curses.wrapper()` and emitted the
terminal alternate-screen restoration sequence. No network or external service
was used.

## Candid assessment and remaining limits

The representation now reads as four different geographic places rather than
four palette-swapped dungeons: shoreline bands and tide routes, woodland
clearings and canopy, terraces and switchbacks, and Hearthford's floodplain/mill
all produce different large-scale movement. The direct objective paths are
longer and contain quiet travel. They can still feel sparse when the courier
takes the safest line, and return remains deliberate backtracking.

The longbow/quiet route, net/cargo route, paired-knives/smoke route, heavy
tool/control route, and light elevated sling route made different positioning,
ammo, packing, terrain, and risk decisions in PTY play. This is enough to call
the builds mechanically distinct. It is not evidence that every combination is
equally balanced; the objective can sometimes be completed while avoiding the
encounters that most strongly express a build.

Treasure is now recognizable once seen: the glyph, name, contents, physical
fit failure, transfer, open state, and persistence all read correctly. However,
the eight ordinary complete routes naturally opened only three distinct caches
(Collapsed cottage coffer, Quayside salt coffer, and Greenwold medicine chest).
The other named containers were verified through deliberate optional-location
PTY sessions. The smallest improvement justified by play is therefore one
authored contact or landmark clue to an optional named cache in each region,
plus placement tuning where necessary. That should be tested before adding a
fifth region or more systemic machinery.

No post-milestone subjective owner play has occurred, so enjoyment remains for
the owner to judge. The Capcom manual URL supplied as a spatial-inventory
reference returned HTTP 403 in this environment; its page content was not
independently verified. Creature and lure voyages were deterministically forced
for PTY coverage because they are intentionally sporadic; only the raider event
occurred in a complete voyage expedition. Not every one of the 18 standard new
archetypes appeared organically during the eight complete routes, although all
role mechanics were covered by PTY scenarios and all compositions by tests and
the audit.
