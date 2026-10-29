# Roag migration: decisions and proposed implementation plan

Consolidated from the migration discussion on 2026-10-26. The subsequent active implementation goal explicitly authorizes resuming gameplay work and supersedes the earlier pause. This document records approved direction and acceptance requirements; implementation evidence belongs in [MIGRATION_PROGRESS.md](MIGRATION_PROGRESS.md).

## Authority and working agreement

- Product: Roag, built in handwritten, idiomatic C# on the imported Vagabonds/MonoGame foundation.
- Adapt overlaps to Roag's systems; preserve existing Roag behavior/content while integrating Vagabonds' world simulation and landscape.
- Original references and prototype status are recorded in [MIGRATION.md](MIGRATION.md) and [the overlap survey](MIGRATION_OVERLAPS.md).
- The generated C# translation/dynamic value runtime is an earlier prototype, not the accepted production foundation. Do not assume its arbitrary integration choices are approved.
- The final build must use only Vagabonds' sprites, selecting them through its generic universal mapping atlas/catalog and preserving stored mappings, stable identifiers and provenance. Add Roag's targeting overlays, previews and combat polish; do not reuse original Roag sprites in the final presentation.
- Clarify uncertainties with the user and stop before dependent engineering. Use concise multiple-choice batches of up to five questions as needed. Recommendations are not decisions until accepted.
- Numerical tuning is delegated: choose documented, configurable initial values and test/adjust them within agreed mechanics (Guard reduction, XP weights, durations and detection distances). This does not authorize changing gameplay rules or expanding scope.
- Linux desktop is required. Add Windows if feasible; Linux-first delivery is acceptable if Windows is not feasible, with actual verification limitations reported.
- Preserve Lua/LÖVE source as reference until native replacements are verified, then retire that runtime/base as requested. Do not equate compilation or generated tests with complete parity.

## Approved gameplay decisions

### Party, builds, items and progression

- Active party maximum: four members. The player can directly control and customize every member.
- Start with one character and recruit others in the world. Recruits already have fixed classes before recruitment.
- Each member has their own Roag build, class, level and boons. Class is fixed and cannot be changed.
- The player chooses a boon's recipient. Assigned boons stay with that member and cannot be transferred.
- Gold, supplies and unequipped items are shared; equipped gear is assigned individually. All party supplies are globally accessible even across separated groups; no physical-carrier restriction for party supplies.
- Equipment can add stats and effects while preserving the class's Roag attack patterns/core moves.
- Changing equipped gear during combat is allowed and consumes an action; browsing inventory is free.
- Compatible Vagabonds talents become class-appropriate boons and equipment effects. Do not retain a separate innate-talent layer merely because the old engine had one.
- Adapt Vagabonds' abilities to Roag class resources and recovery; do not add its separate mana system.
- Kill XP is split among contributing members, weighted by contribution. Noncontributors do not automatically receive an equal party payout.
- Damage, healing, shielding and enemy control all count. Support should have comparable XP weight so support-focused members can progress at a similar pace.
- Downed contributors retain their earned XP entitlement.
- If a contributor permanently dies before the enemy dies, redistribute their share among surviving contributors using the same contribution weights. Downed contributors remain eligible; noncontributors do not inherit the share.
- Summons, sentries and traps credit their owner's contribution.
- Exact contribution scoring and allocation values are delegated tuning; document them and verify comparable progression opportunities for support and damage roles.

Mirror Dividend preserves the original executed behavior: a reflected sentry shot can heal its owning party member, as can a reflected manual attack. The user explicitly chose this over the original manual-only descriptive wording. Preserve the existing unique-action rule and keep sentry damage classified as class-generated.

### Shared time, control and movement

