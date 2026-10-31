#!/usr/bin/env python3
"""Capture original paid door bumps, dash refusal and DoorOpened boon dispatch."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility');local B=require('src.boons')
function capture_door(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/'..input.class})
 local p=G.player(s);p.position.x=15;p.position.y=16;s.entities={p};s.idea_terrain={version=1,nodes={}}
 local original=C.map_for(s);local map={};for k,v in pairs(original)do map[k]=v end
 local rows={};for y,row in ipairs(original.rows)do rows[y]=string.rep('.',#row)end
 map.rows=rows;map.placements={stairs={}};s.generated_map=map
 local door=G.spawn_door(s,16,16,'door:reference')
 if input.ward then G.set_boon_stacks(s,p.id,'core:boon/doorward_lamp','common',1)end
 s.rng.state=37
 V.recompute(s);local result={}
 for _,command in ipairs(input.commands)do
  local preview=G.preview(s,command,1,0);local ok,why,events=G.submit(s,{kind=command,dx=1,dy=0})
  local opened,moved,entered,raised=0,0,0,0
  for _,e in ipairs(events or {})do if e.kind=='DoorOpened'then opened=opened+1 elseif e.kind=='Moved'then moved=moved+1
   elseif e.kind=='TileEntered'then entered=entered+1 elseif e.kind=='GeomancerBlockRaised'then raised=raised+1 end end
  result[#result+1]={valid=ok,clock=s.clock,hp=p.hp,x=p.position.x-15,open=door.open,opened=opened,moved=moved,entered=entered,
   ward=B.ward_total(s,p),raised=raised,recovery=preview.valid and preview.cost or 0}
 end
 return result
end
""")
specs=[dict(name='walk',**{'class':'adventurer'},commands=['move','move','dash']),
       dict(name='refusal',**{'class':'adventurer'},commands=['dash','basic_attack','move']),
       dict(name='ward',**{'class':'adventurer'},ward=True,commands=['move','move']),
       dict(name='fast',**{'class':'ranger'},commands=['move','move']),
       dict(name='open-door-bulwark',**{'class':'geomancer'},commands=['move','sweeping_slash'])]
fields=['valid','clock','hp','x','open','opened','moved','entered','ward','raised','recovery']
cases=[dict(input=s,expected=array(lua.globals().capture_door(table(s)),lambda r:{k:r[k] for k in fields}))for s in specs]
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-doors.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original door scenarios / {sum(len(c["expected"])for c in cases)} public commands.')
