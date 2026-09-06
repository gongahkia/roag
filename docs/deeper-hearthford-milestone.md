# Deeper Hearthford milestone

## Implementation plan

This owner-play milestone keeps the existing direct Python architecture and
extends the one Hearthford expedition rather than introducing another world
layer.

1. Replace the single regional map with 8–12 persistent compact rooms. Three
   bounded functions create Hearthford's hub, the branching Reedwood, and the
   linear mill route with side rooms. Validate reciprocal exits, objective
   reachability, and the physical route home.
2. Consolidate zero-time courier, weapon, gear, and support selection in the
   tavern `C` popup. Add a small authored equipment catalogue, role techniques,
   finite discoveries, and explicit qualitative combination rules.
3. Place five direct threat profiles, one mixed encounter, environmental
   controls, and a seed-rare machinery encounter in those rooms. Keep movement,
   attack, guard, gear, negotiation, and interaction as the action vocabulary.
4. Persist changed rooms, returned equipment, discoveries, contact memories,
   and a deterministic three-item visiting merchant through the existing
   single JSON save.
5. Add semantic colour roles with glyph fallbacks, suppress routine movement
   messages, and retain explicit intent, pressure, discovery, and consequence
   messages.
6. Add focused standard-library tests, run the full verification commands, and
   exercise the requested paths in a real PTY. Record only verification that
   was actually performed below before the final assessment commit.

The milestone does not add a procedural-generation toolkit, encounter or item
DSL, generalized menus, another settlement, onboard attacks, save migrations,
or broader simulation.

## Assessment

Implemented as a bounded extension of the existing state and action path.

### Region and content

Hearthford now has twelve persistent rooms across three recognizable place
types:

- Hearthford Quay, Market Lane, and Tally House form the hub-and-branches
  settlement. A seed may add the market/tally cross-link.
- Reedwood Gate, Willow Islet, Eel Cut, and Bell-Reed Mudflats form a branching
  wilderness with a required loop and a seed-optional second reconnection.
- Lower Towpath, Crane Walk, Mill Yard, and Broken Wheelhouse form the
  required linear mill route. A Gear Shed, Lime Store, or Rope Loft attaches
  to one of three route rooms as the optional fifth works room.

All exits are reciprocal and physical. Room discovery, removed resources,
opened or closed shutters, moved cover, stabilized structures, and disabled
machinery survive backtracking and saving. Selected seeds vary topology,
side-room identity and attachment, terrain, discoveries, threats, and the
finite relic or elite possibility without disconnecting the wheelhouse or the
route home.

The authored build catalogue contains six weapons (billhook, spear, cudgel,
staff, hand axe, and crossbow), eight secondary items, five crew supports,
one persistent technique for each of the six household roles, five situated
discoveries, and two possible finite relics. The direct combination rules
exercise at least these builds:

- quiet shoes + route survey: quiet surveyed detours;
- billhook + rope: hooked rigging for controls and obstacles;
- buckler + guard's set stance: reinforced positional guard;
- cargo harness + factor surety: protected accountable cargo;
- repair tools + carpenter rig: quiet, stable control work;
- field care + healer's field binding: reduced and bound injury;
- cargo harness + porter watch: the largest bounded load; and
- trade seals + factor's measured terms: witnessed negotiation.

Equipment returned to Jomon remains owned. Consumables and relics decrement
when used; defeated couriers can lose readied gear and cargo unless a relevant
support or combination protects it. Discovered techniques stay with their
courier. The occasional deck merchant appears on a deterministic three-return
cycle with a seed-derived phase. Its three lots derive from the current
shortage, prior objective result, and a bounded finite-item roll; purchases use
trade credit and persist.

Five threat profiles keep the existing commands but change decisions: a direct
pursuer, a two-cell spear opponent, a telegraphed ranged watcher, a territorial
reed boar, and alternating mill machinery. The Mill Yard pairs reach and
ranged opponents. Human threats accept witnessed material terms; the boar can be
positioned into mud or redirected with a hooded lantern. Reed gates, towpath
shutters, movable cargo cover, the crane sluice, unstable works, mud, and the
wheelhouse controls affect combinations of pursuit, aim, noise, route safety,
cargo, objective state, or injury. The seed-rare runaway crown wheel also
floods its aisle and threatens cargo, so its danger is about staging, timing,
guarding, and material shutdown rather than health alone.

