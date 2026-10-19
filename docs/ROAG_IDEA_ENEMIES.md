# Approved idea encounters

The [review decisions](sprite-audit/ideas-feedback-summary.md) remain the art
and feature authority. This guide records implemented slices; the full
[goal ledger](goal/ROAG_IDEAS_IMPLEMENTATION_PROGRESS.md) remains incomplete.

## Heavy Rammer

Each newly generated stage floor has a seeded one-in-four eligibility check.
A separate stream can select one ordinary, non-squad enemy for replacement
with an implemented idea encounter. The Rammer is one candidate. The Rammer
does not add actors or change geometry and consumes no combat RNG. Restoring a
saved floor does not repopulate it. Replacement stays at least eight
Manhattan tiles from the entrance. Original runtime entity IDs remain stable.

| Parameter | Value |
| --- | ---: |
| HP / defense | 12 / 1 |
| Raw damage | 2 |
| Speed | 70 |
| Base windup / recovery / move cost | 110 / 140 / 100 |
| XP / currency | 4 / 3 |
| Mixed Kenney / standalone 1-Bit art | Tiny 89 / 1-Bit 129 |

The Rammer approaches using ordinary pathfinding. When cardinally adjacent it
locks the occupied attack tile and the direction of a one-tile shove. A red
dotted outline marks the impact; a yellow dotted outline marks the potential
landing. Fog clipping remains in force. The attack never follows a target
that leaves its committed cell. Speed-based scheduler rules still apply:
there is no guaranteed reaction action.

After ordinary damage, a living occupant of the locked tile can be shoved.
This retains friendly fire, including displacement interrupting an ally's
windup. Armor or a ward blocking damage does not prevent physical movement.
Walls, blocking occupants, stairs, exit cells, inactive floors and invulnerable
actors reject the landing. A rejected landing causes no extra collision damage.
The shove never transitions floors or changes the player's action readiness
itself. Normal tile-entry mechanics still apply: brambles, snares, supplies and
boons see the actual forced entry. A triggered snare delays the queued player
recovery exactly once. The existing slide renderer presents displacement.

## Hollow Wraith

The Wraith uses **only 1-Bit 416 in both Kenney choices**, including static
attack/hurt aliases. Classic shows a W token. Its HP/damage/defense are
7/2/0; speed 90, base windup 100, recovery 120, ordinary move cost 100.

It can commit a two-cell cardinal phase across one wall cell toward the player.
The landing must be visible from the player's current position, walkable,
unoccupied and free of stairs/exits. The cyan support outline announces this
landing. Standing on it cancels the move; it never inflicts an arrival hit.
Stun, displacement and death cancel the commitment. Losing sight of the landing
also prevents arrival. The wall remains intact. Water and wider wall blocks do
not become free traversal. A normal movement/attack fallback remains available.

After phase recovery, the Wraith uses a separate ordinary adjacent strike.
Strict scheduler timing still applies. The blink renderer does not slide it
through the intervening wall; canonical forced arrival triggers terrain and
finite Trapper/enemy snares. Phase animation/update never advances simulation.

New stage generation selects at most one approved idea replacement per floor,
from the currently implemented set, using the existing separate seeded stream.
Old saved floors remain concrete. Read-only sight queries use the observer's
current position rather than the potentially stale end-of-action visibility
cache; exploration is not changed by a query.

## Grave Cantor

The fragile Cantor uses Tiny 88 in the mixed pack and 1-Bit 318 in standalone;
its visible corpse marker uses 1-Bit 539 in both. Classic shows C. HP/damage/
defense are 7/1/0, speed 80, base windup 150, recovery 160 and range four.
It still has an ordinary adjacent attack.

An eligible ordinary ally's death leaves a marker while an unused Cantor is
alive on that floor. The caster commits **one** revival attempt, locking that
corpse and spending its budget immediately. Occupying the corpse, interrupting
or killing the caster, or losing sight of the destination prevents revival.
The attempt remains spent. Multiple casters cannot claim the same corpse.
The support outline does not deal damage or guarantee a player reaction action.

