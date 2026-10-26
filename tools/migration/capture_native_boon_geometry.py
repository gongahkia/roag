#!/usr/bin/env python3
"""Capture original pattern/weapon geometry using controlled world collision inputs."""
import json
import random

from lupa import LuaRuntime
from inventory_boons import ROOT, REVISION, SOURCES, original_source

lua = LuaRuntime(unpack_returned_tuples=True)
lua.globals().original_source = original_source
lua.execute("""
package.path=''; package.cpath=''
package.searchers={function(name) return assert(load(original_source(name),'@original/'..name)) end}
local P=require('src.boon_patterns')
local C=require('src.content')
local Classes=require('src.classes')
local V=require('src.visibility')
local Boons=require('src.boons')
local function setup(walls, body, hidden)
    local state={entities={}, walls=walls, hidden=hidden, active_floor_id='world'}
    C.map_for=function() return {floor_id='world'} end
    C.projectile_passable=function(_,x,y)
        for _,p in ipairs(walls) do if p.x==x and p.y==y then return false end end
        return true
    end
    C.walkable=C.projectile_passable
    V.is_visible=function(_,_,x,y) return not (hidden and body and x==body.x and y==body.y) end
    if body then state.entities[1]={id=2,hp=10,position=body} end
    return state
end
function pattern(name,dx,dy,walls,has_from)
    local state=setup(walls)
    local e={floor_id='world',x=1000,y=2000,source_position={floor_id='world',x=1000-dx,y=2000-dy}}
    if has_from then e.from_x=997;e.from_y=2002 end
    return P.cells(state,name,e)
end
function weapon(form,dx,dy,walls,explicit,known,body_step,hidden,compose)
    local info=P.weapon_forms[form]
    local owner={id=1,definition_id=info.class_id,position={floor_id='world',x=1000,y=2000}}
    local body=body_step>0 and {floor_id='world',x=1000+dx*body_step,y=2000+dy*body_step} or nil
    local state=setup(walls,body,hidden);state.entities[#state.entities+1]=owner
    local definition=Classes.get(info.class_id)
    local kind=definition.basic_id==info.ability_id and 'basic_attack' or 'sweeping_slash'
    local base,_,ranged=Classes.attack(state,owner,kind,dx,dy,C,explicit)
    local current={}
    for i,p in ipairs(base) do
        if not compose or i<=2 then current[#current+1]={floor_id=p.floor_id,x=p.x,y=p.y} end
    end
    if compose then
        current[#current+1]={floor_id='world',x=1000-dx,y=2000-dy}
        current[#current+1]={floor_id='world',x=1000+2*dx+dy,y=2000+2*dy-dx}
    end
    local result,mode,hint=P.weapon_cells(state,form,owner,current,base,dx,dy,ranged,explicit,known)
    return {base=base,current=current,input_mode=ranged or false,cells=result,mode=mode or false,hint=hint~=nil}
end
function ability(class_id,kind,dx,dy,walls,ids,bodies,explicit,known)
    local owner={id=1,definition_id=class_id,position={floor_id='world',x=1000,y=2000},boons={}}
    local state=setup(walls);state.entities={owner}
    for _,id in ipairs(ids) do owner.boons[id]={common=1,rare=1} end
    for _,b in ipairs(bodies) do
        state.entities[#state.entities+1]={id=b.id,hp=10,faction=b.hostile and 'enemy' or 'player',
            position={floor_id='world',x=b.x,y=b.y},statuses=b.burning and {burning={}} or {}}
    end
    owner.faction='player'
    V.is_visible=function(_,_,x,y)
        for _,b in ipairs(bodies) do if b.x==x and b.y==y then return b.visible end end
        return true
    end
    local base,id,ranged=Classes.attack(state,owner,kind,dx,dy,C,explicit)
    local definition=Classes.get(class_id)
    local damage=kind=='basic_attack' and definition.damage or definition.slash_damage
    local result,power,mode,hint=Boons.ability(state,owner,id,base,damage,dx,dy,explicit,ranged,known)
    return {base=base,base_damage=damage,id=id,input_mode=ranged or false,cells=result,damage=power,mode=mode or false,hint=hint~=nil}
end
""")


def cells(table):
    return [[int(table[i].x), int(table[i].y)] for i in range(1, len(table) + 1)]


def obstacles(points):
    return lua.table_from([lua.table_from({"x": x, "y": y}) for x, y in points])