- Fully turn-based, FFX-style ordering using Roag speed/action recovery. No elapsed world time while thinking, inspecting or rendering.
- Visible timeline shows character turns and scheduled enemy attack impacts. The player commands each member when their turn arrives.
- Movement consumes an action; a turn does not grant a separate free movement allowance plus attack.
- Player-directed ordinary movement uses keyboard presses, one tile per press. Remove click-to-move, click-to-pathfind and automatic click-to-approach routes in exploration and combat. Mouse selection, inspection and in-range interactions remain available. Existing movement abilities retain their rules and take keyboard direction input; assigned party workers may still walk automatically to their jobs (explicitly approved). Agreed leader-following and NPC navigation also remain.
- All party members, enemies and world systems use one consistent simulation time. Dungeon actions advance the outside world too.
- Combat actions and individual movement steps take seconds; sustained travel, work and rest accumulate minutes/hours. Exact conversion and pacing remain tuning decisions.
- When together outside combat, members automatically follow the chosen leader. The player can change that leader anytime; leader selection does not determine combat turn order.
- Combat movement/actions are individual.
- Allies occupy and block their tiles; no automatic swapping or passing through them.
- The player can deliberately split the party outside combat or inside a dungeon, including across floors/entrances and between dungeon and outside world.
- While split, every member receives individual commands even outside combat, on the same turn timeline. Control transfers to the member whose turn arrives.
- Regrouping outside combat restores leader-following. Exact split/regroup detection and interface remain to be designed.
- When a separated group encounters enemies, nearby involved members enter individual combat control. Distant grouped members may continue leader-following; explicitly split members still receive individual turns.

### Sight, intent and enemy behavior

- Separate live vision: the player sees through the currently controlled member. Another member's surroundings become visible when control switches to them.
- Explored terrain is retained on a shared party map. Live enemies/activity require the controlled member's sight.
- Species and motives drive enemy targeting. Scavenger/vulture-like creatures may prioritize downed or weakened characters; do not impose a universal preference for standing targets.
- The user explicitly delegated original Roag enemies' new party-targeting assignments: choose fitting species/role priorities, document the assignments, and expose every tendency in inspection. Examples approved in that delegation are guards favoring nearby threats, scavengers favoring downed/weakened members, and ranged enemies favoring clear shots.
- Species targeting tendencies are visible in inspection from the start, rather than requiring discovery.
- Special enemy abilities (including summoning, reviving allies and phasing through walls) may operate offscreen under the same physical conditions and use limits. Original player-visibility/active-floor prerequisites must become world-validity checks; choosing the viewed party member must not enable or disable these abilities. Private vision still controls what the player can inspect or see telegraphed.
- Ordinary enemies can pursue through connected rooms, floors and outdoors where physically possible, according to species and motives. The special-dungeon pursuer restriction is a separate exception.
- Combat clearly communicates an enemy's intended next target. Attacks lock onto telegraphed tiles, allowing movement to evade them.
- Friendly fire applies between party members under Roag's attack rules.
- The timeline shows all party turns, but enemy turns/attack details only when currently visible to the controlled member. Do not leak offscreen hostile intent through the timeline.

### Guard and cooperative abilities

- Guard is a deliberate action, not a timed real-time reaction.
- Guard reduces incoming damage until that member's next turn. The player chooses a facing; protection covers the front and front diagonals, leaving sides/rear exposed.
- Directional Guard reduces direct physical and magical attacks from protected directions. Poison already affecting a member and hazards beneath their feet bypass Guard.
- The damage-reduction amount was initially deferred; subsequent numerical-tuning delegation permits choosing and testing an initial value. The earlier suggested 50% is not a separately user-approved constant.
- Keep cooperative abilities, adapted to two nearby party members contributing to one action and both incurring recovery time.
- The assisting member may participate while already recovering, delaying their next turn further. Both participants need not be simultaneously ready.
- Cooperation range and numerical costs are delegated tuning. Preserve existing eligibility constraints when adapting actions; being in recovery is the explicitly approved additional assistance case. Document per-ability conversions and clarify a genuine conflict with agreed rules before implementing it.

### Downing, rescue and defeat

- Downed members are rescuable. During combat, a rescuer must move adjacent and spend medicine plus an action.
- Revival restores 25% of maximum HP.
- Bleed-out starts at five of the downed member's own scheduled turns, not five actions by other combatants.
- Each damaging hit while downed removes one remaining bleed-out turn. Reaching zero causes death.
- If revived and subsequently downed again, the countdown resets to five turns.
- Abandonment causes permanent death. Species-specific targeting and area/environmental hazards can threaten downed members.
- Special-dungeon expiry is not universally lethal. Apply the dungeon's announced consequence: lethal site destruction/disappearance kills members inside, while nonlethal outcomes can leave survivors and accessible altered remains. Members safely outside are not killed by the local expiry.
- All surviving members being downed does not immediately end the campaign. Allow fast-forwarding the simulation while rescue remains possible, until an NPC rescues someone or everyone remaining dies through bleed-out/other consequences.
- Existing nearby NPCs may rescue according to relationships, motives and willingness to face danger. They must physically reach the casualty and have medicine; rescue is not guaranteed or spawned automatically.
- Final party death ends the campaign/world; the next campaign starts a fresh world. There is no replacement traveller continuing the defeated party's world.
- Roag's permanent achievement records and unlocked content survive; characters, possessions and world state reset.
- Save/resume anywhere using one continuing campaign state, without in-game rollback. No conversion/import of existing saves or profiles is required: development starts from scratch. Profile retention applies between campaigns in the new game.

