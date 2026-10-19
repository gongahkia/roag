# Approved idea classes

The [review feedback](sprite-audit/ideas-feedback-summary.md) is the feature
and art authority. The [goal ledger](goal/ROAG_IDEAS_IMPLEMENTATION_PROGRESS.md)
tracks the unfinished expansion. These additions use the existing class
selection, action scheduler, combat batches and checked run saves.

## Duelist

Choose **New Game → Duelist → Begin New Game**. Arrow keys reach all ten
classes; the existing number shortcuts cover the first nine. Continue keeps
the class and earned riposte of the saved run.

| Parameter | Value |
| --- | ---: |
| Starting HP / raw attack / defense / speed | 20 / 4 / 0 / 115 |
| Base move / thrust / parry / sidestep cost | 100 / 100 / 100 / 85 |
| Initial effective costs, after speed | 87 / 87 / 87 / 74 |
| Riposte bonus / maximum held charges | +2 raw damage / 1 |
| Mixed / standalone body | Tiny 99 / 1-Bit 124 |

- **F — Thrust:** aim an eight-direction, two-cell physical lane. Walls and
  blocked diagonal corners stop it. Both targets share the existing primary
  HP batch. Hybrid bump attacking still hits only its adjacent target.
- **R — Timed Parry:** choose a direction and commit a normal paid action.
  During that action's recovery, block at most one damaging, direct melee
  impact from an adjacent enemy in the front three-cell sector. Behind,
  ranged and damage-over-time hits bypass it. Raising a guard neither attacks
  nor creates Temporal Echo. The stance ends at the next player decision.
- **After a successful parry:** hold one riposte for the next paid thrust,
  including a bump attack. It adds two raw damage before the existing
  ability modifiers. A committed miss spends it; rejected input does not.
  There is no free counterattack. The HUD shows **RIPOSTE READY** when no
  higher-priority threat needs that warning area.
- **X — Sidestep:** one ordinary legal tile, using the existing Dash and
  entry rules. It cannot cross a wall or corner.

Strict timing remains: an impact outside the committed parry recovery is
not blocked. There is no guaranteed reaction action. For example, the
starting speed gives an 87-time guard; a guard begun at time 0 has ended
before an enemy impact at 114. A second guard from 87 to 174 can catch it.
Armor applies before a positive-loss parry; one stance cannot block two
simultaneous attacks. The physical thrust ignores projectile reflectors.

The approved Duelist body deliberately shares Tiny 99 / 1-Bit 124 with the
Oath Pike Twin. The live audit reports this reuse; static attack/hurt aliases
are not new animation or unique art. Original source PNGs remain unchanged.

### Evidence and limits

The frozen source passed **510/510** headless tests, including twelve
Duelist checks. A genuine seed-9 paired-floor legal-action run reached
Victory after **977 actions at 26 HP**. That driver's victory did not need
to parry; separate prepared fixtures prove parry/riposte timing and costs.

Run `sh tests/idea_duelist_disk_resume.sh` for an isolated checked save,
fresh-process Continue, paid riposte, exact events/HP/RNG/schedule and
second save generation. No personal save directory is used. The normal
run schema remains `roag-run/11`; the optional held charge is plain data.

LÖVE windows remain untested under the user's current restriction. No
physical keyboard, mouse, tablet, native GPU or human balance acceptance
is claimed. The other five approved classes remain separate unfinished work.

## Geomancer

HP 24, attack 5, defense 1, speed 85. Base move/F/R/X costs: 100/120/130/130.

- **F — Stone Bolt:** three-cell, first-body projectile with normal armor and corner blocking.
- **R — Raise Bulwark:** choose adjacent cardinal plain floor. Pay the normal action to create one stone block for 300 world time. It blocks walking, sight and projectiles; it has no HP, XP or loot. Replacing it removes its old expiry work.
- **X — Grounded Dash:** the existing two-cell traversal and entry rules.

Placement rejects occupied cells, stairs and any cell whose removal disconnects accessible ground. A block does not rewrite terrain or published art. Its clock pauses with its floor, then rebases exactly on return. Mixed art uses approved Tiny 100 (shared with the Oath Blade Twin) and Micro 116 stone; standalone uses 1-Bit 466 and the existing 262 stone motif.

The title picker shows up to nine rows per page. Arrows/gamepad cycle all classes and move the page; mouse selects the visible rows. Number shortcuts still cover the first nine.

Nine focused checks and 46 combined class/art/UI contracts pass. `sh tests/idea_class_disk_resume.sh geomancer` checks fresh-process Continue through expiry. Full regression is recorded in the goal ledger when finished; GUI/physical input remains pending.

## Spiritbinder

HP 20, attack/command 3, defense 0, speed 100. Base move/F/R/X costs: 100/110/140/120.

- **F — Tether Bolt:** three-cell projectile, stopped by the first body and normal corners.
- **R — Command Spirit:** toward empty ground, deploy/reposition the same familiar up to two visible tiles away. An enemy in the chosen three-cell direction can instead receive a paid strike if adjacent to the familiar. The first deployment stops before an enemy. Invalid reach/visibility costs no time.
- **X — Spirit Step:** normal two-cell Dash.

The spirit does not block movement, scout, act autonomously, draw attacks or award rewards. One paid strike uses the owner's existing damage/proc pipeline; deployment creates no attack/Echo. The same familiar ID survives repositioning. Saved ownership and exact subsequent commands are validated. Mixed body uses Tiny 85, standalone body 122; the approved familiar uses 1-Bit 417 in both choices (representative art also used by an awake Haunted Cache).

Six focused tests pass; combined contracts and new-process Continue are recorded in the goal ledger. `sh tests/idea_class_disk_resume.sh spiritbinder` resumes an actual commanded strike after a checked save. No LÖVE window or physical input acceptance is claimed.

## Tinkerer

HP 23, attack 3, defense 0, speed 95. Base move/F/R/X costs: 100/100/160/120.

- **F — Hand Bolt:** three-cell, first-body projectile.
- **R — Deploy Sentry:** place one nonblocking stand on empty adjacent cardinal plain floor. Three finite attempts begin 100 world time after deployment. Each visible cardinal target within four tiles gets an 80-time locked bolt warning; a spent attempt is not refunded when no target is available. Replacing the stand cancels its old commitment and discards its charges.
- **X — Utility Roll:** ordinary two-cell Dash.

Shots deal fixed 3 raw damage with armor, first-body collision and **friendly fire**, including the owner. The owner's genuine enemy kills receive normal credit exactly once. Sentry-generated records explicitly suppress boon activation: a zero coefficient alone does not suppress guaranteed Echo. No free crit, Echo or proc chain is manufactured by a timed shot. Visibility is checked at commit and impact; there is no scouting or wall penetration. The stand is not a damageable actor or reward source. After three attempts, waiting cannot produce further shots. Frozen-floor work rebases exactly.

Mixed art uses approved Tiny 109 and Micro 119 stand; standalone uses approved 1-Bit 222 and the existing 262 stone/stand motif. Seven focused checks and 59 combined contracts pass. `sh tests/idea_class_disk_resume.sh tinkerer` resumes an actual pending shot in a new process. Physical/GUI readability remains pending.
