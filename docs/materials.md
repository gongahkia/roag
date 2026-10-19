# Ores, water, and lava

**1–9** select a hotbar slot. **Tab** switches through the five palette pages. Left click removes the targeted block; right click places the selected material. Placing into an existing fluid cell replaces that cell, so you can remove sources or plug a flow with a solid block.

| Page | Slots |
|---|---|
| 1 | 1 Grass, 2 Dirt, 3 Stone, 4 Sand, 5 Wood, 6 Leaves, **7 Water**, **8 Lava**, 9 Obsidian |
| 2 | **1 Coal ore, 2 Iron ore, 3 Copper ore, 4 Gold ore, 5 Diamond ore, 6 Redstone ore**, 7 Cobblestone, 8 Snow, 9 Ice |
| 3 | 1 Cactus, 2 Terracotta, 3 Red terracotta, 4 Yellow terracotta, 5 White terracotta, 6 Birch wood, 7 Birch leaves, 8 Spruce wood, 9 Spruce leaves |
| 4 | 1 Acacia wood, 2 Acacia leaves, 3 Jungle wood, 4 Jungle leaves, 5 Cherry wood, 6 Cherry leaves, 7 Mushroom stem, 8 Red mushroom cap, 9 Brown mushroom cap |
| 5 | 1 Mycelium, 2 Mud, 3 Podzol, 4 Dry grass, 5 Lush grass, 6 Swamp grass, **7 Lamp** |

Water and lava placements create persistent sources. They fall into open cells first, then spread across supported ground. Water spreads up to seven cells from a feed; lava spreads up to three and updates more slowly. Opening a channel redirects flow, placing solids blocks it, and removing a lone source lets its downstream flow drain. A supported gap between two water sources regenerates a water source. **Water + lava source → obsidian; water + flowing lava → cobblestone.**

Water has transparent, stepped surfaces. Lava uses animated emissive textures and casts warm voxel light onto its surroundings. The placeable **Lamp** on page 5, slot 7 also emits light. See [lighting](lighting.md) for skylight, shadows, and update behavior. Fluid camera immersion adds tint and distance fog. Walking is the default, with solid block collisions, gravity, jumping, and sprinting. Fluids slow movement and allow swimming with Space/Ctrl. **F4** toggles spectator flight with noclip. Solid blocks cannot be placed inside the walking player's body. There is no health/damage system in this creative tranche.

New worlds generate lowland lakes at level 52, underground lava in caves at heights 6–12, and clustered ores within stone:

| Ore | Height range |
|---|---|
| Coal | 1–100 |
| Iron | 1–64 |
| Copper | 24–80 |
| Gold | 1–40 |
| Diamond | 1–24 |
| Redstone | 1–32 |

Generator 5 adds biome-specific landforms and surface drainage; see [geography](geography.md). Generator 4 adds climate biomes, snow and solid translucent ice, cacti, terracotta strata, species-specific trees, and giant mushrooms. Snow and plants are full voxel blocks. Freezing is static during generation; melting, weather, thin snow layers, slipperiness, and plant damage are not implemented.

Generator 3 also adds contained underground lake chambers, varied surface biomes, and floating islands above normal ground. See [terrain generation](terrain.md) for details and new-world controls.

Existing saves retain their original terrain. Every new material and fluid still works when placed there. To explore newly generated resources without replacing your existing world, use a new save path:

```sh
python3 scripts/build.py run -- --save saves/resources.vox
```

Fluid simulation runs locally around the player with a bounded queue and per-frame work allowance. Distant flows resume when revisited. Large or saturated simulations may take additional ticks to settle. The full F3 panel shows queue size, update time, and refused updates if the world-edit limit is reached. The [architecture](architecture.md) describes simulation and transparency limits.

For the repeatable waterfall, lavafall, reaction, and ore-wall scene:

```sh
python3 scripts/benchmark.py --scene fluids --seconds 10 --radius 2
```

Unit tests cover draining, gravity, source regeneration, lava timing, reactions, chunk boundaries, queue bounds, greedy fluid meshes, generator consistency, and old/new saves. `python3 scripts/smoke.py` also exercises ore and water placement through actual inputs and runs a graphical fluid scene under a 16 MiB graphics allowance and 32 MiB managed CPU budget. These allowances describe application accounting, not total driver VRAM or process RAM.
