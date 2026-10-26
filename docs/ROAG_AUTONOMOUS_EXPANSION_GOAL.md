# ROAG — Autonomous Expansion Goal Specification

**Version:** 1.0 · **Prepared:** 2026-10-12 · **Target:** GPT-6 Sol High, interactive Codex CLI `/goal` on Windows 11 / Fedora WSL2 · **Intended repository:** the current ROAG checkout.

**Status:** User-approved design contract. This file is one long-running goal, not authorization to sacrifice correctness for breadth. Read the actual source and latest project documentation before implementing. The Mac checkout is authoritative until the latest work is explicitly transferred and verified on Windows; Windows becomes the primary development location after this handoff.

## 0. Goal and definition of done

Expand the **existing playable LÖVE 11.5/LuaJIT roguelike** without replacing its architecture. Deliver all the following together:

| Area | Existing baseline (approximate; inspect actual checkout) | Required final outcome |
|---|---|---|
| Verticality | One active 2D floor per stage; multi-floor-ready data model | **Two procedurally connected 2D floors per biome**; no intra-floor elevation, jumping, ramps, or height-based attacks |
| Boons | 24 working boon identities | **50 working identities**, adding exactly **26** genuinely varied boons; Common/Uncommon/Rare/Legendary for every boon |
| Biomes | Overgrown Ruins, Fungal Hollows, Sunken Aqueduct, Ashen Bastion | **Seven** mechanically and visually distinct biomes; autonomously design **three** new themes |
| Terrain | Existing floor/wall/water and secret mechanisms | **Six additional, distinct gameplay-significant terrain mechanics**, tested and editor-authorable |
| Normal enemies | Six | **Sixteen**, with **ten** new distinct tactical archetypes |
| Minibosses | Thorn Warden reused throughout | **Seven distinct biome-specific minibosses** (keep Thorn Warden for Overgrown Ruins; add six); existing Hollow Regent remains final boss |
| Player classes | Adventurer | **Four immediately available classes:** Adventurer, Ranger, Arcanist, Berserker |
| Studio audio | No comprehensive audio picker/binding system | Contextual **sound and music asset browser, import, audition, binding, configurable transitions, publication, runtime playback**, with safe missing-asset fallback |
| Run length | Three biome stages + Hollow Regent final boss | **Unchanged**, with a richer set of optional branch choices; don't require visiting seven biomes in one run |

Not done until the features are **playable and observable in a normal LÖVE session**, included in generated worlds and class selection/reward pools, compatible with saving, and exercised by meaningful headless, GUI and legal-action checks. Adding metadata, placeholders or isolated unit tests alone does not complete a milestone.

Use the existing validated content schemas and extension seams. Prefer concise reusable data-driven mechanisms and content over broad refactors, external frameworks or a new ECS. The user chiefly wants **variety and a good game**.

## 1. Non-negotiable preflight and Mac → Windows handoff gate

1. **Do not edit game code until provenance is established.** The user's Intel MacBook repository is authoritative until its intended source changes are transferred. A GitHub push from Mac followed by a pull on Windows is an acceptable transfer: obtain the user's explicit confirmation, fetch the intended upstream, verify that upstream is an ancestor of local HEAD, inspect every local-only commit for unexpected source changes, check the working tree for conflicts, and confirm this specification and the complete project are present. A legitimate local goal-document commit may make local HEAD a descendant of upstream; exact HEAD equality is not required. If ancestry fails or unexpected source divergence exists, **STOP with the specific discrepancy**. A staged archive, dated export manifest or transfer marker is an alternative, not a mandatory artifact when Git synchronization is verified. Git proves only committed repository contents; it does not transfer uncommitted, ignored or external Studio save-directory data.
2. **Protect preexisting Windows data.** Inventory local changes and any separate staged transfer before merging or copying; preserve unclear differences in a non-destructive backup. Never run `git reset --hard`, `git clean -fdx`, recursive deletion, or bulk checkout overwrites to resolve divergence. Do not rename the existing checkout directory as part of this goal.
3. **Studio's LÖVE save directory is outside Git.** Verify the location with `love.filesystem.getSaveDirectory()` under the actual application identity. The latest Mac save directory, if any, may include editable layered sprites, room/theme/source projects, audio to be added, active-run records and the permanent profile. Preserve or import only after inspection and safeguards; do not overwrite existing player data. Missing old personal artwork/saves is already a known acceptable condition and must **not** block programming; do not invent or claim recovery of it.
4. Once the Mac transfer is verified, treat Windows/Fedora WSL as the **primary development checkout** for this goal. Do not edit the Mac checkout through a shared mount, remote session, or concurrent branch during autonomous implementation. Produce a final **portable handback/export summary** so future Mac work can import changes without losing data.
5. Confirm Fedora WSL, LuaJIT and LÖVE 11.5, actual commands and script dependencies. Start by running the real baseline (`luajit tests/run.lua`, established game and Studio GUI smoke suites, current legal Victory driver, saves). The last reported baseline before additional Mac edits was **272/272 headless**, established game GUI **24/24** and Studio GUI **18/18** across three rounds, and a seed-3 legal-action Victory of **991 actions, 5 HP, world time 98,800**. These are historical reference values, **not** proof of the current baseline. Record actual results.
6. Verify user-supplied source/audio license information and file safety. Only modify this project's checkout and disposable test fixtures; never touch live personal or unrelated project saves as part of testing. Preserve any existing uncommitted work.

