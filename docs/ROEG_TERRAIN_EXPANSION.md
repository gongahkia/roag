# Connected-floor terrain (expansion M2)

The six new terrain symbols are gameplay cells on the existing logical grid.
`src/terrain.lua` owns their semantic flags and event reactions. Art, including
Studio tile images, remains presentation-only. Old floor (`.`), wall (`#`) and
shallow water (`~`) behavior is unchanged.

| Symbol | Terrain | Rules |
|---|---|---|
| `b` | Bramble bed | Entering deals 1 physical-equivalent environmental damage through the canonical damage/death path, at most once per actor action. Defense can block it. Waiting does not retrigger it. |
| `g` | Sticky bog | Adds 40 base simulated-time units to a walk and 20 per traversed bog cell to Dash, scaled by actor speed through normal recovery calculation. |
| `i` | Glazed ice | A Dash that crosses ice in its first two legal cells may continue to a third legal cardinal cell. Ordinary walking never slides automatically. The full path is previewed and animated. |
| `B` | Breakable barricade | Blocks movement, sight and projectiles until an attack targets the cell. The attack clears it, updates fog/navigation, and emits `TerrainBroken`. Broken state is plain data in the floor snapshot. |
| `m` | Obscuring mist | Walkable and projectile-passable, but blocks player vision and enemy ranged sight decisions. Blind projectiles can still pass. |
| `c` | Resonant crystal | A positive lightning hit adjacent to a crystal arcs 1 damage to another hostile adjacent to that crystal. It chooses the nearest target, then lowest entity ID. The arc uses canonical damage, credit and rewards; its distinct damage type prevents self-recursion. |

Numbers are provisional. Paired-floor generation places one each of `b/g/i` on
the upper floor and `B/m/c` on the lower floor. A dedicated RNG stream derived
from the map seed and floor ID chooses safe editable cells. It reserves starts,
elites, enemies, chests, doors and secrets; barricades are excluded from
critical corridor cells. The resulting map is revalidated before use. The
existing single-floor fixtures and stored pre-M2 connected-floor maps contain
none of the new symbols and remain loadable.

In Studio World Editor, press **A** to add the six semantic tile entries to an
existing or new E03 project. **Tab** selects a tile; **5** chooses tile paint;
click/drag paints cells. **Z** changes the selected tile's semantic type and
updates its movement, sight and projectile flags. Assign an E01-created tile
image with the existing **I** binding workflow. The original three-entry E03
tile catalogue remains unchanged until **A** is chosen, so old projects reopen
without a forced source-format migration. Validate with **V** and preview
generated content with **G**. Published authored B cells may be broken in play;
their original image binding is cleared on break without changing source art.

Verification commands:

```sh
TMPDIR=/var/tmp luajit tests/run.lua
TMPDIR=/var/tmp sh tests/expansion_terrain_gui_smoke.sh
TMPDIR=/var/tmp sh tests/expansion_terrain_studio_gui_smoke.sh
TMPDIR=/var/tmp sh tests/expansion_floor_disk_resume.sh
```

The GUI smokes use callback-driven LÖVE in isolated save directories. They do
not represent a human keyboard or pen test. The terrain source definitions are
versioned independently as `roeg-terrain/1`; active-run schema stays
`roeg-run/11` because old concrete maps are preserved and new fields are
optional on restore.
