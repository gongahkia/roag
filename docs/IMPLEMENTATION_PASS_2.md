# Implementation Pass 2: expeditions and exploration

This pass starts from `a6af6a3`. It rebuilds objectives, terrain, landmarks,
patrols, and exploration information while leaving bosses, progression,
difficulty modes, and broad card content for later passes.

## Verified baseline

- `main` and `origin/main` both point to `a6af6a3`; the working tree is clean.
- Content validation passes and the warning-enabled standard-library suite passes
  134 tests in 232.5 seconds on Python 3.14.
- Every layout has twelve fixed anchors. Spine and zigzag share a linear graph;
  branching and clusters share the same graph. Only ring has no mandatory
  articulation room between arrival and the Core.
- Terrain is drawn with biome and decorative glyphs, but movement cost comes
  from the nearest room's biome. The `,`, `=`, and `~` patches therefore make
  claims the rules do not honour.
- Every objective is one immediate choice between a supply payment and a forced
  consequence. It has no stage, approach, landmark, persistent outcome, or map
  relationship beyond its coordinate.
- Hazards are two one-cell, one-shot penalties per selected biome. Patrols share
  guard, hunt, roam, or erratic movement with cadence, aggression, and leash
  changes. Static pickups are globally drawn; dynamic patrol and hazard display
  is radius-limited independently of terminal size.
- Generation and save validation hard-code twelve rooms, twelve pickups, four
  one-cell objectives, and eight one-cell hazards. Save version is 13 and
  content schema is 8.

## Biome decision matrix

The implementation uses shared terrain, hazard-zone, staged-objective,
facility, landmark, perception, and patrol primitives. The identity column is
the decision the biome must repeatedly ask, rather than a cosmetic rule.

| Biome | Existing fiction and overlap | Intended spatial / information identity | Hazard and counterplay | Patrol / facility / combat | Objective and recorded outcome |
| --- | --- | --- | --- | --- | --- |
| Derelict Decks | open machinery; direct damage overlaps Ossuary | choose short rubble cuts or reliable deck lanes; ordinary visibility is the control case | unstable debris fields collapse into costly rubble; a junction can clear one field | sweepers follow lanes; patch bay trades a supply for light and cleared debris; loose cover favours block sequencing | splice two relay nodes in either a short exposed order or insulated long order; record `bridged` or `insulated` |
| Cryogenic Vaults | frozen galleries; universal vulnerability overlaps Foundry | fast slick ice lanes cross slower insulated edges; fractures are visible early | multi-tile coolant fractures weaken and add next-combat brittle pressure; thaw controls suppress one zone | sentries orbit vault landmarks; thaw station spends light to disable a fracture or harvests a supply with stress; combat gives dodge then punishes wounds | recover a key then thaw or shatter its return manifold; record `thawed` or `shattered` |
| Hydroponic Canopy | rooted passages; reversal overlaps Flooded | root mats are slow, maintenance trellises are longer but safe; close static discovery range | wirevine zones pull the route to a stop and wound only on barbed tiles; pruning console clears a route | territorial grafts guard nutrient sites; nutrient vat converts light into healing or reveals growth paths; combat roots front and back rather than reversing all ranks | poll two root memories or cut a direct sample and carry it; record `consulted` or `harvested` |
| Ash Foundry | slag islands and heat; vulnerability overlaps Cryogenic | hot slag is direct but accumulates heat pressure, shielded rails detour | vent footprints drain light and prime enemy focus; coolant control suppresses vents | interceptors contest crossings; quench station spends supply to cool terrain or trades HP for a card upgrade; combat alternates crew/enemy heat advantage | temper an access tooth through two presses or steal a hot blank through one dangerous crossing; record `tempered` or `stolen_hot` |
| Reactor Choir | charged arteries; marks and aggressive hunters | powered conduits are quick but escalate patrol alert, grounded lanes cost more | gamma arcs mark and raise local alert; breaker panel can ground arcs | hunters become more aggressive after powered travel; breaker redirects a patrol or yields supplies at a light cost; combat rewards deliberate marked counterplay rather than a flat enemy focus | align two control votes or overload one bus and escape its arc; record `regulated` or `overloaded` |
| Mycelial Warrens | living pockets; wounds overlap Hydroponic | soft growth is fast but obscures unknown sites; sterile strips are known and longer | recurring spore beds wound but wither after cleansing or facility use | erratic puppets migrate between blooms; culture bench converts a curse/stress risk into healing or suppresses spores; combat makes wounded actors dangerous rather than simply weakening crew | culture samples at separated beds or inhale one volatile bloom; record `cultured` or `inhaled` |
| Flooded Bilges | deep water and causeways; reversal overlaps Hydroponic | deep water is very costly while narrow causeways are fast patrol chokepoints | pressure-sump zones consume supplies or add stress; pumps convert water tiles into causeway | heavy guards orbit pumps and avoid deep water; pump room drains a route or salvages supplies while flooding another local patch; combat moves one flank with undertow | raise a locker through pump and retrieval stages or blow it open across a short flooded route; record `equalised` or `breached` |
| Ion Stormworks | wind-scoured conduits; light drain | east/west wind direction changes real tile cost, so return routes differ | static fronts migrate among a bounded footprint and drain light; grounding masts suppress or relocate them | sweep patrols move with the front; capacitor station stores light for later or spends it to reveal patrol positions; combat grants draw at an energy constraint | ground two pulse nodes with the wind or hold one live mast through local travel ticks; record `grounded` or `held_live` |
| Impossible Archive | indexed aisles; opening-hand loss | indexed aisles expose static sites but contradictory redactions change selected tiles | redaction zones hide their exact tile until indexed and reduce the next opening hand; catalogue terminal reveals and rewrites one | custodians guard objective stages in a predictable circuit; revision desk reveals all sites or transforms a card at a resource cost; combat presents a discard/draw constraint | visit clauses in declared order or forge one signature then file it elsewhere; record `amended` or `forged` |
| Null Expanse | sparse structure; stress pressure | stable bridges are longer; null gaps are short, costly, and forget mobile intel | gaze zones add stress and temporarily erase patrol intel; an anchor restores remembered intel | ambushers wait outside perception then approach on alternate ticks; grief anchor trades light for calm or pins a hazard; combat trades stress for energy with visible risk | focus lens fragments in sequence or carry one absent navigator across a null gap; record `focused` or `carried_absence` |
| Ossuary Engine | bone crypts; weakest-hero damage overlaps Derelict | rib lanes are quick but blood gates tax wounded crews; service aisles detour | marrow grooves apply wounds only to already injured travellers and can be sealed | processions orbit crypt landmarks; marrow pump trades healing for stress or seals grooves; combat rewards riposte while enemy bone cover decays | prime two dead credentials with a controlled transfusion or open one blood gate immediately; record `transfused` or `bled_open` |