**Hard gate:** If Git ancestry/local-only changes or an alternative transfer cannot establish committed-source currency and safe ownership, report `BLOCKED: MAC→WINDOWS HANDOFF` with exact missing inputs. Do not proceed to milestone 1 on an uncertain source tree. The user has confirmed that the completed Mac→Windows source transfer for this goal used GitHub; its verified local evidence is recorded in `docs/goal/ROAG_EXPANSION_PROGRESS.md`. Missing personal Studio data is a separate, nonblocking resource unless a specific feature requires it.

## 2. Architecture and gameplay invariants

Retain and test these established rules throughout:

- **Deterministic, headless simulation**, pure-Lua authoritative state and reproducible seeded RNG with separate generation/reward/combat streams. Rendering, animation, sound, mouse movements, frames and real time must not affect gameplay outcomes.
- World advances **only after committed player gameplay actions**, via integer simulated time and speed-based recovery. Baseline movement cardinal; attacks/abilities have class-specific directional patterns. Basic cardinal bump-attack, explicit directional attacks, empty attacks consume time, invalid movement/aim cancel does not. No ordinary cooldowns, mana or stamina; abilities use action costs.
- **Strict enemy scheduling:** no guaranteed player reaction turn. Telegraph target cells **lock at commitment**. Committed impacts win same-timestamp ties; player wins ties among newly ready actors. Normal damage does not interrupt windups; stun/displacement/death does. Friendly fire is on.
- Primary multi-target damage batches before dependent boon reactions. Boons may observe player and **enemy** actions and spatial/multi-event predicates within a causal chain. Mixed rarities stack without ownership caps; additive chance **overflows above 100%**. Proc coefficients, branch-specific ancestry safety, ordered effects and delayed serialized actions remain correct. Ability transformations modify existing ability identities and declare compatibility.
- Preserve four rarity tiers; automatic randomized stat upgrade on XP level-up; currency drops, roughly **five paid choose-one-of-three chests per biome** plus free elite chest; two optional diegetic secrets **per biome**, existing fixed secret rules; no dedicated inspect button. Existing meta-achievements and unlocked pools stay separate from active-run saves; previously available boons do not suddenly become locked.
- Continuous rooms and corridors, enemy pursuit across rooms on the active floor, fog and remembered minimap; doors are world entities; existing biome generators use authored/socket-marked templates, controlled variations, deterministic validation, allowed automatic repair, bounded retries, fallback. `floor_id, x, y` is authoritative. The editor and runtime use the same validated room and asset definitions. No general 3D engine.
- **Active-floor-only simulation.** Inactive floors retain exact state (scheduled windups, delayed effects, fog, pickups, enemies) and remain frozen; loading or revisiting must not catch up overdue events or duplicate awards. Confirm clock semantics explicitly and test them.
- A stage has **one miniboss**, an elite free chest, a simulated-world-time extraction grace and an effectively invulnerable pursuer. Changing floors must **not reset or bypass** the stage countdown/pursuer. No softlocks. Three chosen biome stages, then Hollow Regent final boss/Victory; preserve distinct victory/defeat and terminal tombstone behavior.
- LÖVE Studio E00/E01/E03/E04/E05/E06 static-art workflows must keep working: editable source, pixel painting, room authoring, visual IDs, world/theme publications, atlas pages, fallback and F5 visual reload. **Sprite animation editor E02 remains deferred.** Never require animation frames for new content.
- Continue to support 11.5, Linux/WSL and Intel macOS portability. Save schema may advance explicitly; **no historical cross-version active-run migration** required. Do not erase incompatible saves/profiles: reject with clear diagnostics and backup/recovery expectations.

