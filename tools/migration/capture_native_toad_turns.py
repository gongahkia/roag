#!/usr/bin/env python3
"""Execute pinned original Toad commands; no game rules or handlers are replaced."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility')
local R=require('src.rng');local Rewards=require('src.rewards')
function capture_toad(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/adventurer'})
 local p=G.player(s);p.position.x=15;p.position.y=16;p.hp=p.max_hp-input.missing
 local map=C.map_for(s);s.generated_map={};for k,v in pairs(map)do s.generated_map[k]=v end
 local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
 s.generated_map.rows=rows;s.generated_map.placements={stairs={}};s.entities={p}
 local e=G.spawn_enemy(s,'core:enemy/gulp_toad',17,16,50);e.hp=3
 local second
 if input.name=='competing'then second=G.spawn_enemy(s,'core:enemy/gulp_toad',16,17,50)end
 if input.kind=='currency'then Rewards.spawn_currency(s,7,s.active_floor_id,15+input.drop_x,16,function()end)
 else Rewards.spawn_supply(s,'health',s.active_floor_id,15+input.drop_x,16,function()end)end
 s.currency=0;s.rng=R.new(37);V.recompute(s);local result={}
 for _,command in ipairs(input.commands)do
  local ok,why,ev=G.submit(s,{kind=command,dx=1,dy=0});assert(ok,why)
  local trace={}
  for _,v in ipairs(ev)do
   if v.kind=='WindupStarted'or v.kind=='SupportResolved'or v.kind=='AttackPerformed'then trace[#trace+1]=v.kind..':'..v.time..':'..v.ability_id
   elseif v.kind=='DropSwallowed'or v.kind=='DropRegurgitated'or v.kind=='CurrencyCollected'or v.kind=='CurrencyDropped'or v.kind=='SupplyCollected'then
    trace[#trace+1]=v.kind..':'..v.time..':'..v.drop_id..':'..v.amount
   elseif v.kind=='GulpBlocked'then trace[#trace+1]=v.kind..':'..v.time
   elseif v.kind=='DamageTaken'then trace[#trace+1]='damage:'..v.time..':'..v.actual_loss end
  end
  local drops={};for _,d in ipairs(s.drops)do if not d.collected then
   drops[#drops+1]={id=d.id,kind=d.kind,amount=d.amount,x=d.position.x-15,y=d.position.y-16,
    carrier=d.carried_by==e.id and 1 or (second and d.carried_by==second.id and 2 or 0)}end end
  local ready,impact;for _,w in ipairs(s.schedule)do if w.actor_id==e.id then
   if w.kind=='actor_ready'then ready=w.due elseif w.kind=='impact'then impact=w.due end end end
  result[#result+1]={trace=trace,clock=s.clock,hp=p.hp,enemy_hp=e.hp,x=p.position.x-15,y=p.position.y-16,
   enemy_x=e.position.x-15,enemy_y=e.position.y-16,random=s.rng.state,gold=s.currency,drops=drops,ready=ready,impact=impact}
 end
 return result
end
""")
cases=[]
for name in ['currency','health','intercept','guarded-health','own-cell','competing','approach']:
 spec=dict(name=name,kind='health'if name in ['health','guarded-health']else'currency',
  missing=0 if name=='guarded-health'else 10,drop_x=2 if name=='own-cell'else 6 if name=='approach'else 1,
  commands=['wait','wait','move','basic_attack','move'])
 if name in ['intercept','guarded-health']:spec['commands']=['wait','move','wait']
 if name in ['competing','approach']:spec['commands']=['wait']*4
 result=lua.globals().capture_toad(table(spec))
 cases.append(dict(input=spec,expected=array(result,lambda s:dict(trace=array(s.trace),clock=s.clock,hp=s.hp,enemy_hp=s.enemy_hp,x=s.x,y=s.y,
  enemy_x=s.enemy_x,enemy_y=s.enemy_y,random=s.random,gold=s.gold,ready=s.ready,impact=s.impact,
  drops=array(s.drops,lambda d:dict(id=d.id,kind=d.kind,amount=d.amount,x=d.x,y=d.y,carrier=d.carrier))))))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-toad-turns.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original Toad scenarios / {sum(len(c["expected"]) for c in cases)} decisions.')
