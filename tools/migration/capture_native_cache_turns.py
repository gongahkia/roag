#!/usr/bin/env python3
"""Pinned original Haunted Cache activation, movement and locked bite commands."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility');local R=require('src.rng')
function capture_cache(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/adventurer'})
 local p=G.player(s);p.position.x=15;p.position.y=16
 local map=C.map_for(s);s.generated_map={};for k,v in pairs(map)do s.generated_map[k]=v end
 local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
 s.generated_map.rows=rows;s.generated_map.placements={stairs={}};s.entities={p}
 local e=G.spawn_enemy(s,'core:enemy/haunted_cache',15+input.x,16,0)
 if input.name=='injured'then e.hp=e.hp-1 end
 if input.name=='alerted'then e.ai_memory.alerted=true end
 s.rng=R.new(37);V.recompute(s);local result={}
 for i,command in ipairs(input.commands)do
  if i==2 and input.name=='injured'then e.hp=e.max_hp end
  local ok,why,ev=G.submit(s,{kind=command,dx=-1,dy=0});assert(ok,why)
  local trace={};for _,v in ipairs(ev)do
   if v.kind=='WindupStarted'or v.kind=='AttackPerformed'then trace[#trace+1]=v.kind..':'..v.time..':'..v.ability_id
   elseif v.kind=='DamageTaken'then trace[#trace+1]='damage:'..v.time..':'..v.actual_loss end end
  result[#result+1]={trace=trace,clock=s.clock,hp=p.hp,enemy_hp=e.hp,x=e.position.x-15,y=e.position.y-16,
   alerted=e.ai_memory.alerted==true,random=s.rng.state}
 end
 return result
end
""")
cases=[]
for name in ['quiet','adjacent','evaded','injured','alerted']:
 spec=dict(name=name,x=1 if name in ['adjacent','evaded']else 3,commands=['wait','move'if name=='evaded'else'wait','wait','wait','wait'])
 result=lua.globals().capture_cache(table(spec))
 cases.append(dict(input=spec,expected=array(result,lambda s:dict(trace=array(s.trace),clock=s.clock,hp=s.hp,enemy_hp=s.enemy_hp,x=s.x,y=s.y,alerted=s.alerted,random=s.random))))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-cache-turns.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original Cache scenarios / {sum(len(c["expected"]) for c in cases)} decisions.')
