#!/usr/bin/env python3
"""Capture original AI.choose decisions without replacing navigation or attack recipes."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local AI=require('src.ai')
function capture_enemy(input)
    local rows={};for y=1,17 do rows[y]=string.rep('.',17)end
    for _,cell in ipairs(input.walls)do local x,y=9+cell[1],9+cell[2];rows[y]=rows[y]:sub(1,x-1)..'#'..rows[y]:sub(x+1)end
    for _,cell in ipairs(input.mist)do local x,y=9+cell[1],9+cell[2];rows[y]=rows[y]:sub(1,x-1)..'m'..rows[y]:sub(x+1)end
    local p={id=1,faction='player',hp=30,max_hp=32,blocks_movement=true,position={floor_id=1,x=9+input.dx,y=9+input.dy}}
    local e={id=2,definition_id=input.id,blocks_movement=true,position={floor_id=1,x=9,y=9},
        ai_memory={attacks=input.attacks},phase_two=input.phase_two}
    local state={clock=0,active_floor_id=1,player_id=1,entities={p,e},
        generated_map={width=17,height=17,floor_id=1,rows=rows},class_special={}}
    for i,cell in ipairs(input.bodies)do state.entities[#state.entities+1]={id=i+2,blocks_movement=true,position={floor_id=1,x=9+cell[1],y=9+cell[2]}}end
    if input.decoy then state.class_special.decoy={floor_id=1,x=5,y=9}end
    local intent=AI.choose(state,e)
    local cells={};for _,p in ipairs(intent.cells or {})do cells[#cells+1]={x=p.x-9,y=p.y-9}end
    local delivery=intent.ranged==true and 'projectile' or intent.ranged or 'melee'
    return {kind=intent.kind,dx=intent.dx,dy=intent.dy,ability=intent.ability_id,cells=cells,delivery=delivery}
end
""")

inventory=json.loads((ROOT/'docs/migration/original-enemy-inventory.json').read_text())
definitions=[d for d in inventory['entries'].values() if d['ai'] not in ['expansion_regular','squad','idea_regular']]
cases=[]
for definition in definitions:
    for dx,dy in [(0,-1),(1,0),(1,1),(2,0),(4,0),(3,2),(-2,-3),(0,7)]:
        for attacks,phase in [(0,False),(1,False),(2,False),(0,True)]:
            for scene in ['open','walls','mist','bodies','decoy']:
                avoid={(0,0),(dx,dy)}
                walls=[list(p) for p in [(1,-2),(1,-1),(1,1),(1,2),(3,0)] if p not in avoid] if scene=='walls' else []
                mist=[[1,0],[0,1],[-1,0],[0,-1]] if scene=='mist' else []
                bodies=[list(p) for p in [(0,-1),(1,1),(2,0),(-1,0)] if p not in avoid] if scene=='bodies' else []
                spec=dict(id=definition['id'],dx=dx,dy=dy,attacks=attacks,phase_two=phase,walls=walls,mist=mist,bodies=bodies,decoy=scene=='decoy')
                result=lua.globals().capture_enemy(table(spec))
                cases.append(dict(input=spec,expected=dict(kind=result.kind,dx=result.dx,dy=result.dy,ability=result.ability,
                    cells=array(result.cells,lambda c:dict(x=c.x,y=c.y)),delivery=result.delivery)))
destination=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-enemy-decisions.json'
destination.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original AI decisions for {len(definitions)} enemy definitions.')
