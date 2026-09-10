# Encounter balance and counterplay

Jomon uses a small utility choice over direct legal actions, not a general
planner. An actor knows current line-of-sight, recent sound origins, a bounded
last-known courier position, nearby allies, its own morale, injury,
ammunition, home, and assigned material duty. It does not inspect hidden pack
contents or the courier's current position after losing contact.

## Role matrix

| Role | Readable plan | Natural partner | Primary counterplay |
|---|---|---|---|
| lookout | reach a signal or raise an alert | flanker / shooter | avoid sight, distract, interrupt |
| shooter | obtain and telegraph a firing lane | protector | cover, smoke, movement, elevation |
| skirmisher | fire while yielding distance | thief / controller | deny retreat terrain, close indirectly |
| protector | preserve a ranged ally's spacing | shooter / suppressor | pull, net, flank, change level |
| suppressor | deny one lane or area | flanker | wait, redirect terrain, use another route |
| controller | constrain current, smoke, nets, or paths | shooter / thief | disable the material anchor |
| thief | take exposed cargo and escape | skirmisher | protect pack, intercept marked exit |
| flanker | move toward a visible side position | shooter / suppressor | reverse direction, use a narrow route, deny the side cell |
| controller | telegraph a net or material denial cell | shooter / thief | leave the marked cell, cut the anchor, change level |
| territorial / tracker | answer intrusion or sound within bounds | lookout | distract, leave territory, break trail |

Every severe ranged attack has at least one full action of visible aim or
setup after awareness. Moving out of the marked lane, reaching full cover,
raising guard, or creating smoke can turn the shot. Longbows need a clean
prepared line and dry string; slings cast quickly and exploit height; heavy
crossbows pierce partial cover but require two reload actions. Ranged actors
retreat when ammunition is gone instead of becoming identical close fighters.
The player's ranged `A` action opens a zero-time target cursor: the path,
effective range, cover, physical ammunition, selected hostile, and invalid
empty cells are visible before Enter commits. Escape cancels without changing
state, Tab cycles currently legal targets, and terminal mouse double-click has
the same confirmed-shot path as Enter.

Steady encounters teach one role. Strained groups combine complementary roles
within five budget points. Critical groups have eight points and may rarely
replace a standard group with a regional elite whose terrain or objective
rule—not health alone—defines the encounter. At most two ranged actors may be
composed together. Each generated region draws its finite production actors
from nine regional standards: an early steady site, a strained site, and a
deep critical site, with bounded additional strained groups when those plans
contain fewer than six actors. Hearthford also places one of its three new
material workers, raiders or scavengers beside the ordinary return loop. The
authored regional elite remains separate and dormant. Actual placement repairs
to a reachable same-level position and
must provide cover or another traversable approach before a shooter can wake.

The local audit commands are:

```console
python -m jomon.audit
python -m jomon.encounters
```

`roster_audit()` additionally validates the live catalogue rather than adding
direct-map actors to a paper total. It reports **72 standard archetypes: nine
per major region**, 72 unique mechanics-driving signatures, 72 unique ASCII
glyphs, 23 catalogue elites plus Hearthford's map-authored crown wheel for 24
elite situations, and eight named recurring claimants. A signature comprises
profile, role, goal, material duty, ecology, ranged kind/range, finite supplies,
vision, hearing and morale—the fields that actually select actions or bound
perception. Every standard row also carries a regional terrain reason, a
secondary reaction, an explicit capability, and two or more counters. Semantic
hostile colour and bold are applied by actor identity, independently of glyph.
The added roles reuse the production ecology reducers deliberately: workers
quench, brace or drain finite material states; raiders telegraph fire or
support damage and retrieve physical goods; animals hunt, flee or scavenge.
Their distinct goals, terrain, perception, reach and counterplay combinations
change mixed encounters without adding an expensive planning layer.

