[![](https://img.shields.io/badge/roag_1.0.0-passing-green)](https://github.com/gongahkia/roag/releases/tag/1.0)

# `Roag` 🗡️

A [persistent](https://cavesofqud.com/) roguelike where you're a [late-medival mailman](https://en.wikipedia.org/wiki/Jōmon_people).

## Stack

* [Lua](https://www.lua.org/)
* [LÖVE2d](https://love2d.org/)

## Screenshots

![](./asset/reference/1.png)
![](./asset/reference/2.png)
![](./asset/reference/3.png)
![](./asset/reference/4.png)

## Usage

> [!NOTE]  
> `Roag` minimally requires a 80-column by 24-row terminal.

The below instructions are for locally running `Roag`.

1. First clone the repository.

```console
$ git clone https://github.com/gongahkia/roag && cd roag
```

2. Then run the below from repo root.

```console
$ love .
```

## Docker development

The repository includes a pinned Alpine/LÖVE/LuaJIT development image so tests,
packaging, and optional graphical runs can be isolated from the host:

```console
$ make build
$ make test
$ make package
```

The Make targets use an isolated Docker client configuration under the user
cache so WSL never needs to execute a Windows-only credential helper. Set
`DOCKER_CONFIG` yourself if a private registry is required. The packager
validates the generated `roag.love` archive before replacing it.

To run the game through the container, configure graphical forwarding first
(WSLg or an X11 server on the host), then run:

```console
$ make play
```

The game service mounts only a named LÖVE save-data volume in addition to the
checkout, keeping saves separate from repository files. On WSL, Docker Desktop
must have WSL integration enabled for this distro before any `docker compose`
command will work.

The repository's graphical authoring tools use the same container and display
forwarding setup:

```console
$ make studio                # Screen Composer and Modifier Workbench
$ make sprite-editor         # Sprite Workbench
$ make generation-inspector  # Read-only deterministic floor inspector
$ make room-editor           # Writable Dungeon/Reactor room templates
```

The editors intentionally write only their declared repository content files;
they never open or alter an active game run. Their source-local files remain
visible on the host through the checkout mount.

## Controls

| Key | Action |
| --- | --- |
| `W` / `A` / `S` / `D` | **Expedition:** cardinal movement; **Campaign:** cardinal movement only; the most recently pressed held direction wins. Movement sets facing. |
| `E` | **Expedition:** class weapon in the facing direction. **Campaign:** attack with the active quick weapon; tools strike enemies or faced material and empty magazines reload. |
| `R` | **Campaign:** swap quick weapon; a successful field swap costs one turn |
| `Q` | **Expedition:** class ability. **Campaign:** active quick ability (Dash is a default ability binding) |
| `B` | Arm a bomb |
| `F` | Light a flare; it stuns enemies in its blast |
| `X` | **Campaign:** swap quick ability; a successful field swap costs one turn |
| `I` | **Expedition:** paused Run Build summary. **Campaign:** carried inventory; `Tab` assigns quick slots while paused. `Delete`/`Backspace` or the explicit drop target drops selected cargo at your feet. |
| Mouse drag in inventory | Repack cargo with a live valid/invalid placement preview. In Campaign corpse salvage, drag body parts and cargo from the fallen grid into your inventory grid. |
| `C` | **Campaign:** enter/exit live Build stance (no turn). In Build stance, `E` places the faced recipe for one turn; `R`/`X` cycle recipes for free. |
| `U` | **Campaign:** use exactly the faced tile: doors, storage, services, stations, connections, loose cargo, or corpse salvage. Ordinary resource/ammo stacks are collected by walking over them. |
| `Tab` / `W` / `S` / Enter / `R` / `F` in reconstruction | Switch body/inventory focus, select, install or uninstall, rotate cargo, and finish reconstruction |
| `W` / `S` or arrow keys | Select a class, boon, curse, or shop item in a menu |
| Enter or `E` | Confirm a menu choice or leave the shop for the boss |
| `B` / `V` in the shop | Buy / sell the selected item |
| Escape | Quit the game |

Campaign uses two identity-bound quick weapon slots and two quick ability slots; `E` and `Q` always use the active selections shown in the HUD. Ranged reserve ammunition is physical 1×1 cargo (bullets, shells, energy cells, or explosives), while a loaded magazine stays on its exact physical weapon component.

**Expedition** is a separate disposable combat run: choose a character, clear compact encounters, and stack passive items without inventory capacity. Its magazine/reload behavior remains, but reserve ammo and SCRAP are run-local abstractions; death ends that run and preserves only character/item unlocks. **Open World Sandbox** remains the persistent body/salvage/construction mode.

Campaign continuously shows your active weapon's direct facing footprint and visible hostile current-threat footprints. These are intent previews, not extra turns: move to rotate your facing and read the outlined cells before committing an attack.

Campaign death is succession, not a reset: your old body, carried cargo, and loaded weapons remain at the death site, while a fresh body reconstructs beside the active Reconstruction Anchor. The succession screen names both places. Face a Reconstruction Station and use `U` to set it as the future anchor; the HUD indicates whether that anchor is in the current zone.

Equipped charms can also create deterministic reactive build effects with your weapons, body capabilities, collisions, reloads, and the environment. Inspect the paused Inventory's **BUILD EFFECTS** section to see active combinations and any missing requirement.

Campaign supplies are deliberately physical but fast: walk onto TIMBER, MASONRY, METAL, or ammunition to collect the whole stack if it fits. Loose components and corpses are deliberate: face them with `U`; corpse salvage opens two spatial grids where rotation and placement matter. Inventory and salvage pause the world, and dropped cargo stays at your feet until you leave and walk back onto that cell.

Tools are physical cargo, not body parts. Assign an Axe, Pickaxe, Cutter, or Drill from the paused Inventory loadout panel, then face a compatible tree, stone, metal, or industrial obstacle and press `E` repeatedly. Tools take durability from physical strikes, remain as repairable broken cargo at zero durability, and repair at Repair Kiosks. Material drops stay on the ground until walked over.

Campaign construction is a live stance rather than a paused editor: carry physical materials, press `C`, move into position, face one cell, and press `E` to place the selected wall, floor, barricade, or device. Each successful placement is one ordinary turn, so threats and environmental systems respond between pieces. `R`/`X` only choose the active recipe and cost no turn.

Legacy/run-based mode retains its compatibility controls: diagonal held WASD movement, arrow-key firing, `G` for nearby corpse salvage, the generic ammo wallet, and the installed-body ability list.

## Reference

Name-wise, `Roag` pays homage to [Rogue (1980)](https://en.wikipedia.org/wiki/Rogue_(video_game)). Duh. 

In its gameplay, `Roag` takes heavy inspiration from [Bob Nystrom](https://github.com/munificent)'s [Hauberk](https://github.com/munificent/hauberk).

![](./asset/logo/hauberk.png)
