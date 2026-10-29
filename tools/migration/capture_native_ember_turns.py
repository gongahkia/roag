#!/usr/bin/env python3
"""Capture original Ember Moth landings, friendly fire and finite scheduled pulses."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility');local R=require('src.rng')
function capture_ember(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/adventurer'})
 local p=G.player(s);p.position.x=15;p.position.y=16
 local map=C.map_for(s);s.generated_map={};for k,v in pairs(map)do s.generated_map[k]=v end
 local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
 s.generated_map.rows=rows;s.generated_map.placements={stairs={}};s.entities={p}
 local moth=G.spawn_enemy(s,'core:enemy/ember_moth',18,16,50);local other
 s.currency=0;s.rng=R.new(37);V.recompute(s);local result={}
 for i,command in ipairs(input.commands)do
  if i==3 and input.name=='friendly'then other=G.spawn_enemy(s,'core:enemy/mossbound_guard',15,16,10000)end
  if i==4 and input.name=='death'then p.position.x,p.position.y=17,16;moth.hp=4 end
  if i==5 and input.name=='death'then p.position.x,p.position.y=15,16 end
  if i==4 and input.name=='other_moth'then moth.position.x,moth.position.y=16,16;other=G.spawn_enemy(s,'core:enemy/ember_moth',15,16,10000)end
  V.recompute(s)
  local ok,why,ev=G.submit(s,{kind=command,dx=command=='move'and 0 or 1,dy=command=='move'and 1 or 0});assert(ok,why)
  local trace={};for _,v in ipairs(ev)do
   if v.kind=='WindupStarted'or v.kind=='AttackPerformed'then trace[#trace+1]=v.kind..':'..v.time..':'..v.ability_id
   elseif v.kind=='EmberCreated'or v.kind=='MothLandingBlocked'then trace[#trace+1]=v.kind..':'..v.time..':'..(v.x-15)..':'..(v.y-16)
   elseif v.kind=='EmberPulsed'then trace[#trace+1]=v.kind..':'..v.time..':'..v.remaining
   elseif v.kind=='DamageTaken'then trace[#trace+1]='damage:'..v.time..':'..v.actual_loss end end
  local embers={};for _,r in ipairs(s.idea_embers or{})do embers[#embers+1]={x=r.x-15,y=r.y-16,remaining=r.remaining,due=r.next_due,expires=r.expires}end
  local ready,impact;for _,w in ipairs(s.schedule)do if w.actor_id==moth.id then
   if w.kind=='actor_ready'then ready=w.due elseif w.kind=='impact'then impact=w.due end end end
  result[#result+1]={trace=trace,clock=s.clock,hp=p.hp,x=p.position.x-15,y=p.position.y-16,
   enemy_x=moth.position.x-15,enemy_y=moth.position.y-16,random=s.rng.state,enemy_hp=moth.hp,
   dives=moth.ai_memory.ember_dives or 0,ready=ready,impact=impact,embers=embers,other_hp=other and other.hp}
 end
 return result
end
""")
cases=[]
for name,commands in [('pulses',['wait']*5),('dodge',['wait','move']+['wait']*3),
 ('leave',['wait']*3+['move','wait']),('death',['wait']*3+['basic_attack','wait']),
 ('friendly',['wait','move']+['wait']*3),('other_moth',['wait','move']+['wait']*3),('twice',['wait']*10)]:
 spec=dict(name=name,commands=commands); result=lua.globals().capture_ember(table(spec))
 cases.append(dict(input=spec,expected=array(result,lambda s:dict(trace=array(s.trace),clock=s.clock,hp=s.hp,x=s.x,y=s.y,
  enemy_x=s.enemy_x,enemy_y=s.enemy_y,random=s.random,enemy_hp=s.enemy_hp,dives=s.dives,ready=s.ready,impact=s.impact,
  embers=array(s.embers,lambda e:dict(x=e.x,y=e.y,remaining=e.remaining,due=e.due,expires=e.expires)),other_hp=s.other_hp))))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-ember-turns.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original Ember Moth scenarios / {sum(len(c["expected"]) for c in cases)} decisions.')