Every objective begins at a recognisable three-by-three landmark, then exposes
one or more seeded stage sites after the player chooses a fully described
approach. Both approaches name their resource, danger category, travel burden,
irreversibility, and expected combat or patrol pressure. No approach requires a
specific archetype, and living-party count is never an eligibility gate.

## Layout matrix

Twelve anchors remain in this pass because encounter/reward opportunity is
currently balanced around ten non-boss rooms, but the quota is no longer used
as the source of layout identity. Topology, terrain bands, landmark placement,
and patrol circulation provide distinct decisions.

| Layout | Verified baseline | Intended route decision | Acceptance property |
| --- | --- | --- | --- |
| Branching | three small diamonds with four mandatory articulation rooms | hub-and-spoke sorties from two safe junctions; objectives occupy different spokes | at least three leaves and a reusable central hub; two objectives need not share their final approach |
| Spine | one eleven-edge chain; every middle room mandatory | long exposed main route with sparse costly bypasses around patrol checkpoints | long primary path, at least two bypass cycles, and more articulation than ring |
| Ring | two arcs plus one middle chord; no articulation | choose either circulation direction or a contested central crossing | two arrival-to-Core routes and no mandatory middle anchor |
| Clusters | same graph as branching with different coordinates | dense local loops joined by two narrow inter-cluster bridges | three cyclic clusters and bridge articulation between them |
| Zigzag | same linear graph as spine | serpentine safe route versus short cross-fold links exposed to patrol interception | several geometric shortcuts and fewer articulation anchors than spine |
| Fracture | four cycles around a high-degree centre | peripheral safer crossings versus a short contested central shard | high-degree central node plus at least two routes that avoid it for part of the journey |

Layout profiles will also define objective spread and patrol circulation labels
shown in inspection. They will not silently change combat numbers.

## Shared structures and save state

