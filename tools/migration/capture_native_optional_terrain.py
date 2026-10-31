#!/usr/bin/env python3
"""Capture original public-command vent/push/shortcut timing and outcomes."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local T=require('src.idea_terrain');local V=require('src.visibility')
function capture_optional(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/adventurer'})
 local p=G.player(s);p.position.x=15;p.position.y=16;s.entities={p};s.idea_terrain={version=1,nodes={}}
 local original=C.map_for(s);local map={};for k,v in pairs(original)do map[k]=v end
 local rows={};for y,row in ipairs(original.rows)do rows[y]=string.rep('.',#row)end
 map.rows=rows;map.placements={stairs={}};map.protected_cells={};map.corridors={};map.start_x=15;map.start_y=16;s.generated_map=map
 for _,n in ipairs(input.nodes)do T.place(s,map,n[1],15+n[2],16+n[3],n[4],n[5])end
 V.recompute(s);local results={}
 for _,c in ipairs(input.commands)do
  local ok,why,events=G.submit(s,{kind=c[1],dx=c[2],dy=c[3]});assert(ok,why)
  local trace={};for _,e in ipairs(events)do
   if e.kind=='HeatVentWarned'or e.kind=='HeatVentBurst'or e.kind=='PushFloorTriggered'or e.kind=='PushFloorBlocked'
    or e.kind=='FragileArmed'or e.kind=='FragileCollapsed'then trace[#trace+1]=e.kind end
  end
  local nodes={};for _,n in ipairs(s.idea_terrain.nodes)do nodes[#nodes+1]={phase=n.phase,due=n.due or n.cooldown_until or 0}end
  results[#results+1]={clock=s.clock,x=p.position.x-15,y=p.position.y-16,hp=p.hp,trace=trace,nodes=nodes,random=s.rng.state}
 end
 return results
end
""")
cases=[]
specs=[
 dict(name='vent_enter',nodes=[['heat_vent',1,0]],commands=[['wait',1,0],['move',1,0],['wait',1,0],['wait',1,0],['wait',1,0]]),
 dict(name='vent_leave',nodes=[['heat_vent',0,0]],commands=[['wait',1,0],['move',-1,0]]),
 dict(name='push',nodes=[['push_floor',1,0,1,0]],commands=[['move',1,0]]),
 dict(name='push_cycle',nodes=[['push_floor',1,0,1,0],['push_floor',2,0,-1,0]],commands=[['move',1,0]]),
 dict(name='dash_past_arrow',nodes=[['push_floor',1,0,0,-1]],commands=[['dash',1,0]]),
 dict(name='shortcut',nodes=[['fragile_crossing',1,0]],commands=[['move',1,0],['wait',1,0],['move',1,0]]),
]
for spec in specs:
 result=lua.globals().capture_optional(table(spec))
 expected=array(result,lambda s:dict(clock=s.clock,x=s.x,y=s.y,hp=s.hp,random=s.random,trace=array(s.trace),
  nodes=array(s.nodes,lambda n:dict(phase=n.phase,due=n.due))))
 cases.append(dict(input=spec,expected=expected))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-optional-terrain.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original optional-terrain scenarios / {sum(len(c["expected"]) for c in cases)} commands.')