Preserve game responsiveness: 80 ms visual tile slides, presentation-only grouped telegraphs/boon events and explicit ignored-input feedback. Autonomous changes should not turn ordinary movement or attack into a sluggish experience.

## 3. Verticality: **separate floors only** (scope is fixed)

**No height levels within a tilemap, no ramps, jumping, falling, climbing physics, z-coordinate combat range, 2.5D perspective or height-aware targeting.** The user explicitly rejected these for now. Here, “z-axis” means distinct traversable **2D floors connected by stairs or similar portals**.

- Each newly generated biome has **two traversable floors** by default; reserve floor identities/transitions so more floors could be added later. The **8–12-room budget is for the entire biome across both floors**, not 8–12 per floor. Default distribution approximately 4–6 rooms each, with biome-specific variation but enough useful encounters on both. Avoid increasing quiet traversal proportion or run duration unintentionally.
- Procedurally generate stair/link endpoints and compatible local geometry on both floors using common sockets/placement rules. Create at least one **bidirectional reachable stair pair** on the critical path, and optional extra connections only if validated. A viable entrance→stairs→miniboss→exit path must exist. Put the miniboss on the deeper/second floor by default. Validated chests, optional secrets, doors and combat encounters are distributed across the **biome**, not duplicated once per floor.
- Floor transition should use **ordinary movement into/on a clearly marked stair tile**, with one well-defined movement recovery cost, no extra Inspect command or repeat bounce. Preserve a safe landing tile, return linkage and correct fog/map display; include an explicit floor label and stair marker in the HUD/minimap. Handle blocked landing safely, don't spawn directly on a hostile or invalid tile. Persist the exact concrete two-floor world and portal metadata.
- Define floor-local scheduler and stage-global pressure/deadline semantics clearly. Only the active floor's AI, scheduled effects and world time advance; the overall stage extraction deadline should still progress through player activity rather than reset after stair use. An inactive floor's pending work freezes exactly. Normal enemies can remain floor-local at first, with documented behavior; within-floor chase across rooms must remain. The invulnerable pursuer **must remain meaningful across floor changes** (e.g. relocation/follow-up through links without duplication or timer reset); choose a deterministic implementation and tests. No exploitable stair ping-pong that permanently evades extraction pressure.
- Expand E03 room/biome editing minimally so room/socket metadata can designate stair-compatible anchors and author/test two-floor generation. Reuse tile painting, markers, preview, validation, repair and publishing. Generate **real playable stair links** rather than decorative floor tags.
- Validate floor reachability and pursuer/exit rules across a robust seeded corpus. Save/resume immediately before and after a stair transition, with pending telegraphs, delayed effects, fog and pickups; verify exact deterministic continuation and no retroactive inactive-floor simulation.

## 4. New terrain: six distinct *meaningful but not annoying* mechanics

Build six reusable terrain interaction definitions (not just colors). Keep old shallow-water semantics unchanged unless an explicit content-version change is justified. Proposed minimum palette; alternate names/themes are fine **only with equally distinct mechanics and tests**:

1. **Bramble bed:** traversable but inflicts clearly telegraphed, bounded contact damage or a short status on first eligible entry; avoid on mandatory choke tiles without counterplay.
2. **Sticky bog:** increases **movement recovery cost** when traversing a marked tile, never blocks all movement and never stacks into an unavoidable severe slow.
3. **Glazed ice:** changes **Dash traversal/extension** along marked cells with a complete preview; ordinary walking must not automatically slide or seize control. No involuntary movement loops.
4. **Breakable barricade:** attacks can remove this obstacle, updating movement, LOS, projectile collision, fog and navigation deterministically. Respect protections on authored secret structures.
5. **Obscuring mist:** traversable and selectively blocks or attenuates **vision** without automatically blocking movement and all projectiles; consistent AI/player rules and readable graphics.
6. **Resonant crystal:** a spatial combat interaction, e.g. lightning or specified proc effects can arc to a second target/terrain anchor; deterministic event ancestry and friendly fire rules apply. Include a boon interaction.

Build terrain definitions with composable properties/event hooks rather than six special cases buried in the central game loop. Keep status and event provenance correct. Make every mechanic visually distinguishable via built-in placeholder appearance and authorable in Studio. Use safe procedural placement budgets: mandatory path remains traversable without unavoidable lethal environmental sequences. Test negative cases, disabled effects, save/restore, AI pathfinding, tile edits/repairs, and at least one tactical benefit/counterplay for each mechanic.

## 5. Seven distinct biomes, three new authored themes