Semantic rendering assigns player yellow/bold, allies cyan/bold, neutral
people green, hostiles red/bold, elites and rare effects magenta/bold, water
blue, structures muted white, exits cyan/bold, resources yellow/bold,
interactables green/bold, and hazards red/bold. A pure mapping returns bold
glyph cues with no colour-pair assumptions when fewer than eight colours or
usable pairs exist. Routine safe movement emits no message; room discovery,
pressure changes, hostile intent, material actions, injuries, objectives, and
persistent consequences retain priority in the bounded log.

### Automated verification

The final commands completed successfully:

```console
python -m unittest discover -s tests -v
python -m compileall -q jomon tests
git diff --check
```

The unit suite ran 34 tests with `OK`. It covers deterministic and varying
topology, reciprocal/reachable routes, persistent room changes, semantic
+ fallback, zero preparation, six qualitative combinations, action
time and log priority, pressure effects, all threat profiles, mixed and elite
encounters, combat and non-combat resolutions, cross-system environment use,
contextual injury/loss/death/succession, capacity and market change,
discoveries, merchant exchange, save rejection/equivalence, and a changed
second expedition.

### PTY verification

The following was actually exercised in curses, not inferred from tests:

- At 100x32, a fresh `owner-play-2` world used the consolidated tavern to
  select a bargemaster, billhook, repair tools, and carpenter rig; departed,
  altered the contact's request, traversed and backtracked through the room
  families, shut the Reedwood gate on a pursuer, bogged the boar through mud,
  blocked readable crossbow aim with a shutter, won direct combat, stabilized
  the crane, met the mixed Mill Yard group, and took a contextual injured
  retreat that lost repair tools.
- At 80x24, fresh `complete-run` play used a factor/st repair build, acquired a
  pulley key from the optional Rope Loft, negotiated the ranged watcher and
  mixed group, and reached the wheelhouse. This exposed two play defects:
  ordinary guarding was too restrictive, and machinery damaged the courier
  before its spatial danger was clear. Both were fixed and regression-tested.
- A continuation from an explicitly constructed wheelhouse checkpoint then
  verified the repaired safe staging bay, guarded aisle timing, material
  machinery shutdown, complete physical backtracking, contact delivery,
  gangplank return, deterministic three-lot merchant, two purchases, save,
  reload at 100x32, retained purchases, and a changed later tavern build. A
  second departure and physical return confirmed the changed world continued.
- A final uninterrupted fresh `final-run` world at 100x32 repeated tavern
  preparation, gangplank departure, objective alteration, gate and mud
  evasions, movable cover, readable ranged aim, shutter use, material
  negotiation, crane repair, full backtracking, contact completion, gangplank
  return, save, clean quit, and terminal restoration after the two fixes.
- A seed-selected elite checkpoint (`elite seed 6`) displayed the magenta
  runaway crown wheel, its west staging bay and alternating east-aisle intent,
  guarded passage, and repair-tool/carpenter-rig shutdown.
- One live session was resized from 100x32 to 70x20 and then 80x24. It showed
  the minimum-size message at 70x20, redrew the startup menu at 80x24, and
  restored the terminal cleanly on quit.

Semantic colours were visually checked at 80x24 and 100x32. The limited-colour
fallback was tested as a function pure mapping but could not be visually verified
because the available PTY advertised normal colour support. Permanent death
and succession were verified by state/action tests, not induced during the
manual play paths.

### Assessment

The expedition is materially longer: the critical journey alone crosses the
settlement, wilderness loop, and four-room works route, with optional branches
and physical backtracking. Builds are more distinct because selection changes
route noise, reach, guard, negotiation, machinery, injury, capacity, loss, or
finite-resource decisions rather than only small numeric bonuses. The room
maps remain deliberately compact and some return travel becomes repetitive;
that pacing, not another framework or content layer, is the smallest issue
that needs owner play next.
