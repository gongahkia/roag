# REGOLITH

**A small life on the Moon.** A playable colony simulator in your terminal, inspired by the work orders and small human stories of Dwarf Fortress. You start with **Ada alone** in a sealed outpost. Stranded people are dying in the lunar wasteland. Send rescue expeditions, bring them home, and build a colony from the people you save. Your crew finds routes, mines, builds, tends crops, eats, rests, and retreats when suit oxygen runs low.

Python 3.10+ · macOS / Linux · no third-party runtime dependencies

```sh
python3 -m regolith
```

Run from this directory. A terminal around **120 × 40** looks best; **76 × 27** is the minimum. Resize at any time. The tutorial has **six short sections**: Enter advances, Left/Right goes back or forward, and Esc skips to the paused outpost. Enter on the final section starts time. Reopen the tutorial with `t`. The game holds time while a menu is open. `q` saves and exits; Ctrl-C also saves.

```sh
python3 -m regolith --load           # resume your outpost
python3 -m regolith --seed 8675309   # another patch of the Moon
python3 -m regolith --demo           # dispatch the first rescue and queue work
python3 -m regolith --ascii          # basic terminal glyphs
```

The optional `./play.sh` launcher works from any directory. For a system-wide `regolith` command, install with `python3 -m pip install -e .` in a virtual environment. The direct module command requires no install or network access.

## Start alone, bring people home

1. **One light in the dark.** Ada is the only colonist. The abandoned habitat has life support, two hydroponic farms, a battery, empty sleeping pods, and a 12 kW generator. Space pauses and resumes time.
2. **A voice in the wasteland.** Mika's distress beacon appears as a red `?`. **Click the survivor on the map** to dispatch Ada with a rescue kit costing **2 food and 2 water**. You can also move the cursor onto them and press **Enter**. A ready colonist is chosen automatically, interrupting their ordinary work. Press **r** to jump between distress markers on the map. Clicking a survivor again shows the current rescue's progress.
3. **Bring them home.** The rescuer travels to the survivor, supplies emergency oxygen, and escorts them back at half walking speed. The survivor joins your crew only after reaching pressure. New arrivals are injured, hungry, and exhausted; they recover before working. More distress calls arrive over time. Survivors lose oxygen, then health, and can die if you leave them. Space pauses those countdowns, as does opening the tutorial or another menu.
4. **Live off the Moon.** Press **d**, move to blue ice `~`, amber ore `%`, or purple crystal `*`, and press **Enter** to mine. Ice supplies water, ore supplies metal, and crystals enable equipment. Use **v** to mark a rectangle. Press **p** to survey open ground near the edge of visibility.
5. **Build a place worth saving.** Press **b** for blueprints. Machines need deck and connected power. Solar arrays can sit inside the starting room, using abstract roof connections. New rooms need deck enclosed by hull with an airlock. The **o** overlays show oxygen, temperature, and power.
6. **A future at Tranquility.** Rescue **at least three people**, reach cycle 3 with **four living crew and 20 food**, and run a pressurized **Earth uplink** to earn the founding charter. The uplink costs 60 metal, 20 crystal, and 5 kW; expand the starting grid's generation and storage. You can keep rescuing and building afterward.

Watch the log. The first debris impact occurs in cycle 2. Move to the impact coordinates, choose **b → Pressure hull**, and order a repair on the missing hull tile. **o** switches to the oxygen overlay so you can see the leak spread and the room recover.

## Controls