Retain the existing four identities: **Overgrown Ruins, Fungal Hollows, Sunken Aqueduct, Ashen Bastion**. Autonomously design **three additional named biomes** with coherent themes, generation rules, visual palettes, encounter pools, terrain distribution, separate room families and unique minibosses. At least one new biome should make strong use of the connected-floor system; all seven use two floors under the goal's default. Choose themes to maximize differences in routing and combat geometry (e.g. vertical labyrinth, fractured archive, glacial observatory), not palette swaps.

- Continue using the current graph/template/socket/corridor generator with true biome-specific constraints: different topology weights, room geometry, corridor connections, sightlines, landmarks, hazards, secrets and pacing. Do **not** create seven independent procedural engines or add WFC merely for its own sake.
- Keep **three stages then finale**. Stage 1 can stay Overgrown Ruins for familiarity; stage 2 and 3 offer **meaningful, cached, deterministic choices** among unvisited eligible biomes (ideally 2–3 choices). Never make all seven mandatory in one run, and do not reroll route choices when reopening a menu. Prevent impossible choices or repeat-only states.
- Each biome's 8–12 rooms **across two floors** contain around five paid chests, 1–2 ordinary secrets (target 2), accessible miniboss arena/exit, initial enemies and escalating pressure. Extra secret depth remains possible, but don't replace learnable secret grammar with random arbitrary puzzles.
- Preserve automatic repair-before-retry, protected authored geometry and fallback semantics. Log seeds, repair decisions and diagnostics. Ensure E03 editor-created room/terrain/stair data can be published and used by real new runs.
- At least 100 generated seeds **per biome**, validating two-floor topology, doors/landing, reachable chest approaches, elite, exit, secrets, and no stall/softlock. Also test intentional faults/repairs. Run seed corpora under Linux CI/headless and observe representative LÖVE frames.

## 6. Regular enemies: ten genuinely new archetypes

Add **exactly ten** new enemy identities, for **sixteen** regular enemies overall. Codex may choose names, themes, stats and detailed attacks, but **every added archetype needs a distinct tactical role**: not merely a palette/stats copy. Suggested roles to cover (adapt to biome themes): cover-breaking bruiser, skirmishing ranged attacker, terrain-denial caster, pack coordinator, delayed ambusher, anti-cluster artillery, status-support unit, mobile charger, summoner and a defensive/retaliatory unit.

For each: validated content ID, biome pools, visuals and readable attack intention, new or reused AI behaviors, appropriate speed/recovery, locked telegraph(s), reasonable counterplay, XP/currency/drop attribution, friendly-fire handling, kill/death interruption, fog/LOS, and save/restore. Ensure they use existing damage/status/effect pipes so boons react correctly to enemy-originated actions. Test at least one unique mechanic per enemy and at least one pair interaction with other enemies or terrain. Present them in normal generated runs, not only developer showcases. Avoid every new enemy being a long-range corridor sniper; account for cover/escape routes and player feedback about long shooting lanes.

## 7. Seven distinct biome minibosses

Keep **Thorn Warden** as the exclusive Overgrown Ruins miniboss. Add **one distinct miniboss for each of the other six biomes**, for **seven total**. This includes the previously existing three other biomes, not just the three new ones.

- Each gets an identity and health/phase UI, **at least two meaningfully different telegraphed attack patterns**, speed-scaled windups, phase/behavior change, readable signature, and fair counterplay within strict speed scheduling. Some can use terrain, summon limited reinforcements or change LOS; none need 3D combat.
- The end-of-biome reward still grants exactly one free elite chest. The exit and extraction phase unlock on canonical miniboss death via direct attack, boon chain, DOT, delayed effect or friendly fire. Handle simultaneous lethal resolution and terminal accounting consistently. No duplicate rewards.
- Keep the Hollow Regent as the final boss; adapt only if essential to retain a fair four-class victory path. Don't create additional mandatory bosses or extra run stages.
- Test each boss's telegraph geometry, phase transition, interruption, friendly fire/secondary kills, persistence and combat in its generated arena on both floors; guided legal-action route must reach each across representative seeds.

## 8. Four playable starting classes

Retain Adventurer unchanged unless a targeted correctness fix is required. Add **Ranger, Arcanist, Berserker** and make all four selectable **immediately** on New Run (not behind achievement unlocks). Shared standard **four-way walking** initially; class-specific attack directions, ranges and ability patterns. Same action-only economy: **no cooldown, mana, stamina or forced real-time mechanics**.

