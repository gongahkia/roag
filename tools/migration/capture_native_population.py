#!/usr/bin/env python3
"""Capture original seeded idea selections and actual squad plans in a clear room."""
import json
from export_native_dungeon_content import lua, data, biomes, ROOT, REVISION, SOURCES

lua.execute("""
local I=require('src.idea_enemies');local S=require('src.enemy_squads')
function capture_population(seed,biome,floor)
 local rows={};for y=1,16 do rows[y]=string.rep('.',16)end
 local map={width=16,height=16,rows=rows,biome_id=biome,floor_id=floor,start_x=2,start_y=2,
  placements={stairs={},chests={}},elite={x=15,y=15},rooms={{id=1,role='combat',markers={enemy={{x=8,y=8}}}}}}
 local plan=S.plan({run_seed=seed,entities={}},map);assert(plan)
 local idea,derived=I.selection(seed,floor)
 return {squad=plan.kind,followers=#plan.points-1,idea=idea and 'core:enemy/'..idea[1] or false,derived=derived}
end
""")
cases=[]
for seed in [1,2,3,7,11,19,37,53,79,101,233,919]:
    for biome in data(biomes.order):
        floor=biomes.definitions[biome].floor_id+'/stage/2/depth/1'
        result=data(lua.globals().capture_population(seed,biome,floor))
        cases.append(dict(seed=seed,biome=biome,floor=floor,expected=result))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-population.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original population selections and formations.')