### Survival, camping and sustained activities

- Retain Vagabonds' hunger, fatigue and injuries; provisions and resting matter during exploration.
- Ordinary food uses a simplified Minecraft-inspired hunger-only model: it replenishes hunger and enables gradual health regeneration while well fed, replacing its original instant healing. No additional saturation/fullness reserve. Dedicated healing items remain distinct.
- Starvation can down and ultimately kill members.
- Eating consumes food and action time. Automatic eating is configurable, including during combat; each character still has individual hunger/health despite globally shared provisions.
- Nearby threats can interrupt rest and return control to the player.
- Fires, traps and buildings can mitigate camping danger.
- Fire provides warmth and can deter some creatures, while light/smoke can attract others depending on species and motives.
- A member can stand watch while others rest, improving warning/detection while missing their own rest.
- Resting members automatically spend their turns resting until interrupted or manually awakened.
- Digging, crafting and standing watch also continue automatically until completed, interrupted or cancelled. These are explicit exceptions to repeated individual commands, not a return to real-time world progression.
- Only members who detect danger or receive a warning stop their activities. Unaware members continue.
- Lookouts automatically sound an audible alarm. Distance and walls affect hearing; nearby enemies can hear the alarm and be attracted too.

### Day/night, seasons and ecology

- Proper day/night cycles, seasons and weather advance with the shared action-driven calendar.
- Day/night affects visibility, resident sleep/routines, shop opening hours and which creatures are active.
- Retain and adapt Vagabonds' existing seasonal/weather simulation and its world-system connections.
- Dungeon enemies continue their routines while the party is away as world time advances.
- Traps, summons and temporary effects also continue operating/expiring offscreen with world time according to their behavior and ownership; do not freeze them solely because no party member is nearby.
- Cleared dungeons can acquire new inhabitants through world events, migration or reproduction, not automatic timed respawning of defeated enemies.
- Preserve individual identities, deaths and one-time reward accounting. New inhabitants are not duplicated old reward sources.

### Dungeon geography and progression

- Blend dungeon content/generation without removing Roag's rooms, biomes, interactions or bosses.
- Original Roag multi-stage progression lives within selected large dungeon complexes.
- Extend Vagabonds' dungeon footprint and underground-floor limits as needed to fit those complexes, preserving spatial connections rather than clipping or compressing layouts.
- Large dungeons do not necessarily contain a boss. Do not make boss placement mandatory for every large site.
- Dungeons physically occupy world terrain. Underground and surface distances correspond; do not introduce Nether-style distance compression.
- Digging/building can generally bypass obstacles; clearly identifiable reinforced or magical structures may resist alteration.
- A dungeon may connect different surface locations, such as opposite sides of a mountain.
- Boss defeat does not automatically extract the party. Players walk out, or sometimes an exit/shortcut becomes available.
- Exits can be physical passages/stairs/lifts or occasional magical portals appropriate to the dungeon. Some bosses provide no shortcut.
- Dungeons persist through retreat and revisiting, subject to explicitly activated special-dungeon consequences.
- On permanent member death, their equipped gear remains on their body for recovery. Globally shared supplies remain available to survivors. Site destruction can affect physical recovery according to its announced consequences.

### Special dungeons and campaign objectives