- **Adventurer:** existing eight-way Sword Strike, Sweeping Slash, cardinal Dash, familiar baseline.
- **Ranger:** lower defensive margin, deliberately strong positional/ranged attacks with honest LOS/projectile obstruction, a distinct multi-target/piercing special and an evasive but action-costed mobility option. Ensure effective diagonal/ranged targeting and readable previews.
- **Arcanist:** tactical area/element/status patterns, reasonable windups/recovery costs, and synergy with status/terrain boons. No separate mana/energy resource. Avoid reproducing Adventurer's melee kit with only a graphic swap.
- **Berserker:** substantially higher starting HP and/or defense, **noticeably slower movement/recovery**, heavy broadsword or sledgehammer, broad cleave and crushing high-impact attacks with long costs. Has deliberate tactical tradeoffs; must remain mobile enough to traverse procedural maps and counter ranges.

Use data-driven class definitions and small reusable ability/targeting/AI effect primitives. UI must make action ranges, costs, stats, class identity and direction limitations clear; preserve hybrid bump-to-attack where applicable. Class-specific static sprites/colors are acceptable placeholders through stable visual IDs; user supplies final art later.

All four share the general boon catalogue, XP/stat growth, currency/chests, rarities, meta profile/unlocks and final boss rules. Add compatible **class-specific transformation boons** for new classes, satisfying the 26-boon count; do not remove or break the existing transformation mechanism. Validate class upgrades across save/resume, pathfinding/movement, GUI control input and all generated biomes. Test legal-action combat progression and actual biome transitions for each; aim for deterministic legal-action Victory coverage for **all four** through at least one validated seed/strategy each, without teleports, god mode or weakened bosses. If not achieved, mark that acceptance row incomplete, diagnose and adjust fair content/balance rather than falsify a victory.

## 9. Exactly 26 new boons (24 → 50), four rarities each

Add **26 functioning new boon identities** with distinct tactical and conditional effects. All rarity tiers must scale the same underlying mechanic; mixed-rarity stacking, chance overflow, effect magnitudes, deterministic RNG, proc coefficients, ancestry safety, automatic event observation and saved content IDs remain correct. Some can use new generic predicates/effect handlers, but avoid boon-specific branches in `game.lua`.

The following **design matrix** sets the expected breadth. The names are provisional; keep a one-to-one mapping to **26 actual implemented, tested IDs**. If an example overlaps an existing boon, replace it with a mechanically distinct equivalent and document why; don't silently reduce the count.

| # | Proposed boon identity | Required gameplay concept |
|---|---|---|
| 1 | Stonewake | After exiting sticky terrain, next eligible strike gains a conditional effect |
| 2 | Bramble Compact | Interacts defensively or offensively with contact-hazard terrain |
| 3 | Froststep Catalyst | Trigger a bonus effect after a Dash crossing ice, without forced walk sliding |
| 4 | Fracture Dividend | Destroying cover emits a limited directed shard or boon proc |
| 5 | Mist Veil | Rewards accurate positioning/attacking relative to obscuring terrain |
| 6 | Crystal Harmony | Crystal/lightning spatial chain amplification with deterministic eligibility |
| 7 | Sunderer's Mark | Repeated hits on one target accumulate a capped vulnerability effect |
| 8 | Counterweight | A fully blocked attack primes the next committed heavy strike |
| 9 | Bloodrush | A player-credited kill grants a short-lived, scheduled speed buff |
| 10 | Pain Dividend | Damage actually taken primes an extra effect on the next hit |
| 11 | Executioner's Thread | Applies a differentiated finisher against wounded enemies |
| 12 | Shared Agony | Nearby enemies carrying the same status share part of one damage event |
| 13 | Windup Hunter | Punishes an enemy preparing an attack without globally interrupting it |
| 14 | Crossfire Omen | Reacts to multiple enemies committing attacks with overlapping locked cells |
| 15 | Pursuit Spark | Reacts to observable hostile flank/reposition movement with spatial predicates |
| 16 | Ordered Ranks | Rewards a line or geometric grouping of three enemies |
| 17 | Far Witness | Reacts to distant observable events according to configurable radius/scope |
| 18 | Betrayal Ward | Enemy friendly fire grants a brief defensive resource, unlike existing retaliation boons |
| 19 | Rhythm of Steel | Alternating movement and attacks primes a distinct bonus |
| 20 | Cluster Rupture | Same-chain group damage threshold triggers a different effect than two-target Resonant Wounds or three-kill Packed Powder |
| 21 | Ranger — Splitflight | Transformation: ranged attack branches across additional eligible targets |
| 22 | Ranger — Recurve | Transformation: arrow behavior extends/ricochets/pierces under clear collision rules |
| 23 | Arcanist — Expanding Sigil | Transformation: shaped spell expands in a controlled pattern |
| 24 | Arcanist — Alchemical Flip | Transformation: an ability converts or exploits existing statuses |
| 25 | Berserker — Earthshaker | Transformation: heavy smash adds shockwave/adjacent geometry |
| 26 | Berserker — Battle March | Transformation: class mobility/heavy attack gains a positioning-dependent follow-up |

