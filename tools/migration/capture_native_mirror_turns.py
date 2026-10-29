#!/usr/bin/env python3
"""Capture original Hexer commitments and real reflected manual/enemy/sentry attacks."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility');local R=require('src.rng')
function capture_mirror(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/'..input.class_name})
 local p=G.player(s);p.position.x,p.position.y=15,16
 local map=C.map_for(s);s.generated_map={};for k,v in pairs(map)do s.generated_map[k]=v end
 local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
 s.generated_map.rows=rows;s.generated_map.placements={stairs={}};s.entities={p}
 local hexer=G.spawn_enemy(s,'core:enemy/mirror_hexer',18,16,50);local guard,blocker,shooter
 if input.boon then G.set_boon_stacks(s,p.id,'core:boon/'..input.boon,'common',1)end
 if input.name=='dividend'or input.name=='sentry'then p.hp=p.hp-4 end
 s.currency=0;s.rng=R.new(37);V.recompute(s);local result={}
 for i,command in ipairs(input.commands)do
  if i==4 and input.name~='expiry'and input.name~='occupied'and input.name~='hook'then
   guard=G.spawn_enemy(s,'core:enemy/mossbound_guard',17,17,10000)
   if input.name=='blocker'or input.name=='needle'then blocker=G.spawn_enemy(s,'core:enemy/mossbound_guard',16,16,10000)
    if input.name=='blocker'then blocker.hp=2 end end
   if input.name=='enemy'then p.position.x=19;shooter=G.spawn_enemy(s,'core:enemy/thornspitter',15,16,s.clock+1)end
  end
  V.recompute(s);local preview=G.preview(s,command,1,0)
  local ok,why,ev=G.submit(s,{kind=command,dx=1,dy=0});assert(ok,why)
  local trace={};for _,v in ipairs(ev)do
   if v.kind=='WindupStarted'or v.kind=='SupportResolved'or v.kind=='AttackPerformed'then trace[#trace+1]=v.kind..':'..v.time..':'..v.ability_id
   elseif v.kind=='MirrorCreated'or v.kind=='MirrorBlocked'or v.kind=='MirrorReflected'then
    trace[#trace+1]=v.kind..':'..v.time..':'..(v.x-15)..':'..(v.y-16)
   elseif v.kind=='DamageTaken'then trace[#trace+1]='damage:'..v.time..':'..v.actual_loss end end
  local mirrors={};for _,r in ipairs(s.idea_mirrors or{})do mirrors[#mirrors+1]={x=r.x-15,y=r.y-16,dx=r.in_dx,dy=r.in_dy,expires=r.expires}end
  local ready,impact;for _,w in ipairs(s.schedule)do if w.actor_id==hexer.id then
   if w.kind=='actor_ready'then ready=w.due elseif w.kind=='impact'then impact=w.due end end end
  local cells={};for _,c in ipairs(shooter and shooter.pending_attack and shooter.pending_attack.target_cells or{})do cells[#cells+1]={x=c.x-15,y=c.y-16}end
  result[#result+1]={trace=trace,clock=s.clock,hp=p.hp,x=p.position.x-15,y=p.position.y-16,
   enemy_x=hexer.position.x-15,enemy_y=hexer.position.y-16,random=s.rng.state,enemy_hp=hexer.hp,
   casts=hexer.ai_memory.mirror_casts or 0,ready=ready,impact=impact,mirrors=mirrors,guard_hp=guard and guard.hp,
   blocker_hp=blocker and blocker.hp,shooter_cells=cells,preview_step=preview.reflection and preview.reflection.step}
 end
 return result
end
""")
cases=[]
for name,cls,boon,commands in [
 ('shot','ranger',None,['wait']*3+['basic_attack','wait']),
 ('blocker','ranger',None,['wait']*3+['basic_attack','wait']),
 ('needle','ranger','needle_flight',['wait']*3+['basic_attack','wait']),
 ('vial','alchemist','volatile_vial',['wait']*3+['basic_attack','wait']),
 ('dividend','ranger','mirror_dividend',['wait']*3+['basic_attack','wait']),
 ('enemy','ranger',None,['wait']*6),('expiry','ranger',None,['wait']*8),
 ('occupied','ranger',None,['wait','move','move','wait']),
 ('hook','hookrunner',None,['wait']*3+['sweeping_slash','wait']),
 ('sentry','tinkerer','mirror_dividend',['wait']*3+['sweeping_slash']+['wait']*3)]:
 spec=dict(name=name,class_name=cls,boon=boon,commands=commands); result=lua.globals().capture_mirror(table(spec))
 cases.append(dict(input=spec,expected=array(result,lambda s:dict(trace=array(s.trace),clock=s.clock,hp=s.hp,x=s.x,y=s.y,
  enemy_x=s.enemy_x,enemy_y=s.enemy_y,random=s.random,enemy_hp=s.enemy_hp,casts=s.casts,ready=s.ready,impact=s.impact,
  mirrors=array(s.mirrors,lambda m:dict(x=m.x,y=m.y,dx=m.dx,dy=m.dy,expires=m.expires)),guard_hp=s.guard_hp,
  blocker_hp=s.blocker_hp,shooter_cells=array(s.shooter_cells,lambda c:dict(x=c.x,y=c.y)),preview_step=s.preview_step))))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-mirror-turns.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original Mirror Hexer scenarios / {sum(len(c["expected"]) for c in cases)} decisions.')