| Key | Action |
| --- | --- |
| Arrows / `hjkl` | Move cursor; scroll at viewport edges |
| Shift + arrows / `HJK` | Move five cells (`L` is reserved for loading) |
| Mouse wheel over map | Zoom in / out around the pointer |
| `+` / `-` (also `=` / `_`, Page Up / Down) | Zoom in / out around the map cursor |
| `0` | Reset view to 100% |
| Map header `[-]` / `[+]` | Click to zoom out / in; click the percentage to reset |
| Space | Run / pause |
| `1` / `2` / `3` | 4 / 12 / 28 simulation ticks per second |
| `.` | Pause and advance one tick |
| `d` | Mine rock and deposits, or salvage hull / machines |
| `b` | Open construction palette; Enter equips a blueprint |
| Enter / left click | Rescue the survivor at that map tile, or place the current work order |
| `v` | Start / cancel a rectangle; Enter applies orders |
| `x` | Equip order cancellation; materials are refunded |
| `p` | Survey open, visible terrain to reveal its surroundings |
| `r` | Jump between active distress markers on the map |
| Esc | Cancel selection and return to inspection |
| `o` | Cycle terrain, oxygen, temperature, and power overlays |
| `g` | Return cursor to the starting habitat |
| `c` | Crew manifest; arrows and Enter locate crew, `1`–`9` are shortcuts |
| `f` | Follow the colonist under the cursor |
| `e` | Expanded mission log; arrows scroll |
| `s` / `L` | Save / load; loading pauses time |
| `?` | Field notes |
| `t` | Reopen the six-section tutorial |
| `q` | Save and quit; after colony loss, preserve the last save |

## Map zoom

The view has **25%, 50%, 100%, 200%, and 300%** zoom levels. Scroll up to zoom in and down to zoom out. Wheel zoom keeps the location beneath the mouse in place as far as the world boundaries allow. Keyboard zoom centers on the map cursor; arrows still move one world tile at a time and scroll the camera at the edges. The map header has clickable zoom controls, and `0` returns to the standard view.

At 25% and 50%, each symbol summarizes a 4×4 or 2×2 area, letting you see more of the moon both horizontally and vertically. Survivors, crew, fires, and work orders take priority over terrain. Clicking targets the object actually shown in that area, so rescue markers remain usable in the overview. At 200% and 300%, map cells expand in both dimensions; zoom in for a closer look at construction and individual rooms. Status panels and controls keep their usual text size.

## Imported artwork

Open [the asset browser](assets/index.html) to inspect the downloaded artwork and enlarged previews. [The complete inventory](assets/catalog.json) records every original image's dimensions, file size, hash, source, and license. All **664 original PNGs** were decoded successfully, and all three ZIP archives passed their CRC checks. Original ZIPs are retained in [assets/archives](assets/archives/).

For individual sprites, see [the sprite guide](assets/SPRITE_GUIDE.md), [the searchable visual reference](assets/sprites.html), and [the complete sprite JSON](assets/sprites.json). They document **7,115 occupied sprite regions** across all three packs, with exact crops, identity confidence, suggested Regolith uses, and future ideas for unmapped artwork. Every bitmap is currently unused by the curses renderer. Full Markdown tables cover [Urizen](assets/reference/urizen.md), [Godot icons](assets/reference/godot-node-icons.md), and [input prompts](assets/reference/input-prompts.md).

| Folder | Contents | Suggested use |
| --- | --- | --- |
| `assets/godot-node-icons/` | 653 individual icons in six variants; mostly 16×16 | HUD, build tools, resources, health, save, pause, and zoom |
| `assets/input-prompts/new/` | Seven keyboard / PS4 / Xbox sprite sheets | Tutorial and control prompts |
| `assets/input-prompts/legacy/` | Two older keyboard / Xbox sheets | Alternative prompt style |
| `assets/urizen/` | Main 12×12 Urizen atlas and the free 2-bit showcase | Terrain, rooms, machinery, items, and colonists |
| `assets/previews/` | Contact sheets and enlarged inspection panels | Browse the artwork without modifying originals |

Urizen is the strongest candidate for the colony map. Its atlases use 12×12 tiles with a one-pixel margin and one-pixel gutters; the main sheet includes magenta guide bands to skip when selecting sprites. The five `icon_reset_2.png` files in the Godot variants are 10×12 rather than 16×16.

