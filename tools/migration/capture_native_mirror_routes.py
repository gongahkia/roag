#!/usr/bin/env python3
"""Capture the original single-reflector geometry independently of world commitment."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local M=require('src.idea_mirrors')
function capture_mirror_route(input)
 local s=G.new(37,'core:map/scrolling_room');local p=G.player(s);p.position.x,p.position.y=15,12
 local map=C.map_for(s);s.generated_map={};for k,v in pairs(map)do s.generated_map[k]=v end
 local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
 s.generated_map.rows=rows;s.generated_map.placements={stairs={}};s.entities={p}
 local dx,dy=input.dx,input.dy;local mx,my=15+dx*input.step,12+dy*input.step
 local owner=G.spawn_enemy(s,M.id,mx+dx,my+dy,10000)
 if input.mode=='dead'then owner.hp=0 end
 local facing=input.mode=='wrong'and -1 or 1
 s.idea_mirrors={{id=1,source_id=owner.id,floor_id=input.mode=='floor'and 'elsewhere'or s.active_floor_id,
  x=mx,y=my,in_dx=dx*facing,in_dy=dy*facing,expires=input.mode=='expired'and 0 or 400}}
 local b=input.mode=='body_before'and 1 or input.mode=='body_marker'and input.step
 if b then G.spawn_enemy(s,'core:enemy/mossbound_guard',15+dx*b,12+dy*b,10000)end
 if input.mode=='wall'then local x,y=15+dx,12+dy;rows[y]=rows[y]:sub(1,x-1)..'#'..rows[y]:sub(x+1)end
 local cells={};for i=1,input.length do cells[#cells+1]={floor_id=s.active_floor_id,x=15+dx*i,y=12+dy*i}end
 local ranged=input.delivery=='projectile'and true or input.delivery=='piercing'and 'piercing_line'or input.delivery=='area'and 'area'or false
 local routed,reflection=M.route(s,p,cells,ranged)
 return {cells=routed,step=reflection and reflection.step}
end
""")
cases=[]
for dx,dy in [(0,-1),(1,0),(0,1),(-1,0)]:
 for length in [2,4,6]:
  for step in range(1,length+1):
   for delivery in ['projectile','piercing','area','melee']:
    for mode in ['open','wall','body_before','body_marker','dead','wrong','floor','expired']:
     spec=dict(dx=dx,dy=dy,length=length,step=step,delivery=delivery,mode=mode)
     result=lua.globals().capture_mirror_route(table(spec))
     cases.append(dict(input=spec,expected=dict(cells=array(result.cells,lambda c:dict(x=c.x-15,y=c.y-12)),step=result.step)))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-mirror-routes.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original mirror routes.')
