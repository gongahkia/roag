# ROAG — Game guide

**Menus and HUD:** the game now uses focused pixel menus inspired by Microcraft, a compact bottom HUD and a wider map view. **I** opens boons/character stats; **F1** opens help; **Esc** opens the navigable pause menu. [Reference screenshots, actual ROAG captures and verification](docs/ROAG_MICROCRAFT_MENUS.md).

**Artwork choices:** only **Classic built-in** (the original shapes, also the fresh-install default) and **Kenney Micro Roguelike** remain. Run `make kenney` to try Kenney, or select **Settings → Kenney Micro Roguelike → Enter** to keep it. The old Dungeon art, Automatic override mode and Published Paint art choice have been removed. Ground no longer contains scattered wall-cap sprites. [Current UI/art cleanup and captured frames](docs/ROAG_UI_POLISH.md).

ROAG is a LÖVE 11.5 roguelike. Three biome stages lead to the Final Sanctum, where defeating the Hollow Regent earns Victory. `love .` opens a title screen for a new or saved run; a new run enters seeded Overgrown Ruins with two connected floors, fog, secrets, a Thorn Warden elite and a timed extraction route. [Connected-floor expansion notes](docs/ROAG_MULTIFLOOR_EXPANSION.md) describe stairs, saves, Studio markers and current pacing limits; [terrain notes](docs/ROAG_TERRAIN_EXPANSION.md) cover the six new mechanical tiles and World Editor controls; [biome notes](docs/ROAG_BIOME_EXPANSION.md) and [encounter notes](docs/ROAG_ENCOUNTER_EXPANSION.md) cover the seven destinations and their enemies. `docs/ROAG_SPEC_v1.0.md` is the original design baseline. The authored 15×11 foundation and 35×25 combat demo remain available by their explicit map IDs to headless tests and GUI fixtures.

## Quick commands

Run these from this checkout: `make game` for the playable game, `make test` for headless gameplay/runtime checks, and `make gui` for game GUI smokes. Run `cd ../paint && make app` for artwork, room, theme, and audio authoring. `make help` lists current shortcuts. On systems with LÖVE on `PATH`, `love .` opens the game.

## Run and controls

Run `make game` (or `love .` when LÖVE is on `PATH`). The title starts with **Continue**, **New Game**, **Achievements**, **Settings**, and **Quit**. Continue shows the existing single saved run's character, stage, floor, seed and HP before resuming. New Game opens character selection; choose one of six characters, then begin. Replacing a resumable, incompatible or damaged save requires confirmation. Arrow keys and Enter navigate, Esc goes back, and the options can be clicked. Existing **C** quick-continue, **N** quick-start and **1–6** class shortcuts remain available. During normal gameplay **Esc** opens a pause menu: **1/Esc Resume**, **2/S Save & Quit**, **3 Settings**, or arrows plus **Enter** on the highlighted option. Esc still cancels aiming, chest and route menus first. Saving, loading, settings and pausing cost no world time or RNG. Save & Quit writes the latest stable player-ready state and exits only after the save is read back successfully; a failed write leaves the game open. A mid-animation save resumes with clean presentation because all authoritative effects have already resolved. Milestone autosaves follow chest claims, elite defeat, pursuer arrival, stage transition, death and Victory. Hard termination resumes from the latest verified milestone; it may lose actions since then. The game does not save every frame.

**Settings → Sprite Pack** offers **Classic built-in** and **Kenney Micro Roguelike**. Up/down or the mouse wheel selects; Enter/click applies, Esc returns. The choice applies without restarting or changing the run and survives relaunch in separate checked `visual-pack-a.roag` / `visual-pack-b.roag` records. Old removed-pack choices safely select Classic until you choose again. Missing/corrupt Kenney artwork falls back to shapes at startup; an invalid warm F5 refresh retains the working visuals. `make spectator` previews Kenney without changing the saved choice.

LÖVE writes two rotating `active-a.roag` / `active-b.roag` records inside its platform save directory under the stable `roag-foundation` identity. Each record stores a generation, snapshot/content compatibility IDs, payload size and checksum. The next generation is written to the inactive slot and read back; startup chooses the newest integrity-valid generation, falling back one generation if a slot is corrupt. A valid newer incompatible save is reported instead of silently loading an older run. Confirmed New Run replacement writes the inactive slot first and verifies it before replacing the older slot; if the second write fails, the game warns that only one new generation is verified, and the next checkpoint replenishes the backup. The codec is inert, length-prefixed plain data: it never evaluates save text. No hardware power-loss fsync guarantee or cross-version migration is claimed. Terminal/dead runs write verified non-resumable tombstones to both slots, so one damaged terminal slot does not revive an older active backup. Permanent unlocks use separate profile records.

