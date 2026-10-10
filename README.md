# ROEG — Tranche 03 boon combat testbed

ROEG is a LÖVE 11.5 roguelike prototype. The current authored 35×25 test map has one Adventurer and three enemy archetypes. `docs/ROEG_SPEC_v1.0.md` is the authoritative design baseline. The 15×11 foundation map remains available to headless tests.

## Run and controls

Run `love .`. Press **N** to restart the test encounter. Move one cardinal tile with WASD or arrows; Space or `.` waits. Press **F** for Sword Strike, **R** for Sweeping Slash, or **X** for Dash; select a direction with WASD/arrows, or Q/E/Z/C for diagonal attacks, inspect the preview, then press Enter. Escape cancels aiming without spending time. Bumping an enemy with a cardinal move performs one basic Sword Strike instead of moving. Walls and a blocked first Dash step consume no time. Dash still moves one cell for its full 130 recovery if only the second cell is blocked.

**Developer showcase:** Press **B** while idle to cycle None → Lightning → Cascade → Transformation → None. This changes the test loadout without advancing world time; it is a debugging control, not an acquisition mechanic. **N** restarts with the selected showcase loadout. A normal new run starts with no boons. The sidebar lists each equipped boon with C/U/R/L rarity stack counts. Lightning supplies Storm Conductor and Static Footsteps; Cascade supplies Storm Conductor, Detonation Bloom, Resonant Wounds and Temporal Echo; Transformation supplies Crescent Reach, Thorn Mirror and Turncoat Spark. Headless callers can use `Game.set_boon_stacks(state, owner_id, boon_id, rarity, count)` at a stable decision boundary, including zero to remove a rarity stack.

The demo places a Mossbound Guard at (7,3), Thornspitter at (3,9), and Ruin Skitter at (6,6). They use distinct shapes and HP bars. The sidebar shows player HP, world time, last action/cost, historical playback, live windups and pending echoes. The game ends when player HP reaches zero; N starts a new test run.

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

`main.lua` renders the authored map, combatants, HP and locked tiles. `src/camera.lua` follows smoothly without affecting world time. `src/playback.lua` consumes **copies of already resolved ordered events** for short visual stages, including boon activations, secondary damage and delayed resolution. During playback, input is briefly gated; orange/red historical telegraphs are labeled as past events. When input is available, red highlights and the sidebar show only **currently pending authoritative strikes** and their simulated time to impact. Presentation state is never included in a simulation snapshot.

## Boon resolution contract

`src/boon_content.lua` contains eight validated definitions with four rarity parameter rows each. `src/boons.lua` indexes subscriptions by event kind, matches composable AND/OR/NOT and identity/tag/faction/damage/spatial predicates, aggregates stacks, and selects activations using the saved simulation RNG. Inventory is one plain table per owner: `boon_id -> rarity -> count`. There is no copy or inventory cap. Storm Conductor uses 2/6/18/50% per Common/Uncommon/Rare/Legendary copy; chances add and overflow into guaranteed plus fractional activations. Proc coefficients are declared on ability/effect content (default 1), validated, and multiply only probability-based activations. Other boon powers add across rarity stacks.

The eight prototypes are Storm Conductor (hit lightning), Detonation Bloom (credited-kill explosion), Thorn Mirror (positive-damage retaliation), Static Footsteps (move lightning), Resonant Wounds (two distinct hostile damage targets adjacent in one chain), Temporal Echo (locked repeat damage after 200 simulated time), Crescent Reach (Slash gains a forward cell and additive damage), and Turncoat Spark (observed enemy friendly-fire retaliation). Current provisional powers by rarity are 1/2/3/4; area radius is one tile. Static Footsteps chances are 5/15/35/70%. Observation radius uses Manhattan distance; Resonant Wounds and Turncoat Spark observe within eight tiles, Storm lightning selects a second hostile within six tiles, and Static Footsteps selects within two. Resonant adjacency and area centers use captured damage-event positions; area damage checks current occupancy. Pair identities are deduplicated per chain. Crescent Reach's forward cell requires an open middle and destination tile, so it cannot reach through a wall.

The game queues emitted events and effects iteratively after each primary damage batch and before the next scheduled actor decision. `AttackPerformed` can describe a miss; `HitConfirmed` requires contact, `DamageTaken` requires positive HP loss, and `EntityKilled` accompanies `Died` when boons are active. `TileEntered` records a committed move. Existing Tranche 02 event arrays retain their exact legacy shape when no boon is equipped; the new trigger events are emitted once at least one boon is owned. Events capture source/target faction, definition tags, positions, action/chain IDs, parent ID, coefficient and branch ancestry. Secondary damage uses the same HP, death, friendly-fire and ordered event pipeline. A boon may recur in sibling branches but cannot reactivate on a branch already containing its owner/boon key. One million resolution steps is a diagnostic error, not a silent effect cap.

Scheduled echoes use `src/scheduler.lua`'s committed-effect priority: they resolve before ready actors at the same timestamp. They retain locked target cells, attribution, chain ID and ancestry. The in-memory snapshot schema is now `roeg-run/3` with `roeg-boons/1`; it includes inventory, delayed payloads, remaining same-chain pair history, RNG and scheduler state. Older snapshots are rejected. Ability modifiers are reapplied in stable boon and declared modifier order from inventory without mutating `src/targeting.lua`'s base patterns. Cross-version save migration and on-disk save UI remain out of scope.

`Game.stun` and `Game.displace` remain the small simulation operations for interruption; there is no player stun/knockback ability or status-effect framework.

## Verification

Run `luajit tests/run.lua` (or `lua tests/run.lua`) for headless tests; no LÖVE global is used. On a desktop with LÖVE and `timeout`, run `sh tests/gui_smoke.sh` for the original no-enemy targeting/camera scenario, `sh tests/enemy_gui_smoke.sh` for enemy windup/impact/live-pending presentation, and `sh tests/boon_gui_smoke.sh` for developer loadout and boon playback. They print temporary screenshot locations for inspection. These callback-driven checks do not replace pressing keys yourself in `love .`.