Balance values are Codex's choice but should be **useful at Common**, materially stronger at higher rarities, and not trivialize the game by turn 50. Transformations must obey compatibility and preserve source ability IDs. Unavailable class-specific boon rewards should not be drawn for unrelated classes if they are unusable; use class-aware eligibility without breaking cached three-offer fairness. Verify each effect in actual combat and at least 10 cross-boon integrations, including class×terrain, enemy-friendly-fire×boon, multi-target damage batches, time-delayed chains and high-stack overflow. Do not introduce an arbitrary copy cap. Perform stress tests and keep visual proc backlogs bounded presentation-only; no gameplay events may be dropped.

## 10. ROAG Studio sound picker and complete audio-binding pipeline

Add a **real user-visible sound editor inside Studio**, not merely `sound.lua` constants or a JSON file the user must edit by hand. It must work even if all third-party placeholder downloads fail or the user imports their own audio later. Implement against LÖVE 11.5; prefer verified **WAV for short SFX and Ogg Vorbis for music/ambience**. Use `love.audio.newSource(..., 'static')` for short clips and `(..., 'stream')` for long audio where appropriate, with reusable/poolable sources, voice limits, playback priorities and error handling. No audio execution in headless simulation.

### Required editor workflow

- **Import** via file drop and/or accessible picker, validate/decode supported files, protect paths from traversal, reject corrupt/oversized inputs with clear errors, retain editable original audio bytes separately from published/derived files. Allow rename, tagging, searching/filtering, replacing a source without changing its stable ID, and removing or safely detecting references. Show provenance/license fields.
- A central **Sound Library** UI with play/stop/audition, waveform or duration information if practical (waveform not mandatory), level/mute preview, and stable audio IDs. Preview must not mutate authoritative state or accidentally trigger in-game rewards.
- **Contextual Sound Picker** surfaced when selecting an existing player class/ability, enemy/miniboss/final boss, boon, terrain, room/biome, door/chest/secret/pickup or HUD/UI theme event. The picker presents valid events from a reusable **audio-cue registry**, not a fixed hardcoded list of 20 sounds. Users can bind/rebind/unbind sound asset IDs with no Lua edits.
- Coverage: player movement/attacks/dash/damage/death; enemy movement/telegraphs/attacks/hits/deaths; bosses' phase/arrival/defeat; terrain footsteps/hazards/stairs; boons/status/procs; world mechanisms/doors/secrets/chests/currency; UI hover/confirm/cancel/level-up/achievements/victory/defeat; biome/title/boss music and local ambient sound. **All categories must be assignable**, even if only some receive real placeholder sounds initially.
- Editable per binding: **volume, pitch, loop, variants/weights, priority, maximum concurrent voices/cooldown to prevent cacophony, optional distance attenuation** for world cues, and optional per-category volume/mute; validate numeric bounds. Cosmetic sound variation must use a dedicated RNG or non-authoritative sampling and cannot consume simulation RNG or alter replays.
- **Music transition controls** in BOTH sound/theme editor AND biome/scene/room editor: switch instantly, optional fade-in/out or crossfade with editable durations; set music for stage entry, standard exploration, miniboss, final boss, victory and defeat. Avoid restarting a track on every tile step or replayed GUI event. Handle pause/resume, floor switches, save/continue and unavailable audio gracefully.
- Audio uses presentation/event routing from existing canonical gameplay events. Preserve provenance, semantic cue names, optional source position/faction, and presentation ordering. Map events to audio independently from game rules. Missing invalid asset → clear diagnostic and **silence/fallback**, never crash or corrupt the run. On F5 refresh, revalidate/swap audio bindings safely without altering game snapshots.
- Publishing: separate checked audio source/metadata store, readable dry-run status and explicit apply, stable ID references, compatible with E00/E01/E03/E04/E05/E06 asset publishing. An atlas is for **graphics**, not audio; do not pack WAV/OGG into PNG texture atlases. Do not require replacing source audio when republishing metadata. Future imported user files take precedence via stable IDs.
- Include several usable sound bindings in the **normal game and editor showcase**, spanning gameplay, enemies, terrain, boons, menus, and at least one looped ambience/music transition. Test actual LÖVE audio source creation/playback in isolated GUI/audio smokes; verify source/missing fallback and silent/headless behavior.