The compact HUD shows health, XP/level, gold, biome, stage/floor and committed-attack feedback. **I** opens the read-only boon list and character stats; **V** pages boons and **Enter** inspects a selected boon. **B** retains its existing developer-loadout shortcut. The minimap shows explored terrain; the arena location appears only after exploration reaches it. **N** proposes restarting the same root seed and **M** proposes the next seed; normal play requires confirmation before either reset replaces the saved run. **Tab** shows/hides the discovered map without spending world time or RNG. Normal New Run uses two floors; `Game.new_stage(seed)` remains a single-floor test fixture for older callers. Move with WASD/arrows; Space or `.` waits. Press **F** for Sword Strike, **R** for Sweeping Slash, or **X** for Dash; aim with WASD/arrows or Q/E/Z/C, then Enter. Escape cancels. Cardinal bump attacks, doors and chests use ordinary movement. After defeating the biome elite, bump the cyan exit to preview three routes; **1/2/3**, Enter and Escape select, confirm and cancel. Stage 1 offers Fungal Hollows, Sunken Aqueduct or Glacial Observatory; stage 2 offers three deterministic unvisited destinations. A third-stage exit offers **Enter the Final Sanctum**. Confirming costs no world time. The final chamber contains the Hollow Regent and no biome pressure or pursuer. Its three locked attacks are Regent Line, Broken Crown and Hollow Sigil; it changes speed below half HP. Entering alone never counts as Victory.

**Explore:** Sight reaches eight tiles on the logical grid. Walls and closed doors block vision; unseen tiles show no known geometry, and previously seen tiles remain dimly remembered. Enemies, drops, chests and telegraphs appear only where currently visible. The minimap uses the same explored state and never shows undiscovered branches. Enemies continue acting outside the player's view. FOV updates at simulation decision boundaries, not each render frame.

**Discover:** Each generated floor places two optional secrets in transformed branch rooms. A thin diagonal moss crack on a wall means an explicit Sword Strike into that wall can open a tiny alcove with a cached free boon chest. A small moss spiral on the floor releases four currency when stepped on; the pattern fades after activation. These are consistent clues at variable seeded locations. Attack misses still cost normal time. There is no inspect/search key, and neither secret is required to reach the arena. A closed brown ruin door sits at an optional branch socket: bump it once to open for one normal movement recovery, then move into its tile. It blocks sight and projectiles while closed. Opening it never shifts a room or loads another screen.

**Earned rewards:** Kill enemies, then step onto their yellow currency drops. Five paid chests are spread across the two generated floors of a biome. A cardinal move into a closed chest opens its three cached boon offers for free. Choose with 1/2/3 and press Enter to claim; Escape cancels without spending currency or world time. A claim costs five currency and one 100-base-time recovery. The claimed chest can be walked through. The chest menu blocks movement and attacks while open. Guard/Thornspitter/Skitter drops are 4/3/2 currency. A fresh run starts with no boons; there is no guarantee of enough initial currency to buy every chest.

Stage enemies can also leave green food (+4 HP) or red health pickups (+10 HP); biome elites guarantee a health pickup. Move onto one while injured to heal, capped at maximum HP. A full-health player leaves it on the floor. The supply roll is derived from the run seed and killed entity ID, independent of combat/chest RNG. Title keys **5/6** choose the Sentinel or Alchemist, both with area attacks. Their normal chest pools also include **Aftershock** (direct-hit burst) and **Reprisal Nova** (burst after taking hostile damage), at all four rarities. Existing classes retain their established offer pools. See the [class and pickup notes](docs/ROAG_CLASS_EXPANSION.md).

**Developer showcase:** Press **B** while idle to cycle None → Lightning → Cascade → Transformation → Firestorm → Blade and Lunge → None. This changes the test loadout without advancing world time; it is a debugging control, not the earned acquisition path. **N** restarts with the selected showcase loadout. The sidebar lists C/U/R/L stack counts, seven boons per page; **V** changes pages without world time. Firestorm demonstrates Emberbrand, Flashpoint and Grave Dividend. Blade and Lunge demonstrates Longblade Geometry, Phantom Lunge and Pulse Step. Headless callers can use `Game.set_boon_stacks(state, owner_id, boon_id, rarity, count)` at a stable decision boundary, including zero to remove a rarity stack.

