#!/usr/bin/env python3
"""Capture the original Sentinel's finite quarters, locked arcs and public commands."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility');local R=require('src.rng')
function capture_sentinel(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/adventurer'})
 local p=G.player(s);p.position.x=15;p.position.y=16
 local map=C.map_for(s);s.generated_map={};for k,v in pairs(map)do s.generated_map[k]=v end
 local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
 if input.name=='wall'then rows[15]=rows[15]:sub(1,14)..'#'..rows[15]:sub(16)end
 s.generated_map.rows=rows;s.generated_map.placements={stairs={}};s.entities={p}
 local sentinel=G.spawn_enemy(s,'core:enemy/rotating_sentinel',16,16,50)
 local guards={};if input.name=='friendly'then
  for _,y in ipairs({15,17})do guards[#guards+1]=G.spawn_enemy(s,'core:enemy/mossbound_guard',15,y,10000)end end
 s.currency=0;s.rng=R.new(37);V.recompute(s);local result={}
 for i,command in ipairs(input.commands)do
  if i==2 and input.name=='death'then sentinel.hp=3 end
  local ok,why,ev=G.submit(s,{kind=command,dx=command=='move'and -1 or 1,dy=0});assert(ok,why)
  local trace={};for _,v in ipairs(ev)do
   if v.kind=='WindupStarted'or v.kind=='AttackPerformed'then trace[#trace+1]=v.kind..':'..v.time..':'..v.ability_id
   elseif v.kind=='DamageTaken'then local target=G.entity(s,v.target_id)
    local role=v.target_id==p.id and 'hero'or v.target_id==sentinel.id and 'sentinel'or 'guard'
    trace[#trace+1]='damage:'..v.time..':'..role..':'..v.actual_loss end end
  local ready,impact;for _,w in ipairs(s.schedule)do if w.actor_id==sentinel.id then
   if w.kind=='actor_ready'then ready=w.due elseif w.kind=='impact'then impact=w.due end end end
  local cells={};for _,c in ipairs(sentinel.pending_attack and sentinel.pending_attack.target_cells or{})do cells[#cells+1]={x=c.x-15,y=c.y-16}end
  local health={};for _,g in ipairs(guards)do health[#health+1]=g.hp end
  result[#result+1]={trace=trace,clock=s.clock,hp=p.hp,x=p.position.x-15,y=p.position.y-16,random=s.rng.state,
   enemy_hp=sentinel.hp,strikes=sentinel.ai_memory.sentinel_strikes or 0,facing=sentinel.ai_memory.sentinel_facing,
   finished=sentinel.ai_memory.sentinel_finished==true,ready=ready,impact=impact,cells=cells,guards=health}
 end
 return result
end
""")
cases=[]
for name,commands in [
 ('quarters',['wait']*14),('leave',['wait','move','wait']),
 ('hit',['wait','basic_attack','wait']),('friendly',['wait']*3),
 ('wall',['wait']*14),('death',['wait','basic_attack']+['wait']*4)]:
 spec=dict(name=name,commands=commands)
 result=lua.globals().capture_sentinel(table(spec))
 cases.append(dict(input=spec,expected=array(result,lambda s:dict(trace=array(s.trace),clock=s.clock,hp=s.hp,x=s.x,y=s.y,
  random=s.random,enemy_hp=s.enemy_hp,strikes=s.strikes,facing=s.facing,finished=s.finished,ready=s.ready,impact=s.impact,
  cells=array(s.cells,lambda c:dict(x=c.x,y=c.y)),guards=array(s.guards)))))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-sentinel-turns.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original Sentinel scenarios / {sum(len(c["expected"]) for c in cases)} decisions.')
