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
The disk script uses two separate LuaJIT processes and an isolated `/var/tmp`
directory, comparing full snapshots and events, including RNG and scheduler.
It does not open LÖVE or touch personal saves.

The guided legal-action driver is separate from the game. New encounter timing
exposed three automation errors: endlessly dodging between two safe cells,
firing at a diagonal foe through a blocked projectile corner, and bumping a
warded guard instead of using existing flank/lure/area-attack counterplay.
The driver now checks actual attack cells, recognizes non-damaging support
markers, and uses ordinary attacks or travel to break a proven stall. Close
combat remains subject to normal damage and strict windup timing. The driver never assigns HP, positions or actor statistics. Recovery uses
normal earned pickups and boons.

Regression fixtures reproduce the old 60-action corridor loop (the corrected
driver progresses in 10 legal actions), blocked-corner shots, an Alchemist
area attack reaching a wizard behind its protected guard, and armor versus
shove threat detection. Prepared fixtures are not full-run playthrough claims.

Runtime art resolution and source pixels are checked headlessly. Actual LÖVE
rendering, physical input and readability remain unverified under the user's
instruction not to open app windows.
