#!/usr/bin/env python3
"""Capture pinned original Alarm Keeper commands and physical reinforcement outcomes."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility');local R=require('src.rng')
function capture_alarm(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/adventurer'})
 local p=G.player(s);p.position.x=15;p.position.y=16
 local map=C.map_for(s);s.generated_map={};for k,v in pairs(map)do s.generated_map[k]=v end
 local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
 s.generated_map.rows=rows;s.generated_map.placements={stairs={}};s.entities={p}
 local keeper=G.spawn_enemy(s,'core:enemy/alarm_keeper',18,16,50)
 s.currency=0;s.rng=R.new(37);V.recompute(s);local result={}
 for i,command in ipairs(input.commands)do
  if i==3 then
   if input.name=='wall'then rows[15]=rows[15]:sub(1,17)..'#'..rows[15]:sub(19)
   elseif input.name=='stairs'then s.generated_map.placements.stairs={{x=18,y=15,to_floor_id='other',to_x=2,to_y=2}}
   elseif input.name=='occupied'then G.spawn_enemy(s,'core:enemy/mossbound_guard',18,15,10000)end
  end
  if i==5 and input.name=='stairs'then s.generated_map.placements.stairs={}end
  local ok,why,ev=G.submit(s,{kind=command,dx=1,dy=0});assert(ok,why)
  local trace={};for _,v in ipairs(ev)do
   if v.kind=='WindupStarted'or v.kind=='SupportResolved'or v.kind=='AttackPerformed'then trace[#trace+1]=v.kind..':'..v.time..':'..v.ability_id
   elseif v.kind=='AlarmReinforcement'or v.kind=='AlarmBlocked'then trace[#trace+1]=v.kind..':'..v.time..':'..(v.x-15)..':'..(v.y-16)
   elseif v.kind=='DamageTaken'then trace[#trace+1]='damage:'..v.time..':'..v.actual_loss end end
  local called={};for _,e in ipairs(s.entities)do if e.called_by then called[#called+1]={x=e.position.x-15,y=e.position.y-16,hp=e.hp}end end
  local ready,impact;for _,w in ipairs(s.schedule)do if w.actor_id==keeper.id then
   if w.kind=='actor_ready'then ready=w.due elseif w.kind=='impact'then impact=w.due end end end
  result[#result+1]={trace=trace,clock=s.clock,hp=p.hp,xp=s.progression.xp_total,random=s.rng.state,gold=s.currency,
   spent=keeper.ai_memory.alarm_spent==true,ready=ready,impact=impact,called=called}
 end
 return result
end
""")
cases=[]
for name in ['open','occupied','stairs','wall']:
 spec=dict(name=name,commands=['wait']*7)
 result=lua.globals().capture_alarm(table(spec))
 cases.append(dict(input=spec,expected=array(result,lambda s:dict(trace=array(s.trace),clock=s.clock,hp=s.hp,xp=s.xp,random=s.random,gold=s.gold,
  spent=s.spent,ready=s.ready,impact=s.impact,called=array(s.called,lambda e:dict(x=e.x,y=e.y,hp=e.hp))))))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-alarm-turns.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original Alarm Keeper scenarios / {sum(len(c["expected"]) for c in cases)} decisions.')
