#!/usr/bin/env python3
"""Capture original Web Weaver decisions, crossing, anchor cuts and forced movement."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility');local R=require('src.rng')
function capture_web(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/'..input.class_name})
 local p=G.player(s);p.position.x=15;p.position.y=16
 local map=C.map_for(s);s.generated_map={};for k,v in pairs(map)do s.generated_map[k]=v end
 local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
 s.generated_map.rows=rows;s.generated_map.placements={stairs={}};s.entities={p}
 local weaver=G.spawn_enemy(s,'core:enemy/web_weaver',18,16,50)
 s.currency=0;s.rng=R.new(37);V.recompute(s);local result={}
 for i,command in ipairs(input.commands)do
  if i==4 and input.name=='blocked_arrow'then local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',16,16,10000);e.hp=2 end
  if i==4 and input.name=='rammer'then G.spawn_enemy(s,'core:enemy/heavy_rammer',14,16,300)end
  local ok,why,ev=G.submit(s,{kind=command,dx=command=='move'and input.name=='rammer'and 0 or 1,
   dy=command=='move'and input.name=='rammer'and -1 or 0});assert(ok,why)
  local trace={};for _,v in ipairs(ev)do
   if v.kind=='WindupStarted'or v.kind=='SupportResolved'or v.kind=='AttackPerformed'then trace[#trace+1]=v.kind..':'..v.time..':'..v.ability_id
   elseif v.kind=='WebCreated'or v.kind=='WebBlocked'or v.kind=='WebCrossed'or v.kind=='WebAnchorCut'then
    trace[#trace+1]=v.kind..':'..v.time..':'..(v.x-15)..':'..(v.y-16)
   elseif v.kind=='DamageTaken'then trace[#trace+1]='damage:'..v.time..':'..v.actual_loss end end
  local webs={};for _,w in ipairs(s.idea_webs or{})do webs[#webs+1]={ax=w.ax-15,ay=w.ay-16,bx=w.bx-15,by=w.by-16,expires=w.expires}end
  local ready,impact;for _,w in ipairs(s.schedule)do if w.actor_id==weaver.id then
   if w.kind=='actor_ready'then ready=w.due elseif w.kind=='impact'then impact=w.due end end end
  result[#result+1]={trace=trace,clock=s.clock,hp=p.hp,x=p.position.x-15,y=p.position.y-16,xp=s.progression.xp_total,
   random=s.rng.state,enemy_hp=weaver.hp,casts=weaver.ai_memory.web_casts or 0,ready=ready,impact=impact,webs=webs}
 end
 return result
end
""")
cases=[]
for name,cls,commands in [
 ('walk','adventurer',['wait']*3+['move','wait']),
 ('dash','adventurer',['wait']*3+['dash','wait']),
 ('arrow','ranger',['wait']*3+['basic_attack','wait']),
 ('blocked_arrow','ranger',['wait']*3+['basic_attack','wait']),
 ('anchor_occupied','adventurer',['wait','move','move','wait']),
 ('expiry','adventurer',['wait']*7),
 ('rammer','adventurer',['wait']*5+['move'])]:
 spec=dict(name=name,class_name=cls,commands=commands)
 result=lua.globals().capture_web(table(spec))
 cases.append(dict(input=spec,expected=array(result,lambda s:dict(trace=array(s.trace),clock=s.clock,hp=s.hp,x=s.x,y=s.y,xp=s.xp,
  random=s.random,enemy_hp=s.enemy_hp,casts=s.casts,ready=s.ready,impact=s.impact,
  webs=array(s.webs,lambda w:dict(ax=w.ax,ay=w.ay,bx=w.bx,by=w.by,expires=w.expires))))))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-web-turns.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original Web Weaver scenarios / {sum(len(c["expected"]) for c in cases)} decisions.')
