#!/usr/bin/env python3
"""Capture original pressure-plate gates, refresh, occupied closure and free refusal."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility')
function capture_gate(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/'..(input.class or 'adventurer')})
 local p=G.player(s);p.position.x=15;p.position.y=16;s.entities={p};s.idea_terrain={version=1,nodes={}}
 local map={};for k,v in pairs(C.map_for(s))do map[k]=v end
 local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
 map.rows=rows;map.placements={stairs={}};s.generated_map=map
 local gate=G.spawn_door(s,input.blocked and 16 or 17,16,'test:gate')
 gate.controller='review_gate';gate.definition_id='core:object/review_gate';gate.closes_at=nil
 s.review_features={{id='test:plate',kind='plate',floor_id=s.active_floor_id,x=16,y=input.blocked and 17 or 16,gate_id=gate.id}}
 s.rng.state=37;V.recompute(s);local result={}
 for _,command in ipairs(input.commands)do
  local kind,dx=command[1],command[2];local preview=G.preview(s,kind,dx,0)
  local ok,why,events=G.submit(s,{kind=kind,dx=dx,dy=0});local opened,closed=0,0
  for _,e in ipairs(events or {})do if e.kind=='TimedGateOpened'then opened=opened+1 elseif e.kind=='TimedGateClosed'then closed=closed+1 end end
  result[#result+1]={valid=ok,clock=s.clock,x=p.position.x-15,open=gate.open,due=gate.closes_at or 0,
   opened=opened,closed=closed,recovery=preview.valid and preview.cost or 0}
 end
 return result
end
""")
specs=[dict(name='occupied',commands=[['move',1],['move',1],['wait',1],['wait',1],['move',1]]),
       dict(name='refresh',commands=[['move',1],['move',-1],['move',1],['wait',1],['wait',1]]),
       dict(name='blocked',blocked=True,commands=[['move',1],['basic_attack',1]]),
       dict(name='dash',commands=[['dash',1],['move',1],['move',1]]),
       dict(name='fast',**{'class':'ranger'},commands=[['move',1],['wait',1],['wait',1],['wait',1]])]
fields=['valid','clock','x','open','due','opened','closed','recovery']
cases=[dict(input=s,expected=array(lua.globals().capture_gate(table(s)),lambda r:{k:r[k] for k in fields}))for s in specs]
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-gates.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original gate scenarios / {sum(len(c["expected"])for c in cases)} public commands.')
