# Room content and compilation

ROAG registers fifteen approved room families with seven biome variants each:
**105 distinct layouts**. Original rooms and the authored final sanctum remain.
Generation selects rooms, rotations, mirrors and optional structure slots by
seed. `src/corridor_branches.lua` supplies five compact optional side-branch
topologies without cutting off the required route.

## Edit and verify

- `tools/room_prototypes.py`: approved family geometry and biome variants.
- `tools/room_data.py` / `tools/export_rooms.lua`: validate live base-room data.
- `tools/write_room_expansion.py`: compile plain Lua into `src/room_expansion.lua`.
- `src/rooms.lua`: register definitions in the appropriate biome role pools.

```sh
make room-expansion-check           # read-only freshness check
make room-expansion                 # deliberately rewrite derived Lua
python3 -m tests.room_prototype_tests
TMPDIR=/var/tmp luajit tests/run.lua
make environment-preview-check
```

The prototype check validates all 105 definitions and 700 biome/seed floor
pairs, including connectivity, template sampling and absence of fallback/repair.
The game itself requires neither Python nor a browser. Saved floors retain
their stored geometry; visual refresh never regenerates the current floor.

The [approved diagram archive](room-review/review.html) preserves the user's
review and historical sprite comparisons. It is no longer compiler input.
The [current environment gallery](visuals/environments/index.html) shows exact
source-pixel composites for the fixed Kenney 1-Bit look.
