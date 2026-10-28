#!/usr/bin/env python3
"""Pinned Wraith commands for physical phase rules; offscreen adaptation is tested natively."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility');local R=require('src.rng')
function capture_wraith(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/'..input.class})
 local p=G.player(s);p.position.x=15;p.position.y=16
 local map=C.map_for(s);s.generated_map={};for k,v in pairs(map)do s.generated_map[k]=v end
 local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
 rows[16]=rows[16]:sub(1,12)..'#'..rows[16]:sub(14)
 s.generated_map.rows=rows;s.generated_map.placements={stairs={}};s.entities={p}
 if input.name=='stairs'then s.generated_map.placements.stairs={{x=14,y=16,to_floor_id='other',to_x=2,to_y=2}}end
 local e=G.spawn_enemy(s,'core:enemy/hollow_wraith',12,16,0)
 if input.name=='trap'then s.class_special.snare={floor_id=s.active_floor_id,x=14,y=16}end
 s.rng=R.new(37);V.recompute(s);local result={}
 for i,command in ipairs(input.commands)do
  if i==2 and input.name=='removed-wall'then local row=s.generated_map.rows[16];s.generated_map.rows[16]=row:sub(1,12)..'.'..row:sub(14)end
  local ok,why,ev=G.submit(s,{kind=command,dx=-1,dy=0});assert(ok,why)
  local trace={}
  for _,v in ipairs(ev)do
   if v.kind=='WindupStarted'or v.kind=='SupportResolved'or v.kind=='AttackPerformed'then trace[#trace+1]=v.kind..':'..v.time..':'..v.ability_id
   elseif v.kind=='DamageTaken'then trace[#trace+1]='damage:'..v.time..':'..(v.target_id==p.id and'hero'or'enemy')..':'..v.actual_loss
   elseif v.kind=='Displaced'then trace[#trace+1]='phase:'..v.time..':'..(v.x-15)..':'..(v.y-16)
   elseif v.kind=='PhaseBlocked'or v.kind=='SnareTriggered'or v.kind=='SnareHeld'then trace[#trace+1]=v.kind..':'..v.time end
  end
  result[#result+1]={trace=trace,clock=s.clock,hp=p.hp,enemy_hp=e.hp,x=p.position.x-15,y=p.position.y-16,
   enemy_x=e.position.x-15,enemy_y=e.position.y-16,random=s.rng.state}
 end
 return result
end
""")
cases=[]
for name in ['phase','occupied','removed-wall','stairs','trap']:
 spec=dict(name=name,**{'class':'trapper'if name=='trap'else'adventurer'},commands=['wait','move'if name=='occupied'else'wait','wait','wait'])
 result=lua.globals().capture_wraith(table(spec))
 cases.append(dict(input=spec,expected=array(result,lambda s:dict(trace=array(s.trace),clock=s.clock,hp=s.hp,enemy_hp=s.enemy_hp,x=s.x,y=s.y,
  enemy_x=s.enemy_x,enemy_y=s.enemy_y,random=s.random))))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-wraith-turns.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original Wraith scenarios / {sum(len(c["expected"]) for c in cases)} decisions.')
