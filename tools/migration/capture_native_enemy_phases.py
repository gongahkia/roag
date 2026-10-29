#!/usr/bin/env python3
"""Original paid-command evidence for elite/Regent phase reactions and locked timing."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility');local R=require('src.rng')
function capture_phase(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/adventurer'})
 local p=G.player(s);p.position.x,p.position.y=15,16
 local map=C.map_for(s);s.generated_map={};for k,v in pairs(map)do s.generated_map[k]=v end
 local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
 s.generated_map.rows=rows;s.generated_map.placements={stairs={}};s.entities={p}
 local e=G.spawn_enemy(s,'core:enemy/'..input.enemy,16,16,50)
 e.hp=math.floor(e.max_hp/2)+1
 if input.enemy=='hollow_regent'then s.finale={phase='active',boss_id=e.id}
 else s.stage={phase='elite_active',elite_id=e.id}end
 s.rng=R.new(37);V.recompute(s);local result={}
 for _,command in ipairs(input.commands)do
  local ok,why,events=G.submit(s,{kind=command,dx=1,dy=0});assert(ok,why)
  local trace={};for _,v in ipairs(events)do
   if v.kind=='WindupStarted'or v.kind=='AttackPerformed'then trace[#trace+1]=v.kind..':'..v.time..':'..v.ability_id
   elseif v.kind=='ElitePhaseChanged'or v.kind=='RegentPhaseChanged'then trace[#trace+1]=v.kind..':'..v.time..':'..v.phase
   elseif v.kind=='DamageTaken'or v.kind=='DamageBlocked'then trace[#trace+1]=v.kind..':'..v.time..':'..v.actual_loss end end
  local ready,impact;for _,w in ipairs(s.schedule)do if w.actor_id==e.id then
   if w.kind=='actor_ready'then ready=w.due elseif w.kind=='impact'then impact=w.due end end end
  local cells={};for _,c in ipairs(e.pending_attack and e.pending_attack.target_cells or{})do cells[#cells+1]={x=c.x-15,y=c.y-16}end
  result[#result+1]={trace=trace,clock=s.clock,hp=e.hp,speed=e.speed,phase=e.phase_two or false,
   ready=ready,impact=impact,cells=cells,attacks=e.ai_memory.attacks or 0,random=s.rng.state}
 end
 return result
end
""")
cases=[]
for enemy in ['thorn_warden','mycelial_matriarch','canal_lockmaster','ash_marshal',
              'prism_curator','index_keeper','ember_crucible','hollow_regent']:
    spec=dict(enemy=enemy,commands=['wait','basic_attack','wait','wait','wait','basic_attack','wait','wait'])
    result=lua.globals().capture_phase(table(spec))
    cases.append(dict(input=spec,expected=array(result,lambda s:dict(trace=array(s.trace),clock=s.clock,
        hp=s.hp,speed=s.speed,phase=s.phase,ready=s.ready,impact=s.impact,attacks=s.attacks,random=s.random,
        cells=array(s.cells,lambda c:dict(x=c.x,y=c.y))))))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-enemy-phases.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original boss phase scenarios / {sum(len(c["expected"]) for c in cases)} commands.')
