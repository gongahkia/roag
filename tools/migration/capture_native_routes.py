#!/usr/bin/env python3
"""Capture every pinned-original biome route branch."""
import json
from export_native_dungeon_content import lua, data, biomes, ROOT, REVISION, SOURCES
cases=[]
def capture(stage,history):
    routes=data(biomes.routes_for(stage,lua.table_from(history)))
    cases.append(dict(stage=stage,history=history,routes=routes))
    if stage<3:
        for route in routes: capture(stage+1,history+[route['id']])
capture(1,[])
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-routes.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original route branches.')
