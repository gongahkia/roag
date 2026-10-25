# Vagabonds play guide

New games begin with one generated traveller in a finite, simulated fantasy world. Explore a 4096×4096 surface, recruit generated inhabitants, visit 24 towns and 48 sites, build a camp, tunnel through ruins, find equipment and confront the selected final boss. Side arcs are optional; every dungeon/depth and the final threat are available immediately. Three underground floors connect through generated stairs. Nearby people and threats move in real time; encounters use turn-based combat.

## Start and save

```sh
./vagabonds.sh
./vagabonds.sh --new --seed 1337
./vagabonds.sh --save /path/to/another-slot.json
```

Normal launch resumes the selected slot if present; a missing slot starts a new world. Invalid existing saves report an error and never silently start over. `--new` explicitly starts over; normal saving or quitting replaces the selected slot. `--load` requires an existing save. Default slot: `~/.vagabonds/campaign.json`. World chunk snapshots live beside it in `campaign.json.chunks/`; back up both. World saves pin the content hash, generation version and sample manifest, so incompatible generation changes are rejected rather than rerolling terrain.

Existing `lantern-rpg` saves open the original three-area game with its original rules. [Legacy campaign guide](LEGACY_RPG.md) documents those controls. `--import-legacy PATH` validates and copies an old RPG save without changing the source. Lunar field sessions are rejected by both clients.

Ordinary new and loaded worlds begin **running**. Inhabitants act even while you stand still or switch away from the window. Use Pause to stop time, or click a reachable destination to travel. New ordinary runs use a fresh seed unless you specify one. One real second represents one game minute. Conversations, reference/game menus and combat choices pause simulation; Party and Work panels run until you press Pause; completed combat rounds advance one minute. Closing a menu resumes the previous pause state. There is no offline progression.

## Controls

The world client uses mouse input throughout; gameplay keyboard bindings are disabled. The compatibility client retains its [legacy controls](LEGACY_RPG.md).

| Mouse / button | Action |
| --- | --- |
| Hover ground | Highlight the cell and inspect its contents |
| Click ground | Calculate a route and start travelling; only the hovered tile is highlighted |
| Click a person, cache, chest or stairs | Approach along observed ground, then interact automatically |
| Play / Pause | Resume / pause time; pausing stops travel |
| Left-drag | Select a rectangle of observed tiles (up to 256) |
| Right-click terrain / selection | Stop travel and open relevant actions at the cursor |
| Right-click a panel | Close the panel |
| Stop | Stop travel and suspend party work; resume orders in Work or at the cursor |
| Conversation buttons | Recruit, trade, hear stories, share meals or receive treatment when offered |
| Buy 1 / Sell 1 | Trade the selected supply for its displayed price |
| Party | Click a companion and equipment, then Equip / Remove slot; revive, part ways or abandon when eligible |
| Cursor menu → Build / Dig / Repair | Queue work on the chosen tile or selection; available companions take separate local jobs |
| Work | Automatic or named workers; priority, suspend/resume and cancel/refund; time keeps running |
| Right-click camp → Craft / Rest | Choose a recipe or rest beside that camp |
| Actions | Wait one minute/hour or gather food |
| Right-click a townsperson | Speak, present side-arc evidence or deliver requested food when applicable |
| Right-click a companion → Lead | Change the leader without moving anyone; choose the last companion to reverse the formation |
| Storage → Store 1 / Take 1 | Transfer actual supplies between the pack and chest |
| Journal / Tactics | Final threat, boss tactics, optional side arc and paged history |
| World map | Overview of known places across the large world |
| Mouse wheel / + / − / View | Zoom exploration / reset the close RPG camera |
| Combat ability, then fighter | Select an ability and explicitly target an enemy or companion; ordinary self/group abilities execute immediately; cooperative abilities first need a helper |
| Save / Load | Save / restore the selected compatible world slot |
| Menu | Guide, procedural inspector, zoom reset, fullscreen, Mark this moment, save and quit |
| Inspector → Raw / Final / Repairs | Inspect generation stages |
| Ending buttons | New traveller in the same world after a wipe, or begin a fresh world |
| Window close | Save and quit normal play |

Clicking a reachable destination starts travel even while paused. Opening a panel stops the selected travel route. Party and Work keep the world running; other panels freeze time. Play/Pause remains available without closing the panel. Conversations always pause. Disabled buttons consume clicks. In combat, selecting a targeted ability alone spends no turn or random state; click its intended fighter to act.

