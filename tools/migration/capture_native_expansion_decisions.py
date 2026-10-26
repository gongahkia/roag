#!/usr/bin/env python3
"""Capture the original ten expansion controllers, including support/advance metadata."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local AI=require('src.ai')
function capture_expansion(input)
 local rows={};for y=1,17 do rows[y]=string.rep('.',17)end
 for _,kind in ipairs({'walls','mist','barricades'})do
  local symbol=kind=='walls' and '#' or kind=='mist' and 'm' or 'B'
  for _,c in ipairs(input[kind])do local x,y=9+c[1],9+c[2];rows[y]=rows[y]:sub(1,x-1)..symbol..rows[y]:sub(x+1)end
 end
 local p={id=1,faction='player',controller='player',hp=30,max_hp=32,blocks_movement=true,position={floor_id=1,x=9+input.dx,y=9+input.dy}}
 local e={id=2,definition_id=input.id,faction='enemy',controller='ai',hp=10,max_hp=10,blocks_movement=true,position={floor_id=1,x=9,y=9},
  ai_memory={alerted=input.alerted,retaliate=input.retaliate,summons=input.summons}}
 local state={clock=0,active_floor_id=1,player_id=1,entities={p,e},generated_map={width=17,height=17,floor_id=1,rows=rows},class_special={}}
 for _,n in ipairs(input.neighbors)do state.entities[#state.entities+1]={id=n.id,hp=n.hp,max_hp=n.max_hp,controller=n.ally and 'ai' or 'player',faction=n.ally and 'enemy' or 'player',blocks_movement=true,position={floor_id=1,x=9+n.dx,y=9+n.dy}}end
 if input.decoy then state.class_special.decoy={floor_id=1,x=5,y=9}end
 local intent=AI.choose(state,e)
 local cells={};for _,p in ipairs(intent.cells or {})do cells[#cells+1]={x=p.x-9,y=p.y-9}end
 return {kind=intent.kind,dx=intent.dx,dy=intent.dy,ability=intent.ability_id,cells=cells,
  delivery=intent.ranged==true and 'projectile' or intent.ranged or 'melee',bonus=intent.damage_bonus or 0,
  alert=intent.alert or false,clear_retaliation=intent.clear_retaliate or false,support=intent.support,advance=intent.advance}
end
""")
inventory=json.loads((ROOT/'docs/migration/original-enemy-inventory.json').read_text())
definitions=[d for d in inventory['entries'].values() if d['ai']=='expansion_regular']
cases=[]
for d in definitions:
 for dx,dy in [(0,-1),(1,0),(1,1),(2,0),(4,0),(3,2),(-2,-3),(0,7)]:
  for alerted,retaliate,summons in [(False,False,0),(True,False,0),(False,True,0),(True,True,0),(False,False,2),(False,False,3)]:
   for scene in ['open','walls','mist','barricades','allies','wounded','bodies','decoy']:
    avoid={(0,0),(dx,dy)}
    points=[p for p in [(0,-1),(1,0),(1,1),(-1,0)] if p not in avoid]
    neighbors=[dict(id=3+i,dx=x,dy=y,hp=(2+i if scene=='wounded' else 10),max_hp=(10+i*5 if scene=='wounded' else 10),ally=scene!='bodies') for i,(x,y) in enumerate(points)] if scene in ['allies','wounded','bodies'] else []
    spec=dict(id=d['id'],dx=dx,dy=dy,alerted=alerted,retaliate=retaliate,summons=summons,
      walls=[list(p) for p in [(1,-2),(1,-1),(1,1),(1,2),(3,0)] if p not in avoid] if scene=='walls' else [],
      mist=[[1,0],[0,1],[-1,0],[0,-1]] if scene=='mist' else [],barricades=[list(p) for p in points] if scene=='barricades' else [],neighbors=neighbors,decoy=scene=='decoy')
    r=lua.globals().capture_expansion(table(spec))
    support=None if r.support is None else dict(kind=r.support.kind,power=r.support.power,definition=r.support.enemy_id,maximum_active=r.support.max_active)
    advance=None if r.advance is None else dict(dx=r.advance.dx,dy=r.advance.dy,steps=r.advance.max_steps)
    cases.append(dict(input=spec,expected=dict(kind=r.kind,dx=r.dx,dy=r.dy,ability=r.ability,cells=array(r.cells,lambda c:dict(x=c.x,y=c.y)),delivery=r.delivery,
      bonus=r.bonus,alert=r.alert,clear_retaliation=r.clear_retaliation,support=support,advance=advance)))
p=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-expansion-decisions.json'
p.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original expansion decisions for {len(definitions)} definitions.')