Each generated floor places enemies from its biome pool across rooms and selected corridor alcoves. Thorn Warden guards Overgrown Ruins; the other six biomes have distinct named elites with two locked attack footprints. Defeating the elite creates a free elite chest and unlocks the nearby exit. The old authored demo remains `Game.new(seed, "core:map/scrolling_room", {demo_enemies=true,demo_chests=true})`. The compact bottom HUD shows HP, XP, gold and attack feedback. I opens boons and character stats. Level-up notifications are brief and do not block input. Death ends the run; N proposes restarting its seed.

## Generated floor contract

`src/biomes.lua` registers seven stable biome IDs, route descriptions, generator strategies, visual symbols, encounter pools and elite IDs. `src/rooms.lua` contains protected socket-marked templates and biome-specific room families. Studio E03 can author compatible optional room content; opening Studio does not rewrite bundled rooms. The new Observatory uses radial star nodes and ice, Archive uses interrupted shelf aisles and mist, and Forge uses offset furnace bays and crystals. Fungal, Aqueduct and Bastion retain their winding pockets, parallel canal galleries and courtyard hubs.

Normal `src/generation.lua` stage creation makes two connected 104×68 floors with six rooms each, five paid chests and two secrets across the pair, reciprocal reachable stairs, a lower-floor elite and an exit after the elite. Its original single-floor generator remains available to older test fixtures. Each generated map stores collision rows separately from visual symbols, room/corridor provenance, protected masks, placements and deterministic diagnostics. The shared AI, targeting, fog and rewards systems operate on the active concrete floor. See the connected-floor, terrain and biome expansion notes for measured generation corpora and repair limits.

Generation validates the **actual cardinally traversable tiles**, sockets, protected mask, graph loop/connectivity, start safety, room clearance, unique placement IDs, reachable arena, chest approaches, two optional secret clue approaches, their intact hidden wall/cache geometry, and an approach to the optional door. A blocked editable corridor cell or damaged clue visual can be repaired locally and is revalidated; protected faults remain untouched. Two deterministic retries follow an unrepaired failure; a known-reference candidate is independently validated as fallback and flagged in diagnostics. Seeded test faults exercise repair and fallback. The visual layer adds moss, vines and scattered rubble without changing collision.

`state.generated_map` is the authoritative concrete map for generated biome runs, and `state.final_map` stores the concrete authored final chamber in finale snapshots; the content registry remains unchanged for fixture maps. Movement, AI BFS, ranged blocking, Dash, targeting, FOV and rendering all use `Content.map_for(state)`. The saved run stores concrete geometry, revealed passages, explored cells, secret and door states, cached offers and runtime entities; restore validates and uses that map without regeneration. Snapshot schema is `roag-run/11`, boon content version `roag-boons/3`, content version `roag-content/8`, and exploration content version `roag-world/1`; older snapshots require a fresh run.

Generation uses `RNG.derive(run_seed, floor_id + attempt)` and never consumes the combat/progression RNG. Generated chest offers each use a stream derived from the run seed, floor identity, and stable chest marker ID; offers are cached once. Adding decoration or another generated chest therefore does not change crit/stat rolls in an otherwise controlled combat sequence. Legacy authored-map chest RNG behavior stays as before.

## Provisional combat and timing

| Actor | HP | Damage | Defense | Speed | Windup | Recovery | Movement cost | Range |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Adventurer | 24 | 4 Sword / 3 Sweep | 0 | 100 | Immediate | 100 Sword, 150 Sweep | 100 | Adjacent |
| Mossbound Guard | 10 | 3 | 1 | 70 | 80 | 120 | 100 | Cardinal adjacent |
| Thornspitter | 6 | 2 | 0 | 90 | 90 | 130 | 100 | Cardinal lane, 2–7 |
| Ruin Skitter | 4 | 1 | 0 | 160 | 35 | 70 | 100 | Adjacent, prefers diagonal flank |
| Spore Herald (Fungal) | 7 | 2 | 0 | 85 | 100 | 135 | 100 | Locked three-cell spore patch; retreats from close contact |
| Canal Lancer (Aqueduct) | 9 | 3 | 1 | 90 | 75 | 120 | 100 | Locked two-cell cardinal thrust |
| Cinder Bellkeeper (Bastion) | 12 | 3 | 1 | 65 | 115 | 145 | 100 | Locked five-cell cross around the player |