A successful revival creates a fresh ordinary actor with half the original
maximum HP (rounded up), no copied statuses, and no repeat XP, currency or
supply reward. It cannot become another revival corpse. Elites, bosses,
special squads and Cantors cannot be revived. Real kill effects still occur;
the finite budget and excluded bodies prevent resurrection chains. Corpses
are bounded to 16 per floor and 32 total, then pruned when no caster needs them.
Pending claims, spent budgets and revived actors survive exact save/Continue.

A 210-stage / 420-floor probe initially found 26 Cantors and **zero** eligible
allies in clear four-tile range. Placement now reuses a nearby eligible ally or
adds one ordinary companion using the replaced actor's existing definition.
It uses an unoccupied adjacent ordinary floor cell at least eight tiles from
the entrance, avoiding markers, stairs, quest cells and the elite. If no safe
formation fits, that replacement is skipped. It never carves terrain to fit.
The same fixed corpus asserts that every generated Cantor has an eligible ally
in clear range. Existing concrete saves are never repopulated.

## Gulp Toad

The Toad uses Tiny 123 / 1-Bit 422. Its HP/damage/defense are 14/2/1, speed
80, base windup 100 and recovery 140. Without suitable loot it approaches and
uses an ordinary adjacent bite.

An empty Toad seeks one visible loose currency, food or health drop within
four tiles and clear sight. At close range it commits a locked gulp. Collect
the drop first, stand over unneeded food, interrupt the Toad or defeat it to
prevent the swallow. Two simultaneous gulps cannot claim the same item.
The Toad never steals inventory or currency the player has already collected.

It carries the **original** drop ID, kind and amount; the ground image is hidden
and a visible carrier gets that drop's icon badge. Defeating it releases the
same item at its actual death cell. Its own ordinary kill reward is separate.
A revived Toad can release carried original loot but still earns no repeat
enemy reward. One Toad carries at most one drop. There is no replacement-loot
roll or combat RNG draw. Pending gulps, carried loot, ownership, interception
and release survive exact save/Continue; malformed ownership is rejected.

## Haunted Cache

This is a suspicious **enemy**, not a paid boon chest. It uses Tiny 90 while
dormant and Tiny 92 after waking; standalone uses 1-Bit 303 then 417. A visible
question mark is its tell. Classic also uses a question mark while dormant.
It retains ordinary enemy/minimap treatment and never offers chest choices.

HP/damage/defense are 8/2/0, speed 75, base windup/recovery 140/140. It stays
still at full HP until a player approaches cardinally adjacent or injures it.
Waking grants no instant strike: the adjacent bite locks a tile and uses the
ordinary scheduler. Leaving that cell avoids that bite; interruption and
friendly fire retain their usual rules. An injured cache may approach through
normal movement. It pays one ordinary 4 XP / 3 currency reward, with the normal
seeded supply policy. Awake state, pending bites and rewards persist exactly.

## Bone Splitter

The skeleton has 8 HP, 2 damage, no defense, speed 80, base windup 100 and
recovery 130. It approaches and commits ordinary adjacent strikes. After its
first death, the complete primary HP batch and removals finish before up to
two weak fragments appear on free nearby cells. Walls, occupied cells, stairs
and exits are never overwritten or used to force two fragments to fit.

Each fragment has 2 HP / 1 damage, speed 95, windup 100 and recovery 110. Its
first scheduled action is one simulation unit after birth; it may already
move or begin a normal windup during the killing action's recovery. There is
no guaranteed reaction action. Fragments cannot split, revive or award extra
XP, currency or supplies. The original skeleton pays one ordinary 4 XP / 3
currency reward. Normal credited kill effects still observe actual finite
deaths. A terminal simultaneous player death suppresses new fragments.

Both choices use the approved skeleton **1-Bit 323**, with fragments rendered
smaller in whole 16-pixel source increments (32px on a 48px tile). Native-pixel
inspection found proposed cell 374 was a rat; it is not used for this enemy.
The audit explicitly records this single shared silhouette instead of claiming
every form has unique art. Base/attack/hurt remain static; no animation editor
or new PNG is involved. Classic fragments use the same smaller placement.

Focused tests cover full-batch deaths before fragmentation, normal fragment
movement/attacks, one reward budget, cramped placement, exact saved pending
attacks, invalid ownership and bounded fragment counts. The disk scenario
continues two live fragments in a separate process with identical events,
state and RNG, then writes the second checked generation.

## Alarm Keeper

