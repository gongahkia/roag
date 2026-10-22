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

New and loaded worlds begin **paused**. Space resumes time. New ordinary runs use a fresh seed unless you specify one. One real second represents one game minute. Conversations, menus, combat choices and focus loss pause simulation; completed combat rounds advance one minute. closing a menu resumes the previous pause state. There is no offline progression.

## Controls

| Input | Action |
| --- | --- |
| WASD / arrows | Move while time is running |
| Hover / left click | Highlight a cell and preview its route / follow the selected gold route |
| Space | Pause/resume; stops a running route when pausing |
| E / Enter | Open an adjacent cache, speak to an inhabitant or take stairs |
| R in conversation | Recruit that inhabitant, up to four active travellers |
| T in conversation | Open town trade |
| 1–4 in trade / Shift+1–4 | Buy / sell one food, wood, stone or medicine |
| I | Pack; 1–4 revives the corresponding downed companion with medicine |
| Shift+1–4 in pack | Deliberately abandon that downed companion; permanent death |
| B | Building tool; 1 wall, 2 floor, 3 door, 4 bridge, 5 storage, 6 camp |
| X | Digging tool; click adjacent rock, wall, tree, door or empty storage |
| G / H | Store / retrieve one wood in adjacent storage |
| R outside menus | Spend an hour recovering beside a serviceable camp, using food; danger interrupts |
| Period / Shift+Period | Explicitly wait one minute / one hour; danger interrupts |
| K | Work orders: Up/Down select, Tab chooses a worker, Enter assigns, C cancels/refunds |
| C / O / P | Camp crafting / queue food gathering / repair the hovered structure |
| Tab / Up/Down / Enter / U in pack | Select companion / select equipment / equip / remove that slot |
| D in pack | Part ways with the selected living companion in their home town |
| J | Main arc, generated delivery/repair requests and history; PageUp/PageDown scroll |
| F | Present evidence at the main arc's town, or negotiate with another compact |
| V | Deliver 20 food for an active request at the current town |
| M | Known world map |
| Mouse wheel | Zoom |
| 1–6 in battle | Class abilities, Guard, medicine or Retreat |
| Click a fighter | Use the selected ability on that fighter |
| F1 / F3 | Guide / generation inspector; Tab switches raw/final/repair views |
| F5 / F9 | Save / load this world format |
| Escape / right click | Close panel/tool / cancel tool or route |
| F11 | Fullscreen |
| Ctrl+Q / window close | Save and quit normal play |

Hover routes are teal; selected routes remain gold as you travel. Clicking while paused prepares a route for Space. The selector also marks build/dig targets. Movement and the following camera interpolate between cells; horizontal movement flips the sprite, while vertical movement retains its last facing. Collision and interaction still use the tile grid.

Outdoors, the entire local view is clear, including terrain and inhabitants behind walls. The world map records observed places without revealing distant towns. Underground keeps exploration fog, line of sight and dim remembered terrain. Zooming out observes more outdoor ground, within the local view limit.

Click routes cannot cross unknown ground. Building, gathering and excavation take work; reservations are paid immediately and terrain/output changes on completion. Stay nearby and resume time or wait; K can assign a background resident instead. Cancelled/invalidated orders refund their reservations once. Natural excavation costs food according to hardness and yields materials. Dismantling owned structures refunds their recorded construction materials without food, so a misplaced structure cannot trap you after supplies run out. Water needs a bridge. Storage transfers keep actual quantities. Stairs cannot be removed or built over, and excavation cannot create vertical shafts. Walking through doors is automatic.

## Companions, consequences and stories

Everyone follows shared hunger, fatigue and wound rules. The party eats carried food; residents eat stocks/cargo or spend time gathering. O queues gathering for the party. Exhaustion and wounds reduce speed/power/work. R takes time to recover, while healers/medicine help injuries. Camps wear and can be repaired with P. Memories affect stress and work, and shared meals/conversations build trust with a cooldown. A dismissed companion retains their identity, body, equipment and history.

Inhabitants have generated classes, backgrounds, traits, motivations, friends and jobs. Recruiting someone removes them from their town's workforce, affecting production or protection. Towns produce and consume goods, caravans carry actual cargo, shortages raise prices, and guard vacancies expose faction roads to raids. Blocking a complete road width causes caravans to reroute or wait; repair requests and shortages appear in the journal. Restoring access lets supply recover.

Combat preserves the four classes, initiative, guard, mark, stun, burn/bleed, stress, mana, revival and levels. Enemies have authored phase/cadence/target behaviours and saved visible intentions; J explains boss tactics. Status labels use G guard, B burn, L bleed, M mark and S stun. Enemies remain wounded after retreat. A surviving party can revive downed companions with medicine. Deliberate abandonment or a party wipe causes permanent death and immediately saves to the current slot; if saving fails, F5 must succeed before continuation. A wipe leaves a recoverable storage cache. **N** starts a new generated lone traveller in the same changed world; **Shift+N** starts a fresh seed after the deaths are saved.

The selected final threat is described in the journal and HUD, including its site and depth. Bosses draw from 48 authored archetypes with generated variants; guardians keep their actual strength rather than scaling to the party. Defeating the selected boss completes the expedition immediately; N starts a fresh generated world. Other dungeons, caches, companions and side arcs support the journey without unlocking the boss.

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
```

Explicit scenes and frame-limited captures ignore the player slot and disable automatic saving. `--load` explicitly reads the selected slot. F5 saves explicitly; a successful F9 resumes autosaving. Preview scene preparation is for inspection and is never a campaign migration. The old `--headless`, `--validate-seeds`, `--generation-report` and `--smoke-test` retain legacy coverage.

F3 shows authored WFC samples, learned pattern counts, constraints, attempts, fallback counts, repairs, cache bounds and raw/final footprint diagrams. It is an inspector, not a painting editor. [WORLD.md](WORLD.md) describes the layered generation and simulation architecture. [VERIFICATION.md](VERIFICATION.md) records measured checks and performance limits.