These numbers are in `src/content.lua` and are provisional. Spore Herald, Canal Lancer and Cinder Bellkeeper grant XP/currency of 5/3, 5/4 and 6/4 respectively. Dash costs 130. Effective time is `max(1, round(base × 100 / speed))`, calculated with integer inputs. Damage is `max(0, attack damage − defense)`, capped at remaining HP. Direct Adventurer Sword and Sweep attacks roll the seeded crit chance once per attack; a crit rounds `raw damage × crit damage percent / 100` before defense and HP caps. Starting chance is zero, multiplier 150%; boon secondary damage does not crit automatically. Player strikes resolve immediately; enemies lock their target cells when a windup begins. An enemy has **no ready turn during its windup**. Its next ready time is impact time plus effective recovery. Ordinary damage does not cancel the locked strike; stun, displacement and death do. `Game.stun` cancels a windup and makes the enemy ready no earlier than the supplied integer stun expiry; `Game.displace` cancels it and applies one normal recovery before the enemy is ready again. A Thornspitter shot stops at the first combatant in its locked lane, including an allied enemy. The three new area and thrust attacks use locked cell batches, can hit allies in multiple cells, and respect projectile-blocking geometry at impact. Terrain walls block firing lanes.

Sweep retains its provisional three-cell counterclockwise/center/clockwise order. All hit HP changes in an area attack are applied as one batch before `DamageBatchResolved` and individual `DamageAttempted`, `DamageTaken` or `DamageBlocked`, and `Died` records. `DamageTaken` requires positive actual HP loss. `HitConfirmed`, `EntityKilled`, and `TileEntered` are now emitted in boon-free runs too. Each actual enemy death yields one `EntityKilled`. Ordinary and elite enemies drop currency; credited player kills grant XP, including player-owned boon and delayed kills. The terminal Regent gives no extra drop or XP. Enemy-only friendly-fire kills yield no player XP. Events retain causal and spatial facts for downstream rewards and boons.

## Provisional progression and chests

Enemies grant Guard/Thornspitter/Skitter XP of 5/4/3. Cumulative level thresholds are 5, 15, 30, 50, then continue by an extra five XP per interval. Every crossed threshold automatically chooses one of six equally weighted stats through the saved simulation RNG: max HP +4, Sword and Sweep damage +1, defense +1, speed +10, crit chance +5 percentage points, or crit damage +25 percentage points. Max HP growth adds the same absolute amount to current HP; it does not refill missing health. An upgrade never changes recovery already committed. The HUD reports total XP and the next cumulative threshold. All parameters and weights live in `src/content.lua`.

Standard chests cost five currency and roll each of three **distinct boon identities** once, with independently rolled Common/Uncommon/Rare/Legendary weights 70/25/4/1. The same boon may be offered by separate chests and stacks by rarity. `Game.spawn_chest(state, "core:chest/free_elite", x, y)` supplies the Warden's zero-price reward chest with 20/45/28/7 rarity weights. Offers are cached in the chest's plain-data entity before interaction, so Escape, reopening, and restore do not reroll them. The committed claim validates adjacency, funds, choice, and boon compatibility before one debit, one stack grant, one claim flag, and one effective 100-base recovery. `Game.chest_options` reads a copy of offers at a player decision boundary.

At equal simulated time, committed impacts resolve before newly ready actors. The player acts before newly ready enemies, then remaining actor ties use stable schedule sequence/ID order. A fast enemy can strike before the player is ready again; no reaction turn is inserted. Guard uses cardinal BFS around walls, Thornspitter seeks a clear lane and repositions, and Skitter seeks a diagonal flank. AI decisions use only simulation state and stable neighbor order.

## Presentation and architecture

`src/game.lua` owns combat state, action validation, damage, occupancy, interruptions, events, and snapshot/restore. `src/rewards.lua` consumes canonical kill events for XP/drops, collects drops on movement, and handles chest offer generation/claims. `src/ai.lua` chooses read-only enemy intents. `src/scheduler.lua` orders actor readiness and committed impacts. `src/targeting.lua` keeps Adventurer attack shapes. `src/content.lua` keeps authored maps/tile properties and tunable combat/reward definitions. `src/rng.lua` owns serializable gameplay RNG and stable stream derivation. None of these simulation modules references LÖVE.