Eight of the catalogue elites materialise only after a completed regional
ending has produced physical aftermath. Ysolde Lockhand, Bran Wreckward, Mara
Ashstep and Orren Bellrope join the four retained named claimants and can
retreat once, spend real local stock to return, retain injuries and lost kit,
settle through witnessed terms, or die permanently. Four additional machines
move water, cover, clay slip or seasonal ice. Every operation warns for a full
action, spends one of three charges, accepts movement/guard plus a regional
material control, and persists the changed cell or exact stolen item.

They build all three added regions for 100 deterministic seeds and sample all
three pressure bands at six site indices. The second command also reports the
actual finite production compositions and their archetype frequency. The
completed regional-quest milestone audit result is:

| Measure | Result |
|---|---:|
| seeds / composed plans | 100 / 900 |
| represented standard/added-region elite archetypes | 24 of 24 |
| unique composed plans | 138 |
| most repeated composition | 49 |
| distinct finite production compositions | 290 |
| ranged actor appearances | 501 |
| composed critical-band elite appearances | 14 |
| placed dormant regional elites | 300 |
| invalid or over-budget groups | 0 |
| unreachable placed actors | 0 |
| unavoidable opening attacks | 0 |

The pressure-band actor totals were 358 steady, 549 strained, and 735
critical. The aggregate elite count printed by `jomon.audit` is 314 because it
includes both the 14 rare composer rolls and one dormant authored elite in each
of three generated regions for every seed. Critical pressure therefore changes
both group size and the rare composed elite possibility. The audit is a
deterministic development command, not telemetry, and sends nothing off the
machine.

## Adjustments from integration play

- The expansion's Marlbank PTY exposed a separate generation problem: three
  identical foxes occupied ordinary worksite slots in addition to the fixed
  predator/prey pair. Frontier works now draw only working adults, prefer
  previously unused roles, cap each definition at two, and introduce an
  available ranged role at the second site. The wildlife pair remains a
  separate observable situation. Group budgets, opposed-claim exclusions and
  the two-shooter cap remain in force. Existing saved populations are not
  replaced. This improves role coverage, not proof of 48 distinct mechanics.
- Two-step pressure pursuit could enter the courier's occupied tile. It now
  stops adjacent, and tactical moves do not commit a step onto that tile.
- Ranged actors enter awareness through aim/setup, never immediate severe
  damage. Longbows withdraw when crowded, slings can suppress a vacated lane,
  and heavy crossbows commit to reload.
- Protectors now move between the courier and a ranged ally rather than merely
  labelling that intention, and can cover a wounded group member's retreat.
  Flankers use a visible side target. Net controllers mark one cell before the
  haul, so reposition is real counterplay.
- Ranged actors with an authored height-seeking capability path through a real
  stair or climb before aiming. Burn and lime smoke-tenders spend actions
  placing bounded smoke lanes. Territorial actors return once the courier
  leaves their explicit home boundary instead of pursuing forever.
- Negotiation now reaches at most two nearby members of one group. Elites need
  witnessed regional evidence, and confident actors already struck in combat
  reject unsupported terms. Other groups keep their independent goals.
- Lost actors investigate sound or last sight and then return to guard/patrol.
  The movement executor no longer falls back to hidden courier coordinates.
- Smoke and water displace actors whose material role does not protect them.
  Regional controls remove the corresponding elite advantage.
- Generated placements are moved to the nearest same-level reachable tile when
  an authored coordinate lands in seeded obstruction. The audit verifies zero
  unreachable actors after this repair.

The audit does not establish subjective encounter quality. Eight complete PTY
routes from the preceding milestone and controlled reducer sessions found the
plans readable, but this milestone's PTY work exercised only one complete
Hearthford quest branch. Several safe objective routes can still avoid the
richest mixed groups. Owner play should tune budgets and sites before more
archetypes are added.

## Seeded elite alternatives