The Keeper uses approved 1-Bit 173 in both choices, with bell 915 above an
unused or channeling caster. HP/damage/defense are 6/1/0, speed 75, base windup
220 and recovery 150. A visible caster commits one long alarm on up to two
visible adjacent ordinary floor cells. Its only call is spent on commitment.

Stun, displacement and death cancel the pending call. Occupying a marked cell,
losing sight of it, or a protected/blocked destination prevents that particular
arrival. The other marked cell can still resolve. There is no retargeting or
second attempt. Stairs, exits and map boundaries stay clear. Afterward the
Keeper uses ordinary movement and adjacent attacks.

The fixed arrivals are ordinary Skitters with one owner and no extra XP,
currency or supply rewards. They may move from one simulation unit after
arrival, using normal attacks and strict scheduling; there is no free reaction
action. Killing the caster after the call does not erase already living foes.
Its own normal kill reward remains unchanged. Pending cells, budget and called
actor ownership survive fresh-process Continue. Malformed or excessive claims
are rejected. Tests also reject an unseen caller beginning a visible alarm.

## Web Weaver

| Parameter | Value |
| --- | ---: |
| HP / defense / raw bite | 7 / 0 / 1 |
| Speed / base windup / recovery | 90 / 140 / 140 |
| Mixed / standalone body | Tiny 122 / 1-Bit 366 |
| Lifetime / crossing recovery / cast budget | 400 / +50 / 2 |

A visible caster announces a five-cell anchored line near the player. Its
endpoints and footprint lock at commitment. A blocked or occupied anchor
cancels that placement without refunding the attempt. Protected stairs/exits,
non-floor cells and overlapping lines are excluded. Lines are drawn and
fog-clipped, not a mesh sprite. The user-rejected 414/463/Micro 60 are unused
for this encounter; 366 is a compact native crawler inspected from the sheet.

A line never blocks movement or deals damage. Crossing its interior consumes
it and adds 50 recovery once, including Dash and forced arrival; the caster
is immune. Direct attacks cut either endpoint. Real projectile blockers are
captured before damage, so killing a blocker cannot let that same shot cut an
anchor behind it. DOT cannot cut anchors. Caster death and simulation-time
expiry remove lines, and inactive floors freeze their remaining lifetime.

Nine focused tests cover locked geometry, cancellation, overlap, movement,
shove recovery, pre-damage projectile obstruction, expiry and exact saves.
The disk test saves an active line, reopens it in a second process, then
crosses it and checks the third save generation in a third process. No preview
or drawing call advances simulation time or consumes RNG.

## Oath Twins

| Parameter | Pike / Blade |
| --- | ---: |
| HP / defense | 8 / 0 each |
| Raw damage / speed | 2 / 90; 1 / 100 |
| Base windup / recovery | 100 / 130; 110 / 130 |
| Mixed / standalone art | Tiny 99 + 100 / 1-Bit 124 + 127 |
| Maximum link distance | 4 Manhattan tiles |

These warriors enter generation together in a safe adjacent formation. The
visible link shares half (rounded down) of each mitigated, HP-capped loss
inside the same primary HP batch. Recipient armor is not applied a second
time. An area attack computes both original losses before exchanging shares;
all HP is committed before death or secondary boons. Shared loss does not
manufacture a second hit or recursive damage proc. Actual deaths retain each
original warrior's normal finite XP, currency and kill-boon behavior.

Death, distance beyond four tiles or blocked sight in either direction breaks
the oath permanently. Returning to range cannot reconnect it. The sprite badge
and dotted link are fog-clipped, read-only presentation. Nine focused checks
cover batch symmetry, mitigation, proc exclusion, simultaneous kills, breaks,
malformed saves and a 100-stage natural paired-ownership corpus. A real disk
save/reopen preserves both bodies, exact shared HP and their reciprocal IDs.

## Rotating Sentinel

An optional side-room champion, not an eighth biome miniboss. Seeded placement
requires a room interior outside the critical route, away from the entrance.
No room is carved and no mandatory elite is replaced. Its body is approved
1-Bit 224 in both looks. HP 14, defense 1, raw damage 2, speed 80, base
windup 160 and recovery 80; normal finite rewards remain 4 XP / 3 currency.
It is marked as a special encounter for the requested future dice exemption.