- Only a randomly selected special subset has Roag extraction pressure/pursuers.
- Pursuers remain within their special dungeon's boundaries; they do not follow the party into the wider world.
- Activation must be deliberate and clearly signposted. Simply discovering/entering does not start the deadline.
- Once activated, the deadline continues with world time, including actions outside the dungeon. Thinking/menus never consume it.
- Leaving and re-entering is allowed before expiry without restarting the deadline. After expiry, accessible remains may be revisited in their changed state.
- The expiry consequence and deadline are clearly communicated before activation.
- The user revised the earlier universally lethal expiry rule: outcomes depend on the dungeon. Explicitly lethal collapse/disappearance is possible; other outcomes include lost loot, partial collapse or changed inhabitants. Announce the expected consequence before activation and make it persist.
- Expiry leaves persistent local aftermath appropriate to the event: rubble, sealed cavern, empty site, etc. Existing surrounding places remain.
- There are multiple major bosses and a true final boss. Exploration order is free.
- The true final boss is selected from multiple candidates at world generation, including the Hollow Regent. Boss difficulty reflects world role, region and depth; the selected final boss has an appropriately stronger version. Do not scale dynamically to party level.
- Additional bosses use distinct designed mechanics with seeded ability/trait/encounter variations. Approved scope is 3–5 distinct bosses, not eight new families. The user approved all four concepts in [the boss design record](MIGRATION_BOSS_PROPOSALS.md): Bellkeeper, Sluice-Maw, Threadmother and Ossuary Captain. Implementation is authorized; additional/replacement concepts would require vetting.
- Defeating the other major bosses is not required to attempt the true final boss: the player may challenge it early, but is likely to die without preparation. Their loot, XP and discoveries prepare the party; their defeat does not weaken the final boss, remove its wards or remove its allies.
- Defeating the true final boss records victory and allows continued exploration of the same world.

## Deferred expansions

- Race-driven politics, with Caves of Qud and Songs of Syx as references, is a general future expansion note. No race roster, political rules or racial bonuses were approved for this migration.
- Earthquakes, titans, kaiju and comparable landscape-reshaping events are later expansion work (option A).
- The migration should support persistent terrain changes now, including normal world work and agreed local special-dungeon aftermath. Do not implement disaster generation, giant creatures or broad collateral terrain destruction under that requirement.

## Implementation discretion and escalation

The known major gameplay choices have been resolved in the discussion above. Do not reinterpret the earlier overlap survey's open alternatives as unanswered questions.

- Numerical tuning is delegated: document/configure initial values, test them, and adjust within the approved mechanics. This includes contribution weights, Guard reduction, recovery/resource costs, hearing ranges and calendar pacing.
- Routine C# architecture, algorithms, interface wiring and content bindings should implement these rules and the original systems. Preserve original semantics where the user has not approved a change. Similar names do not establish equivalent behavior.
- Complete the detailed source/content inventory and per-entry conversions as engineering work. Keep all Roag content except explicitly approved adaptations such as food behavior and final sprite sourcing; keep the retained Vagabonds world capabilities. No unapproved loss of features to make the port easier.
- Four additional boss concepts are approved. Their tuning, atlas selections and implementation may proceed; new or replacement concepts still need vetting.
- A real contradiction, unsupported content conversion or newly discovered gameplay decision requires clarification before dependent engineering. This is not permission to add missing mechanics or alter scope silently. Ask concise MCQs only when necessary; do not repeatedly reopen approved decisions.
- No old save/profile conversion is required. No expansion into race-driven politics, disasters or titans/kaiju is authorized.

## Completion criteria for the active implementation goal

The user selected acceptance option 30A: implement all agreed features, check the preservation inventory, pass relevant automated tests, verify rendered end-to-end gameplay, and provide a playable Linux build with documented limitations. Their hands-on playtest is welcome but is not a mandatory approval gate for declaring implementation complete. The subsequent explicit goal activates this scope; this document alone is not evidence of completion.

Use the following as concrete evidence for that accepted standard:

