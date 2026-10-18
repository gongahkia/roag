# Approved idea encounters

The [review decisions](sprite-audit/ideas-feedback-summary.md) remain the art
and feature authority. This guide records implemented slices; the full
[goal ledger](goal/ROAG_IDEAS_IMPLEMENTATION_PROGRESS.md) remains incomplete.

## Heavy Rammer

Newly generated stage floors can replace one ordinary, non-squad enemy with
a Rammer. An independent floor-seeded selection attempts this on one quarter
of floors; it never adds another crowd, changes terrain, consumes combat RNG,
or repopulates an existing saved floor. Replacement stays at least eight
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
The disk script uses two separate LuaJIT processes and an isolated `/var/tmp`
directory, comparing full snapshots and events, including RNG and scheduler.
It does not open LÖVE or touch personal saves.

Runtime art resolution and source pixels are checked headlessly. Actual LÖVE
rendering, physical input and readability remain unverified under the user's
instruction not to open app windows.