The champion holds position through four separately committed quarter-turn
strikes. The first faces the player; the next three rotate clockwise. Each
three-cell front arc locks when its own windup begins. A yellow arrow shows
the next quarter, and the notice reports the current direction and step.
After four commitments it returns to ordinary movement and adjacent strikes.
Normal damage does not interrupt. Stun, displacement or death ends the sequence,
including an interruption during the recovery gap between attacks.

Eight focused cases cover strict timing, pure aiming, fixed cells, normal
friendly fire, interruption gaps, exact saves and 100 naturally generated
stages with optional placement. A separate-process disk save resolves the
same pending quarter after reopening. Actual LÖVE readability is still pending.

## Mirror Hexer

HP 7, defense 0, raw damage 2, speed 85, base windup 150 and recovery 140.
The body uses approved 1-Bit 320 in both looks; 493/494 mark reflection.
At range two to four on a clear cardinal line, a visible Hexer can spend one
of two casts to lock an adjacent plain-floor reflector. Occupying that cell,
interrupting the caster or protecting it as stairs/exit prevents placement;
that cast stays spent. An active reflector lasts 400 world time or one use.
It remains directional and cannot overlap another marker.

A straight cardinal projectile entering along its arrow turns clockwise once.
Its original shooter, damage and remaining range stay unchanged. Preview shows
the incoming and outgoing legs; melee, area, diagonal and side shots bypass it.
Charges and Hookrunner grapples keep their original movement-linked geometry.
A creature or wall before the marker blocks use; killing that creature with
the same shot cannot allow reflection through it. A second marker cannot
bounce the same shot again. DOT and secondary boon effects are never routed.

Enemy paths consume a reflector **when the windup commits**, so marker expiry
or caster death cannot move an announced attack. The locked reflected path
survives a save; normal friendly fire remains. After placement the Hexer uses
ordinary locked bolts and movement. Fog clips all marker/path drawing.

Twelve focused checks cover these contracts. Eleven disk scenarios include a
third-process real bow shot through a previously saved reflector, comparing
HP, full events, RNG and the third checked save generation. The full 475-test
gate passes. No source PNGs or gameplay RNG are used by reflection preview.

## Silt Worm

HP 10, defense 0, raw damage 2, speed 85, base windup/recovery 140/140.
Approved 1-Bit 371 supplies the body in both looks. It is the small native
four-legged creature from the approved pool, not a newly drawn worm. Mixed
silt uses Caves 64; monochrome reuses bog 268. Original images are untouched.

At two to three clear cardinal tiles, a visible Worm can commit one of two
short charges. The marked lane and direction lock. Its strike stops at the
first creature, preserving friendly fire and pre-damage obstruction. The body
then advances at most two genuinely free tiles. Stairs, exits and blocked or
unwalkable lanes cannot be selected. Stun/displacement cancels that attempt;
ordinary damage does not. After spending its charges it pursues and bites.

Up to two actually vacated plain-floor cells receive a 300-time bog overlay.
The landing itself is clear. Walking adds 40 recovery; Dash adds 20 per crossed
cell, reflected in actual preview/costs for player and ordinary enemy movement.
The overlay never rewrites terrain rows or source art. Expiry therefore cannot
erase a painted tile, and inactive-floor lifetimes pause/rebase. Trails remain
briefly after a Worm dies, cannot overlap or occupy protected cells, and are
bounded to four per owner / twelve total. Preview never advances the clock.

Eleven focused cases, genuine natural selection, fresh-process pending-charge
continuation, a full 485-test gate and the later complete 498-test all-enemy
gate verify these contracts. Four driver fixtures and all 109 current combined
encounter checks pass after the unrelated final driver hardening.

## Ember Moth

HP 6, defense 0, raw damage 2, speed 100, base windup/recovery 180/150.
Its body uses approved 1-Bit 411 in both looks. At two to three clear tiles a
visible Moth can commit one of two dives. The destination is the player's
current plain-floor cell, locked at windup; a small fire cue distinguishes the
follow-up hazard. Stun/displacement cancels the attempt without refunding it.
Stairs, exits and blocked ground are excluded. Ordinary damage cannot cancel.

