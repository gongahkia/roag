#!/usr/bin/env python3
"""Capture the original Oath Twins' damage, death, proc and permanent bond-break rules."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility');local R=require('src.rng')
function capture_oath(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/adventurer'})
 local p=G.player(s);p.position.x=15;p.position.y=16
 local map=C.map_for(s);s.generated_map={};for k,v in pairs(map)do s.generated_map[k]=v end
 local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
 s.generated_map.rows=rows;s.generated_map.placements={stairs={}};s.entities={p}
 local a=G.spawn_enemy(s,'core:enemy/oath_twin_a',16,16,10000)
 local b=G.spawn_enemy(s,'core:enemy/oath_twin_b',input.area and 16 or 17,input.area and 15 or 16,10000)
 a.oath_partner_id=b.id;b.oath_partner_id=a.id
 if input.name=='turns'then
  a.position.x=18;b.position.x=18;b.position.y=17
  for _,work in ipairs(s.schedule)do
   if work.actor_id==a.id then work.due=50 elseif work.actor_id==b.id then work.due=60 end
  end
  require('src.scheduler').sort(s)
 end
 if input.name=='armour'then a.defense=2;b.defense=99 end
 if input.name=='both_die'or input.name=='grave_dividend'then a.hp=2;b.hp=2;p.hp=10 end
 if input.name=='shared_kill'or input.name=='shared_grave_kill'then b.hp=1 end
 if input.name=='shared_grave_kill'then p.hp=10 end
 if input.boon then G.set_boon_stacks(s,p.id,'core:boon/'..input.boon,'common',input.stacks or 1)end
 local seed=37
 if input.name=='skull'then
  for n=1,100 do if R.next_int(R.new(n),8)==7 then seed=n;break end end
 end
 s.currency=0;s.rng=R.new(seed);V.recompute(s);local result={}
 for i,command in ipairs(input.commands)do
  if input.name=='distance'then b.position.x=i==1 and 22 or 17 end
  if input.name=='wall'then
   b.position.x=18
   rows[16]=rows[16]:sub(1,16)..(i==1 and '#'or'.')..rows[16]:sub(18)
  end
  local ok,why,ev=G.submit(s,{kind=command,dx=1,dy=0});assert(ok,why)
  local trace={};local damage={};local actions={};local hits=0
  for _,v in ipairs(ev)do
   if v.kind=='WindupStarted'or v.kind=='AttackPerformed'then
    local source=v.source_id==a.id and 'a'or v.source_id==b.id and 'b'or 'hero'
    actions[#actions+1]=v.kind..':'..v.time..':'..source..':'..v.ability_id
   end
   local target=v.target_id==a.id and 'a'or v.target_id==b.id and 'b'or v.target_id==p.id and 'hero'or 'other'
   if v.kind=='DamageTaken'or v.kind=='DamageBlocked'then
    damage[#damage+1]={time=v.time,target=target,loss=v.actual_loss,shared=v.oath_shared_only==true,
     direct=v.direct_attack==true,element=v.damage_type,given=v.oath_given or 0,received=v.oath_received or 0}
   elseif v.kind=='HitConfirmed'then hits=hits+1
   elseif v.kind=='EntityKilled'then trace[#trace+1]='killed:'..target
   elseif v.kind=='BoonActivated'then trace[#trace+1]='boon:'..v.boon_id
   elseif v.kind=='ExperienceGained'or v.kind=='CurrencyDropped'then trace[#trace+1]=v.kind..':'..v.amount end
  end
  local drops=0;for _,d in ipairs(s.drops)do if not d.collected then drops=drops+1 end end
  result[#result+1]={clock=s.clock,hp=p.hp,a_hp=a.hp,b_hp=b.hp,a_broken=a.oath_broken==true,b_broken=b.oath_broken==true,
   xp=s.progression.xp_total,gold=s.currency,drops=drops,random=s.rng.state,damage=damage,hits=hits,trace=trace,actions=actions,
   ax=a.position.x-15,ay=a.position.y-16,bx=b.position.x-15,by=b.position.y-16}
 end
 return {seed=seed,steps=result}
end
""")
cases=[]
for name,area,boon,commands in [
 ('single',False,None,['basic_attack']*2),('area',True,None,['sweeping_slash']*2),
 ('armour',False,None,['basic_attack']*2),('resonant',False,'resonant_wounds',['basic_attack']*2),
 ('both_die',True,None,['sweeping_slash']),('grave_dividend',True,'grave_dividend',['sweeping_slash']),
 ('shared_kill',False,None,['basic_attack']),('skull',False,'fateful_dice',['basic_attack']),
 ('shared_grave_kill',False,'grave_dividend',['basic_attack']),
 ('distance',False,None,['wait','basic_attack']),('wall',False,None,['wait','basic_attack']),
 ('burn',False,'emberbrand',['basic_attack','wait']),('turns',False,None,['wait']*8)]:
 spec=dict(name=name,area=area,boon=boon,commands=commands,stacks=3 if name=='burn'else 1)
 result=lua.globals().capture_oath(table(spec));spec['combat_seed']=result.seed
 cases.append(dict(input=spec,expected=array(result.steps,lambda s:dict(clock=s.clock,hp=s.hp,a_hp=s.a_hp,b_hp=s.b_hp,
  a_broken=s.a_broken,b_broken=s.b_broken,xp=s.xp,gold=s.gold,drops=s.drops,random=s.random,hits=s.hits,trace=array(s.trace),actions=array(s.actions),
  ax=s.ax,ay=s.ay,bx=s.bx,by=s.by,
  damage=array(s.damage,lambda d:dict(time=d.time,target=d.target,loss=d.loss,shared=d.shared,direct=d.direct,element=d.element,given=d.given,received=d.received))))))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-oath-turns.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original Oath Twin scenarios / {sum(len(c["expected"]) for c in cases)} decisions.')
