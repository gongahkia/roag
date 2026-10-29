#!/usr/bin/env python3
"""Capture original Silt Worm charge, trail lifetime and movement costs."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility');local R=require('src.rng')
function capture_silt(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/adventurer'})
 local p=G.player(s);p.position.x=15;p.position.y=16
 local map=C.map_for(s);s.generated_map={};for k,v in pairs(map)do s.generated_map[k]=v end
 local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
 s.generated_map.rows=rows;s.generated_map.placements={stairs={}};s.entities={p}
 local worm=G.spawn_enemy(s,'core:enemy/silt_worm',18,16,50);local guard
 if input.name=='blocker'then guard=G.spawn_enemy(s,'core:enemy/mossbound_guard',17,16,10000)end
 if input.name=='spent'then worm.ai_memory.silt_charges=2 end
 s.currency=0;s.rng=R.new(37);V.recompute(s);local result={}
 for i,command in ipairs(input.commands)do
  if i==4 then
   if input.name=='walk'then p.position.x,p.position.y=17,17 end
   if input.name=='dash'or input.name=='enemy'or input.name=='twice'then p.position.x,p.position.y=19,16 end
   if input.name=='enemy'then guard=G.spawn_enemy(s,'core:enemy/mossbound_guard',17,17,301)end
   if input.name=='death'then worm.hp=4 end
  end
  V.recompute(s)
  local dx,dy=1,0
  if input.name=='walk'then dx,dy=0,-1 elseif input.name=='dash'then dx,dy=-1,0 elseif input.name=='leave'then dx,dy=0,1 end
  local preview=G.preview(s,command,dx,dy);local ok,why,ev=G.submit(s,{kind=command,dx=dx,dy=dy});assert(ok,why)
  local trace={};for _,v in ipairs(ev)do
   if v.kind=='WindupStarted'or v.kind=='AttackPerformed'then trace[#trace+1]=v.kind..':'..v.time..':'..v.ability_id
   elseif v.kind=='SiltTrailCreated'then trace[#trace+1]=v.kind..':'..v.time..':'..(v.x-15)..':'..(v.y-16)
   elseif v.kind=='DamageTaken'then trace[#trace+1]='damage:'..v.time..':'..v.actual_loss end end
  local trails={};for _,r in ipairs(s.idea_silt or{})do trails[#trails+1]={x=r.x-15,y=r.y-16,expires=r.expires}end
  local ready,impact,guard_ready;for _,w in ipairs(s.schedule)do
   if w.actor_id==worm.id then if w.kind=='actor_ready'then ready=w.due elseif w.kind=='impact'then impact=w.due end end
   if guard and w.actor_id==guard.id and w.kind=='actor_ready'then guard_ready=w.due end end
  result[#result+1]={trace=trace,cost=preview.cost,clock=s.clock,hp=p.hp,x=p.position.x-15,y=p.position.y-16,
   enemy_x=worm.position.x-15,enemy_y=worm.position.y-16,random=s.rng.state,enemy_hp=worm.hp,
   charges=worm.ai_memory.silt_charges or 0,ready=ready,impact=impact,trails=trails,guard_hp=guard and guard.hp,guard_ready=guard_ready}
 end
 return result
end
""")
cases=[]
for name,commands in [('expiry',['wait']*6),('leave',['wait','move','wait']),('blocker',['wait']*3),
 ('walk',['wait']*3+['move','wait']),('dash',['wait']*3+['dash','wait']),('enemy',['wait']*4),
 ('twice',['wait']*9),('death',['wait']*3+['basic_attack','wait','wait']),('spent',['wait']*4)]:
 spec=dict(name=name,commands=commands); result=lua.globals().capture_silt(table(spec))
 cases.append(dict(input=spec,expected=array(result,lambda s:dict(trace=array(s.trace),cost=s.cost,clock=s.clock,hp=s.hp,x=s.x,y=s.y,
  enemy_x=s.enemy_x,enemy_y=s.enemy_y,random=s.random,enemy_hp=s.enemy_hp,charges=s.charges,ready=s.ready,impact=s.impact,
  trails=array(s.trails,lambda r:dict(x=r.x,y=r.y,expires=r.expires)),guard_hp=s.guard_hp,guard_ready=s.guard_ready))))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-silt-turns.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original Silt Worm scenarios / {sum(len(c["expected"]) for c in cases)} decisions.')
