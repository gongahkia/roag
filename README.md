# ROEG — Tranche 02 combat testbed

ROEG is a LÖVE 11.5 roguelike prototype. The current authored 35×25 test map has one Adventurer and three enemy archetypes. `docs/ROEG_SPEC_v1.0.md` is the authoritative design baseline. The 15×11 foundation map remains available to headless tests.

## Run and controls

Run `love .`. Press **N** to restart the test encounter. Move one cardinal tile with WASD or arrows; Space or `.` waits. Press **F** for Sword Strike, **R** for Sweeping Slash, or **X** for Dash; select a direction with WASD/arrows, or Q/E/Z/C for diagonal attacks, inspect the preview, then press Enter. Escape cancels aiming without spending time. Bumping an enemy with a cardinal move performs one basic Sword Strike instead of moving. Walls and a blocked first Dash step consume no time. Dash still moves one cell for its full 130 recovery if only the second cell is blocked.

The demo places a Mossbound Guard at (7,3), Thornspitter at (3,9), and Ruin Skitter at (6,6). They use distinct shapes and HP bars. The sidebar shows player HP, world time, last action/cost, historical playback, and live windups. The game ends when player HP reaches zero; N starts a new test run.

## Provisional combat and timing

| Actor | HP | Damage | Defense | Speed | Windup | Recovery | Movement cost | Range |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Adventurer | 24 | 4 Sword / 3 Sweep | 0 | 100 | Immediate | 100 Sword, 150 Sweep | 100 | Adjacent |
| Mossbound Guard | 10 | 3 | 1 | 70 | 80 | 120 | 100 | Cardinal adjacent |
| Thornspitter | 6 | 2 | 0 | 90 | 90 | 130 | 100 | Cardinal lane, 2–7 |
| Ruin Skitter | 4 | 1 | 0 | 160 | 35 | 70 | 100 | Adjacent, prefers diagonal flank |

These numbers are in `src/content.lua` and are provisional. Dash costs 130. Effective time is `max(1, round(base × 100 / speed))`, calculated with integer inputs. Damage is `max(0, attack damage − defense)`, capped at remaining HP. There are no critical hits because this slice defines no crit stat. Player strikes resolve immediately; enemies lock their target cells when a windup begins. An enemy has **no ready turn during its windup**. Its next ready time is impact time plus effective recovery. Ordinary damage does not cancel the locked strike; stun, displacement and death do. `Game.stun` cancels a windup and makes the enemy ready no earlier than the supplied integer stun expiry; `Game.displace` cancels it and applies one normal recovery before the enemy is ready again. A ranged shot stops at the first combatant in its locked lane, including an allied enemy. Terrain walls block firing lanes.

Sweep retains its provisional three-cell counterclockwise/center/clockwise order. All hit HP changes in an area attack are applied as one batch before `DamageBatchResolved` and individual `DamageAttempted`, `DamageTaken` or `DamageBlocked`, and `Died` records. `DamageTaken` requires positive actual HP loss. Every action/strike event carries source and action IDs. Reward and kill-credit policy is deferred.

At equal simulated time, committed impacts resolve before newly ready actors. The player acts before newly ready enemies, then remaining actor ties use stable schedule sequence/ID order. A fast enemy can strike before the player is ready again; no reaction turn is inserted. Guard uses cardinal BFS around walls, Thornspitter seeks a clear lane and repositions, and Skitter seeks a diagonal flank. AI decisions use only simulation state and stable neighbor order.

## Presentation and architecture

`src/game.lua` owns combat state, action validation, damage, occupancy, interruptions, events, and snapshot/restore. `src/ai.lua` chooses read-only enemy intents. `src/scheduler.lua` orders actor readiness and committed impacts. `src/targeting.lua` keeps Adventurer attack shapes. `src/content.lua` keeps map/tile properties and tunable combat definitions. `src/rng.lua` remains the serializable simulation RNG. None of these simulation modules references LÖVE.

`main.lua` renders the authored map, combatants, HP and locked tiles. `src/camera.lua` follows smoothly without affecting world time. `src/playback.lua` consumes **copies of already resolved ordered events** for short visual stages. During playback, input is briefly gated; orange/red historical telegraphs are labeled as past events. When input is available, red highlights and the sidebar show only **currently pending authoritative strikes** and their simulated time to impact. Presentation state is never included in a simulation snapshot.

The in-memory plain-data snapshot schema is `roeg-run/2`, with content version `roeg-content/2`. It includes enemy HP/AI memory, locked target geometry, impact IDs and due times, scheduler sequence, RNG, and other state. Older snapshots are rejected; no cross-version migration or on-disk save UI exists yet. `Game.stun` and `Game.displace` are small simulation operations for interruption; there is no player stun/knockback ability or status-effect framework.

## Verification

Run `luajit tests/run.lua` (or `lua tests/run.lua`) for headless tests; no LÖVE global is used. On a desktop with LÖVE and `timeout`, run `sh tests/gui_smoke.sh` for the original no-enemy targeting/camera scenario and `sh tests/enemy_gui_smoke.sh` for enemy windup/impact/live-pending presentation. Both print temporary screenshot locations for inspection. These callback-driven checks do not replace pressing keys yourself in `love .`.