directions = [(0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1)]
patterns = []
generator = random.Random(1739)
walls = [[]] + [[[1000 + generator.randrange(-5, 6), 2000 + generator.randrange(-5, 6)] for _ in range(12)] for _ in range(15)]
for name in ["lance", "rook", "bishop", "knight", "crown", "crosswind", "fan", "backtrail"]:
    for dx, dy in directions + [(0, 0), (2, 1)]:
        for points in walls:
            for has_from in ([False, True] if name == "backtrail" else [True]):
                result = lua.globals().pattern(name, dx, dy, obstacles(points), has_from)
                patterns.append({"pattern": name, "source": [1000-dx, 2000-dy], "hasFrom": has_from,
                                 "walls": points, "cells": cells(result)})
weapons = []
for form in ["pommel_return", "needle_flight", "hollow_sigil", "backswing", "distant_bastion", "volatile_vial", "split_thrust"]:
    for facing, (dx, dy) in enumerate(directions):
        blockers = [[], [[1000+dx, 2000+dy]], [[1000+2*dx, 2000+2*dy]],
                    [[1000+dx, 2000], [1000, 2000+dy]], [[1000-dx, 2000-dy]], [[1000+2*dx+dy, 2000+2*dy-dx]]]
        for points in blockers:
            for explicit, known, body_step, hidden, compose in [
                    (True, False, 0, False, False), (False, False, 0, False, False),
                    (True, False, 2, False, False), (True, True, 2, True, False),
                    (True, False, 2, True, False), (True, False, 2, False, True)]:
                result = lua.globals().weapon(form, dx, dy, obstacles(points), explicit, known, body_step, hidden, compose)
                weapons.append({"form": form, "facing": facing, "walls": points, "explicit": explicit,
                                "knownOnly": known, "bodyStep": body_step, "hidden": hidden,
                                "base": cells(result.base), "current": cells(result.current),
                                "inputMode": result.input_mode, "mode": result.mode, "hint": result.hint,
                                "cells": cells(result.cells)})
abilities = []
inventory = json.loads((ROOT / "docs/migration/original-boon-inventory.json").read_text())
for class_name in ["adventurer", "ranger", "arcanist", "berserker", "sentinel", "alchemist", "duelist"]:
    class_id = "core:actor/" + class_name
    ids = [d['id'] for d in inventory['definitions'] if d.get('modifiers')
           and (not d.get('eligible_classes') or class_id in d['eligible_classes'])]
    for loadout in [[identifier] for identifier in ids] + [ids]:
        for action in ["basic_attack", "sweeping_slash"]:
            for facing, (dx, dy) in enumerate(directions):
                for variant in range(4):
                    points = [[], [[1000+dx, 2000+dy]], [[1000+2*dx, 2000+2*dy]], [[1000+2*dx+dy, 2000+2*dy]]][variant]
                    bodies = [{"id": 20, "x": 1000+2*dx, "y": 2000+2*dy, "hostile": True, "burning": variant != 3, "visible": variant != 2},
                              {"id": 5, "x": 1000+2*dx-dy, "y": 2000+2*dy+dx, "hostile": True, "burning": True, "visible": True},
                              {"id": 8, "x": 1000+3*dx, "y": 2000+3*dy, "hostile": True, "burning": True, "visible": True},
                              {"id": 3, "x": 1000+2*dx+dy, "y": 2000+2*dy-dx, "hostile": variant != 1, "burning": False, "visible": True}]
                    explicit, known = variant != 1, variant == 2
                    result = lua.globals().ability(class_id, action, dx, dy, obstacles(points), lua.table_from(loadout),
                                                   lua.table_from([lua.table_from(b) for b in bodies]), explicit, known)
                    abilities.append({"class": class_id, "facing": facing, "walls": points, "boons": loadout, "bodies": bodies,
                                      "explicit": explicit, "knownOnly": known, "base": cells(result.base), "baseDamage": result.base_damage,
                                      "ability": result.id, "inputMode": result.input_mode, "cells": cells(result.cells),
                                      "damage": result.damage, "mode": result.mode, "hint": result.hint})
result = {"revision": REVISION, "sources": dict(sorted(SOURCES.items())), "patterns": patterns, "weapons": weapons, "abilities": abilities}
destination = ROOT / "tests/Roag.Core.Tests/Tactics/Fixtures/original-boon-geometry.json"
destination.write_text(json.dumps(result, separators=(",", ":")) + "\n")
print(f"Captured {len(patterns)} pattern cases, {len(weapons)} weapon-form cases and {len(abilities)} composed ability cases.")