### Source audio and licenses

The user will provide final sound/music files later. Codex may obtain placeholders from **OpenGameArt** or similar reputable sources, but **prioritize individually verified CC0** assets. For every externally sourced asset record title, author, exact source URL, license/permission and source hash; keep an attribution/provenance manifest even for CC0. Don't assume every OpenGameArt asset is CC0, don't scrape ambiguous licenses, don't download executable code from an art portal, and don't represent placeholders as original work. If network/source verification is unavailable, synthesize simple **original** placeholder chimes/steps/bursts using small offline audio generation code and document that choice. Do not block the goal merely due to unavailable licensed assets. No external paid services, credentials or API accounts required.

## 11. Internal milestones and autonomous operating rules

Treat this as **one goal with checkpoints**, not nine separate prompts. After each milestone, update `docs/goal/ROAG_EXPANSION_PROGRESS.md` with completion status, test commands/results, open risks, next action and any new schema IDs. Also maintain an objective checkable feature matrix and concise `docs/goal/ROAG_EXPANSION_REPORT.md` throughout. Keep human-readable progress and test artifacts in project-local or disposable locations, avoid huge binary/log commits, preserve unrelated files.

Suggested dependency order (adjust if justified, but explain changes):

- **M0 Handoff & baseline:** verified latest Mac transfer, safe working snapshot, actual tests and architecture scan, initial progress report.
- **M1 Multiple floors:** schema, stairs, active/inactive floor scheduler, routing, GUI, snapshot/restore, Editor E03 markers; seed tests.
- **M2 Terrain:** six meaningful, safe terrain types with render/AI/LOS/boon hooks and Studio painting.
- **M3 Biomes:** three new distinct types; two-floor generation across all seven; route graph and validation.
- **M4 Enemies and minibosses:** ten regular archetypes plus six new biome-specific minibosses, integrated rewards and extraction.
- **M5 Classes:** Ranger, Arcanist, Berserker, class selector, class-aware attacks and transformations interface.
- **M6 Boons:** 26 new distinct functioning boons with four rarities, broad trigger coverage and proper chest offers.
- **M7 Studio audio:** import/preview/bind/publish, scene transition controls, runtime playback with fallbacks.
- **M8 Full integration and release-grade verification:** legal-action routes, repair regressions, fresh-process saves, audio GUI, stress/performance, cross-platform-safe export/handoff documentation.

At every milestone:

1. Inspect before editing; implement the smallest cohesive functionality slice.
2. Run relevant headless, deterministic/replay, snapshot, generation and existing GUI tests; add new coverage that asserts state and rendered behavior. Don't rewrite tests simply to conceal a regression or inflate counts. If expected snapshots change due to intentional new content, preserve explicit historical-fixture coverage where possible and document legitimate behavior changes.
3. If a failure occurs, reproduce it, diagnose source vs GUI harness/environment, fix, rerun; keep failure reports. Do not call transient signal-139/timeout issues resolved without a root cause. Maintain bounded timeouts/isolation for WSLg GUI tests.
4. Keep all legal interactions possible in normal play, not only test fixtures; verify observable UI and tooltip descriptions, floor indicators, biome names, class stats and sound assignment statuses.
5. Record a milestone-level status before continuation; don't pause for routine user approval after every successful milestone. If the goal budget/session ends, preserve work and progress so a resumed `/goal` can continue at the correct milestone. Goal persistence does not make the computer run after the CLI stops.

Autonomous discretion: choose names, art placeholders, numerical balances, internal structures, and test harness improvements consistent with this spec. Do not spend time on Git commit IDs/repo spelling, deep refactors, engine changes, generic scripting frameworks, nonessential animation tooling, online features or multiplayer.

**Stop and ask only for a hard blocker:** uncertain Mac source sync, credible risk of destroying user work, mandatory permission/system/security prompt, unverified third-party license that cannot be replaced, or an architectural conflict with no safe contained solution. Do not attempt to bypass filesystem permissions or system restrictions. Otherwise continue iterating toward all acceptance criteria. No arbitrary wall-clock promises.

## 12. Acceptance evidence and mandatory test plan

Before declaring `COMPLETE`, provide concrete evidence for **each** item:

