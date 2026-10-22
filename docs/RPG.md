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

New and loaded worlds begin **paused**. Click Play to resume time, or click a reachable destination to start travelling. New ordinary runs use a fresh seed unless you specify one. One real second represents one game minute. Conversations, menus, combat choices and focus loss pause simulation; completed combat rounds advance one minute. Closing a menu resumes the previous pause state. There is no offline progression.

## Controls

The world client uses mouse input throughout; gameplay keyboard bindings are disabled. The compatibility client retains its [legacy controls](LEGACY_RPG.md).

| Mouse / button | Action |
| --- | --- |
| Hover ground | Highlight the cell and preview an observed route in teal |
| Click ground | Start travelling immediately; the selected route stays gold |
| Click a person, cache, chest or stairs | Approach along observed ground, then interact automatically |
| Play / Pause | Resume / pause time; pausing stops travel |
| Stop / right-click | Cancel travel and targeting; right-click also closes a panel |
| Interact | Interact with a nearby cache, person or stairs |
| Conversation buttons | Recruit, trade, hear stories, share meals or receive treatment when offered |
| Buy 1 / Sell 1 | Trade the selected supply for its displayed price |
| Party | Click a companion and equipment, then Equip / Remove slot; revive, part ways or abandon when eligible |
| Build | Choose a structure, then click adjacent ground |
| Dig / Actions → Repair | Click adjacent terrain or an owned structure |
| Work | Select an order and worker, then Assign; Cancel and refund returns reservations |
| Craft | Click a camp recipe to reserve supplies and begin work |
| Actions | Rest at camp, wait one minute/hour, gather food, repair, access storage, present evidence or deliver food |
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

Clicking a reachable destination starts travel even while paused. Opening a panel stops the selected route and freezes time; closing it restores the previous running/paused state. Disabled buttons consume clicks. In combat, selecting a targeted ability alone spends no turn or random state; click its intended fighter to act.

The selector also marks build/dig targets. Movement and the following camera interpolate between cells; horizontal movement flips the sprite, while vertical movement retains its last facing. Collision and interaction still use the tile grid. The world remains **4096×4096**. Normal exploration starts at 36–64 pixels per tile (larger on very large displays), with a minimum of 24 pixels and at most 48 columns / 32 rows visible; use World map for an overview.

Outdoors, the entire local view is clear, including terrain and inhabitants behind walls. The world map records observed places without revealing distant towns. Underground keeps exploration fog, line of sight and dim remembered terrain. Zooming out observes more outdoor ground, within the local view limit.

Click routes cannot cross unknown ground. Building, gathering and excavation take work; reservations are paid immediately and terrain/output changes on completion. Stay nearby and resume time or wait; Work can assign a background resident instead. Cancelled/invalidated orders refund their reservations once. Natural excavation costs food according to hardness and yields materials. Dismantling owned structures refunds their recorded construction materials without food, so a misplaced structure cannot trap you after supplies run out. Water needs a bridge. Storage transfers keep actual quantities. Stairs cannot be removed or built over, and excavation cannot create vertical shafts. Walking through doors is automatic.

## Companions, consequences and stories

Everyone follows shared hunger, fatigue and wound rules. The party eats carried food; residents eat stocks/cargo or spend time gathering. Actions → Gather food queues gathering for the party. Exhaustion and wounds reduce speed/power/work. Actions → Rest takes time to recover, while healers/medicine help injuries. Camps wear and can be repaired through Actions → Repair. Memories affect stress and work, and shared meals/conversations build trust with a cooldown. A dismissed companion retains their identity, body, equipment and history.

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
