#!/usr/bin/env python3
"""Capture effect target selection directly from the pinned original game function."""
import copy
import json
import random

from lupa.luajit21 import LuaRuntime
from inventory_boons import ROOT, REVISION, SOURCES, original_source

lua = LuaRuntime(unpack_returned_tuples=True)
lua.globals().original_source = original_source
lua.execute("""
require('bit') -- Original game tooling uses LuaJIT's built-in bit module.
package.path='';package.cpath=''
package.loaders={function(name) return assert(loadstring(original_source(name),'@original/'..name)) end}
local G=require('src.game')
local C=require('src.content')
local function find(fn,wanted,seen)
    if seen[fn] then return end; seen[fn]=true
    for i=1,200 do
        local name,value=debug.getupvalue(fn,i)
        if not name then break end
        if name==wanted then return value end
        if type(value)=='function' then local found=find(value,wanted,seen);if found then return found end end
    end
end
local select_cells
for _,fn in pairs(G) do
    if type(fn)=='function' then select_cells=find(fn,'effect_cells',{});if select_cells then break end end
end
assert(select_cells)
local function pos(p) return p and {x=p.x,y=p.y,floor_id=p.floor} end
function select_effect(input)
    local state={entities={}}
    for _,a in ipairs(input.actors) do
        state.entities[#state.entities+1]={id=a.id,hp=a.hp,faction=a.faction,position=pos(a.position),
            statuses=a.burning and {burning={}} or {}}
    end
    local e={}
    for key,value in pairs(input.event) do e[key]=value end
    if e.position then e.x=e.position.x;e.y=e.position.y;e.floor_id=e.position.floor end
    if e.from then e.from_x=e.from.x;e.from_y=e.from.y;e.floor_id=e.floor_id or e.from.floor end
    e.source_position=pos(e.source_position);e.pattern_origin=pos(e.pattern_origin)
    e.path={};for _,p in ipairs(input.event.path or {}) do e.path[#e.path+1]=pos(p) end
    e.target_cells={};for _,p in ipairs(input.event.target_cells or {}) do e.target_cells[#e.target_cells+1]=pos(p) end
    C.map_for=function() return {floor_id=input.floor} end
    C.projectile_passable=function(_,x,y)
        for _,p in ipairs(input.walls) do if p.x==x and p.y==y and p.floor==input.floor then return false end end
        return true
    end
    local activation={event=e,owner_id=1,source_faction='player'}
    return select_cells(state,activation,input.effect)
end
""")


def table(value):
    if isinstance(value, dict): return lua.table_from({k: table(v) for k, v in value.items()})
    if isinstance(value, list): return lua.table_from([table(v) for v in value])
    return value


def p(x, y, floor=0): return {'x': x, 'y': y, 'floor': floor}


effects = [{'kind': kind} for kind in ['damage_source', 'damage_target', 'adjacent_hostiles', 'path_adjacent_hostiles']]
effects += [{'kind': 'status_neighbors', 'status': 'burning'}]
effects += [{'kind': 'damage_nearest', 'radius': radius, **({'clear_path': True} if clear else {})}
            for radius in [0, 1, 3, 6] for clear in [False, True]]
effects += [{'kind': 'area', 'radius': radius} for radius in [0, 1, 2]]
effects += [{'kind': 'pattern', 'pattern': name} for name in ['lance', 'rook', 'bishop', 'knight', 'crown', 'crosswind', 'fan', 'backtrail']]
generator = random.Random(8113)
cases = []
for variant in range(80):
    floor = variant % 2
    x, y = 100 + variant % 3, 100 + variant % 4
    event = {'id': 1, 'chain_id': 1, 'kind': 'HitConfirmed', 'root_action_id': 1, 'source_id': 1, 'target_id': 2,
             'position': p(x, y, floor), 'from': p(99, 100, floor), 'source_position': p(100, 100, floor),
             'path': [p(99, 100, floor), p(100, 100, floor), p(101, 100, floor)]}
    if variant % 5 == 0: event['pattern_origin'] = p(98, 99, floor)
    if variant % 7 == 0: event.pop('position')
    if variant % 11 == 0: event.pop('from')
    actors = [{'id': 1, 'hp': 20, 'faction': 'player', 'position': p(100, 100, floor)}]
    for identifier in [7, 3, 5, 2, 4, 6]:
        actors.append({'id': identifier, 'hp': 0 if generator.randrange(6) == 0 else 10,
                       'faction': 'player' if generator.randrange(4) == 0 else 'enemy',
                       'position': p(98+generator.randrange(7), 98+generator.randrange(7), floor if identifier != 6 else 1-floor),
                       'burning': generator.randrange(2) == 0})
    if variant % 13 == 0: actors = actors[1:]
    walls = [p(98+generator.randrange(7), 98+generator.randrange(7), floor) for _ in range(variant % 7)]
    for effect in effects:
        if 'position' not in event and effect['kind'] in ['adjacent_hostiles', 'status_neighbors']:
            continue # These original handlers are only called by events with a target position.
        sample = {'floor': floor, 'actors': actors, 'event': event, 'walls': walls, 'effect': effect}
        cells = lua.globals().select_effect(table(sample))
        expected = [p(int(cells[i].x), int(cells[i].y), int(cells[i].floor_id)) for i in range(1, len(cells)+1)]
        cases.append({'input': copy.deepcopy(sample), 'expected': expected})
result = {'revision': REVISION, 'sources': dict(sorted(SOURCES.items())), 'cases': cases}
destination = ROOT / 'tests/Roag.Core.Tests/Tactics/Fixtures/original-boon-effect-targets.json'
destination.write_text(json.dumps(result, separators=(',', ':')) + '\n')
print(f'Captured {len(cases)} original effect targeting cases across {len(effects)} effect configurations.')