The dive checks its fixed cell at impact. If empty, the Moth relocates there
with a canonical forced arrival; if occupied, the real occupant takes normal
friendly-fire damage and the Moth stays put. Bodies never overlap. A brief
ember remains on valid ground either way. The existing relocation presentation
snaps an untraversed multi-cell displacement instead of inventing a walked path.

Each ember pulses **1 raw fire damage at +100 and +200 world time**, then ends.
Leaving avoids later pulses; armor/wards still apply. Only its source Moth is
immune. Fire may outlive its caster for those finite remaining pulses. At most
eight embers exist, cells cannot overlap, and each caster has two spent dives.
Pulse timers use the existing effect queue with explicit validated ownership.
They freeze/rebase with their floor and appear in the read-only pending-threat
query, keeping the actual next pulse time visible to the HUD and tactical UI.
No render clock changes their timing and no source terrain is overwritten.

Twelve focused cases cover pure preview, exact times, landing occupancy,
friendly fire, source immunity, interruption, frozen floors, corrupted timers
and exact continuation alongside Burning and Temporal Echo. Thirteen disk
scenarios pass, including a third-process real fire pulse and checked save
generation 3. The complete all-enemy suite is **498/498**. Actual LÖVE input
and rendered readability remain pending under the user's no-window restriction.

## Verification

```sh
TMPDIR=/var/tmp luajit tests/run.lua
sh tests/idea_enemy_disk_resume.sh
make sprite-ideas-test surplus-audit-check enemy-review-check
node tests/sprite_surplus_page_test.js
```

Focused assertions cover locked timing, leaving the target cell, pure landing
queries, blocked landings, canonical hazard/pickup contacts, exact snare delay,
friendly fire, interruption, corrupted pending data rejection, save continuation,
presentation-only sliding and seven-biome/two-floor seeded population.
Cantor assertions cover finite claims, occupied/cancelled attempts, simultaneous
caster/ally deaths, reward suppression, corpse bounds, natural company and
reserved placement. Its disk scenario saves a pending revival in one process
and resolves it identically in another, then saves the second generation.
The Toad disk scenario similarly saves before a gulp and restores the original
cargo in another process. Focused tests also cover all three drop kinds,
post-release pickup exactly once, a full-health player protecting food and two
competing claims at the same timestamp.
Cache checks cover dormancy, real ranged wake-up, the locked delayed bite,
leaving its target cell, one normal kill reward, invalid wake data and native
art. Its disk scenario saves an awakened pending bite, restarts LuaJIT and
resolves that same legal attack and the second checked save generation.
The disk script uses separate LuaJIT processes and an isolated `/var/tmp`
directory, comparing full snapshots and events, including RNG and scheduler.
It does not open LÖVE or touch personal saves.

The guided legal-action driver is separate from the game. New encounter timing
exposed three automation errors: endlessly dodging between two safe cells,
firing at a diagonal foe through a blocked projectile corner, and bumping a
warded guard instead of using existing flank/lure/area-attack counterplay.
The driver now checks actual attack cells, recognizes non-damaging support
markers, and uses ordinary attacks or travel to break a proven stall. Close
combat remains subject to normal damage and strict windup timing. Paired-floor ranged and melee combat break a proven
six-move chase cycle with a reachable
attack or one ordinary wait, and can use an actual Dash landing out of a
footprint with no safe adjacent step. The paired-floor melee Forge strategy
uses Dash for its centered box; old single-floor and close-range Regent
tactics remain unchanged. These are test strategies, not player input logic.
Arcanist also considers its actual Fire Sigil before chasing an off-axis foe.
Paired ranged combat breaks equal-distance escape ties laterally. On the stair
route, only after a proven six-move, unchanged-HP cycle, the driver can ignore
fully armored attacks. Damaging attacks and physical shoves still demand
escape. This fixes two observed legal-strategy stalls without changing the
game's controls, actor stats, damage, RNG or scheduler.
The driver never assigns HP, positions or actor statistics. Recovery uses
normal earned pickups and boons.

Regression fixtures reproduce the old 60-action corridor loop (the corrected
driver progresses in 10 legal actions), blocked-corner shots, an Alchemist
area attack reaching a wizard behind its protected guard, and armor versus
shove threat detection. Prepared fixtures are not full-run playthrough claims.

Runtime art resolution and source pixels are checked headlessly. Actual LÖVE
rendering, physical input and readability remain unverified under the user's
instruction not to open app windows.