- A current-source handoff audit confirms latest Mac baseline, no overwritten divergent Windows work; original save/profile and Studio source directories preserved as instructed.
- All **four** classes selectable from a new run; each performs distinct basic, special and mobility actions, earns XP, draws usable boons, persists across floors/stages and reaches a meaningful miniboss encounter through **ordinary legal submitted actions**. Guided ordinary-action victory for every class is the target; any missing victory is explicitly a remaining acceptance failure, not silently waived.
- **Seven** biomes exist and are actually reachable in the 3-stage route graph, each with distinct generated layouts and **two connected floors**; each has a **unique miniboss**, ~five normal chests and optional discoverable secrets over the entire biome, not per floor. At least **100 seeds per biome** validate actual traversability across floor links, exit, loot and safe pressure/pursuer rules; injected repairs/fallbacks are tested.
- **Six** new functional terrain behaviors; **ten** new normal enemy identities and **six** added miniboss identities in legitimate encounter pools, with distinctive verified mechanics, reliable telegraphs and safe handling of friendly fire, interruption, statuses and delayed effects.
- **Fifty** functioning boon IDs in runtime registry, with four rarity variants, deterministic stack and proc behavior; **26** are new. Each new boon has an actual activation and tests. Validate **10 or more cross-system synergies**, including new terrain, enemy-originated events, multi-floor travel, class transformations, and chain reactions; stress the high-stack queue without losing gameplay events.
- Proper save/Continue of multi-floor maps, all new classes/boons/terrain, pending enemy telegraphs/status/DOT/delayed effects, elite reward, extraction countdown, profile and sound-binding publications. Reopened saves yield deterministic authoritative future events where gameplay rules don't intentionally change. Never create overdue inactive-floor turns or extra rewards.
- The **Studio sound browser and context picker are functional on-screen**: import and audition real or original placeholder sounds; bind every event family through data-driven slots; edit transitions in audio/theme and biome/room views; publish and have audio actually play in LÖVE; test missing/corrupt audio fallback and dry-run/apply. Source audio and provenance survive fresh-process reopen. No changes to authoritative snapshots caused by audio playback, variation, F5 or cosmetic settings.
- UI/keyboard/controller-compatible menus are navigable; room editing, static pixel painting, image/theme/world publishers, atlases and hot reload still work. **No animation editor** is required.
- All preexisting headless and GUI regression suites pass on the updated platform; add dedicated GUI checks for stairs, distinct classes, new minibosses, terrain, audio picker and transitions; repeat flaky suites. Include at least one honest normal-action victory, plus non-assisted class-specific progress and challenge scenarios. Name assisted fixtures as such.
- Measure representative simulation p50/p95, 50–100 actor stress, chain cost, generation time, GUI frame intervals and memory on the **actual Windows/WSLg environment**. Do not claim native GPU 60 FPS based on llvmpipe. Compare before/after on equivalent hardware. Do not silently degrade responsiveness or turn these into unjustified performance promises.

**Stop conditions and verdict:**

- `COMPLETE` only if every major deliverable is implemented, normally playable, verifiably integrated, and all hard acceptance checks pass.
- `PARTIAL` if an explicit deliverable is missing or any critical test remains failing; give a precise feature-gap checklist and reproduction instructions.
- `BLOCKED` if transfer safety, user permissions or another hard safety gate prevents defensible progress; do not guess or bulldoze through it.

Provide a final structured report: (1) verdict and remaining issues, (2) migration/preflight evidence, (3) seven biomes/two floors/terrain and room geometry, (4) classes/enemies/minibosses, (5) 50-boon catalogue and integrations, (6) Studio audio editor workflow and legal source provenance, (7) files/modules, (8) baseline/final automated and GUI tests, seed corpora, legal play and disk resume, (9) performance and actual limits, (10) user playtest checklist for Windows keyboard/Wacom/audio, (11) portable handback to Mac and upcoming priorities.

## 13. Reference facts and scope limits

- This is a **static-sprite, tile-based, turn-based** game, not a real-time platformer or a three-dimensional title. Existing board-game sliding is presentation interpolation only.
- Shader/audio/editor improvements may add presentation metadata, never authoritative simulation nondeterminism. Do not entangle UI sound playback with game rules.
- Source references: OpenAI Codex Goals guidance: https://developers.openai.com/cookbook/examples/codex/using_goals_in_codex . LÖVE audio (`static`/`stream`): https://love2d.org/wiki/love.audio.newSource . OpenGameArt licensing: https://opengameart.org/node/5571 .
- This checkout is the ROAG project. Its existing directory name is not a development blocker; do not rename the directory as part of this goal.

**Begin working as soon as M0's handoff gate passes; keep pursuing this goal autonomously, with milestone verification and honest status.**