Selected tiles remain highlighted until you act, dismiss the menu, or begin another selection. Clicking an unavailable destination opens its context menu. Movement and the following camera interpolate between cells; horizontal movement flips the sprite, while vertical movement retains its last facing. Collision and interaction still use the tile grid. The world remains **4096×4096**. Normal exploration starts at 36–64 pixels per tile (larger on very large displays), with a minimum of 24 pixels and at most 48 columns / 32 rows visible; use World map for an overview.

Outdoors, the entire local view is clear, including terrain and inhabitants behind walls. The world map records observed places without revealing distant towns. Underground keeps exploration fog, line of sight and dim remembered terrain. Zooming out observes more outdoor ground, within the local view limit.

Click routes cannot cross unknown ground. Building, gathering and excavation take work; supplies are reserved immediately, then collected and brought to the site before progress begins. Extraction changes terrain or resource stock once and puts produced material in the worker’s cargo for delivery. Cursor orders guide the party along observed free ground to work beside each target; Work can assign a background resident instead. Unreachable orders wait for access. Up to 64 work orders can be pending; invalid or unaffordable tiles are skipped with feedback. Orders and automatic approach intent survive saving. Pause freezes work; Stop or a new map click stops automatic approach. Right-click queued tiles to resume or cancel their orders. Cancelled/invalidated unfinished orders release their reserved materials once, locally or in a recoverable supply bundle. Cancelling after extraction releases only the undelivered output; consumed crafting inputs stay spent. Excavation uses work time and normal hunger/fatigue without charging food per tile. Gather food or buy town supplies to support longer jobs. Dismantling owned structures recovers their recorded construction materials into worker cargo without food, so a misplaced structure cannot trap you after supplies run out. Characters, camp furniture, storage, unopened chests, solid landmarks and barrels block walking. Routes go around observed obstacles and stop if someone moves into their path. Barrels can be dismantled; landmark furniture stays protected. Roads, doors, stairs, bridges, bones and other flat decorations remain walkable. Water needs a bridge. Storage transfers keep actual quantities. Stairs cannot be removed or built over, and excavation cannot create vertical shafts. Walking through doors is automatic. Active diggers occasionally wiggle while breaking or dismantling blocks; Pause, suspended work and reduced motion stop these gestures.

## Companions, consequences and stories

Everyone follows shared hunger, fatigue and wound rules. The party eats carried food; residents eat stocks/cargo or spend time gathering. Actions → Gather food queues gathering for the party. Exhaustion and wounds reduce speed/power/work. Right-click camp → Rest takes time to recover, while healers/medicine help injuries. Camps wear and can be repaired through their right-click Repair action. Memories affect stress and work, and shared meals/conversations build trust with a cooldown. A dismissed companion retains their identity, body, equipment and history.

Activated farmers gather grain, woodcutters fell trees, and quarriers mine rock near home. They spend time at the actual source, carry what they produce and walk it back to town storage. Healers collect town ingredients, build a camp workplace if needed, craft medicine and repair worn workplaces. Full stores leave the remaining goods with the worker. Needs, danger and your assigned jobs interrupt their ordinary job without duplicating its output; hover a person to see the current activity. These residents keep working when you leave their region. Unvisited settlements retain regional production. Recruiting a working resident brings the job’s carried goods into the pack, leaving any overflow in a recoverable nearby bundle.

Each party member stands on a separate walkable tile and follows the leader with their own smooth movement and facing. Followers round corners without passing through other characters. To turn back in a narrow corridor, right-click the last companion and choose Lead; downed companions remain with the group on their own tiles. Recruitment, stairs and retreat keep everyone separated. Removing the leader promotes the next companion where they stand. Compatible saves with the former stacked formation spread the party out on load, without changing the save file until you save normally.

Inhabitants have generated classes, backgrounds, traits, motivations, friends and jobs. Once encountered, they keep walking to food, home and work after you leave the region or change floors. Blocked trips wait for access; food and rest require arrival. Recruiting someone removes them from their town's workforce, affecting production or protection. Towns produce and consume goods, caravans carry actual cargo, shortages raise prices, and guard vacancies expose faction roads to raids. Blocking a complete road width causes caravans to reroute or wait; repair requests and shortages appear in the journal. Restoring access lets supply recover.

Combat preserves the four classes, initiative, guard, mark, stun, burn/bleed, stress, mana, revival and levels. Enemies have authored phase/cadence/target behaviours and saved visible intentions; Tactics or Journal explains boss tactics. Condition icons show guard, burn, bleed, mark and stun; hover a fighter for their meanings and durations. The initiative strip shows this round's order, with spent actions crossed out. Enemy intentions include target portraits and effect ranges based on current defenses; these can change before the enemy acts. Enemies remain wounded after retreat. A surviving party can revive downed companions with medicine. Deliberate abandonment or a party wipe causes permanent death and immediately saves to the current slot; if saving fails, Save must succeed before continuation. A wipe leaves a recoverable storage cache. The ending offers New traveller here or Begin a new world after the deaths are saved.

