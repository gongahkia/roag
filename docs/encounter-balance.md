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
composed together. Each generated region now uses these plans for six finite
standard production actors: an early steady site, a strained site, and a deep
critical site, with bounded additional strained groups only when those plans
contain fewer than six actors. The authored regional elite remains a seventh,
dormant actor. Actual placement repairs to a reachable same-level position and
must provide cover or another traversable approach before a shooter can wake.

The local audit commands are:

```console
python -m jomon.audit
python -m jomon.encounters
```

They build all three added regions for 100 deterministic seeds and sample all
three pressure bands at six site indices. The second command also reports the
actual finite production compositions and their archetype frequency. The last
completed four-region audit result (before this milestone's final rerun) is:

| Measure | Result |
|---|---:|
| seeds / composed plans | 100 / 900 |
| represented archetypes | 21 of 21 |
| unique compositions | 136 |
| most repeated composition | 49 |
| ranged actor appearances | 502 |
| elite appearances | 14 |
| invalid or over-budget groups | 0 |
| unreachable placed actors | 0 |
| unavoidable opening attacks | 0 |

The pressure-band actor totals were 358 steady, 549 strained, and 735
critical. Critical pressure therefore changes both group size and the rare
elite possibility. The audit is a deterministic development command, not
telemetry, and sends nothing off the machine.

## Adjustments from integration play

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
routes and controlled role sessions found the plans readable, but several safe
objective routes can avoid the richest mixed groups. Owner play should tune
budgets and sites before more archetypes are added.

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