`main.lua` renders only camera-visible generated tiles plus combatants, HP and locked tiles. `src/camera.lua` follows the player's **visual** tile position smoothly without affecting world time. `src/playback.lua` consumes **copies of already resolved ordered events** for short visual stages, including boon activations, secondary damage and delayed resolution. Player and enemy movement slides between tile centers in 80 ms per tile; actors in one movement beat slide together, and Dash follows its recorded traversed cells. A crowded beat is capped at 320 ms of presentation time. During playback, input is briefly gated; orange/red historical telegraphs are labeled as past events. When input is available, red highlights and the sidebar show only **currently pending authoritative strikes** and their simulated time to impact. Enemy sprites face their displayed horizontal movement or committed attack direction; vertical motion and symmetric area attacks keep their previous facing. Knockback does not turn them. Attack warnings use dashed tile outlines with clear interiors (amber during windup playback, red for impacts and live pending strikes), so the art remains visible. Presentation state is never included in a simulation snapshot.

`src/visibility.lua` computes deterministic radius-eight wall/door-occluded FOV and per-floor explored memory. `src/world.lua` holds plain-data secret definitions and consumes canonical `AttackPerformed`/`TileEntered` events after the primary damage batch. It opens the wall/cache and creates a free chest, or creates and immediately collects the movement cache's normal currency drop. Secret placement uses a derived seed stream; the free chest uses its own stable marker offer stream. The `World.on_event` subscription seam also recognizes a future `ItemUsed` event shape (`source_id`, `item_id`, `target_position`, `action_id`); no item inventory or item action exists yet.

## Boon resolution contract

`src/boon_content.lua` contains twenty base definitions and four profile unlock definitions with four rarity parameter rows each. `src/boons.lua` indexes subscriptions by event kind, matches composable AND/OR/NOT and identity/tag/faction/damage/spatial predicates, aggregates stacks, and selects activations using the saved simulation RNG. Inventory is one plain table per owner: `boon_id -> rarity -> count`. There is no copy or inventory cap. Storm Conductor uses 2/6/18/50% per Common/Uncommon/Rare/Legendary copy; chances add and overflow into guaranteed plus fractional activations. Proc coefficients are declared on ability/effect content (default 1), validated, and multiply only probability-based activations. Other boon powers add across rarity stacks.

The eight prototypes are Storm Conductor (hit lightning), Detonation Bloom (credited-kill explosion), Thorn Mirror (positive-damage retaliation), Static Footsteps (move lightning), Resonant Wounds (two distinct hostile damage targets adjacent in one chain), Temporal Echo (locked repeat damage after 200 simulated time), Crescent Reach (Slash gains a forward cell and additive damage), and Turncoat Spark (observed enemy friendly-fire retaliation). Current provisional powers by rarity are 1/2/3/4; area radius is one tile. Static Footsteps chances are 5/15/35/70%. Observation radius uses Manhattan distance; Resonant Wounds and Turncoat Spark observe within eight tiles, Storm lightning selects a second hostile within six tiles, and Static Footsteps selects within two. Resonant adjacency and area centers use captured damage-event positions; area damage checks current occupancy. Pair identities are deduplicated per chain. Crescent Reach's forward cell requires an open middle and destination tile, so it cannot reach through a wall.

The twelve additional boons are Emberbrand (three Burning ticks), Flashpoint (burst near two Burning enemies), Grave Dividend (credited-kill heal), Opening Cut (full-health first hit), Crowdbreaker (bonus per neighboring foe), Harrier's Response (windup spark), Perfect Guard (blocked-hit retaliation), Final Spark (offense at ≤40% HP), Pulse Step (two-neighbor movement pulse), Packed Powder (one blast per disjoint triple of same-chain deaths), Longblade Geometry (explicit Sword Strike two-cell line), and Phantom Lunge (one strike per enemy beside the actual Dash path). Their Common/Uncommon/Rare/Legendary powers are authored per identity in `src/boon_content.lua`; Harrier's proc chances are 8/18/35/65% per copy. Burns refresh to three remaining ticks and retain the stronger current power, with one scheduled tick per affected entity every 100 simulated units. Tick damage follows ordinary defense and kill attribution. Dash remains cardinal with 130-base recovery, and a first-cell block rejects it without time. Longblade leaves bump attacks one cell wide. These values are provisional.

The game queues emitted events and effects iteratively after each primary damage batch and before the next scheduled actor decision. `AttackPerformed` can describe a miss; `HitConfirmed` requires contact, `DamageTaken` requires positive HP loss, and `EntityKilled` accompanies every actual death. `TileEntered` records each committed tile entry, including both cells of a full Dash. These factual events do not depend on owned boons. Existing exact-array tests were updated for the additional stable events while retaining their behavioral assertions. Events capture source/target faction, definition tags, positions, action/chain IDs, parent ID, coefficient and branch ancestry. Secondary damage uses the same HP, death, friendly-fire and ordered event pipeline. A boon may recur in sibling branches but cannot reactivate on a branch already containing its owner/boon key. One million resolution steps is a diagnostic error, not a silent effect cap.

