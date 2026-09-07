# Regional questlines, tactical encounters, and build content

## Baseline

Work begins from clean `main` commit
`0af193e4b9882d44d5227bea0f0cca112834d19d`, equal to `origin/main`. The
unchanged standard-library build ran 129 tests in 277.165 seconds with no
failures. The older 126-test figure in the living-vessel assessment describes
an earlier point in that milestone; the active tracker correctly reports 129.

The game already has four persistent regions, 26 containers, 14 weapon
definitions, 18 armour pieces, 30 passive definitions, six regional recruit
techniques, five relics, eight drinks, perceptual enemy state, a regional
encounter composer, and a spatial physical inventory. Repository inspection
shows that definition count currently exceeds ordinary-play depth: some
effects are descriptive, the composer is audit-facing rather than used for
normal placement, treasure clues are sparse, objectives share one cargo
shape, and several physical-state reconciliation paths need regression tests.

## Bounded implementation sequence

1. Repair recoverable objectives, physical possession reconciliation,
   transactional cancellation, finite consumable/ammunition authority,
   selected relic use, reconstructed environmental state, and format-5 to
   format-6 migration.
2. Give every retained passive and regional recruit technique an observable
   production hook; expose bounded armour and status trade-offs; make the pike
   and heavy crossbow obtainable.
3. Use the existing regional budget composer for persistent finite production
   groups and implement perceived, readable enemy goals plus explicit ranged
   target selection and constrained negotiation.
4. Author one three-stage material questline per existing region, activate
   secondary contacts and causal recruitment terms, then unlock one four-to-six
   chapter cross-region arc after meaningful progress in two regions.
5. Add one alternative rule-changing elite per region, improve three distinct
   treasure clues per region, and add only bounded weapons and rewards that
   complete twelve verified cross-system builds.
6. Verify serially with focused tests, the full test suite, compilation,
   whitespace checks, encounter/quest seed audits, and real curses PTYs at
   80×24 and 100×32. Record only branches and builds actually exercised.

## Architectural and content limits

`GameState` remains the persistence root. Quest progress is a small set of
authored records and direct regional actions, not a quest engine. Enemy
decisions continue to select from concrete legal actions using perceived
facts, not a planner or omniscient controller. Encounter composition remains
bounded by the four authored regional pools. No region, faction simulator,
economy simulation, real-time advancement, network dependency, generic DSL,
or unlimited magic is added.

The new-world bargemaster remains equipped at Jomon's gangplank and can enter
Hearthford immediately. Deeper systems are discoverable choices, never
mandatory preparation chores.

## Implemented state integrity

Format 6 now has a deterministic format-5 migration. It preserves people,
relationships, deaths, regional exploration and processes, physical item
identity and placement, equipment, markets, contacts, objectives, route state,
calendar, schedules, and cargo. The river-glass ward becomes a physical item
without inventing a replacement for an item already recorded as lost.

The pack is authoritative for finite arrows, bolts, stones, javelins, powder
charges, and bottled drinks. Relics carried together have an explicit selected
item. Inventory cancellation restores both panes, containers, equipment,
ownership, ammo mirrors, credits, passive derivation, and all other touched
state. Lost objective cargo may be recovered from its physical location,
replaced once from the material source, resolved through the authored material
alternative, or reported as a consequential failure. Death leaves possessions
at the recorded defeat position; a defeated thief drops its stolen item, while
an escaped thief records that exact item as causally lost.

Greywash water, Greenwold smoke, and Whitecairn instability reconstruct their
physical tiles from persistent process stage after load and travel. No general
fluid or terrain simulator was added.

## Regional quests and Working Marks

