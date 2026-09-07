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
| territorial / tracker | answer intrusion or sound within bounds | lookout | distract, leave territory, break trail |

Every severe ranged attack has at least one full action of visible aim or
setup after awareness. Moving out of the marked lane, reaching full cover,
raising guard, or creating smoke can turn the shot. Longbows need a clean
prepared line and dry string; slings cast quickly and exploit height; heavy
crossbows pierce partial cover but require two reload actions. Ranged actors
retreat when ammunition is gone instead of becoming identical close fighters.

Steady encounters teach one role. Strained groups combine complementary roles
within five budget points. Critical groups have eight points and may rarely
replace a standard group with a regional elite whose terrain or objective
rule—not health alone—defines the encounter. At most two ranged actors may be
composed together. Actual placement must provide cover or another traversable
approach before a shooter can wake.

The local audit command is:

```console
python -m jomon.audit
```

It samples 100 deterministic seeds across all three new regions and three
pressure bands. Final placement reachability and play adjustments will be
recorded after the regional generators and PTY balance pass are complete.
