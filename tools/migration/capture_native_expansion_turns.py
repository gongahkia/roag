#!/usr/bin/env python3
"""Original public-command sequences for expansion effects, not only AI decisions."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES
lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility');local R=require('src.rng')
function capture_expansion_turns(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/adventurer'})
 local p=G.player(s);p.position.x=15;p.position.y=16
 local map=C.map_for(s);s.generated_map={};for k,v in pairs(map)do s.generated_map[k]=v end
 local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
 if input.wall then rows[16]=rows[16]:sub(1,16)..'B'..rows[16]:sub(18)end
 s.generated_map.rows=rows;s.entities={p}
 local e=G.spawn_enemy(s,'core:enemy/'..input.enemy,15+input.dx,16,0)
 if input.retaliate then e.ai_memory.retaliate=true end
 if input.summons then e.ai_memory.summons=input.summons end
 local ally
 if input.ally then ally=G.spawn_enemy(s,'core:enemy/mossbound_guard',17,16,10000);ally.hp=2 end
 s.rng=R.new(37);V.recompute(s)
 local function label(id)
  if id==p.id then return 'hero' elseif id==e.id then return 'enemy' elseif ally and id==ally.id then return 'ally' end
  return 'summon'
 end
 local results={}
 for index,command in ipairs(input.commands)do
  if index==2 and input.evade then p.position.y=15 end
  if index==2 and input.ally_moves then ally.position.y=18 end
  local ok,why,events=G.submit(s,{kind=command,dx=1,dy=0});assert(ok,why)
  local trace={}
  for _,v in ipairs(events)do
   if v.kind=='AttackPerformed'or v.kind=='SupportResolved'then trace[#trace+1]=v.kind..':'..v.time..':'..label(v.source_id)..':'..v.ability_id
   elseif v.kind=='DamageTaken'then trace[#trace+1]='damage:'..v.time..':'..label(v.source_id)..':'..label(v.target_id)..':'..v.actual_loss
   elseif v.kind=='Healed'then trace[#trace+1]='heal:'..v.time..':'..label(v.source_id)..':'..label(v.target_id)..':'..v.amount
   elseif v.kind=='EnemySummoned'then trace[#trace+1]='summon:'..v.time..':'..(v.x-15)..':'..(v.y-16)
   elseif v.kind=='TerrainBroken'then trace[#trace+1]='break:'..v.time..':'..(v.x-15)..':'..(v.y-16)
   end
  end
  local actors={}
  for _,a in ipairs(s.entities)do if a.hp and a.hp>0 then
   actors[#actors+1]={label=label(a.id),hp=a.hp,dx=a.position.x-15,dy=a.position.y-16}
  end end
  table.sort(actors,function(a,b)if a.label==b.label then if a.dx==b.dx then return a.dy<b.dy end return a.dx<b.dx end return a.label<b.label end)
  results[#results+1]={clock=s.clock,random=s.rng.state,actors=actors,trace=trace,summons=e.ai_memory.summons or 0,retaliate=e.ai_memory.retaliate or false,alerted=e.ai_memory.alerted or false,wall=s.generated_map.rows[16]:sub(17,17)}
 end
 return results
end
""")
cases=[]
specs=[dict(name='heals-ally',enemy='cinder_mender',dx=3,ally=True),dict(name='healing-lock',enemy='cinder_mender',dx=3,ally=True,ally_moves=True),
 dict(name='summons',enemy='forge_conjurer',dx=3),dict(name='summon-limit',enemy='forge_conjurer',dx=3,summons=3),
 dict(name='charge-held',enemy='glass_charger',dx=3),dict(name='charge-evaded',enemy='glass_charger',dx=3,evade=True),
 dict(name='break-barricade',enemy='mossbreaker',dx=3,wall=True),dict(name='retaliation',enemy='bastion_bulwark',dx=1,retaliate=True),
 dict(name='pack-bonus',enemy='pack_chorister',dx=1,ally=True),dict(name='lurker-alert',enemy='vault_lurker',dx=3)]
for spec in specs:
 spec['commands']=['wait']*4
 r=lua.globals().capture_expansion_turns(table(spec))
 cases.append(dict(input=spec,expected=array(r,lambda s:dict(clock=s.clock,random=s.random,actors=array(s.actors,lambda a:dict(label=a.label,hp=a.hp,dx=a.dx,dy=a.dy)),trace=array(s.trace),summons=s.summons,retaliate=s.retaliate,alerted=s.alerted,wall=s.wall))))
p=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-expansion-turns.json'
p.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} expansion turn scenarios / {sum(len(c["expected"]) for c in cases)} decisions.')