Each region now selects between two authored spatial problems. Hearthford
chooses a crown-wheel lane sweep or a floodgate claimant who telegraphs a
three-cell sluice surge. Greywash chooses the storm-chain captain or a
wreck-chain reeve who removes loose cover before a sling cast. Greenwold
chooses a crosswind fire warden or a resin tracker who marks ground before
smoke rises to the aligned level. Whitecairn chooses false-bell rockfall or a
bridge breaker who opens a warned floor cell and changes the usable crossing.

Every alternative has movement/guard counterplay, a second route or elevation
answer, and a material answer established by its regional control or quest
evidence. Selection is seeded and persistent; the actor begins dormant, so
entering awareness cannot cause immediate damage.

## Frontier work and wildlife

Fresh Dunmire, Rillscar, Marlbank and Frostmere populations now use the same
bounded production composer, plus a nearby prey/predator pair. Eight actors
are placed per frontier. Saved populations are not replaced on return.
Twenty-four frontier definitions combine region, material duty, perception,
movement, weapon and allegiance. Shared reducers remain intentional, while
their decision-driving combinations differ. Six Hearthford production rows now
formalize its existing bank lookout, reed boar, roof keeper, mill protector,
gantry suppressor and cargo reaver without replacing their persistent IDs or
their established map positions. Focused tests exercise each live actor's
alarm, charge/mud counter, prepared shot, ally interception, smoke control, and
exact stolen-item recovery.

| Work/interest | Actual action | Player answer |
|---|---|---|
| Pail keeper | spends a finite pail on visible fire | let the worker clear the lane, draw attention elsewhere |
| Cinder thrower / kiln ward | warns then ignites a resin feed; switches to ranged fire when fuel ends | wet the feed, disrupt preparation, use the new smoke |
| Scaffold cutter | warns then damages support, starting delayed collapse | brace, leave the cell, interrupt the cut |
| Brace ward | spends supports on damaged ground | lure it toward a threatened crossing, exhaust its stores |
| Ditch keeper / thaw runner | removes observed deep water locally | follow the drained route or preserve water as an obstacle |
| Field surgeon / net mender | spends dressings tending a seen wounded ally | separate the group or interrupt treatment |
| Signal caller | spends signals rallying a wavering ally | disrupt the caller, break visual contact |
| Escort | occupies space beside a vulnerable carrier/ally | flank it or draw it away from the retreat |
| Recoverer / sack taker | takes an actual dropped item and attempts escape | intercept; combat, fire and rival kills all release the same item |
| Predator | stages an attack on visible prey, not a hidden courier | move prey into its sight, leave its hunting ground |
| Grazer | flees observed hunters into local terrain | observe without approaching or use its flight as distraction |

Wardens and raiders oppose one another through recorded interests. Alert
groups cannot combine those opposed interests; audible group information is
local, not region-wide telepathy. Active ecological decisions are capped at
24 actors within 24 tiles using reconstructible 8-cell spatial buckets.
Far actors retain state and no process runs while the terminal is idle.

`O` lists only currently visible actors with actual intent, supplies or
ammunition and counterplay. Neutral grazers have a distinct semantic colour;
untargeted melee does not pick one instead of a hostile.

Real 80×24 PTY, seed `working fen`: walked to Dunmire, passed a fleeing hare,
found the ordinary-road flood-mark chest, observed a cinder thrower warn and
ignite its feed, then a pail keeper move over and quench that same material.
Opened the drying spill while avoiding telegraphed sling lanes, returned to
Yara, chose to hold the winter fuel bank, physically returned to Jomon and
saved/quit. The saved courier retained 10/10 health; the cinder thrower had
zero fuel supplies and three sling shots, the pail keeper two supplies, and
the resin feed remained wet and extinguished. This demonstrates one natural
interaction and an evasion route, not all ecological or tactical combinations.

The run also exposed a quest guard assignment selecting a hare. Guard
assignment now excludes animals and prefers an actual bank protector; a
regression covers the observed seed. The first full ecology run had 236
tests with one failure in 194.246 seconds at that assignment boundary.