The publishers declare CC0 for these packs: [pixel-boy](https://pixel-boy.itch.io/icon-godot-node), [Hyohnoo](https://hyohnoo.itch.io/keyboard-controller-keys), and [vurmux](https://vurmux.itch.io/urizen-onebit-tileset). Source references are also recorded in [assets/CREDITS.txt](assets/CREDITS.txt).

## What is actually simulated

**Geology.** Five generations of an eight-neighbor majority cellular automaton form the basalt terrain. Seeded random walks place ice, iron, and crystal seams. Exploration uncovers terrain as people move through it.

**Atmosphere and heat.** Every tick reads the previous cell states and writes the next states to separate buffers. Four-neighbor oxygen exchanges are equal and opposite. Hull and basalt block gas. Surface cells vent to vacuum. Airlocks allow people and power through while blocking atmospheric exchange. Heat diffuses locally, with surface cells tending toward the current sunlight temperature.

**Fire.** Fire consumes local oxygen, adds heat, probabilistically spreads to oxygenated neighboring deck tiles, and dies without oxygen. It damages people and machines. Idle crew fight adjacent fires when it is safe to do so.

**Power and life support.** Connected deck and airlock cells form separate electrical networks. The RTG provides constant power; solar output follows a light cycle and drops during particle storms. Each island stores energy in its own batteries. Life support receives priority when power is short; it consumes water and supplies oxygen and heat. Farms need power, water, pressure, and viable temperature. Botanists accelerate nearby growth.

**People.** Crew use breadth-first pathfinding, prefer work matching their profession, track hunger, health, energy, morale, and suit oxygen, and interrupt work to retreat or sleep. Construction reserves materials when ordered and refunds them on cancellation. Inaccessible jobs stay queued. Named crew have flavor traits; professions affect work and crop tending.

**Rescue expeditions.** Click a stranded survivor's `?` directly on the map to send help. A nearby survivor is available from the start, with further signals broadcast during play. Injured survivors consume suit oxygen while stranded. An expedition reserves one ready colonist, spends a supply kit, follows an actual route, supplies emergency oxygen at contact, and escorts the survivor into pressurized shelter. Only then does that person join the crew, retaining their name, profession, and injuries. A blocked route, exposure, or the rescuer's death can cause an expedition to fail. Living survivors can be reached by a later attempt. Distress markers are visible beyond ordinary exploration. The crew manifest scrolls as the colony grows.

**Pressure and hazards.** Meteoroids breach the hull; particle storms affect solar output and EVA oxygen; electrical faults can start fires. Multiple breaches can overwhelm life support. The telemetry pressure gauge measures the starting habitat; inspect tiles or use overlays to assess additional rooms.

This is a compact, single-layer game, with accelerated fictional day/night cycles. Resources and life support are deliberately abstract: mined material goes straight to the colony inventory, all crew carry EVA suits, and enclosed machines have automatic roof/radiator connections. There is no hauling, vertical excavation, combat, or realistic lunar orbital model.

## Saves and reproducibility

Saves default to `~/.regolith/colony.json`. Use `--save PATH` for another slot. Writes are atomic, and saves include the terrain, atmosphere, fire, jobs, paths, crew, per-bank energy, survivor condition, active expeditions, future distress timing, and random generator state. Loading resumes the same simulation rather than regenerating it. Older saves retain their existing crew and gain future distress signals; start a new game without `--load` for the one-colonist opening. A new game does not replace your save until you save or exit.

For a deterministic run without a terminal:

```sh
python3 -m regolith --seed 42 --headless 700 --demo
```

This prints a JSON summary and does not write a save. The UI is separate from the simulation, so experiments can also use `Simulation` directly:

```python
from regolith.simulation import Simulation

colony = Simulation(seed=42)
cx, cy = colony.world.center
colony.designate(cx + 12, cy - 5, "dig")
colony.step(120)
print(colony.summary())
```

## Checks

```sh
python3 -m unittest discover -v
```

The suite checks atmosphere conservation, local update symmetry, breach / repair behavior, airlocks, heat, fire, reachable work, material reservations, isolated electrical networks, batteries, exploration, loss and victory states, rescue travel and recruitment, supply costs, survivor deaths, expedition failures, and saved simulation continuation. Interaction tests cover direct map clicks, keyboard rescue, repeated clicks, cursor and pointer zoom anchors, zoom limits, and rescue and mining at every zoom level. Real pseudo-terminal tests exercise mouse-wheel zoom in both directions, tutorial sections, mouse and keyboard rescue, crew selection, save / load, and resizing.

Python's built-in `curses` module is needed for the interface. It ships with standard macOS and most Linux Python builds. The headless simulator does not import curses.
