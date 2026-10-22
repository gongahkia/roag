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

New and loaded worlds begin **paused**. Click Play to resume time, or click a reachable destination to start travelling. New ordinary runs use a fresh seed unless you specify one. One real second represents one game minute. Conversations, reference/game menus, combat choices and focus loss pause simulation; Party and Work panels run until you press Pause; completed combat rounds advance one minute. Closing a menu resumes the previous pause state. There is no offline progression.

## Controls

The world client uses mouse input throughout; gameplay keyboard bindings are disabled. The compatibility client retains its [legacy controls](LEGACY_RPG.md).

| Mouse / button | Action |
| --- | --- |
| Hover ground | Highlight the cell and preview an observed route in teal |
| Click ground | Start travelling immediately; the selected route stays gold |
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
| Combat ability, then fighter | Select an ability and explicitly target an enemy or companion; self/group abilities execute immediately |
| Save / Load | Save / restore the selected compatible world slot |
| Menu | Guide, procedural inspector, zoom reset, fullscreen, save and quit |
| Inspector → Raw / Final / Repairs | Inspect generation stages |
| Ending buttons | New traveller in the same world after a wipe, or begin a fresh world |
| Window close | Save and quit normal play |

Clicking a reachable destination starts travel even while paused. Opening a panel stops the selected travel route. Party and Work keep the world running; other panels freeze time. Play/Pause remains available without closing the panel. Conversations always pause. Disabled buttons consume clicks. In combat, selecting a targeted ability alone spends no turn or random state; click its intended fighter to act.

Selected tiles remain highlighted until you act, dismiss the menu, or begin another selection. Clicking an unavailable destination opens its context menu. Movement and the following camera interpolate between cells; horizontal movement flips the sprite, while vertical movement retains its last facing. Collision and interaction still use the tile grid. The world remains **4096×4096**. Normal exploration starts at 36–64 pixels per tile (larger on very large displays), with a minimum of 24 pixels and at most 48 columns / 32 rows visible; use World map for an overview.

Outdoors, the entire local view is clear, including terrain and inhabitants behind walls. The world map records observed places without revealing distant towns. Underground keeps exploration fog, line of sight and dim remembered terrain. Zooming out observes more outdoor ground, within the local view limit.

Click routes cannot cross unknown ground. Building, gathering and excavation take work; reservations are paid immediately and terrain/output changes on completion. Cursor orders guide the party along observed free ground to work beside each target; Work can assign a background resident instead. Unreachable orders wait for access. Up to 64 work orders can be pending; invalid or unaffordable tiles are skipped with feedback. Orders and automatic approach intent survive saving. Pause freezes work; Stop or a new map click stops automatic approach. Right-click queued tiles to resume or cancel their orders. Cancelled/invalidated orders refund their reservations once. Natural excavation costs food according to hardness and yields materials. Dismantling owned structures refunds their recorded construction materials without food, so a misplaced structure cannot trap you after supplies run out. Characters, camp furniture, storage, unopened chests, solid landmarks and barrels block walking. Routes go around observed obstacles and stop if someone moves into their path. Barrels can be dismantled; landmark furniture stays protected. Roads, doors, stairs, bridges, bones and other flat decorations remain walkable. Water needs a bridge. Storage transfers keep actual quantities. Stairs cannot be removed or built over, and excavation cannot create vertical shafts. Walking through doors is automatic.

## Companions, consequences and stories

Everyone follows shared hunger, fatigue and wound rules. The party eats carried food; residents eat stocks/cargo or spend time gathering. Actions → Gather food queues gathering for the party. Exhaustion and wounds reduce speed/power/work. Right-click camp → Rest takes time to recover, while healers/medicine help injuries. Camps wear and can be repaired through their right-click Repair action. Memories affect stress and work, and shared meals/conversations build trust with a cooldown. A dismissed companion retains their identity, body, equipment and history.

Each party member stands on a separate walkable tile and follows the leader with their own smooth movement and facing. Followers round corners without passing through other characters. To turn back in a narrow corridor, right-click the last companion and choose Lead; downed companions remain with the group on their own tiles. Recruitment, stairs and retreat keep everyone separated. Removing the leader promotes the next companion where they stand. Compatible saves with the former stacked formation spread the party out on load, without changing the save file until you save normally.

Inhabitants have generated classes, backgrounds, traits, motivations, friends and jobs. Recruiting someone removes them from their town's workforce, affecting production or protection. Towns produce and consume goods, caravans carry actual cargo, shortages raise prices, and guard vacancies expose faction roads to raids. Blocking a complete road width causes caravans to reroute or wait; repair requests and shortages appear in the journal. Restoring access lets supply recover.

Combat preserves the four classes, initiative, guard, mark, stun, burn/bleed, stress, mana, revival and levels. Enemies have authored phase/cadence/target behaviours and saved visible intentions; Tactics or Journal explains boss tactics. Status labels use G guard, B burn, L bleed, M mark and S stun. Enemies remain wounded after retreat. A surviving party can revive downed companions with medicine. Deliberate abandonment or a party wipe causes permanent death and immediately saves to the current slot; if saving fails, Save must succeed before continuation. A wipe leaves a recoverable storage cache. The ending offers New traveller here or Begin a new world after the deaths are saved.

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

## Conversations and delegated work

Click a person to approach and talk. Both participants appear at a larger size, face one another and take turns speaking. The current speaker bobs while the text appears and makes original synthesized speech sounds. Click the text or Show full line to reveal the rest, then Continue to advance. Choices appear after the greeting or reply. A choice applies its cost/effect once; skipping speech never repeats it. Voice cycles through volume levels including mute; Motion switches to Reduced. These preferences are saved beside your slot, separately from game state.

Left-drag observed tiles and right-click to designate Build, Dig or Repair. Available party members automatically take distinct reachable jobs within a **16-tile rally radius**, shown on the map. The four-member limit and shared pack still apply. They return toward the rally when idle. Work lists current workers and waiting reasons. Click an order to change Low/Normal/High priority, suspend it, resume it, assign a named person or return it to automatic assignment. Supplies are reserved when orders are accepted; unaffordable cells are reported/skipped. Cancel returns reservations once; suspension retains progress and supplies. Named assignments retain that person while suspended.

Inspecting tiles or opening panels does not cancel jobs. A travel click temporarily brings companions along and moves the rally; work resumes when travel stops. Orders outside the rally wait until you move closer. Workers respect occupied tiles, observed routes, rest and wounds. An enemy contacting any member interrupts work for the usual shared turn-based battle. Retreat suspends party jobs; resume them deliberately when the area is safe.