The selected final threat is described in the journal and HUD, including its site and depth. Bosses draw from 48 authored archetypes with generated variants; guardians keep their actual strength rather than scaling to the party. Defeating the selected boss completes the expedition immediately; Begin a new world starts a fresh generated world. Other dungeons, caches, companions and side arcs support the journey without unlocking the boss.

The optional side arc uses trade conflict, blight or succession premises, referring to actual towns, sites and patrons. Evidence is found underground; tunnelling can bypass a defended entrance. Return with the requested supplies or a companion's support. A dead patron's townspeople can receive evidence, and another compact can produce an alternative outcome. Long-running severe shortages can explicitly fail the arc. Exploration and simulation continue after either outcome.

## Previews and diagnostics

```sh
./vagabonds.sh --scene world
./vagabonds.sh --scene world-site
./vagabonds.sh --scene world-boss
./vagabonds.sh --scene world-battle
./vagabonds.sh --scene world-inspector
./vagabonds.sh --world-headless
./vagabonds.sh --world-report
./vagabonds.sh --validate-worlds 1000 --seed -500
./vagabonds.sh --world-smoke --frames 360 --screenshot artifacts/world.png
./vagabonds.sh --scene atlas
./vagabonds.sh --scene world --ui-report artifacts/ui.json
```

Explicit scenes, UI reports and frame-limited captures ignore the player slot and disable automatic saving. `--load` explicitly reads the selected slot. The Save button saves explicitly; successful Load resumes autosaving. Preview scene preparation is for inspection and is never a campaign migration. The old `--headless`, `--validate-seeds`, `--generation-report` and `--smoke-test` retain legacy coverage.

Menu → World inspector shows authored WFC samples, learned pattern counts, constraints, attempts, fallback counts, repairs, cache bounds and raw/final footprint diagrams. It is an inspector, not a painting editor. [WORLD.md](WORLD.md) describes the layered generation and simulation architecture. [VERIFICATION.md](VERIFICATION.md) records measured checks and performance limits.

## Illustrated management panels

Party shows each companion's actual sprite and health bar. Equipment cards show the authored item sprite, carried count and whether the selected companion wears it; unavailable items are dimmed but remain selectable for comparison. Hover an item for its stats and the change from current gear. Supply icons accompany the pack totals.

Work shows task/material icons, progress bars and the assigned worker's portrait; available people have portraits and party/resident labels. Journal separates the boss, side story and history, with boss/patron sprites and character portraits for identifiable events. Older/newer history retains long entries and pending requests.

## Conversations and delegated work

Click a person to approach and talk. Both participants appear at a larger size, face one another and take turns speaking. The current speaker bobs while the text appears and makes original synthesized speech sounds. Click the text or Show full line to reveal the rest, then Continue to advance. Choices appear after the greeting or reply. A choice applies its cost/effect once; skipping speech never repeats it. Voice cycles through volume levels including mute; Motion switches to Reduced. These preferences are saved beside your slot, separately from game state.

Left-drag observed tiles and right-click to designate Build, Dig or Repair. Available party members automatically take distinct reachable jobs within a **16-tile rally radius**, shown on the map. The four-member limit and shared pack still apply. They return toward the rally when idle. Work lists current workers and waiting reasons. Click an order to change Low/Normal/High priority, suspend it, resume it, assign a named person or return it to automatic assignment. Supplies are reserved when orders are accepted; unaffordable cells are reported/skipped. Cancel releases unconsumed reservations or remaining produced output once; remote material stays in a recoverable bundle. Suspension retains progress and material ownership. Named assignments retain that person while suspended. Work → **To: Pack** cycles output delivery through observed storage in the rally and back to the shared pack. Workers must approach that destination; full or blocked storage keeps the remaining goods in cargo. Change the destination to reroute a delivery. A replacement must reach the previous carrier to take over its cargo.

Inspecting tiles or opening panels does not cancel jobs. A travel click temporarily brings companions along and moves the rally; work resumes when travel stops. Orders outside the rally wait until you move closer. Workers respect occupied tiles, observed routes, rest and wounds. An enemy contacting any member interrupts work for the usual shared turn-based battle. Retreat suspends party jobs; resume them deliberately when the area is safe.

### Gathering in the landscape

Hover fruit trees, fishing banks, herb patches, nests, mushrooms and iron deposits to inspect their stock and yield. Right-click **Gather resources**, or drag-select multiple tiles and use the same command. Available companions take separate jobs. Exhausted plants and fishing banks show a depleted sprite and a recovery time; mineral deposits remain exhausted. Digging removes terrain and the resource on it, so harvest a deposit before clearing it if you want its ore.