Scheduled echoes use `src/scheduler.lua`'s committed-effect priority: they resolve before ready actors at the same timestamp. They retain locked target cells, attribution, chain ID and ancestry. The in-memory snapshot includes the generated concrete map and diagnostics, level/XP, currency, drops, chests with cached offers, player stats, inventory, delayed payloads, RNG and scheduler state. Older snapshots are rejected. Ability modifiers are reapplied in stable boon and declared modifier order from inventory without mutating `src/targeting.lua`'s base patterns. Cross-version save migration remains out of scope.

`Game.stun` and `Game.displace` remain the small simulation operations for interruption; there is no player stun/knockback ability. Burning is the current compact scheduled damage status, stored as plain entity data.

## Tranche 07 stage rules (provisional)

Normal `love .` uses `Game.new_stage`; older generated-map fixtures use `Game.new_generated` without a stage director. The Warden waits in the reserved arena until the player comes within seven cardinal tiles. Its three-cell Thorn Sweep and locked cardinal Root Line alternate when both are available. Below 50% HP it enters phase two and gains 25 speed for future commitments. Existing stun, displacement, death, friendly fire, and boon damage work through the combat resolver.

Before elite death, the pressure director first checks at world time 1,200 and then at 1,200/900/650-unit intervals as successful reinforcement count grows. It uses a separate seeded RNG, unseen reachable room anchors, an eight-tile player exclusion radius and an 18-normal-enemy cap. There are no frame-time spawns. Elite death produces one free enhanced-rarity three-offer chest and unlocks one exit. A 1,500-unit simulated-time grace period begins at the actual death timestamp; when due, one immune pursuer spawns at a distant unseen reachable point. The pursuer uses normal navigation, a locked adjacent strike and the scheduler; attacks against it produce blocked-damage feedback but no positive damage, kill reward or hit proc. The exit remains usable during pursuit.

Exit menus cost no world time. Route confirmation is an atomic **zero-time** stage handoff; the next generated stage retains player HP, stats, XP, currency, boons and gameplay RNG while stage-local actors, attacks, echoes, pressure and fog are retired in a frozen plain-data record. Later stages receive distinct floor instance IDs and route-derived seeds. Stage 1 is Overgrown Ruins; Stage 2 offers Fungal Hollows, Sunken Aqueduct or Glacial Observatory; Stage 3 offers three deterministic unvisited choices drawn from the remaining biome identities, including Fractured Archive and Sundered Forge. [Biome expansion notes](docs/ROAG_BIOME_EXPANSION.md) describe the route graph and terrain geometry. The Thorn Warden is exclusive to Overgrown Ruins; each other biome has its own elite with two attack footprints and a phase change. The third exit enters the authored Final Sanctum. Earlier stages, including their floor-local effects, remain frozen and cannot damage the finale floor. Snapshot schema `roag-run/11` stores the active boss, terminal result and retired stages. Persistence envelope `roag-save/1` serializes that existing snapshot without regenerating the floor.

## Verification

Run `luajit tests/run.lua` for headless tests, including the 100-seed elite placement corpus, controlled stage loop, codec, corruption/failure injection and byte-level resume tests. The eight `tests/*gui_smoke.sh` harnesses include `stage_gui_smoke.sh` and `persistence_gui_smoke.sh`; the latter saves in one LÖVE process and continues from the same platform save directory in another. These callback-driven checks do not replace personally pressing keys through an unassisted run in `love .`.

## Tranche 09 acceptance and diagnostics

Run `luajit tests/run.lua` for deterministic headless regressions. `luajit tests/acceptance_trace.lua /tmp/roag-acceptance.tsv` writes a replayable seed-3 **guided legal-action** trace: ordinary enemy kills, a paid chest and functioning boon, Warden phase/death, free chest, route transition, and stage-two combat. The guide reads world state to choose inputs and does not grant health, currency, damage, or position. It is an automated strategy, not a human survival run. `tests/repeat_gui_smokes.sh 5` runs all eight callback-driven GUI harnesses in isolated LÖVE processes and retains every result and stderr under a temporary directory. `tests/persistence_gui_smoke.sh` uses an isolated LÖVE save directory and two processes. None of these commands writes a player's normal save.