- A tile's glyph determines its terrain definition and travel cost. Biome base
  glyphs, rubble, causeways, and explicit special tiles are generated with
  honest names, costs, and optional entry effects. Route preview sums those
  exact tiles and reports expected light use.
- `Landmark` stores a validated template ID, biome, anchor, footprint, discovery,
  and bounded state. Landmark art is an overlay and cannot block connectivity.
- `BiomeHazard` stores a multi-tile footprint, per-tile trigger history,
  active/suppressed state, and telegraph state. Known route hazards can be
  counted; deliberately hidden tiles remain only a qualitative unknown.
- `BiomeFacility` stores its biome, position, charges, and chosen persistent
  effect. Facilities manipulate existing resources, terrain, hazard, patrol,
  card, or status state through a small validated effect vocabulary.
- `AccessObjective` stores its approach, stage, ordered stage sites, outcome,
  optional completion, and bounded facts. Content owns the labels and effects;
  the engine owns progression and validation.
- Patrols store doctrine and bounded doctrine state. Reusable doctrines are
  territorial, orbit, sweep, intercept, migrate, hunt, and ambush.
- Static feature knowledge is stored by ID. Terrain remains generally readable;
  dynamic patrol positions and active hazard detail use simulation perception,
  never viewport dimensions. A large terminal can draw more known space but
  cannot acquire more simulation knowledge.
- Content schema and save version advance together. Older saves fail with the
  existing explicit incompatibility error. All random selection uses either the
  serialized engine RNG or a named seed-derived generation stream.

## Atomic implementation sequence

1. Record targeted research that materially constrains prefab placement,
   transient mobile intel, exact route previews, and topology-first layouts.
2. Introduce validated terrain profiles and make glyphs, cost, inspection, and
   weighted pathfinding agree; bump schema/save versions with round-trip tests.
3. Add terminal-size-independent static knowledge and route-risk summaries.
4. Add validated landmark templates and seeded, connected placement.
5. Add staged objective state and telegraphed two-approach content primitives.
6. Add persistent hazard footprints and suppression counterplay.
7. Add biome facilities and their small shared effect vocabulary.
8. Add patrol doctrines and objective-driven escalation.
9. Rework each biome as an independent content/rules commit with focused tests.
10. Revise branching, spine, ring, clusters, zigzag, and fracture independently,
    asserting their topology and completion-path properties.
11. Rework biome-bound events only where they alter current spatial state.
12. Integrate objective/route/terrain/facility inspection at 80x24 and verify
    equivalent simulation knowledge on a larger terminal.
13. Reproduce seed 42, tune only evidenced pacing defects, then run the broad
    seed sweep, full suite, warning-enabled compilation, and PTY play matrix.

## Acceptance criteria

- Four biome objectives remain reachable and any two open the Core. Each has
  two disclosed approaches, real map stages, a serialized outcome, and remains
  completable by a reduced party.
- Every selected biome places a recognisable landmark, multi-tile hazard with
  counterplay, useful facility, readable patrol doctrine, honest terrain, and a
  combat condition that changes tactics rather than duplicating another biome's
  primary rule.
- Every movement cost derives from the traversed glyph/profile. Route preview
  reports weighted cost, expected light, known hazards, and perceived patrol
  risk without leaking hidden hazards.
- All six graphs satisfy distinct documented topology properties while world
  terrain stays connected and two objectives plus the final path stay reachable.
- Static memory and dynamic perception round-trip and produce the same known
  information at 80x24 and larger terminal sizes.
- Content validation, focused state-transition tests, all-layout/all-biome seed
  sweeps, full tests, compilation, diff checks, and required real-PTY scenarios
  pass. Three natural runs are reported without debug assistance, including
  failures and unresolved pacing uncertainty.

## Pacing checkpoint

A deterministic 60-seed route audit measured the cheapest travel-only route
from arrival through any two objective approaches to the Core. The median was
185.5 weighted ticks, the 90th percentile 213, and the range 133–259. At the
original two-tick cadence this consumed a median 92.5 light and 106 light at
the 90th percentile before combat-room detours, hazards, or discoveries. Seed
42 required 199 ticks, or 99 light, on its best route.

The cadence is therefore three travel ticks per light. The same route sample
costs a median 61.5 light and 71 at the 90th percentile. Low-light ambush,
stress, extra reward choice, supplies, and biome light costs remain unchanged;
this is a bounded correction to cumulative traversal pressure, not a general
difficulty reduction. Natural play remains necessary to evaluate the result.
