#!/usr/bin/env python3
"""Execute pinned original Cantor commands, with actual corpse creation and reward rules."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility');local R=require('src.rng')
function capture_revival(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/adventurer'})
 local p=G.player(s);p.position.x=15;p.position.y=16
 local map=C.map_for(s);s.generated_map={};for k,v in pairs(map)do s.generated_map[k]=v end
 local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
 s.generated_map.rows=rows;s.generated_map.placements={stairs={}};s.entities={p}
 local cantor=G.spawn_enemy(s,'core:enemy/grave_cantor',18,16,200)
 local victim=G.spawn_enemy(s,'core:enemy/mossbound_guard',16,16,10000);victim.hp=3;victim.max_hp=6
 if input.name=='competing'then G.spawn_enemy(s,'core:enemy/grave_cantor',18,17,200)end
 s.currency=0;s.rng=R.new(37);V.recompute(s);local result={}
 for i,command in ipairs(input.commands)do
  if i==4 and input.name=='wall'then rows[16]=rows[16]:sub(1,15)..'#'..rows[16]:sub(17)end
  if i==4 and input.name=='stairs'then s.generated_map.placements.stairs={{x=16,y=16,to_floor_id='other',to_x=2,to_y=2}}end
  local ok,why,ev=G.submit(s,{kind=command,dx=1,dy=0});assert(ok,why)
  local trace={};for _,v in ipairs(ev)do
   if v.kind=='WindupStarted'or v.kind=='SupportResolved'or v.kind=='AttackPerformed'then trace[#trace+1]=v.kind..':'..v.time..':'..v.ability_id
   elseif v.kind=='EnemyRevived'then trace[#trace+1]='revived:'..v.time..':'..v.definition_id
   elseif v.kind=='RevivalBlocked'then trace[#trace+1]='blocked:'..v.time
   elseif v.kind=='DamageTaken'then trace[#trace+1]='damage:'..v.time..':'..v.actual_loss
   elseif v.kind=='ExperienceGained'or v.kind=='CurrencyDropped'then trace[#trace+1]=v.kind..':'..v.time..':'..v.amount end end
  local corpses={};for _,c in ipairs(s.idea_corpses or{})do corpses[#corpses+1]={x=c.x-15,y=c.y-16,claimed=c.claimed_by~=nil}end
  local revived={};for _,e in ipairs(s.entities)do if e.revival_of then revived[#revived+1]={x=e.position.x-15,y=e.position.y-16,hp=e.hp,max_hp=e.max_hp}end end
  local ready,impact;for _,w in ipairs(s.schedule)do if w.actor_id==cantor.id then
   if w.kind=='actor_ready'then ready=w.due elseif w.kind=='impact'then impact=w.due end end end
  local drops=0;for _,d in ipairs(s.drops)do if not d.collected then drops=drops+1 end end
  result[#result+1]={trace=trace,clock=s.clock,hp=p.hp,xp=s.progression.xp_total,random=s.rng.state,gold=s.currency,drops=drops,
   spent=cantor.ai_memory.revival_spent==true,ready=ready,impact=impact,corpses=corpses,revived=revived}
 end
 return result
end
""")
cases=[]
for name in ['revived','occupied','stairs','wall','competing']:
 spec=dict(name=name,commands=['basic_attack','wait','wait','move'if name=='occupied'else'wait',
  'basic_attack'if name in ['revived','competing']else'wait','wait'])
 result=lua.globals().capture_revival(table(spec))
 cases.append(dict(input=spec,expected=array(result,lambda s:dict(trace=array(s.trace),clock=s.clock,hp=s.hp,xp=s.xp,random=s.random,gold=s.gold,
  drops=s.drops,spent=s.spent,ready=s.ready,impact=s.impact,corpses=array(s.corpses,lambda c:dict(x=c.x,y=c.y,claimed=c.claimed)),
  revived=array(s.revived,lambda e:dict(x=e.x,y=e.y,hp=e.hp,max_hp=e.max_hp))))))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-revival-turns.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original Cantor scenarios / {sum(len(c["expected"]) for c in cases)} decisions.')