Run `luajit tests/profile_headless.lua 9` for opt-in simulation timing on a generated stage, valid synthetic crowds, and a high-stack combat chain. Run `sh tests/profile_gui.sh` for callback-driven observed frame intervals and Lua update/draw CPU times on the current display; it writes samples and screenshots to a temporary directory. Press **F3** during `love .` to show or hide the presentation diagnostics overlay. The overlay reports observed frame intervals, callback CPU timing, FPS, visible actors, draw calls and texture bytes when LÖVE exposes them. This instrumentation is disabled by default and has no authoritative game-state effects. See `docs/TRANCHE_09_ACCEPTANCE.md` for interpretation and a manual playtest checklist.

For movement frame verification, run `sh tests/motion_gui_smoke.sh`; it captures rendered start/mid/end positions for the Adventurer and a Guard, a partial and full Dash, and a scrolling camera. `sh tests/build_gui_smoke.sh` checks Burning, Longblade targeting and the playback input gate, while `sh tests/boon_disk_resume.sh` checks a new-boon status and Temporal Echo across fresh LuaJIT processes using a temporary save directory. `sh tests/profile_gui.sh` samples a synthetic 40-enemy scene and each new biome under the current display/renderer. See [Tranche 10 movement notes](docs/TRANCHE_10_MOVEMENT.md) for timing and corridor findings. `sh tests/biome_gui_smoke.sh` captures all three new biome visual identities, signature telegraphs and route UI. `sh tests/biome_disk_resume.sh` verifies Save & Quit / Continue in two fresh LÖVE processes per new biome, using isolated save directories. `luajit tests/biome_acceptance_trace.lua /tmp/roag-biome-trace.tsv` writes a guided ordinary-action route through Fungal Hollows and Ashen Bastion, including a paid boon, specific enemy kills and currency pickup. `luajit tests/profile_biomes.lua` measures simulation cost. `sh tests/finale_gui_smoke.sh` captures a prepared final route, locked telegraph, phase change, Victory and New Run. `sh tests/finale_disk_resume.sh` verifies Save & Quit, Continue and a terminal Victory tombstone across three fresh LÖVE processes with a pending Regent attack and Temporal Echo. `luajit tests/finale_playthrough.lua 3 /tmp/roag-finale-seed-3.trace` writes a complete ordinary-command Victory trace; `luajit tests/profile_finale.lua 3` profiles its boss actions; `luajit tests/finale_corpus.lua` validates 100 assisted three-stage route combinations and final chambers. These tests do not replace physical-keyboard play. Permanent variety unlocks are described below.


## Permanent variety unlocks

Press **U** on the title to view four achievements and their reward boons. A normal New Run can earn them through Thorn Warden defeat, a diegetic secret, three distinct boon activations in one causal chain, and surviving a Hollow Regent Victory. The rewards are Guardian's Vow, Prospector's Heart, Circuit Chorus and Crownfire. They expand the three-offer chest pool in **subsequent** runs; the original 20 boons remain available immediately. The current run keeps the pool it started with. Developer **B** loadouts and GUI autostart fixtures are ineligible for permanent awards.

The profile uses separate `profile-a.roag` / `profile-b.roag` generations in LÖVE's platform save directory. A checked write to the inactive slot protects the previous valid record against a failed or truncated write, subject to platform flush limits. A terminal run with an uncommitted award first writes two verified non-resumable pending-terminal run records, verifies the profile award in both profile generations, then writes the existing terminal tombstone. If an award write fails, the title shows the pending status; press **R** to retry after storage is restored. Corrupt or incompatible profiles are preserved and block progress-bearing New Run until repaired externally. Active-run schema is now `roag-run/11`, boon content `roag-boons/3`; older active runs can be rejected without erasing a compatible separate profile. See [Tranche 14 notes](docs/TRANCHE_14_META.md).

Run `luajit tests/meta_acceptance_trace.lua` for a legal seed-1 secret unlock followed by a seed-34 paid chest purchase and actual activation of the new boon. Run `sh tests/meta_gui_smoke.sh` for the isolated two-process title/profile/future-run demonstration. Both tests use disposable storage and do not touch a player's normal profile.

## Paint authoring and optional visuals

### Spectator art inspection

Run `make spectator` to start a disposable, invulnerable inspection run using
the **Kenney Micro Roguelike pack**. This developer view does not read
or write active-run saves or achievement profiles and cannot earn unlocks.

