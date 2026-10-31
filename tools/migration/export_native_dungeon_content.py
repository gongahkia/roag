#!/usr/bin/env python3
"""Export pinned original authored dungeon data; generation remains handwritten C#."""
import json
from capture_native_boon_chains import lua, array
from inventory_boons import ROOT, REVISION, SOURCES
from lupa.luajit21 import lua_type

def data(value):
    if lua_type(value) == "table":
        keys = list(value.keys())
        if set(keys) == set(range(1, len(keys) + 1)):
            return [data(value[i]) for i in range(1, len(keys) + 1)]
        return {str(key): data(value[key]) for key in sorted(keys)}
    if value is None or isinstance(value, (str, int, float, bool)):
        return value
    raise TypeError(f"Non-data catalog entry: {lua_type(value)}")

rooms=lua.eval("require('src.rooms')")
biomes=lua.eval("require('src.biomes')")
finale=lua.eval("require('src.finale')")
def main():
    room_data=data(rooms.definitions)
    biome_data=data(biomes.definitions)
    result=dict(schema_version=1,source_revision=REVISION,
        rooms=[room_data[key] for key in sorted(room_data)],
        room_order=data(rooms.order),biomes=[biome_data[key] for key in data(biomes.order)],
        generation=data(rooms.biome),finale=data(finale.map),sources=dict(sorted(SOURCES.items())))
    path=ROOT/'assets/tactics/dungeons.json'
    path.write_text(json.dumps(result,sort_keys=True,indent=2)+'\n')
    print(f'Exported {len(room_data)} authored rooms, {len(biome_data)} biomes and the final sanctum.')
    print('Room fields:',sorted(set().union(*(set(v) for v in room_data.values()))))
    print('Room roles:',sorted(set().union(*(set(v['roles']) for v in room_data.values()))))

if __name__ == "__main__":
    main()