Digging no longer charges food per tile; it uses work time and the normal fatigue/hunger simulation. Equip the starting pickaxe or fishing rod under Party for faster work. Tools have their own slot, leaving weapons equipped. Party → Shared pack shows gathered ingredients. Both tools can be crafted beside a camp.

### Preparing at camp

Right-click an existing camp and choose **Assign camp duties**. Select a companion portrait and Cooking, Treatment, Watch or Stories. Each person has one job at a time; different duties can run together, and Work allows reassignment or cancellation. The camp panel keeps time running until you press Pause. Resting the party lets assigned camp workers continue their duties.

Cooking uses gathered ingredients to prepare against burning, bleeding, physical attacks or dread. Hover recipes for the exact effect and cost. Meals serve conscious party members within six tiles; effects and remaining time appear on portrait tooltips. Treatment consumes medicine to heal wounds. Watch grants initiative and an opening guard for the next three game hours. Stories respond to recent events and reveal actual sites on World map; **Camp conversation** replays them with portraits, speech and animation. Repeating a story requires waiting for the speaker's cooldown.

### Choosing equipment effects

Dungeon and landmark rewards offer seeded equipment variants. Their choice cards show the item's sprite, numbers and named effects; hover for the exact rule before choosing. A Warden's shield can protect another injured companion once each round, while an Ember staff strengthens burning attacks. Mending and Steadfast variants support healing and resistance to dread, and tool variants improve particular kinds of work. Effects from equipment combine up to the documented caps; preparation from camp still applies.

A chosen variant keeps its identity and effects through saving, equipping and storage. You cannot reroll a cache by loading. Party lists acquired variants as well as the standard items; the separate tool slot leaves weapons equipped. Crafting a Warden's shield uses wood and iron ore gathered from deposits.

With two or more companions in combat, Twin strike and Shared aid appear beside the usual abilities. Choose a helper who is conscious, unstunned and still has a turn ahead this round. Twin strike costs 3 MP each and then needs an enemy target; Shared aid costs 4 MP from the active companion and 3 MP from the helper, and heals/cleanses the conscious party immediately after helper selection. Both companions spend their action for this round. Hover shows eligibility and predicted damage/healing; right-click cancels selection for free. A helper's burn/bleed still ticks on their scheduled turn. These abilities work with any generated companions.


### Battle presentation controls

The battle occupies the full play area with larger facing fighters and icon-based ability buttons. The chosen action plays a short anticipation, travel, impact and recovery sequence; further actions wait until it finishes. Twin strike still resolves as one combined hit. Defeated fighters remain visible through the finishing blow, then the reward screen shows awarded supplies and survivor condition.

Use **Speed 1x / 2x**, **Motion full / low**, **Sound** (cycles volume through mute) and **Skip** above the stage. Skip finishes the current visual sequence without repeating or changing the resolved action. Inspection panels and focus loss pause its animation. Party, Tactics and Log remain accessible. These controls are mouse-only and do not change damage, costs, initiative or the minute advanced by a completed round.

### Developing your companions

Each generated person has a personal combat trait, visible before recruitment and in Party → Developments. At levels 2 and 4, choose one of two class specialisations. Select a card to inspect its full rule, then use **Learn permanently** to commit. You can inspect locked alternatives, but a learned choice cannot be replaced. Choices apply between encounters and cost no supplies or world time. Mana reductions affect the acting companion; cooperative helper costs stay as displayed.

### Reading encounter groups

Dungeon populations depend on the region, kind of site and depth. Patrols, warbands and elites have fixed compositions and strength; every floor is accessible from the start. Hover a visible foe or open **Tactics** during combat to read the group's identity, danger tier and counterplay. Up to four nearby unaffiliated foes can also join an encounter, regardless of party size; hover estimates count the visible ones. A mender can sustain an attacker, a marked companion can become a priority target, and bleeding or dread can wear down a party across several fights.

A **Charging / stun** cue means an enemy has spent its turn preparing an attack for the next round. Stun before release to break it, defeat the caster, or guard to reduce the impact. A later cleanse cannot restore a broken preparation. Release keeps its declared single target even if that target falls. Some special actions need a different action before they can repeat; remaining mana and current intentions are visible.

Victories automatically add the group's themed supplies and equipment to the shared pack. Guardian victories also grant a signature item from their family. Dungeon caches offer related equipment choices. Use **Manage party and equipment** to equip the new items or choose an unlocked development. Revisiting, retreating or reloading cannot respawn a defeated group or pay its reward twice.