**F6** toggles god play and a free camera. In free-camera mode, **WASD/arrow
keys** pan one tile without advancing world time; hold **Shift** to pan eight
tiles. The map and enemies are revealed, and the cyan outline marks the
inspected cell. The bottom overlay names that cell's resolved visual ID and
source. **F7** cycles biomes, **F8** switches between their two generated
floors, **F9** cycles classes, **M** advances the seed, **F5**
refreshes the selected sprite pack, and **Esc** quits without saving. F6 returns to
invulnerable gameplay so you can inspect combat poses. Ordinary `make game`
retains its normal saves, achievements and fog rules.

Studio's editor code now lives in the sibling **TAMS Paint** checkout. Run
`cd ../paint && make app` to open the ROAG artwork checklist. The migrated
editable project uses Paint's separate `tams-paint` LÖVE save identity; the
original `roag-foundation` run/profile data are untouched. Paint retains the
World, UI and Audio editors as secondary views. Its README documents the
individual save/bind/publish flow and the explicit CLI publishers.

ROAG retains read-only format validators and published-content readers because
saved generated floors and optional loose/world/theme/audio/atlas packs refer
to their stable IDs. None of these readers opens editor UI or changes simulation.
The hand-painted published PNGs were archived in Paint and removed from this
game checkout. The later generated Dungeon pack and its compiler have also been
removed at the user's request. Game artwork now comes exclusively from Classic
shapes or Kenney; Paint visual/theme/atlas overrides are not activated by either
choice. World geometry and audio readers remain independent. Historical Studio
source, publication contracts and Paint authoring remain in the sibling project.

Historical Studio E00–E06 guides remain in `docs/` for format and workflow
background; the active authoring commands now live in Paint.

## Autonomous expansion in progress

Normal New Run now starts in a connected two-floor biome; reciprocal stairs
preserve the inactive floor and its pending events. Seven biome identities,
six gameplay terrain mechanics, 16 regular enemy archetypes and seven distinct
biome elites are integrated. On the title, press **1–6** to choose Adventurer,
Ranger, Arcanist, Berserker, Sentinel or Alchemist before **N**. All classes use the existing
WASD/arrow movement, **F** basic attack, **R** special and **X** mobility aim
and confirmation flow. See the [class kit guide](docs/ROAG_CLASS_EXPANSION.md)
and [expansion progress](docs/goal/ROAG_EXPANSION_PROGRESS.md) for exact
mechanics, current regression evidence and outstanding milestones. Separate
ordinary submitted-action traces now clear six generated floors and the final
boss for the original four classes; the older single-floor guide is still retained as
a narrower regression check.

**Continue → Continue Run** resumes the saved class and sprite.
**New Game → Character → Begin New Game** starts the selected class; an
existing save asks for confirmation before replacement. The original single-run
save format and files are unchanged. **C** and **N** remain direct shortcuts;
**1–6** opens the character screen as a shortcut.
In game, moving or aiming left/right mirrors the selected class sprite; up/down
keeps its last horizontal facing. The camera uses a fixed 40px tile size, slightly
wider than the previous 48px view. Wheel and +/- zoom controls are disabled.
Facing and camera presentation do not advance world time or alter collision.

Standard LÖVE gamepad callbacks provide a menu path: the D-pad moves the title
selection, **A** opens or confirms, **B** goes back, shoulder buttons cycle
characters on the character screen, and **Y** quick-starts from the home or
character screen.
The D-pad also selects chest/route offers. During play the D-pad
moves or aims cardinally, **X/Y/right shoulder** choose basic/special/mobility,
and **Start** pauses. This reuses the keyboard input gate; eight-direction
diagonal aim still uses the keyboard. Controller hardware has not been
physically tested.

Normal New Runs also draw from the expanded **46-boon** ordinary pool, with
class-compatible transformations; the four achievement rewards remain
separate unlocks for a total of 50. See the
[M6 boon catalogue](docs/ROAG_BOON_EXPANSION.md) for exact triggers,
provisional rarity strengths and the seeded legal chest-acquisition trace.

Studio's [audio guide](docs/ROAG_AUDIO_EXPANSION.md) documents the Sound
Library, contextual cue picker, original WAV/Ogg import, checked dry-run and
explicit publication, scene music, and per-cue original fallback. F8 opens
audio assignment from a Studio room or theme; normal-game F5 refreshes the
optional audio pack alongside visuals. Audio is presentation-only.
The [expansion handback and physical-play checklist](docs/goal/ROAG_EXPANSION_HANDOFF.md)
lists reproducible gates and keeps Git source transfer separate from personal
LÖVE saves and Studio projects.