| Region | Quest | Mechanical branch and persistent result |
|---|---|---|
| Hearthford | The Mill Race Compact | public sluice access keeps the mill door and wetland bypass open; a private charter changes repair obligation and market control |
| Greywash | The Ledger Beneath the Ebb | delay for the safer route or take the exposed tide window; the ending reallocates salvage custody and route risk |
| Greenwold | A Fire Kept to Its Bounds | protect the medicine coppice or expand charcoal work; smoke, sight, and patrol ecology change |
| Whitecairn | The Honest Bell | restore an honest warning or expose the false toll; quarry sightlines, alarm duty, and vertical pursuit change |

Each line uses direct authored state and has an opening commitment/refusal, an
optional material task, a finite existing actor assigned to the dispute, three
independent treasure clues, a secondary-contact service, and two ending
choices. Refusal is a different route through the dispute rather than a dead
end. Consequences persist through return, defeat, succession, and save/load.

Completing two regional lines unlocks the five-part Working Marks arc. Its
evidence route crosses Greywash, Greenwold, Whitecairn, and Hearthford. The
open-compact ending lowers cargo risk and selected demands; Jomon surety adds
six bounded household credits while accepting liability; local marks increase
contact authority without centralising the routes. All three outcomes enter
the bounded causal history and affect later travel, markets, or relationships.

## Encounters, characters, and builds

The existing regional composer now places six finite standard production
actors per added region, distributed between an early teaching site, a
strained mixed site, and a deep critical site. Authored quest actors and the
dormant elite remain explicit. Spawns are repaired only to a reachable
same-level tile; dangerous shooters begin dormant and telegraph before severe
fire. Negotiation reaches at most two nearby members of one group and checks
evidence, morale, role, proximity, and prior violence.

Enemy decisions use perceived sight, heard position, last-known position,
group alert, morale, injury, ammunition, home, allies, and an assigned material
duty. Implemented actions include physical height seeking, smoke-lane feeding,
territorial return, protecting a shooter or wounded ally, flanking, net-cell
telegraphing, theft and escape, alarm, investigation, and morale retreat.
Repeated blocked-pursuit prose is suppressed.

Four seed-selected alternative elites add different spatial rules: a
Hearthford floodgate claimant sweeps a warned sluice lane; a Greywash
wreck-chain reeve removes loose cover; a Greenwold resin tracker marks ground
before smoke rises between levels; and a Whitecairn bridge breaker opens a
warned floor cell. Each has positional, route/elevation, and material
counterplay. They supplement rather than replace the earlier four elite
situations.

The six recruits now check witnessed regional requirements tied to their
techniques. Veyra Bale is a persistent named merchant with a schedule, memory,
relationship, and outcome-sensitive stock. Secondary contacts offer a clue,
training, treatment, or relevant material service. Treatment still costs a
finite supply or obligation and action-clock time.

The retained-content implementation index is
`docs/build-content-matrix.md`. All 30 original passives and all six recruit
techniques have production hooks. Coverage, condition, noise, mobility, and
terrain tags now participate in armour resolution. The pike and trestle
arbalest have ordinary regional merchant sources.

Four weapon families were added: staff sling, hooked river javelin, crossbar
boar spear, and powder handgonne. Seven passives and the finite stillwater
filament relic accompany them. The current bounded totals are 18 weapons, 18
armour pieces, 37 passives, eight secondary tools, five supports, eight drinks,
six relics, and 30 persistent containers. The reducers expose more than 12
three-system combinations; documented examples include guarded stance, mobile
hook, controlled breach, masked smoke, flood rig, field binding, high sling,
weatherfast bow, retrieval javelin, grounded boar brace, masked handgonne,
fixed roof aim, witnessed sluice work, cache sounding, and accounted cargo.

Ranged `A` opens a zero-time target cursor. It renders the selected actor,
line-of-fire cells, cover, effective range, physical ammunition, reload state,
and invalid obstructions before confirmation. Keyboard movement, target
cycling, mouse selection, and `Escape` cancellation use the same reducer;
only a confirmed shot advances time.

## Deterministic audits

