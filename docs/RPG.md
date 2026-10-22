# Vagabonds play guide

New games begin with one generated traveller in a finite, simulated fantasy world. Explore a 4096×4096 surface, recruit generated inhabitants, visit 24 towns and 48 sites, build a camp, tunnel through ruins and follow a generated main arc. Three underground floors connect through generated stairs. Nearby people and threats move in real time; encounters use turn-based combat.

## Start and save

```sh
./vagabonds.sh
./vagabonds.sh --new --seed 1337
./vagabonds.sh --save /path/to/another-slot.json
```

Normal launch resumes the selected slot if present; a missing slot starts a new world. Invalid existing saves report an error and never silently start over. `--new` explicitly starts over; normal saving or quitting replaces the selected slot. `--load` requires an existing save. Default slot: `~/.vagabonds/campaign.json`. World chunk snapshots live beside it in `campaign.json.chunks/`; back up both. Worlds pin their generation version and sample manifest, so incompatible generation changes are rejected rather than rerolling terrain.

Existing `lantern-rpg` saves open the original three-area game with its original rules. [Legacy campaign guide](LEGACY_RPG.md) documents those controls. `--import-legacy PATH` validates and copies an old RPG save without changing the source. Lunar field sessions are rejected by both clients.

New and loaded worlds begin **paused**. Space resumes time. One real second represents one game minute. Conversations, menus, combat and focus loss pause simulation; closing a menu resumes the previous pause state. There is no offline progression.

## Controls

| Input | Action |
| --- | --- |
| WASD / arrows | Move while time is running |
| Hover / left click | Highlight a cell and preview its route / follow the selected gold route |
| Space | Pause/resume; stops a running route when pausing |
| E / Enter | Speak to a nearby inhabitant or take current/adjacent stairs |
| R in conversation | Recruit that inhabitant, up to four active travellers |
| T in conversation | Open town trade |
| 1–4 in trade / Shift+1–4 | Buy / sell one food, wood, stone or medicine |
| I | Pack; 1–4 revives the corresponding downed companion with medicine |
| Shift+1–4 in pack | Deliberately abandon that downed companion; permanent death |
| B | Building tool; 1 wall, 2 floor, 3 door, 4 bridge, 5 storage, 6 camp |
| X | Digging tool; click adjacent rock, wall, tree, door or empty storage |
| G / H | Store / retrieve one wood in adjacent storage |
| R outside menus | Rest beside a camp, spending food |
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

Click routes cannot cross unknown ground. Building costs stone or wood; natural excavation costs food according to hardness and yields materials. Dismantling your own walls, doors, empty storage or camps refunds their construction materials without food, so a misplaced structure cannot trap you after supplies run out. Water needs a bridge. Storage transfers keep actual quantities. Stairs cannot be removed or built over, and excavation cannot create vertical shafts. Walking through doors is automatic.

## Companions, consequences and stories

Inhabitants have generated classes, backgrounds, traits, motivations, friends and jobs. Recruiting someone removes them from their town's workforce, affecting production or protection. Towns produce and consume goods, caravans carry actual cargo, shortages raise prices, and guard vacancies expose faction roads to raids. Blocking a complete road width causes caravans to reroute or wait; repair requests and shortages appear in the journal. Restoring access lets supply recover.

Combat preserves the four classes, initiative, enemy intentions, guard, mark, stun, burn/bleed, stress, mana, revival and levels. Enemies remain wounded after retreat. A surviving party can revive downed companions with medicine. Deliberate abandonment or a party wipe causes permanent death and immediately saves to the current slot; if saving fails, F5 must succeed before continuation. A wipe leaves a recoverable storage cache. **N** starts a new generated lone traveller in the same changed world; **Shift+N** starts a fresh seed after the deaths are saved.

The main arc uses trade conflict, blight or succession premises, referring to actual towns, sites and patrons. Evidence is found underground; tunnelling can bypass a defended entrance. Return with the requested supplies or a companion's support. A dead patron's townspeople can receive evidence, and another compact can produce an alternative outcome. Long-running severe shortages can explicitly fail the arc. Exploration and simulation continue after either outcome.

## Previews and diagnostics

```sh
./vagabonds.sh --scene world
./vagabonds.sh --scene world-site
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