1. **Feature preservation:** trace every original Roag subsystem/content family into its native destination or an explicitly approved adaptation. Cover all 15 classes, boons/proc interactions, enemies/bosses, rooms/biomes, services/terrain, progression/profile, controls/presentation and authoring capabilities. Retain the Vagabonds world, economy, ecology, work, social and content/asset tooling capabilities identified in the overlap map. A compiling prototype or thin combat adapter is insufficient.
2. **Native architecture:** handwritten C# is the production rules implementation; actual world tiles, persistent actor identities, one authoritative health/build state and one consistent simulation timeline underpin gameplay. No generated Lua-semantic runtime or isolated fixed-window battle remains as the foundation.
3. **Deterministic combat and party checks:** exercise solo and four-person play, distinct and duplicate classes, recovery ordering, locked telegraphs, movement/blocking, friendly fire, Guard, cooperation during recovery, contribution XP, assigned boons, equipment changes and globally shared supplies. Verify free inspection/invalid actions and absence of idle real-time simulation.
4. **Split-party and survival checks:** exercise different rooms/floors and dungeon/outside groups; separate vision/shared terrain map and hidden hostile timeline data; local combat transitions; automatic eating, resting/work and hearing-based interruption; rescue, five-own-turn bleed-out, damaging hits, repeat downing, all-down fast-forward/NPC rescue and final death. Verify item/resource accounting and corpse gear recovery.
5. **World and dungeon checks:** verify offscreen routines/effects, day/night and seasonal effects, finite resources and natural repopulation, physical dungeon paths/shortcuts, excavation exceptions, the four approved new bosses and preserved roster, fixed role-based difficulty, final-boss selection/early access, and continued play after victory. Special deadlines must persist across retreat/reentry and produce their announced lethal or nonlethal local aftermath without resetting rewards.
6. **Save/profile lifecycle:** save and resume committed gameplay exactly, including pending attacks, per-member recovery/builds, separated positions, jobs, rescue timers, activated deadlines, changed terrain and reward receipts. Defeat starts a fresh world while retaining achievements/unlocks created in the new game. One continuing campaign state provides no in-game rollback and no resurrection of terminal campaigns.
7. **Rendered acceptance:** run the actual native client and inspect representative playable journeys and captures, including split-party selection, combat previews/timeline, rescue, camp interruption and dungeon consequences. Confirm readable controls/feedback using only Vagabonds sprites chosen through the universal atlas, with Roag polish. Source queries, fixture assertions and screenshots without interaction are not by themselves end-to-end proof.
8. **Delivery and reporting:** provide reproducible Linux launch/build instructions and retained verification evidence, document tuning and remaining nonblocking limitations, and remove Lua/LÖVE and obsolete prototype dependencies only after replacements are verified. Attempt Windows support where feasible and report build-only versus runtime-tested status accurately. An unavailable Windows test environment must not be described as a Windows runtime pass.

Missing agreed functionality, known gameplay blockers or unverified preservation are unfinished work rather than limitations to waive at completion. Compilation, passing selected tests or reaching a convenient milestone do not alone satisfy this goal's completion criteria.

## Implementation sequence

| Phase | Deliverable | Acceptance evidence before moving on |
| --- | --- | --- |
| 1. Native foundation and content inventory | Typed C# ownership/state and complete Roag feature/content inventory; explicit mappings into the imported world/content tooling and sprite catalog. | Every preserved feature/content family has a destination; unresolved semantics are flagged; existing world/tooling baselines are identified. Prototype retirement is planned without deleting unverified references. |
| 2. Timeline and tile combat | Roag movement, targeting, recovery, telegraphs, effects, friendly fire and enemy rules using the actual world terrain and persistent actors. | Deterministic action ordering, free inspection/invalid actions, preview/commit agreement, no real-time simulation leakage and exact save continuation. Original Roag cases and meaningful native integration cases agree. |
| 3. Party and build integration | Four independently owned builds, split/regroup control, separate vision/shared map, XP/boons/items, downing/rescue, Guard and cooperation. | No shared class-state/summon leakage; contributor rewards pay once; separate members cannot gain turns by switching; five-turn bleed-out and recovery interaction verified. Visible input/feedback matches the agreed behavior. |
| 4. Living world and sustained actions | Action-driven world/calendar integration, survival, day/night, resident/ecology behavior, automatic work/rest and alarms. | Offscreen/on-screen outcomes remain consistent; committed long actions interrupt correctly; resource/cargo accounting, actor attribution and deterministic saves remain intact. |
| 5. Persistent dungeon and campaign integration | Terrain-connected complexes, optional bosses, shortcuts, natural repopulation, special activation/deadlines/aftermath and open campaign progression. | Cross-floor/world geometry and party splitting work; deadlines continue outside and apply the announced site-specific outcome; sites and rewards survive reload; victory permits continuation and final party death creates a fresh world while retaining profile unlocks. |
| 6. Presentation, native tooling and retirement | Required Vagabonds sprites/mappings, preserved Roag controls/readability, updated content previews/audits and complete native replacements. | End-to-end rendered checks and feature inventory review, relevant original regression coverage, save/profile lifecycle validation, and verified native launch/tooling. Only then remove Lua/LÖVE and obsolete translation/prototype dependencies. |

Cross-cutting constraints: one authoritative actor/health state, one consistent world timeline, actual shared terrain, stable content/asset identities, bounded simulation bookkeeping, no duplicate rewards, exact deterministic continuation, and faithful preservation of Roag behavior except for explicitly approved adaptations above.

The original discussion consolidation changed documentation only. Subsequent implementation, checks and remaining work are recorded separately in [MIGRATION_PROGRESS.md](MIGRATION_PROGRESS.md).