`python -m jomon.audit` completed over 100 seeds and 900 pressure-band plans.
It represented all 24 audited standard and added-region elite archetypes, found
138 unique composed plans and 290 distinct finite production compositions,
reported 501 ranged-actor appearances, 14 critical-band elite rolls, zero
invalid groups, zero unreachable actors, and zero unavoidable opening attacks.
The largest repeated composition count was 49. Actor totals rose from 358 at
steady pressure to 549 strained and 735 critical. Its aggregate elite count of
314 additionally includes 300 dormant placed elites: one per added region for
each seed.

The quest audit checked 25 seeds and 100 generated regions. All 100 geographies
were distinct, every required quest position and return path was reachable,
all eight regional and three arc endings were represented, and no invalid path
was found. Container totals were 225 Hearthford and 175 in each other region,
corresponding to 9/7/7/7 per generated world.

`python -m jomon.living_audit` checked 100 route graphs and 12 schedule seeds:
no graph or schedule was disconnected, nondeterministic, overlapping a vessel
control, or invalid. Graphs contained 13–16 edges. Each schedule contained 22
named actors and exercised away, chart, cargo, repair, service, watch, helm,
training, treatment, and regional-work states.

## Automated verification

The unmodified starting build ran 129 tests in 277.165 seconds. The final suite
ran serially:

```text
python -m unittest discover -s tests -v
Ran 177 tests in 567.579s
OK

python -m compileall -q jomon tests
(no output; exit 0)

git diff --check
(no output; exit 0)
```

Coverage includes format-5 migration, cargo recovery, death and theft
ownership, exact transaction cancellation, all retained passives and recruit
techniques, armour fields and terrain, production composition, perceived AI,
ranged selection/cancellation/fairness, negotiation boundaries, every regional
branch, all arc endings, treasure marks, both elite variants per region,
mid-quest saves, and succession during a quest. These results establish
deterministic mechanics, not subjective balance or enjoyment.

## Real PTY verification and limitations

Real `curses` sessions were run at 80×24 and 100×32. A fresh world departed
immediately from the gangplank; map, help, inventory, upper deck, route chart,
tavern, deck links, save, quit confirmation, and terminal restoration were
exercised. A live PTY was shrunk from 80×24 to 70×20, displayed the established
minimum-size warning, returned to 80×24, and recovered its layout. This exposed
and led to fixes for clipped inventory controls, route facts, route controls,
ranged-target facts, and long dialogue requirements.

One complete ordinary Hearthford quest route was played at 100×32: Soren Ford
walked from Jomon to Sela Moss, refused the initial cargo request, chose the
public-sluice compact, returned across the physical region and gangplank,
saved, quit, reloaded, and displayed `refused; Q3/3` plus the changed mill-door
and wetland-bypass consequence. Clean terminal restoration occurred after each
normal quit.

The requested two live branches in every region, two Working Marks endings,
and six complete build-specific PTY expeditions were not manually completed.
Those paths have reducer, reachability, persistence, and layout coverage, but
their subjective pacing and balance remain unverified. Mouse input was covered
through normalized production events rather than a terminal that reports real
mouse clicks. macOS and WSL terminals were not available. Therefore this
assessment does not claim that every branch is fun, that treasure is found at
the intended cadence, or that all twelve builds feel distinct in owner play.

## Candid assessment

[Inference] The regional lines are mechanically less repetitive than the old
shared cargo objective because their persistent results alter different map,
process, patrol, route, market, and elevation facts. The code and tests verify
those differences; only Hearthford's complete branch was felt through normal
play. The explicit target cursor makes ranged commitment materially more
legible at 80×24, and finite mixed groups have substantially more production
variation than the prior fixed placements. Their final encounter density and
reward cadence require owner play.

Treasure is more visible through three clue routes and persistent map marks,
but marking is not the same as naturally noticing a container while travelling.
The smallest justified next step is owner play across the other three regional
lines, recording which clue was actually noticed and which encounter role was
understood; tune those authored placements before adding another item, enemy,
quest, or region.
