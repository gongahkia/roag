#!/usr/bin/env python3
"""Pinned original public commands for death-batch fragmentation and fragment combat."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility');local R=require('src.rng')
function capture_bone(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/adventurer'})
 local p=G.player(s);p.position.x=15;p.position.y=16
 local map=C.map_for(s);s.generated_map={};for k,v in pairs(map)do s.generated_map[k]=v end
 local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
 s.generated_map.rows=rows;s.generated_map.placements={stairs={}};s.entities={p}
 local e=G.spawn_enemy(s,'core:enemy/bone_splitter',16,16,10000);e.hp=3
 if input.name=='batch'then local ally=G.spawn_enemy(s,'core:enemy/mossbound_guard',16,15,10000);ally.hp=2 end
 if input.name=='cramped'then
  s.generated_map.placements.stairs={{x=16,y=15,to_floor_id='other',to_x=2,to_y=2}}
  rows[16]=rows[16]:sub(1,16)..'#'..rows[16]:sub(18)
  G.spawn_enemy(s,'core:enemy/mossbound_guard',16,17,10000)
 end
 if input.name=='exempt'then e.reward_exempt=true end
 s.currency=0;s.rng=R.new(37);V.recompute(s);local result={}
 for _,command in ipairs(input.commands)do
  local ok,why,ev=G.submit(s,{kind=command,dx=1,dy=0});assert(ok,why)
  local trace={}
  for _,v in ipairs(ev)do
   if v.kind=='WindupStarted'or v.kind=='AttackPerformed'then trace[#trace+1]=v.kind..':'..v.time..':'..v.ability_id
   elseif v.kind=='BoneFragmented'then trace[#trace+1]='fragment:'..v.time..':'..(v.x-15)..':'..(v.y-16)
   elseif v.kind=='DamageTaken'then trace[#trace+1]='damage:'..v.time..':'..v.actual_loss
   elseif v.kind=='ExperienceGained'or v.kind=='CurrencyDropped'then trace[#trace+1]=v.kind..':'..v.time..':'..v.amount end
  end
  local fragments={};for _,f in ipairs(s.entities)do if f.fragment_of then
   local ready,impact;for _,w in ipairs(s.schedule)do if w.actor_id==f.id then
    if w.kind=='actor_ready'then ready=w.due elseif w.kind=='impact'then impact=w.due end end end
   fragments[#fragments+1]={hp=f.hp,x=f.position.x-15,y=f.position.y-16,ready=ready,impact=impact}end end
  result[#result+1]={trace=trace,clock=s.clock,hp=p.hp,random=s.rng.state,xp=s.progression.xp_total,drops=#s.drops,fragments=fragments}
 end
 return result
end
""")
cases=[]
for name in ['ordinary','batch','cramped','exempt']:
 spec=dict(name=name,commands=['sweeping_slash'if name=='batch'else'basic_attack','wait','sweeping_slash','wait'])
 result=lua.globals().capture_bone(table(spec))
 cases.append(dict(input=spec,expected=array(result,lambda s:dict(trace=array(s.trace),clock=s.clock,hp=s.hp,random=s.random,xp=s.xp,drops=s.drops,
  fragments=array(s.fragments,lambda f:dict(hp=f.hp,x=f.x,y=f.y,ready=f.ready,impact=f.impact))))))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-bone-turns.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original Bone Splitter scenarios / {sum(len(c["expected"]) for c in cases)} decisions.')
