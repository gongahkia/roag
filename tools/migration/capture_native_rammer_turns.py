#!/usr/bin/env python3
"""Pinned public Rammer command sequences, including blocked and forced arrivals."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility');local R=require('src.rng');local Rewards=require('src.rewards')
function capture_rammer(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/adventurer'})
 local p=G.player(s);p.position.x=15;p.position.y=16
 local map=C.map_for(s);s.generated_map={};for k,v in pairs(map)do s.generated_map[k]=v end
 local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
 if input.name=='wall'then rows[16]=rows[16]:sub(1,15)..'#'..rows[16]:sub(17)end
 s.generated_map.rows=rows;s.generated_map.placements={stairs={}};s.entities={p}
 if input.name=='stairs'then s.generated_map.placements.stairs={{x=16,y=16,to_floor_id='other',to_x=2,to_y=2}}end
 local e=G.spawn_enemy(s,'core:enemy/heavy_rammer',14,16,0)
 if input.name=='occupied'then G.spawn_enemy(s,'core:enemy/mossbound_guard',16,16,10000)end
 if input.name=='snare'then
  local setter=G.spawn_enemy(s,'core:enemy/snare_scribe',19,19,10000)
  s.enemy_snares={{source_id=setter.id,floor_id=s.active_floor_id,x=16,y=16}}
 end
 if input.name=='currency'then Rewards.spawn_currency(s,9,s.active_floor_id,16,16,function()end)end
 s.currency=0;s.rng=R.new(37);V.recompute(s);local result={}
 for i,command in ipairs(input.commands)do
  local ok,why,ev=G.submit(s,{kind=command,dx=0,dy=-1});assert(ok,why)
  local trace={}
  for _,v in ipairs(ev)do
   if v.kind=='WindupStarted'then trace[#trace+1]='windup:'..v.time..':'..v.ability_id
   elseif v.kind=='DamageTaken'then trace[#trace+1]='damage:'..v.time..':'..v.actual_loss
   elseif v.kind=='Displaced'then trace[#trace+1]='shove:'..v.time..':'..(v.x-15)..':'..(v.y-16)
   elseif v.kind=='ShoveBlocked'then trace[#trace+1]='blocked:'..v.time..':'..v.reason
   elseif v.kind=='EnemySnareTriggered'then trace[#trace+1]='snare:'..v.time end
  end
  local ready,impact
  for _,w in ipairs(s.schedule)do if w.actor_id==e.id then if w.kind=='actor_ready'then ready=w.due elseif w.kind=='impact'then impact=w.due end end end
  result[#result+1]={trace=trace,clock=s.clock,hp=p.hp,enemy_hp=e.hp,x=p.position.x-15,y=p.position.y-16,
   enemy_x=e.position.x-15,enemy_y=e.position.y-16,random=s.rng.state,ready=ready,impact=impact,traps=#(s.enemy_snares or{}),gold=s.currency}
 end
 return result
end
""")
cases=[]
for name in ['open','evade','wall','stairs','occupied','snare','currency']:
 spec=dict(name=name,commands=['wait','move'if name=='evade'else'wait','wait'])
 result=lua.globals().capture_rammer(table(spec))
 cases.append(dict(input=spec,expected=array(result,lambda s:dict(trace=array(s.trace),clock=s.clock,hp=s.hp,enemy_hp=s.enemy_hp,x=s.x,y=s.y,
  enemy_x=s.enemy_x,enemy_y=s.enemy_y,random=s.random,ready=s.ready,impact=s.impact,traps=s.traps,gold=s.gold))))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-rammer-turns.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original Rammer scenarios / {sum(len(c["expected"]) for c in cases)} decisions.')
