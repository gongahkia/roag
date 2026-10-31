#!/usr/bin/env python3
"""Capture pinned original public commands on semantic dungeon terrain."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility')
function capture_terrain(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/adventurer'})
 local p=G.player(s);p.position.x=15;p.position.y=16;s.entities={p}
 local original=C.map_for(s);local map={};for k,v in pairs(original)do map[k]=v end
 local rows={};for y,row in ipairs(original.rows)do rows[y]=string.rep('.',#row)end
 map.rows=rows;map.placements={stairs={}};s.generated_map=map
 for _,t in ipairs(input.tiles)do local x,y=15+t[1],16+t[2]
  rows[y]=rows[y]:sub(1,x-1)..t[3]..rows[y]:sub(x+1)end
 V.recompute(s);local result={}
 for _,command in ipairs(input.commands)do
  local preview=G.preview(s,command,1,0)
  local ok,why,events=G.submit(s,{kind=command,dx=1,dy=0});assert(ok,why)
  local triggered,broken,hits=0,0,0
  for _,e in ipairs(events)do
   if e.kind=='TerrainTriggered'then triggered=triggered+1 end
   if e.kind=='TerrainBroken'then broken=broken+1 end
   if e.kind=='DamageTaken'then hits=hits+1 end
  end
  result[#result+1]={clock=s.clock,hp=p.hp,x=p.position.x-15,y=p.position.y-16,
   recovery=preview.cost,triggered=triggered,broken=broken,hits=hits}
 end
 return result
end
""")
specs=[
 dict(name='bramble',tiles=[[1,0,'b']],commands=['move','wait']),
 dict(name='bramble_dash',tiles=[[1,0,'b'],[2,0,'b']],commands=['dash','wait']),
 dict(name='bog_walk',tiles=[[1,0,'g']],commands=['move']),
 dict(name='bog_dash',tiles=[[1,0,'g'],[2,0,'g']],commands=['dash']),
 dict(name='ice',tiles=[[1,0,'i']],commands=['dash']),
 dict(name='ice_bog',tiles=[[1,0,'i'],[2,0,'g'],[3,0,'g']],commands=['dash']),
 dict(name='ice_wall',tiles=[[1,0,'i'],[2,0,'#']],commands=['dash']),
 dict(name='barricade',tiles=[[1,0,'B']],commands=['basic_attack','move']),
]
cases=[]
for spec in specs:
 expected=array(lua.globals().capture_terrain(table(spec)),lambda s:{k:s[k] for k in ['clock','hp','x','y','recovery','triggered','broken','hits']})
 cases.append(dict(input=spec,expected=expected))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-terrain.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original terrain scenarios / {sum(len(c["expected"]) for c in cases)} commands.')
